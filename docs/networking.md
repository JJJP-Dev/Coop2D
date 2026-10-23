# La red en Coop 2D: cómo usamos FishNet

Esta guía explica **cómo piensa la red del juego**, qué herramienta de FishNet usamos para cada cosa y los patrones que se repiten en el código. Al final están los errores que ya nos han pasado, para no repetirlos. La [guía de sistemas](systems.md) dice qué hace cada sistema; esta dice **cómo se hablan los PCs**.

Versión: FishNet 4.7.3R (en `Assets/ThirdParty/FishNet`, no se edita nunca). Si dudas de cómo funciona una función de FishNet, su código está ahí y sus comentarios explican cada parámetro.

## Contenido

1. [La idea: el host decide](#1-la-idea-el-host-decide)
2. [La vida de un objeto de red](#2-la-vida-de-un-objeto-de-red)
3. [Las herramientas y cuándo usar cada una](#3-las-herramientas-y-cuándo-usar-cada-una)
4. [Patrones del proyecto](#4-patrones-del-proyecto)
5. [Errores que ya nos han pasado](#5-errores-que-ya-nos-han-pasado)
6. [Lista rápida antes de escribir algo de red](#6-lista-rápida-antes-de-escribir-algo-de-red)
7. [Probar la red](#7-probar-la-red)

---

## 1. La idea: el host decide

En una partida hay **un host y hasta tres clientes**. El PC del host hace dos papeles a la vez:

- **Servidor:** la única copia «de verdad» de la partida. Decide la vida, el daño, las auras, las reacciones, los enemigos, las oleadas, la experiencia y las mejoras.
- **Cliente:** el host también juega, y ve la partida igual que los demás.

Los clientes **proponen y enseñan**. Nunca deciden algo que afecte a los demás: lo piden al host, y el host comprueba y decide.

**«Lo tuyo, al instante».** Esperar al host para todo se notaría con ping (200 ms de retraso al moverte o disparar). Por eso hay una excepción: lo que hace **tu propio jugador** lo haces al momento en tu PC, y el host lo comprueba después.

| Lo haces tú al instante | Lo comprueba el host |
|---|---|
| Moverte | No (tu movimiento se copia a los demás tal cual) |
| Disparar el básico y ver tu bala (o tus balas, con Doble disparo) | Que no disparas más rápido de lo permitido, ni desde lejos, ni con más balas de las que tienes |
| Que tu bala toque a un enemigo | Que ese disparo existe, no se ha usado y pasa cerca del enemigo |
| Lanzar una habilidad y ver el círculo | Alcance, recarga y que cada objetivo estaba dentro |
| Abrir la franja de mejoras y elegir | Que la oferta sigue pendiente y la opción vale |

Lo que **no** es tuyo (el daño que haces, una reacción, un orbe, subir de nivel) lo decide el host y llega con el retraso del ping.

**Por qué no hay trampas fáciles:** un cliente modificado puede mandar lo que quiera, pero el host solo acepta lo que cuadra con su copia de la partida, con un margen pensado para el ping.

## 2. La vida de un objeto de red

Un objeto existe en todos los PCs si tiene un **NetworkObject**. Sus scripts de red heredan de **NetworkBehaviour**, que trae estas llamadas, en este orden:

| Llamada | Dónde se ejecuta | Para qué la usamos |
|---|---|---|
| `Awake` | En todos | Coger componentes, suscribirse a `OnChange` de las SyncVars |
| `OnStartNetwork` | Una vez por PC (en el host, **una sola vez** aunque sea servidor y cliente) | Poner el `Current` estático de los objetos de partida |
| `OnStartServer` | Solo en el host | Dejar el estado inicial (importante si el objeto se reutiliza), suscribirse a eventos del servidor |
| `OnStartClient` | En cada PC que lo ve, también el host | Lo que necesita saber si este PC es el dueño (`IsOwner`) |
| `OnStopClient` / `OnStopServer` / `OnStopNetwork` | Al desaparecer | Desuscribirse y limpiar |

Preguntas útiles dentro del código:

| Propiedad | Verdad cuando |
|---|---|
| `IsServerInitialized` | Este PC es el host y el objeto ya está en marcha en él. Ya vale `true` dentro de `OnStartServer` |
| `IsClientInitialized` | Este PC lo ve como cliente |
| `IsOwner` | Este PC es el dueño (tu jugador). Fiable a partir de `OnStartClient` |
| `IsSpawned` | El objeto existe en la red |
| `Owner` | La conexión dueña (para mandarle un `TargetRpc`) |

**En el host pasan las dos cosas**: `OnStartServer` y luego `OnStartClient`, en el mismo PC. Mucho código distingue «la copia del servidor» de «lo que ve el cliente del host», sobre todo en las SyncVars (ver 4.2).

## 3. Las herramientas y cuándo usar cada una

| Herramienta | Qué hace | Úsala para | Ejemplo en el proyecto |
|---|---|---|---|
| **SyncVar** | Un valor que escribe el host y llega solo a todos, también a quien entra tarde | **Estado**: algo que «es» y hay que poder leer en cualquier momento | La vida (`Health`), el aura (`AuraHolder`), el nivel (`RunExperience`), la fase de la partida (`MatchFlow`) |
| **SyncList / SyncDictionary** | Una lista o un diccionario que se sincroniza por cambios | Muchos valores pequeños sin crear objetos de red | Los orbes (`ExperienceOrbs`), las mejoras de cada jugador (`PlayerUpgrades`) |
| **ServerRpc** | Un mensaje de un cliente al host | **Pedir** algo al host | Disparar, lanzar una habilidad, elegir una mejora |
| **ObserversRpc** | Un mensaje del host a todos los que ven el objeto | **Avisar** de que algo ha pasado, una vez | Un golpe, una reacción, quién recogió un orbe |
| **TargetRpc** | Un mensaje del host a una sola conexión | Algo que solo debe saber un jugador | Su oferta de mejoras, un aviso de los trucos, teletransportarlo |
| **Broadcast** | Un mensaje sin objeto de red | Algo que no tiene un objeto que lo mande | La cadena de Electrocución, la versión al conectar, «el host cierra» |

**Regla para elegir entre SyncVar y RPC:**

- ¿Alguien que entra **más tarde** tiene que saberlo? → **SyncVar** (o lista). Un RPC que ya pasó no le llega.
- ¿Es un **momento** («ha pasado esto ahora») y no un estado? → **RPC**.
- A veces hacen falta las dos: la vida es una SyncVar (estado) y el golpe un `ObserversRpc` (momento, para el número y el destello).

**Detalles que importan:**

- Un `ServerRpc` exige por defecto que lo mande **el dueño** del objeto. Así nadie puede elegir la mejora de otro. Los objetos de escena sin dueño (trucos, laboratorio) usan `[ServerRpc(RequireOwnership = false)]`, y entonces el host comprueba quién llama con el parámetro `NetworkConnection caller = null`.
- `[ObserversRpc(ExcludeOwner = true, ExcludeServer = true)]` evita que lo reciban quien ya lo hizo y el host (por ejemplo, el dibujo del disparo, que el tirador ya hizo).
- Los RPC del mismo objeto llegan **en el orden en que se mandan** (canal fiable). Lo aprovechamos: el disparo llega antes que su impacto, y «quién recogió el orbe» antes de quitarlo de la lista.
- Por un RPC viajan tipos simples, structs con campos públicos, arrays (`string[]`, `bool[]`), `NetworkObject` y componentes de red (`Health`). Un `NetworkObject` que ya no existe llega como `null`.

## 4. Patrones del proyecto

### 4.1 El `Current` estático de los objetos de partida

Los objetos que hay uno por partida (`MatchFlow`, `MatchClock`, `RunExperience`, `ExperienceOrbs`, `ReviveCharges`, `UpgradeOffers`…) tienen un `public static X Current`. Se pone en `OnStartNetwork` y se quita en `OnStopNetwork` (solo si sigue siendo él). Así cualquier script los encuentra (`RunExperience.Current.Level`) sin buscarlos en la escena. Siempre comprueba que no sea `null`: en ReactionLab o en el menú no existen.

### 4.2 Presentación solo con `!asServer`

Una SyncVar avisa de cambios con `OnChange(anterior, nuevo, asServer)`. En el host avisa **dos veces**: como servidor (`asServer = true`) y como cliente (`false`). Lo que se **enseña** (textos, eventos para la interfaz) se hace solo cuando `asServer` es `false`. Así el cliente del host lo ve en el mismo momento que cualquier otro cliente, y nunca se enseña dos veces.

### 4.3 Valores antes de crear el objeto

Si el host pone una SyncVar **antes** de crear el objeto en la red (`Spawn`), el valor viaja dentro del mensaje de creación: todos ven el objeto ya con ese valor, sin un fotograma con el valor por defecto. Lo usamos para el hueco del jugador (`PlayerIdentity.SetSlot`) y para que alguien aparezca derribado (`Health.SpawnDowned`).

### 4.4 El cliente del host llega tarde

El host carga la escena **antes** de arrancar su propio cliente. Todo lo que pase en esos primeros instantes (la primera oleada, la semilla) no le llega por RPC. Por eso lo que deba ver todo el mundo va en **SyncVar**: le llega al conectarse, como a cualquiera que entre tarde. Ejemplos: `MatchSeed`, el número de oleadas de `WaveSpawner`.

### 4.5 Avisar antes de desaparecer

Un objeto que se destruye ya no puede mandar RPCs. Si al morir hay que avisar (el golpe mortal, una reacción que mata), el RPC se manda **antes** de destruirlo. Por eso `Health` manda el golpe antes de lanzar `Died`, y `AuraHolder` avisa de la reacción antes de aplicar sus efectos.

### 4.6 Hablar con el dueño después de que esté listo

Un `TargetRpc` mandado justo al crear un objeto puede llegar antes de que exista en el PC del dueño, y se pierde. Por eso `PlayerUpgrades` espera a que el dueño avise desde su `OnStartClient` (`ServerOwnerReady`) antes de mandarle ninguna oferta.

### 4.7 El tiempo con ticks: mandar una vez y que cada uno calcule

El tick de FishNet es el mismo en todos los PCs. En vez de mandar un valor que cambia cada fotograma, el host manda **el tick en que empezó** y cada PC calcula:

- `MatchClock` manda el tick de inicio; cada PC calcula los segundos con `TimeManager.TimePassed(tick)`.
- `PlayerRevive` manda una «recta»: el progreso en un tick y la velocidad. Cada PC calcula el porcentaje, y el host solo manda algo nuevo cuando cambia la velocidad.
- Los disparos llevan el tick en que salieron, para que los demás dibujen la bala ya adelantada.

### 4.8 Validar en el host con margen

El host ve a los demás jugadores un poco en el pasado (el ping). Toda comprobación lleva una **tolerancia** en un ScriptableObject: distancia desde la que sale un disparo, distancia de un impacto a la trayectoria, alcance extra de una habilidad, margen de recarga. Cuando la comprobación falla por poco, a veces el host **corrige** en vez de rechazar (un disparo desde muy lejos sale desde donde el host ve al jugador). Las tolerancias se ajustan pensando en 200 ms de ping.

### 4.9 Ritmos con tiempo real en el host

El host cuenta los ritmos de los clientes (cadencia, recargas) con `Time.realtimeSinceStartupAsDouble` y no con `Time.time`. Si el host se congela un momento, `Time.time` se salta parte del tiempo y rechazaría disparos honestos que llegan todos juntos. Además hay un pequeño «presupuesto» (hasta 3 disparos guardados) para absorber los tirones de la red.

### 4.10 Los jugadores, desde el host

Cada conexión tiene como **primer objeto** su jugador: `connection.FirstObject`. El host recorre `ServerManager.Clients.Values` y mira el `FirstObject` de cada uno para encontrar a los jugadores (oleadas, orbes, fin de partida…). Una conexión que todavía no tiene jugador (está entrando) tiene `FirstObject` a `null`: hay que saltarla.

### 4.11 Objetos reutilizados

FishNet puede reutilizar objetos (*pooling*) en vez de destruirlos. Un objeto reutilizado vuelve con lo que tenía en su vida anterior, así que **el estado inicial se pone en `OnStartServer`/`OnStartClient`, no solo en la declaración**. Casi todos los scripts lo hacen («Pooled objects come back with…»).

### 4.12 Trucos solo en desarrollo, comprobados en el host

Los trucos funcionan solo si `Debug.isDebugBuild` es cierto **en el host**: aunque un cliente modificado los pida en una build normal, el host los ignora.

### 4.13 Números de versión en lo que se puede quedar viejo

Cuando el cliente responde a algo que puede haber cambiado mientras tanto (una oferta que se rerolleó), la respuesta lleva el número de la versión que vio. El host ignora respuestas a versiones viejas. Así un doble clic o un mensaje cruzado nunca aplica algo que el jugador ya no veía.

## 5. Errores que ya nos han pasado

| Qué pasó | Por qué | Cómo se arregló / la regla |
|---|---|---|
| **Orbes fantasma** en la ventana del host, que no se podían recoger | En el cliente del host, al quitar un elemento de una `SyncList`, FishNet avisa con el elemento quitado **vacío** (el host no vuelve a tocar su lista). La vista quitaba el orbe equivocado | No seguir los cambios uno a uno: al terminar cada lote de cambios (`SyncListOperation.Complete`), comparar la lista entera con lo que hay en pantalla (`ExperienceOrbsView`) |
| Un jugador que entraba en una partida **ya perdida** aparecía de pie, bajo la pantalla de fin | Aparecía con la vida llena, y nadie lo derribaba | Ponerle la vida a 0 **antes** de crearlo en la red (`Health.SpawnDowned`), así llega ya derribado y sin evento falso |
| El reinicio no podía **recargar la escena** | FishNet no vuelve a cargar una escena global ya cargada, y los jugadores viven en ella | Reiniciar en el sitio: cada sistema tiene su `ResetForNewRun` (ver systems.md 4.13) |
| El host no podía **teletransportar** a un jugador | El `NetworkTransform` del jugador lo controla su dueño: lo que el host mueve se pisa | El host pide al dueño que se mueva (`TargetRpc`) y este usa `NetworkTransform.Teleport()` para que los demás no lo vean deslizarse |
| Apuntar por delante a un enemigo no funcionaba en los clientes | En los clientes el cuerpo del enemigo es cinemático y su velocidad siempre es 0 | Medir la velocidad por lo que se ve moverse, fotograma a fotograma (`MotionTracker`) |
| Contar jugadores con `ServerManager.Clients` daba de más | Incluye conexiones que aún no han entrado del todo (sin autenticar o sin jugador) | Contar solo las conexiones con `FirstObject` |
| Un cambio de **estado elegido por el dueño** podía quedarse colgado con ping | Comparar con el valor replicado, que llega con retraso, hacía que no se mandara el «cierro» | El dueño recuerda lo último que **mandó** (`PlayerUpgrades.SetChoosing`) |
| Las tiradas de la semilla se desincronizaban según dónde estuvieran los jugadores | Se tiraba un número de veces distinto según la situación | Tirar siempre el mismo número de veces, antes de mirar la situación (`WaveSpawner.PickSpawnPosition`) |

## 6. Lista rápida antes de escribir algo de red

1. **¿Quién decide?** Si afecta a los demás, el host. El cliente solo pide.
2. **¿Es estado o momento?** Estado → SyncVar o lista. Momento → RPC. ¿Quien entre tarde lo necesita? → estado.
3. **¿Lo hace el jugador sobre sí mismo?** Hazlo al instante en el dueño y valídalo en el host con tolerancia.
4. **¿Puede llegar tarde, dos veces o desordenado?** Piensa en 200 ms de ping y en el doble clic.
5. **¿Qué ve el cliente del host?** Presentación solo con `!asServer`.
6. **¿Qué pasa si el objeto desaparece en medio?** Avisa antes; comprueba `null` al recibir.
7. **¿Se reinicia con la partida?** Añade su `ResetForNewRun`.
8. **¿Lo puede pedir un cliente malicioso?** El host comprueba propietario, rangos, ritmos y que la opción exista.
9. **Pruébalo** con Multiplayer Play Mode y latencia (abajo).

## 7. Probar la red

- **Multiplayer Play Mode** (*Window › Multiplayer › Multiplayer Play Mode*): abre 2-4 jugadores dentro del editor. Siempre desde Bootstrap.
- **Latencia:** en el `NetworkManager` de Bootstrap, el *Latency Simulator* del TransportManager a 100 ms da unos 200 ms de ida y vuelta. Prueba todo lo de red también así.
- **Entre PCs:** sección 13 de [testing.md](testing.md).
- Cada sistema tiene su lista de pruebas en [testing.md](testing.md); pásala antes de subir algo que toque la red.
