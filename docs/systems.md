# Guía de sistemas de Coop 2D

Esta guía explica **qué hace cada sistema del juego, con qué piezas está hecho, cómo funciona por dentro y cómo cambiarlo**. Está pensada para poder retomar cualquier parte del proyecto sin tener delante a quien la escribió: para aprender cómo está hecho, para crear contenido nuevo (enemigos, oleadas, habilidades, mejoras) y para ampliar, mover, quitar u optimizar sistemas con seguridad.

Documentos que la acompañan:

- [networking.md](networking.md): cómo usamos FishNet, los patrones de red del proyecto y los errores que ya nos han pasado. **Léelo antes de tocar algo que viaje por la red.**
- [testing.md](testing.md): cómo comprobar que cada sistema funciona.
- [launcher-events.md](launcher-events.md): el contrato de eventos con Palito Launcher.
- El documento de diseño (GDD) y el catálogo de progresión: qué juego queremos y por qué. Esta guía explica **cómo está hecho**; el diseño explica **qué queremos**.

Estado: actualizada al Pre 3 (build 0.0.5 en preparación).

## Contenido

1. [Cómo leer esta guía y el código](#1-cómo-leer-esta-guía-y-el-código)
2. [Vocabulario mínimo](#2-vocabulario-mínimo)
3. [El mapa del proyecto](#3-el-mapa-del-proyecto)
4. [Sistemas](#4-sistemas)
   - 4.1 [Conexión, versiones y escenas](#41-conexión-versiones-y-escenas)
   - 4.2 [Servicios (GameServices)](#42-servicios-gameservices)
   - 4.3 [Jugadores](#43-jugadores)
   - 4.4 [Vida, daño y eventos de combate](#44-vida-daño-y-eventos-de-combate)
   - 4.5 [Ataque básico](#45-ataque-básico)
   - 4.6 [Habilidades](#46-habilidades)
   - 4.7 [Elementos y auras](#47-elementos-y-auras)
   - 4.8 [Reacciones](#48-reacciones)
   - 4.9 [Enemigos](#49-enemigos)
   - 4.10 [Oleadas](#410-oleadas)
   - 4.11 [Semilla y azar](#411-semilla-y-azar)
   - 4.12 [Derribado y reanimación](#412-derribado-y-reanimación)
   - 4.13 [La partida: reloj, fin y reinicio](#413-la-partida-reloj-fin-y-reinicio)
   - 4.14 [Experiencia, niveles y orbes](#414-experiencia-niveles-y-orbes)
   - 4.15 [Mejoras y ofertas](#415-mejoras-y-ofertas)
   - 4.16 [Interfaz](#416-interfaz)
   - 4.17 [Cámara](#417-cámara)
   - 4.18 [Herramientas de prueba](#418-herramientas-de-prueba)
   - 4.19 [Eventos del launcher](#419-eventos-del-launcher)
   - 4.20 [Estadísticas del jugador](#420-estadísticas-del-jugador)
   - 4.21 [Efectos de las mejoras](#421-efectos-de-las-mejoras)
5. [Recetas: cómo hacer cosas nuevas](#5-recetas-cómo-hacer-cosas-nuevas)
6. [Quitar, mover y optimizar sin romper nada](#6-quitar-mover-y-optimizar-sin-romper-nada)
7. [Cómo mantener esta guía](#7-cómo-mantener-esta-guía)

---

## 1. Cómo leer esta guía y el código

**Cada sistema tiene el mismo esquema**, para que sepas dónde mirar:

| Apartado | Qué responde |
|---|---|
| Qué hace | Lo que nota el jugador |
| Piezas | Los scripts y assets, con su ruta |
| Dónde está montado | Escena, objeto o prefab que lo lleva |
| Cómo funciona | El recorrido paso a paso: quién decide, en qué orden |
| Qué viaja por la red | Qué se sincroniza y cómo |
| Qué ajustar | Los valores de diseño: en qué asset y qué campo |
| Cuidado con | Lo que rompe el sistema si lo cambias sin saberlo |

**Las rutas** son relativas a `Assets/_Project`. `Code/Combat/Health/Health.cs` es `Assets/_Project/Code/Combat/Health/Health.cs`.

**El código se explica solo, si sabes dónde leer:**

- **Arriba de cada clase** hay un comentario que dice qué hace, quién la ejecuta (el host o cada cliente) y por qué está hecha así. Empieza siempre por ahí.
- **Cada método público** tiene un `/// <summary>` que dice si es «Server only» (solo el host puede llamarlo), «Owner side» (solo el dueño del objeto) o «Client side» (cada pantalla).
- **Cada valor de un ScriptableObject** tiene un *Tooltip*: pasa el ratón por encima del campo en el Inspector y te dice qué es y en qué unidad.
- **Los comentarios citan el issue** que motivó el código (`#79`). Si no entiendes por qué algo es así, busca el número en GitHub: el issue cuenta el porqué.
- **Para encontrar algo**, busca en el proyecto (Ctrl+Shift+F en Rider o Visual Studio) el nombre de la clase, el número del issue o el texto que sale en el juego («Derribado»).

**Unidades:** 1 unidad de Unity = 1 casilla = 16 píxeles del arte. Los tiempos van en segundos salvo que se diga otra cosa.

## 2. Vocabulario mínimo

Lo justo para leer el resto. La explicación larga está en [networking.md](networking.md).

| Palabra | Qué es en este proyecto |
|---|---|
| **Host** | El jugador que crea la partida. Su PC hace de **servidor** (decide lo que pasa) y a la vez de **cliente** (juega y ve la partida como los demás) |
| **Cliente** | Cada PC que se une. Ve lo que el host decide y manda lo que hace su jugador |
| **Dueño** (*owner*) | El PC que controla un objeto. Tu personaje es tuyo: tú lo mueves y disparas con él. Los enemigos no son de nadie: los mueve el host |
| **Autoridad del host** | Regla de oro del proyecto: lo que importa (vida, daño, auras, reacciones, enemigos, oleadas, experiencia, mejoras) lo decide el host. Los clientes solo proponen y enseñan |
| **NetworkObject** | Componente de FishNet que hace que un objeto exista en todos los PCs a la vez (jugadores, enemigos, el objeto `Match`) |
| **NetworkBehaviour** | Un script de un NetworkObject que puede usar la red |
| **SyncVar** | Un valor que el host cambia y llega solo a todos los PCs, también a los que entran tarde |
| **RPC** | Un mensaje puntual: «ha pasado esto». `ServerRpc` va de un cliente al host; `ObserversRpc`, del host a todos; `TargetRpc`, del host a uno solo |
| **Tick** | El reloj de la red: FishNet lo mantiene igual en todos los PCs. Sirve para que cada uno calcule el mismo tiempo sin mandarlo |
| **ScriptableObject** (SO) | Un asset de datos (`.asset` en `Data/`). Aquí van todos los valores de diseño, para cambiarlos sin tocar código |
| **Placeholder** | Algo provisional (un color, una etiqueta de texto) hasta que llegue el arte |

## 3. El mapa del proyecto

### 3.1 Escenas y arranque

```
Bootstrap ──► MainMenu ──► Arena_Test          (la partida)
                     └───► ReactionLab         (laboratorio de reacciones, solo builds de desarrollo)
             ResolutionTest                    (banco de pruebas suelto, fuera de la build)
```

1. **Bootstrap** se abre primero y **nunca se descarga**: guarda los servicios que viven toda la sesión (red, input, avisos…). Siempre se pulsa Play desde Bootstrap.
2. Bootstrap abre **MainMenu**, una escena normal de Unity (todavía sin red).
3. Al pulsar «Crear partida», el PC arranca como servidor y FishNet carga **Arena_Test** para todos. Cuando termina de cargar, el host arranca también su cliente. Por eso **el cliente del host se conecta después de que la escena ya existe** (importante: ver networking.md).
4. Al salir o perder la conexión, se vuelve a MainMenu.

### 3.2 Dónde vive cada cosa

| Sitio | Qué hay |
|---|---|
| `Bootstrap` › objeto `NetworkManager` | FishNet y casi todos los servicios: ConnectionService, SceneFlowService, PlayerSpawnService, LocalPlayerService, InputService, CombatEventService, ReactionService, PlayerRegistry, RunRandomService, VersionAuthenticator, PerformanceLogger, CombatEventLogger |
| `Bootstrap` › objeto `GameServices` | `GameServices` (el acceso a los servicios), `LauncherEventReporter`, `LauncherDiagnostics` |
| `Bootstrap` › `ToastCanvas`, `PersistentUI`, `NetworkStats`, `ConnectionFeedback` | Avisos en pantalla, estadísticas de red (F3) y mensajes de conexión |
| `Arena_Test` › objeto **`Match`** | Todo lo que es «de la partida»: `MatchClock`, `MatchSeed`, `ReviveCharges`, `MatchFlow`, `RunExperience`, `ExperienceOrbs`, `ExperienceOrbsView` y `UpgradeOffers` |
| `Arena_Test` › `WaveSpawner` | Las oleadas |
| `Arena_Test` › `DebugCheats` y `DebugCanvas` | El panel de trucos (F1) |
| `Arena_Test` › `CombatFeedback` | Lo que se dibuja de las reacciones y los números flotantes |
| `Arena_Test` › `InGameMenuCanvas` | El HUD: barra de experiencia, habilidades, franja de mejoras, fin de partida, botón de salir |
| `Arena_Test` › `PlayerSpawnPoints` | Dónde aparece cada jugador |
| `Prefabs/Player/Player.prefab` | El jugador y todos sus componentes |
| `Prefabs/Enemies/Chaser.prefab` | El perseguidor |
| `Prefabs/Enemies/Arc.prefab` | El dron (Arc) |
| `Data/` | Todos los valores de diseño, en ScriptableObjects, por carpetas |

### 3.3 Carpetas de código

`Code/` tiene una carpeta por sistema: `Camera`, `Cheats`, `Combat`, `Core` (servicios, azar, eventos del launcher, input), `Elements` (elementos, auras, reacciones, laboratorio), `Enemies`, `Networking` (conexión, partida), `Player`, `Progression` (experiencia, orbes, mejoras), `Skills` y `UI`.

---

## 4. Sistemas

### 4.1 Conexión, versiones y escenas

**Qué hace.** Crear una partida, unirse por IP, rechazar a quien tenga otra versión del juego, cargar la arena para todos y volver al menú al salir.

**Piezas.**
- `Code/Networking/Services/ConnectionService.cs`: arranca host o cliente y traduce lo que dice FishNet a un estado sencillo (`SessionState`: Connecting, Connected, Lost, Failed, Disconnected), con el motivo del fallo (`FailReason`).
- `Code/Networking/Services/SceneFlowService.cs`: carga la escena de partida cuando arranca el servidor y vuelve a MainMenu cuando se pierde la sesión.
- `Code/Networking/Authentication/VersionAuthenticator.cs` (+ `VersionBroadcast`, `VersionResultBroadcast`): el cliente manda su versión; si no es la del host, el host lo echa.
- `Code/Networking/HostClosingBroadcast.cs`: aviso de «el host cierra a propósito», para distinguirlo de una caída.
- `Code/UI/MainMenu/ConnectionMenuController.cs`, `MainMenuController.cs`, `Code/UI/Feedback/ConnectionFeedback.cs`: botones y mensajes del menú.

**Dónde está montado.** Servicios en `Bootstrap` › `NetworkManager`. Menú en `MainMenu`.

**Cómo funciona.**
1. «Crear partida» → `ConnectionService.StartHost()` arranca el servidor.
2. Al arrancar, `SceneFlowService` carga Arena_Test con FishNet (`LoadGlobalScenes`, reemplazando las escenas anteriores).
3. Al terminar de cargar, `ConnectionService` arranca el cliente del host.
4. Cada cliente se conecta, `VersionAuthenticator` compara versiones (*Project Settings › Player › Version*) y, si coinciden, FishNet lo deja entrar y le carga la escena. Solo entonces el estado pasa a Connected.
5. Un cliente que entra recibe la escena y su jugador (ver 4.3).

**Cuidado con.**
- La versión del juego (*Player › Version*, ahora 0.0.4) es la que se compara: dos builds con la misma versión pero código distinto se dejan entrar y fallarán raro. Sube la versión en cada build para testers.
- Las escenas de partida tienen que estar en la lista del *Build Profile*, o no se cargan.
- FishNet **no vuelve a cargar una escena global que ya está cargada**: por eso el reinicio de partida no recarga la escena (ver 4.13).

### 4.2 Servicios (GameServices)

**Qué hace.** Da acceso desde cualquier script a los sistemas que viven toda la sesión.

**Piezas.** `Code/Core/Services/GameServices.cs`. Se usa así: `GameServices.Instance.ToastService.Show("Hola", ToastType.Info)`.

| Servicio | Para qué |
|---|---|
| `ConnectionService` | Estado de la conexión |
| `SceneFlowService` | Cambiar de escena |
| `ToastService` | Avisos cortos en pantalla |
| `PlayerSpawnService` | Crear el jugador de cada conexión |
| `LocalPlayerService` | El jugador de **este** PC (`Current` y evento `Changed`) |
| `InputService` | Las teclas (ver 4.16) |
| `CombatEventService` | Golpes, muertes y reacciones confirmados por el host, para la presentación |
| `ReactionService` | Los efectos de las reacciones (en el host) |
| `PlayerRegistry` | Todos los jugadores que ve este PC |
| `RunRandomService` | La semilla y el azar de la partida |

**Cuidado con.** Si añades un servicio hacen falta tres cosas: el campo y la propiedad en `GameServices`, la comprobación en su `Awake` y el componente en el objeto `NetworkManager` de Bootstrap, asignado en el Inspector de `GameServices`. Un servicio es solo para algo que debe vivir toda la sesión; lo que es «de la partida» va en el objeto `Match` (ver 4.13).

### 4.3 Jugadores

**Qué hace.** Cada conexión recibe un jugador con un hueco (J1-J4), un color y un **personaje** (Brasa, Marea o Voltio), que decide su elemento y sus habilidades. Lo mueve su dueño.

**Piezas.**
- `Code/Player/Spawning/PlayerSpawnService.cs` y `PlayerSpawnPoints.cs`: crear el jugador y dónde.
- `Code/Player/Identity/`: `PlayerIdentity` (el hueco, 0-3), `PlayerSlots` (reparte huecos libres), `PlayerRegistry`, `LocalPlayerService`, `PlayerColorMarker` + `PlayerColors` (el color del hueco).
- `Code/Player/Input/PlayerInputReader.cs`: lee el movimiento, solo en el PC dueño.
- `Code/Player/Movement/PlayerMovement.cs` + `PlayerMovementSettings`: mover el cuerpo.
- `Code/Player/Characters/`: `CharacterDefinition` (la ficha de un personaje: id, nombre, elemento y habilidades de inicio), `PlayerCharacters` (todos los personajes y el de cada hueco) y `PlayerCharacter` (en el jugador: cuál juega).
- `Code/Player/Elements/PlayerElement.cs`: el elemento del jugador, que sale de su personaje.

**Dónde está montado.** `Prefabs/Player/Player.prefab` (capa Player). Lleva: NetworkObject, NetworkTransform, PlayerInputReader, PlayerMovement, PlayerIdentity, PlayerCharacter, PlayerColorMarker, Health, PlayerAutoAttack, PlayerElement, PlayerSkills, HitFlash, PlayerDowned, PlayerDownedView, PlayerRevive, PlayerUpgrades, PlayerChoosingView, PlayerStats y PlayerUpgradeEvents. Hijos: `Visual` (sprite), `SlotMarker` (punto de color), `HealthLabel`, `DownedLabel` y `ChoosingLabel`.

**Cómo funciona.**
1. Cuando una conexión ha cargado la escena, `PlayerSpawnService` le da el primer hueco libre, crea el jugador en su punto de aparición y le pone el hueco **antes de crearlo en la red** (así el hueco llega a todos con el jugador).
2. El jugador es el **primer objeto** de su conexión (`connection.FirstObject`): muchos sistemas del host buscan a los jugadores así.
3. Su dueño lo mueve en su propio PC (sin esperar al host) y `NetworkTransform` copia la posición a los demás. Es «autoritativo del cliente»: por eso, para teletransportarlo, el host le pide al dueño que lo haga (`PlayerMovement.PlaceAt`).
4. **El personaje** sale del hueco hasta que haya sala de espera para elegirlo (#173): J1 Brasa, J2 Marea, J3 Voltio, J4 Brasa (`Data/Players/PlayerCharacters.asset`). `PlayerSpawnService` lo pone junto al hueco, **antes de crear el jugador en la red**, así que llega a todos con el jugador. Solo viaja su id (`ember`); cada PC busca la ficha en el mismo asset.
5. **El elemento** sale del personaje: Brasa fuego, Marea agua, Voltio electricidad. F6 lo cambia en desarrollo (el personaje no cambia).
6. **Las habilidades** son las de inicio del personaje (4.6).

**Qué ajustar.**

| Valor | Asset | Ahora |
|---|---|---|
| Velocidad, aceleración, frenada | `Data/Characters/DefaultPlayerMovement.asset` | 5 · 50 · 60 |
| Color de cada hueco | `Data/Players/PlayerColors.asset` | rojo, azul, verde, amarillo |
| Personaje de cada hueco, y la lista de personajes | `Data/Players/PlayerCharacters.asset` | Brasa, Marea, Voltio, Brasa |
| Nombre, elemento, habilidad inicial y kit de cada personaje | `Data/Characters/Ember.asset`, `Tide.asset`, `Volt.asset` | Brasa fuego, empieza con Llamarada y su kit es Llovizna · Marea agua, Ola y Burbuja prisión + Chispa · Voltio electricidad, Rayo y Cortocircuito. Muro ígneo (Brasa) y Bobina (Voltio) entrarán en el kit cuando existan |

**Cuidado con.** Máximo 4 jugadores (`PlayerSlots.MaxPlayers`); el quinto se desconecta. Los puntos de aparición están en `PlayerSpawnPoints` de cada escena: sin ellos, todos aparecen en el origen.
- **Un personaje que no está en la lista *Characters* de `PlayerCharacters` no se puede jugar**: los demás PCs reciben su id y no encontrarían su ficha. El asset avisa en consola si das a un hueco uno que no está.
- El id del personaje (`ember`, `tide`, `volt`) lo usan los requisitos de las mejoras (`char:ember`) y el launcher: no lo cambies.

### 4.4 Vida, daño y eventos de combate

**Qué hace.** La vida de jugadores, enemigos y muñecos; los golpes; el destello blanco; los números que salen al golpear.

**Piezas.**
- `Code/Combat/Health/Health.cs` + `HealthSettings`: la vida. Es **la única puerta para hacer daño**: `TryTakeDamage(cantidad, atacante, tipo, elemento)`, solo en el host.
- `Code/Combat/Events/CombatEventService.cs`, `HitInfo`, `DamageKind`: los avisos de «golpe confirmado», «muerte confirmada», «reacción» y «cadena».
- `Code/Combat/Feedback/HitFlash.cs` + `FeedbackProfile`: el destello.
- `Code/UI/FloatingText/`: los números y titulares flotantes (`FloatingTextSpawner`, `FloatingText`, `FloatingTextStyle`).
- `Code/Combat/Health/HealthDebugLabel.cs`: el texto de vida provisional.

**Cómo funciona.**
1. Alguien en el host llama a `TryTakeDamage`. Si el objetivo es invulnerable o ya está a 0, no pasa nada (devuelve false).
2. La vida es una SyncVar: baja y llega a todos.
3. El host manda un `ObserversRpc` con el golpe **antes** de avisar de la muerte (un enemigo muerto desaparece y ya no puede mandar nada).
4. Cada PC recibe el golpe y lo publica en `CombatEventService`. Ahí escuchan el destello, los números flotantes, el registro de eventos del launcher… **Nada de lo que escucha ahí cambia el juego**: solo enseña.
5. Tras recibir un golpe, el objetivo es invulnerable un momento (jugador 0,5 s).

**Qué ajustar.**

| Valor | Asset | Ahora |
|---|---|---|
| Vida y invulnerabilidad del jugador | `Data/Characters/DefaultPlayerHealth.asset` | 100 · 0,5 s |
| Vida del perseguidor | `Data/Enemies/ChaserHealth.asset` | 60 |
| Vida del muñeco | `Data/Enemies/TrainingDummyHealth.asset` | 500 |
| Duración del destello | `Data/Feedback/DefaultFeedback.asset` | 0,05 s |
| Estilo de cada número flotante | `Data/UI/FloatingText/*.asset` | básico, habilidad, reacción, titular |

**Cuidado con.** Para hacer daño nuevo, usa siempre `Health.TryTakeDamage` en el host, nunca cambies la vida a mano: así funcionan la invulnerabilidad, los números, el destello y los eventos. Para quitar toda la vida ignorando la invulnerabilidad, `Health.Kill`.

### 4.5 Ataque básico

**Qué hace.** Cada jugador dispara solo al enemigo más cercano en su alcance, apuntando un poco por delante si se mueve. El disparo hace daño y deja un **aura débil** de su elemento.

**Piezas.** `Code/Combat/AutoAttack/PlayerAutoAttack.cs` + `AutoAttackSettings`, `ShotRecord`; `Code/Combat/Projectiles/Projectile.cs` + `ProjectileSettings`; `Code/Combat/Targeting/AimPrediction.cs` y `MotionTracker.cs`.

**Cómo funciona** (modelo «lo tuyo, al instante»):
1. El dueño decide cuándo y hacia dónde dispara, con lo que ve en su pantalla, y ve su bala al momento.
2. Manda el disparo al host (`ServerFire`). El host comprueba que no dispara más rápido de lo permitido (un pequeño «presupuesto» de disparos) y que sale de un sitio razonable; si no, lo corrige o lo rechaza.
3. El host avisa a los demás para que dibujen la bala (`ObserversFire`), adelantada por el tiempo que tardó en llegar.
4. Cuando la bala del dueño toca algo, el dueño lo avisa (`ServerHit`). El host comprueba que ese disparo existe, que no se ha usado ya y que el enemigo está cerca de su trayectoria. Si cuadra, aplica el daño y el aura.

**Ráfagas (Doble disparo).** Un ataque puede soltar varias balas a la vez, abiertas en abanico alrededor de la dirección de apuntado (con 2 balas y 12°: una a −6° y otra a +6°). Cuántas y cuánto se abren son estadísticas (`BasicProjectiles` y `BasicSpread`, 4.20).
- Cada bala de la ráfaga es un disparo con su propio número (`shotId` seguidos), así que cada una avisa de su propio impacto.
- El dueño manda **un solo** `ServerFire` con cuántas balas son, y gasta **un solo** disparo del presupuesto: la ráfaga cuenta como un ataque.
- El host acepta como mucho las balas que él cree que tiene el jugador. Si el cliente pide más, no las consigue.
- **Un enemigo recibe un solo golpe por ráfaga** (regla 6 del motor, en el catálogo). Las balas de una ráfaga comparten en el host una lista de enemigos ya golpeados (`ShotRecord.VolleyHits`); el segundo golpe al mismo enemigo se ignora en silencio. Así Doble disparo sirve para repartir auras entre dos enemigos, no para doblar el daño a uno.

**Qué ajustar.** `Data/Characters/DefaultAutoAttack.asset`: cadencia 0,6 s, alcance 6, daño 10, cuánto se adelanta (*Lead Factor*), y las tolerancias de validación. `Data/Characters/BasicProjectile.asset`: velocidad 10, radio, vida de la bala 0,8 s.

**Cuidado con.** Las tolerancias existen por el ping: con 200 ms, el host ve al jugador «en el pasado». Si las bajas mucho, los clientes con ping verán golpes que no cuentan. Solo se golpea lo que está en la capa **Enemy**, para no dañar nunca a un compañero.
- El host siempre se entera antes que el dueño de un cambio de balas (las mejoras las aplica él). Al coger Doble disparo, el cliente dispara una bala durante lo que tarda en llegarle: no pasa nada. Al reiniciar la partida es al revés: durante ese instante el cliente aún suelta dos y la segunda no cuenta (`Rejected a hit … unknown` en el host). Es esperado y pasa una sola vez.

### 4.6 Habilidades

**Qué hace.** Tres huecos de habilidad: clic izquierdo, clic derecho y Q (catálogo E.0, regla 1). **Empiezas con la habilidad inicial de tu personaje** en el clic izquierdo (Llamarada, Ola o Rayo) y los otros dos huecos se llenan **durante la partida**, en el orden en que consigues las habilidades. Cada habilidad tiene **3 niveles**: el 1 al conseguirla, y el 2 y el 3 cambian sus números (más daño, menos recarga, más alcance…).

Hay además dos habilidades **de prueba**, Estallido (círculo que detona) y Marca (círculo que solo marca), que las pruebas de reacciones usan. No salen en partida: se ponen con F1 › «Habilidades de prueba».

**De dónde salen las habilidades** (`SkillLibrary` en `Data/Skills/SkillLibrary.asset`):
- **La biblioteca** tiene todas, para encontrar cada una por su id en cualquier PC.
- **Las del personaje**: su *Starting Skill* (la inicial) y sus *Own Skills* (el resto de su kit) en `Data/Characters/`.
- **Las genéricas** (*Generic*, catálogo E.2): las puede recibir cualquier personaje. Vacía en el Pre 3: llegan en la 0.1 (#88).

**Los huecos de cada jugador viajan por la red.** `PlayerSkills` guarda una lista sincronizada (`SyncList<OwnedSkill>`: id y nivel por hueco) que reciben todos los PCs, porque todos dibujan los lanzamientos de todos. Cada PC convierte cada entrada en una `SkillInstance`: la ficha con los números de su nivel ya calculados. Así el host comprueba un lanzamiento con los mismos números que dibujan los clientes. Solo el host cambia la lista (`TryAddSkill`, `TryLevelUp`, `SetSkills`, `ResetSkills`); al reiniciar la partida vuelve a la inicial sola. Un hueco que recibe otra habilidad empieza listo: no hereda la recarga de la anterior.

**Formas** (`SkillShape`):

| Forma | Cómo se apunta | Ejemplo |
|---|---|---|
| *Circle* | Donde apuntas, como mucho a su alcance (*Cast Range*) | Estallido, Marca, Llovizna, Cortocircuito |
| *Line* | Desde ti hacia el cursor, siempre a todo su largo; golpea a todos los que toca | Chispa (5 × 0,5 m) |
| *Cone* | Desde ti hacia el cursor; una cuña de *Length* de radio y *Angle* grados de abertura | Ola (3 m, 60°) |
| *Projectile* | Vuela desde ti hacia el cursor hasta *Length*. Golpea directamente (*Impact Radius* 0) a 1 + *Pierce* enemigos, o estalla en un círculo de *Impact Radius* al tocar el primero (y al final de su recorrido, con *Burst At End*) | Rayo (directo, atraviesa a 1), Llamarada (estalla 1,5 m), Burbuja (estalla 1,5 m y atrapa) |

**Piezas.** `Code/Skills/PlayerSkills.cs`, `SkillDefinition`, `SkillLevelModifier` (lo que cambia un nivel), `SkillInstance` (una habilidad con su nivel), `OwnedSkill` (lo que viaja de cada hueco), `SkillLibrary`, `SkillShape`, `SkillArea` (la zona de un lanzamiento ya colocada, con sus cuentas), `SkillCastSettings`, `SkillShapeVisual` (el dibujo provisional) y `Prefabs/Skills/SkillShapeVisual.prefab`; para los proyectiles, `SkillProjectile` (lo que vuela) con `Prefabs/Skills/SkillProjectile.prefab` y `SkillShotRecord` (lo que recuerda el host de cada uno); en el HUD, `Code/UI/InGame/SkillSlotView.cs`.

**Cómo funciona.**
1. El dueño pulsa. El juego toma **desde dónde** lanza (su posición) y **hacia dónde** (el cursor; un círculo se queda a su alcance, una línea o un cono solo usan la dirección) y arma la zona (`SkillArea`). Busca los enemigos que la tocan **en su pantalla** y la dibuja al momento.
2. Manda al host el hueco, el origen, el punto apuntado y la lista de objetivos (`ServerCast`).
3. El host comprueba que el origen está cerca de donde él ve al jugador (*Position Tolerance*), que el punto cuadra con la forma, la recarga, y **arma la misma zona con esos dos puntos**. Cada objetivo tiene que tocarla, con un margen (*Target Tolerance*) porque los enemigos se mueven mientras llega el mensaje. Aplica primero el **aura fuerte** (que puede detonar una reacción), después el daño y, si la habilidad empuja (Ola, 0,5 casillas), el empuje, siempre alejándolo de donde estaba el lanzador. Los muñecos no se mueven.
   - **Con Aura expansiva**, después el host busca él mismo enemigos hasta 1 m por fuera de la zona, sea cual sea su forma, y les pone el aura de la habilidad, hasta 8: **sin daño y sin detonar nunca** (regla 6 del motor). Sirve para marcar a un grupo sin provocar reacciones. Los busca el host con lo que él ve, porque el aura es decisión suya; el dueño no los manda. La zona que se dibuja no cambia de tamaño.
4. Los demás dibujan la zona (`ObserversCast`) con el origen que mandó el lanzador: una línea sale de donde estaba al lanzarla, aunque en otra pantalla se le vea un poco atrás.

**Proyectiles** (Llamarada, Burbuja, Rayo). Funcionan como el ataque básico (4.5), «lo tuyo, al instante»:
1. El dueño lanza: su proyectil sale al momento en su pantalla y manda al host desde dónde y hacia dónde, con un número de lanzamiento (`ServerLaunch`).
2. El host hace las mismas comprobaciones que con una habilidad instantánea (origen, recarga) y **recuerda el lanzamiento** (`SkillShotRecord`): su recorrido como una banda del ancho del proyectil, cuántos enemigos puede golpear y hasta cuándo puede llegar un aviso (lo que tarda en volar + 1 s). Avisa a los demás para que dibujen su copia, adelantada por el tiempo que tardó el mensaje (`ObserversLaunch`). Tempo cuenta desde aquí.
3. **Si golpea directo** (Rayo): cada enemigo que toca el proyectil del dueño se avisa (`ServerProjectileHit`). El host comprueba que el lanzamiento existe, que le quedan golpes y que el enemigo está cerca del recorrido, y le aplica aura, daño y empuje. Cada enemigo, una vez.
4. **Si estalla** (Llamarada, Burbuja): donde se para, el dueño arma un círculo, busca los enemigos que toca **en su pantalla**, lo dibuja y lo avisa una sola vez (`ServerBurst`), con el enemigo que lo paró. El host comprueba que el punto está en el recorrido y valida cada objetivo como en un círculo normal. Burbuja **atrapa** al enemigo que tocó: lo aturde 1,5 s, el mismo aturdimiento de Sobrecarga (no se mueve ni pega). Los demás dibujan el estallido con `ObserversBurst`.
5. Las copias de los demás PCs solo son el dibujo: chocan con lo que ven, pero no deciden nada.
5. **El lanzamiento lleva su tick** (el reloj del host tal como lo estima el dueño). Las mejoras que escuchan lanzamientos (Tempo) cuentan desde ese tick, así que su efecto empieza y acaba en el mismo tick en el host y en el dueño. El host no acepta un tick del futuro ni de más de 0,5 s atrás.
6. **Recarga devuelta** (Relevo, Asistencia): `RefundCooldowns` (host) quita segundos a cada recarga que esté corriendo, en el host y, con un `TargetRpc`, en el dueño; cada uno lo calcula con su reloj. Nunca deja una recarga por debajo del **60 %** de su tiempo completo desde que se lanzó (regla 1 del motor): con 7 s, como pronto a los 4,2 s.

**Qué ajustar.** Cada habilidad es un asset en `Data/Skills/`: id, nombre, icono, si usa el elemento de quien la lanza (o el suyo), si detona, fuerza del aura, recarga, alcance (solo círculo), forma con sus medidas (*Radius*; *Length* y *Width*; *Length* y *Angle*), daño, empuje y el dibujo (*Visual* = `SkillShapeVisual.prefab`).

| Habilidad | Asset | Elemento | Forma | Daño | Recarga | Detona | Nivel 2 · nivel 3 |
|---|---|---|---|---|---|---|---|
| Llamarada (inicial de Brasa) | `EmberFlare` | fuego | proyectil 14 m/s hasta 7, estalla 1,5 | 30 | 5 s | sí | +20 % daño · −15 % recarga |
| Llovizna (Brasa) | `EmberDrizzle` | agua | círculo 2, a 7 | 10 | 7 s | sí | +20 % daño · −15 % recarga |
| Ola (inicial de Marea) | `TideWave` | agua | cono 3 m, 60°; empuja 0,5 | 20 | 6 s | sí | +20 % daño · cono de 75° |
| Burbuja prisión (Marea) | `TideBubble` | agua | proyectil 12 m/s hasta 8, estalla 1,5 (moja) y atrapa 1,5 s al que toca | 10 | 9 s | no | trampa 2 s · moja en 2 m |
| Chispa (Marea) | `TideSpark` | electricidad | línea 5 × 0,5 | 15 | 7 s | sí | +20 % daño · 7 m de largo |
| Rayo (inicial de Voltio) | `VoltBolt` | electricidad | proyectil 16 m/s hasta 9, atraviesa a 1 (golpea a 2) | 22 | 4 s | sí | +20 % daño · −12,5 % recarga |
| Cortocircuito (Voltio) | `VoltShort` | fuego | círculo 1,5, a 7 | 16 | 7 s | sí | +25 % daño · −15 % recarga |
| Estallido (prueba) | `TestBurst` | el tuyo | círculo 1,5, a 7 | 16 | 7 s | sí | — |
| Marca (prueba) | `TestMark` | el tuyo | círculo 2, a 7 | 10 | 7 s | no | — |

Las tolerancias de validación, en `DefaultSkillCast.asset` (como mucho 12 objetivos), con el suelo de la recarga devuelta (*Min Cooldown Fraction*, 0,6) y la edad máxima del tick de un lanzamiento (*Max Cast Tick Age*, 0,5 s).

**Cuidado con.**
- `SkillShape` se guarda como número en los assets: formas nuevas, **al final**.
- **Proyectiles:** Aura expansiva funciona con los que estallan (anillo alrededor del círculo), no con los de golpe directo (Rayo). Burbuja también estalla al final de su recorrido si no toca a nadie (*Burst At End*): moja la zona, pero no atrapa a nadie. Un proyectil no lo para una pared: solo choca con la capa Enemy, como el básico.
- Si el dueño se desconecta con un proyectil en el aire, su copia ya no avisa de nada: no hace daño.
- Un cono de más de 180° no es una cuña: las cuentas de `SkillArea` lo dan por hecho (el Inspector no deja pasar de 180).
- El dibujo (`SkillShapeVisual`) es solo eso: construye una malla con la misma `SkillArea`, así que siempre enseña la zona real. Llegará el arte de verdad con las familias de efecto del catálogo (E.5).
- El id de la habilidad (`burst`, `mark`, `tide_wave`…) lo usa el launcher (`ability_cast`): no lo cambies una vez publicada una build.
- **Toda habilidad que se pueda tener tiene que estar en `SkillLibrary`**: los demás PCs reciben su id y la buscan ahí. Si falta, sale un error en consola y ese hueco queda vacío.
- **Los niveles** son la lista *Levels* de la ficha (`SkillLevelModifier`): el primero es el nivel 2 y el segundo el 3, y se suman. Los porcentajes son de los números base. Una habilidad sin *Levels* solo tiene nivel 1 (las de prueba).
- Quien usa los números de una habilidad lee la `SkillInstance` (con su nivel), nunca la ficha directamente: si no, el nivel 2 no se notaría.

### 4.7 Elementos y auras

**Qué hace.** Hay tres elementos: fuego, agua y electricidad. Golpear a un enemigo le deja un **aura** del elemento: **débil** con el básico (3,5 s) y **fuerte** con habilidades (6 s). Un aura de otro elemento, aplicada por algo que **puede detonar**, provoca una reacción.

**Piezas.** `Code/Elements/`: `ElementType` (el enum que viaja), `ElementDefinition` (nombre y color), `ElementLibrary`; `Auras/AuraHolder.cs` (el aura de un enemigo), `AuraSettings`, `AuraStrength`, `AuraApplyResult`, `AuraDebugLabel` (texto provisional).

**Cómo funciona.** `AuraHolder.TryApply(elemento, fuerza, quién, qué ataque, puede detonar)`, en el host:
1. Si el mismo ataque del mismo jugador ya aplicó un aura a ese enemigo hace menos de 1 s, se ignora (el daño sí cuenta).
2. Sin aura → se pone. Mismo elemento → se refresca (nunca se acorta) y pasa a ser de quien la refresca.
3. Otro elemento → solo si puede detonar, no está en el bloqueo de 0,5 s tras otra reacción y la tabla tiene esa pareja: **reacción** (se borra el aura y se avisa a todos).
4. El aura (elemento, fuerza, dueño) son SyncVars: todos la ven.
5. **Cuánto dura** sale de `DefaultAuras.asset`, más las mejoras de quien la aplica: `AuraHolder` le pide al `PlayerStats` de ese jugador `StrongAuraDuration` o `WeakAuraDuration` (4.20). Con Auras tenaces una fuerte dura 7,5 s; como mucho, el doble de la base (12 s). Solo el host lo calcula: la duración no viaja, los clientes solo ven si el aura está o no.

**Qué ajustar.** `Data/Elements/DefaultAuras.asset`: débil 3,5 s, fuerte 6 s, enfriamiento 1 s. Colores y nombres en `Data/Elements/Fire|Water|Electric.asset`.

**Cuidado con.** `ElementType` viaja por la red como un número: añade elementos nuevos **al final** del enum.

### 4.8 Reacciones

**Qué hace.** Combinar dos elementos provoca un efecto, y el orden importa (el aura que había decide la variante):

| Aura → detonador | Reacción | Efecto |
|---|---|---|
| Fuego → agua | Vapor (nube) | 15 de daño en 2 m y nube de 3 s que frena un 30 % |
| Agua → fuego | Vapor (golpe) | 30 de daño en 2 m y nube corta de 1,5 s al 15 % |
| Agua → electricidad | Electrocución (cadena) | 30 al objetivo y 3 saltos de 20 a enemigos mojados a ≤ 3 m |
| Electricidad → agua | Electrocución (área) | 25 de daño en 2 m |
| Electricidad → fuego | Sobrecarga (empuje) | 35 en 1,5 m y empuja 1 casilla |
| Fuego → electricidad | Sobrecarga (aturde) | 35 en 1,5 m y aturde 0,5 s |

**Piezas.** `Code/Elements/Reactions/`: `ReactionDefinition` (una fila de la tabla), `ReactionTable`, `ReactionService` (los efectos, en el host), `ReactionInfo`, `ReactionCloud`, `ReactionChain` + `ReactionChainBroadcast`; presentación: `ReactionVisuals`, `ChainVisual`, `Code/Camera/ReactionCameraShake.cs`, los titulares de `FloatingTextSpawner`.

**Cómo funciona.**
1. `AuraHolder` detecta la reacción y avisa a todos (`ObserversReaction`) **antes** de los efectos.
2. `ReactionService.Resolve` (host) busca los enemigos del radio y aplica aturdimiento y empuje, la nube, la cadena y el daño.
3. Las nubes viven en el host y frenan cada 0,25 s a quien esté dentro.
4. La cadena se manda como *broadcast* (no como RPC de un enemigo, porque cualquiera de la cadena puede morir antes de mandarla).
5. Cada PC dibuja el estallido, la nube, la cadena, el temblor de cámara y el titular desde `CombatEventService`.
6. Al terminar, `ReactionService` lanza el evento `Resolved` (solo en el host). Lo escuchan las mejoras de los dos jugadores que participaron (`PlayerUpgradeEvents`, 4.21): Viento a favor, Relevo, Asistencia.

**Qué ajustar.** Cada fila es un asset en `Data/Reactions/` (daño, radios, nube, saltos, empuje, aturdimiento, colores). La lista y el bloqueo de 0,5 s, en `DefaultReactions.asset`. El temblor, en `Data/Camera/ReactionShake.asset` (0,2 solo, 0,35 en dúo).

**Cuidado con.** El *Variant Id* de cada fila (`vapor_fire_water`…) lo usa el launcher. Las reacciones solo dañan la capa Enemy. Los muñecos no tienen `EnemyStatus`: no se frenan, no se aturden y no se empujan.

### 4.9 Enemigos

**Qué hace.** Dos enemigos, que caminan hacia el jugador en pie más cercano y le pegan al tocarlo. Al morir sueltan un orbe de experiencia.
- **Perseguidor** (`chaser`): el básico.
- **Dron** (`arc`): un ojo volador con hélice, más rápido y frágil, que aparece desde el minuto 0:46. Por ahora se mueve y ataca como el perseguidor (es su mismo prefab con otros datos); su movimiento a distancia y su ataque propio llegarán con la base de ataques de enemigo.

**Piezas.** `Code/Enemies/`: `Enemy` (lo común: su ficha, morir y soltar el orbe), `EnemyDefinition` (la ficha), `ChaserMovement` (cómo se mueve), `EnemyStatus` (frenado, aturdido, empujado), `Attacks/EnemyContactAttack` + `ContactAttackSettings` (cómo ataca), `TrainingDummy` (muñeco que revive).

**Dónde está montado.** `Prefabs/Enemies/Chaser.prefab` (capa Enemy): NetworkObject, NetworkTransform, Enemy, EnemyStatus, Health, ChaserMovement, MotionTracker, AuraHolder, collider, Rigidbody2D, HitFlash, EnemyContactAttack. Hijos: `Visual`, `HealthLabel`, `AuraLabel`.

**Cómo funciona.** Idea clave: **«verbos en código, sustantivos en datos»**. Cada forma de moverse o atacar es un componente pequeño con su propio asset de valores. Un enemigo nuevo que se mueva y ataque como uno que ya existe es **solo un prefab y unos assets**, sin código. Solo el host simula enemigos; en los clientes su cuerpo es cinemático y `NetworkTransform` lo mueve.

**Qué ajustar.**

| Valor | Asset | Ahora |
|---|---|---|
| Velocidad, aceleración, distancia a la que para, experiencia | `Data/Enemies/Chaser.asset` · `Arc.asset` | 2,5 · 20 · 0,5 · 1 · dron 3,5 · 25 · 0,5 · 2 |
| Vida | `Data/Enemies/ChaserHealth.asset` · `ArcHealth.asset` | 60 · dron 45 |
| Daño, alcance y ritmo del contacto | `Data/Enemies/ChaserContactAttack.asset` · `ArcContactAttack.asset` | 10 · 0,75 · 1 s · dron 8 · 0,8 · 0,7 s |

**Cuidado con.** El *Id* (`chaser`, `arc`) lo usa el launcher. Los enemigos se destruyen al morir (no se reutilizan). Un enemigo nuevo tiene que estar en las dos listas de prefabs de FishNet (`DefaultPrefabObjects` y `FishNet_PrefabObjects`), o al generarlo da error.

### 4.10 Oleadas

**Qué hace.** Saca enemigos según un horario: 6 oleadas en 3 minutos, con un tope de 40 vivos a la vez, siempre lejos de los jugadores.

**Piezas.** `Code/Enemies/Waves/WaveSpawner.cs`, `WaveSchedule`, `WaveEntry`.

**Dónde está montado.** `Arena_Test` › `WaveSpawner`, con su horario y sus puntos de aparición (`SpawnerPoints`).

**Cómo funciona.** Cada fotograma (en el host) avanza su reloj y saca los enemigos que tocan mientras haya sitio. Elige el punto con una tirada de la semilla entre los que están a ≥ 8 casillas de todos (o el más lejano). Cuenta las oleadas empezadas y si se han acabado (SyncVars, para el launcher). `MatchFlow` lo pausa al perder y lo reinicia al reiniciar.

**Qué ajustar.** `Data/Enemies/Waves/ArenaTestWaves.asset`: duración (180 s), tope de vivos (40), distancia mínima (8) y la lista de oleadas (segundo de inicio, prefab, cantidad e intervalo). Los drones salen a los 46, 76, 106 y 141 s (2, 5, 7 y 10), entre las tandas de perseguidores.

**Cuidado con.** El ritmo de experiencia depende de cuántos enemigos salen: ahora las oleadas llevan al nivel 3-4 en 3 minutos, cuando el catálogo espera el 7. Toca ajustarlas junto con la curva de experiencia.

### 4.11 Semilla y azar

**Qué hace.** Cada partida tiene una semilla que decide todo lo aleatorio que cambia el juego (oleadas, ofertas de mejoras y, más adelante, salas). Con la misma semilla y las mismas decisiones, sale la misma partida.

**Piezas.** `Code/Core/Random/`: `SeededRandom` (generador PCG32 propio, igual en todos los PCs), `RandomStream` (un flujo por propósito: Waves 1, Rooms 2, Offers 100-103 uno por jugador), `RunRandomService` (el servicio), `RunRandomSettings` (`Data/Core/RunRandom.asset`); `Code/Networking/MatchSeed.cs` (la semilla para todos los PCs).

**Cómo funciona.** El host elige la semilla al arrancar (o la fija del asset en desarrollo). Cada sistema pide **su flujo**: lo que gaste uno nunca cambia los números de otro (una tirada más en las oleadas no cambia las ofertas). `MatchSeed` la copia en una SyncVar para que todos puedan verla (F1).

**Qué ajustar.** `RunRandom.asset`: *Fixed Seed* (8 caracteres hex) para repetir una partida y *Log Rolls* para escribir las tiradas en consola. Solo funcionan en el editor y en builds de desarrollo.

**Cuidado con.**
- **Solo el host tira**, y solo para lo que cambia el juego. Lo cosmético (partículas, temblor, números) usa `UnityEngine.Random`: si lo mezclas con la semilla, la misma semilla dejará de dar la misma partida.
- **No renumeres los flujos** de `RandomStream`: cambiaría lo que da cada semilla. Para algo nuevo, un número nuevo.
- Tira siempre **el mismo número de veces** pase lo que pase (las oleadas tiran una vez por enemigo antes de mirar a los jugadores); si no, dos partidas con la misma semilla se desincronizan.

### 4.12 Derribado y reanimación

**Qué hace.** A 0 de vida no mueres: quedas **derribado** (translúcido, sin moverte ni atacar). Un compañero en pie que se queda cerca te levanta. El grupo tiene un número limitado de reanimaciones por partida.

**Piezas.** `Code/Player/Downed/`: `PlayerDowned` (derribado = vida a 0), `PlayerDownedView` (el aspecto y la etiqueta), `PlayerRevive` (la reanimación), `ReviveCharges` (las cargas del grupo), `ReviveSettings` (`Data/Players/DefaultRevive.asset`).

**Cómo funciona.**
1. Derribado no es un valor aparte: es `Health` a 0. Todo lo que devuelve la vida (reanimar, el truco «Curar a todos») levanta.
2. El host mira cada 0,1 s qué compañeros en pie están a ≤ 1,5 m y no han recibido un golpe en el último segundo.
3. La primera reanimación tarda 3 s; cada una más tarda 1 s más. Cada ayudante extra acelera ×0,5 más. Sin nadie cerca, el progreso vuelve a 0.
4. El host no manda el progreso continuamente: manda una «recta» (dónde estaba en un tick y a qué velocidad crece) y cada PC calcula el porcentaje. Solo manda algo nuevo cuando cambia la velocidad.
5. Al terminar se gasta una carga y el jugador se levanta con el 50 % de vida y 1 s invulnerable.
6. Cargas: 1 solo, 2 con dos, 3 con tres o cuatro. El total solo sube cuando entra alguien (nunca baja). Sin cargas: «Sin reanimaciones».

**Qué ajustar.** `DefaultRevive.asset`: radio, duración, segundos extra por uso, aceleración por ayudante, bloqueo tras golpe, vida al levantarse, invulnerabilidad y cargas por número de jugadores.

**Cuidado con.** Sin `ReviveCharges` en la escena no hay límite (así está ReactionLab). Un jugador derribado que sale y vuelve reaparece de pie: no hay identidad persistente hasta Steam.

### 4.13 La partida: reloj, fin y reinicio

**Qué hace.** Cuenta el tiempo de partida, la termina cuando todos están derribados («Habéis caído») y deja al host reiniciarla para todos con una semilla nueva.

**Piezas.** `Code/Networking/MatchClock.cs`, `MatchFlow.cs`, `MatchPhase.cs`; `Code/UI/InGame/GameOverPanel.cs`.

**Dónde está montado.** El objeto **`Match`** de Arena_Test. ReactionLab solo lleva `MatchSeed` y `MatchClock`.

**Cómo funciona.**
1. `MatchClock` guarda el tick en que empezó; cada PC calcula los segundos con su copia del tick.
2. `MatchFlow` (host) mira cada 0,25 s si todos los jugadores están derribados. Si es así, congela el tiempo y las oleadas para el panel y pausa el `WaveSpawner`.
3. **Reinicio sin recargar la escena** (FishNet no recarga una escena global ya cargada y los jugadores viven en ella). `MatchFlow.RestartMatch` deja cada pieza como al empezar, en un solo fotograma y en este orden:
   1. Quita los enemigos sin «matarlos» (así no hay números, muertes ni eventos).
   2. Cura los muñecos y les quita el aura, y quita las nubes.
   3. Quita los orbes.
   4. Reinicia las oleadas y el reloj.
   5. Pide una semilla nueva.
   6. Cura a los jugadores, cancela sus reanimaciones y los vuelve a su punto.
   7. Repone las cargas y vuelve al nivel 1.
   8. Borra las mejoras y las ofertas.
   9. Sube `RunNumber`.
4. Quien entra en una partida ya perdida aparece derribado (`MatchFlow.ShouldSpawnDowned`).

**Cuidado con.** **Si creas algo nuevo que pertenece a la partida, tiene que saber reiniciarse**: dale un método `ResetForNewRun()` (solo host) y llámalo desde `MatchFlow.RestartMatch`, con un campo opcional en `MatchFlow` (ver receta 5.9). Piensa en qué orden: por ejemplo, los orbes se quitan antes de volver al nivel 1, para que ninguno se recoja en la partida nueva.

### 4.14 Experiencia, niveles y orbes

**Qué hace.** La experiencia es **del grupo**: una sola barra, todos suben a la vez. Los enemigos sueltan orbes; los recoge el jugador en pie más cercano.

**Piezas.** `Code/Progression/`: `RunExperience` (nivel y barra), `XpCurve` (`Data/Progression/DefaultXpCurve.asset`), `ExperienceOrbs` (los orbes del suelo), `ExperienceOrb`, `OrbSettings` (`DefaultOrbs.asset`), `ExperienceOrbsView` (dibujarlos); `Code/UI/InGame/ExperienceBarView.cs`.

**Cómo funciona.**
1. Un enemigo muere → `ExperienceOrbs.Drop` (host) añade un orbe a una **lista sincronizada** (no son objetos de red: así cuesta muy poco tener muchos). Con más de 40, el nuevo se suma al más cercano.
2. Cada 0,1 s el host da cada orbe al jugador en pie más cercano a ≤ 1,5 m. Avisa a todos de quién lo cogió (para el vuelo) y lo quita de la lista.
3. `RunExperience.AddExperience` suma y sube los niveles que toque, uno a uno, avisando con `LeveledUp(nivel)` en el host (es lo que crea las ofertas de mejoras).
4. Lo que pide un nivel se fija al empezarlo, con los jugadores de ese momento: entrar o salir cambia el siguiente nivel, no el actual.

**Qué ajustar.** `DefaultXpCurve.asset`: 10 + 8 por nivel, ×1 / ×1,5 / ×1,9 / ×2,2 según jugadores. `DefaultOrbs.asset`: radio de recogida 1,5, tope 40, vuelo 0,25 s. La experiencia de cada enemigo, en su ficha.

**Cuidado con.** La vista de los orbes compara la lista entera cada vez que cambia, en vez de seguir los cambios uno a uno: en la ventana del host, FishNet no dice qué orbe se quitó (fue el bug de los orbes fantasma; ver networking.md).

### 4.15 Mejoras y ofertas

**Qué hace.** En cada subida de nivel, cada jugador recibe una **oferta de 3 mejoras** (con su propio azar) y elige en una franja abajo sin pausar el juego. Puede volver a tirar (como mucho 2 veces por partida), descartar una opción (1 vez por partida) o saltar la oferta.

**Piezas.** `Code/Progression/Upgrades/`:

| Pieza | Qué es |
|---|---|
| `UpgradeDefinition` | La ficha de una mejora: id, nombre, descripción, rareza, acumulaciones, peso, requisitos, exclusiones, cuándo puede salir, personaje |
| `UpgradeRarity`, `UpgradeCategory`, `UpgradeElement`, `UpgradeAppearIf`, `RarityWeights` | Las piezas de la ficha |
| `UpgradePool` | La lista de mejoras que pueden salir, más Respiro aparte |
| `OfferSettings` | Todos los números de las ofertas |
| `OfferGenerator` | Arma una oferta siguiendo las reglas del catálogo (C) |
| `OfferContext`, `IUpgradeHolder`, `OfferOption`, `OfferPlace` | Lo que el generador necesita y lo que devuelve |
| `PlayerUpgrades` | En el jugador: lo que tiene, sus rerolls y descartes, su cola de ofertas y la conversación con su PC |
| `UpgradeOffers` | En `Match`: reparte una oferta a cada jugador por subida, da los rerolls por tiempo |
| `OfferAnswer`, `ReceivedOffer` | La respuesta del jugador y la oferta tal como le llega |
| `PlayerChoosingView` | La etiqueta «Eligiendo» |

Interfaz: `Code/UI/InGame/UpgradeOfferPanel.cs` y `UpgradeOfferRow.cs`. Datos: `Data/Progression/Upgrades/` (17 fichas), `DefaultUpgradePool.asset`, `DefaultOffers.asset`.

**Cómo se arma una oferta** (`OfferGenerator`, en el host):
1. **Plaza A, «lo tuyo»:** otra acumulación de algo que ya tienes, una mejora que pide algo tuyo (tu personaje, una habilidad, tu elemento u otra mejora) o **el siguiente nivel de una de tus habilidades** (#162). Si hay una evolución lista, va aquí. Si no hay nada, la plaza pasa a ser abierta.
2. **Plazas B y C, abiertas:** primero el tipo: **una habilidad nueva el 25 % de las veces** mientras tengas hueco y quede alguna que darte (las de tu personaje pesan 3, las genéricas 1). Si no, una mejora: la familia (35 % de tu personaje, 65 % genérica), luego la rareza según el nivel (2-4: 80/20/0 · 5-8: 65/30/5 · 9-12: 50/38/12 · 13+: 40/40/20, común/rara/épica) y al final la mejora, según su peso.
3. **Garantías:** la plaza B es **una habilidad nueva en los niveles 2 y 6** (si tienes hueco), al menos rara en el 5 y épica en el 10. Jugando solo, desde el 6, si no tienes ninguna habilidad de otro elemento, la garantía solo ofrece de otro elemento (catálogo E.0, regla 1): así siempre puedes reaccionar tú solo.
4. **Filtros:** fuera lo que no cumple requisitos, lo excluido por algo que tienes, lo que está al máximo, lo descartado, lo que no toca por nivel o número de jugadores, lo que ya tiene otro jugador si es única, y más de una de riesgo. Lo que salió en la oferta anterior pesa la mitad.
5. **Si no queda nada:** baja la rareza, cambia de familia, sube a rarezas permitidas en ese nivel y, por último, **Respiro**.

**Habilidades en las ofertas** (#162). Una opción puede ser una mejora, una habilidad nueva (nivel 1) o el siguiente nivel de una que tienes. Por la red viajan como **claves**: el id para una mejora (`def_vitality`), `skill:tide_bubble` para una habilidad nueva y `skill:tide_wave@2` para un nivel. Lo visto y lo descartado se guarda por clave, así que descartar `skill:tide_bubble` quita esa habilidad de las ofertas, pero no los niveles de otra. Al elegirla, el host la pone en el siguiente hueco libre (`PlayerSkills.TryAddSkill`) o le sube el nivel (`TryLevelUp`), y la lista de habilidades llega a todos. **Cambia lo que da cada semilla:** desde el #162 las habilidades entran en las tiradas.

**El recorrido por la red:**
1. El host sube de nivel → `UpgradeOffers` encola una oferta en el `PlayerUpgrades` de cada jugador (también derribados).
2. La primera de la cola se genera **cuando le toca** (con lo que el jugador tiene en ese momento) y se manda **solo a su dueño** (TargetRpc con un número de oferta, el nivel y los ids).
3. El panel la enseña al instante. El jugador pulsa 1/2/3, F, X o C y el panel manda la respuesta (ServerRpc con el número de oferta).
4. El host comprueba que es la oferta que sigue pendiente y que la opción vale todavía. Aplica, contesta y abre la siguiente.
5. Cada cambio (reroll, descarte) lleva un número nuevo: una respuesta a una versión vieja se ignora. Si algo no vale, reenvía la misma oferta para que el panel no se quede esperando.

**Qué ajustar.**
- Cada mejora: su ficha en `Data/Progression/Upgrades/` (rareza, acumulaciones, peso, requisitos, *Appear If*).
- Qué mejoras existen: la lista de `DefaultUpgradePool.asset`.
- Las reglas, en `DefaultOffers.asset`: opciones por oferta, tabla de rarezas, suerte, familia propia, peso de lo visto, niveles de garantía, habilidades (niveles con habilidad garantizada 2 y 6, probabilidad de habilidad nueva 25 %, pesos 3 / 1 de las del personaje y las genéricas, peso de un nivel 1 y nivel del solitario 6), nivel de riesgo y dúo, rerolls (tope 2, +1 a los 240 y 480 s), tercios por saltar y por Respiro, descartes e invulnerabilidad al elegir (1 s, como mucho cada 10 s).

**Cuidado con.**
- **Lo que hace cada mejora** está en su lista de bloques de efecto (ver 4.21). **Una mejora sin bloques no sale en las ofertas**: no haría nada. Así están las 6 de personaje hasta que lleguen sus bloques con el #162. El filtro mira solo la ficha, antes de cualquier tirada, así que no cambia lo que da una semilla.
- **Los ids de las mejoras viajan por la red y los usa el launcher**: no los cambies.
- **El orden de la lista del pool** es el orden en que el generador las recorre: cambiarlo cambia lo que da cada semilla (las partidas nuevas no se rompen, pero una semilla apuntada ya no dará lo mismo).
- Requisitos escritos como en el catálogo (`basic:projectile`, `char:ember`, `skill:tide_wave`, `element:fire`…). `char:` se cumple con el personaje del jugador (4.3). Un tipo que el juego aún no conoce **nunca se cumple** (aviso en consola): es lo seguro.
- Los pesos se pasan a enteros (×100) a propósito: con floats, dos PCs podrían redondear distinto y la semilla dejaría de repetir.

### 4.16 Interfaz

**Qué hace.** HUD de partida, menús, avisos y paneles.

**Piezas y montaje.**

| Pieza | Dónde | Qué hace |
|---|---|---|
| `Code/UI/PixelCanvasScaler.cs` | `InGameMenuCanvas` | Escala el canvas por el mismo número entero que los píxeles del juego: **1 unidad de UI = 1 píxel del juego** |
| `ExperienceBarView` | `InGameMenuCanvas/ExperienceBar` (arriba al centro, 160×4) | Barra y nivel del grupo |
| `SkillSlotView` + `Prefabs/UI/SkillSlot.prefab` | `InGameMenuCanvas/SkillBar` (abajo al centro, 88×24) | Habilidades del jugador local y su recarga |
| `UpgradeOfferPanel` + `UpgradeOfferRow` | `InGameMenuCanvas/UpgradeOfferPanel` (franja de 360×60 sobre la barra de habilidades) | Elegir mejora |
| `GameOverPanel` | `InGameMenuCanvas/GameOverPanel` | Fin de partida y reinicio |
| `LeaveSessionButton` | `InGameMenuCanvas/LeaveSessionBtn` | Salir |
| `ToastService` + `ToastView` | `Bootstrap` › `ToastCanvas` | Avisos cortos |
| `NetworkStatsView` | `Bootstrap` › `NetworkStats` | Ping y estadísticas (F3) |
| `ConnectionMenuController`, `MainMenuController`, `BuildVersionLabel` | `MainMenu` | Menú y versión |

**Resolución.** El juego se ve a **640×360** con la Pixel Perfect Camera y recorte (*Crop Frame Windowbox*): todos ven la misma zona y, si la pantalla no es múltiplo exacto, salen bandas negras. 1080p = ×3, 1440p = ×4, 4K = ×6. Ventana por defecto de 1280×720. Los tamaños del HUD se dan **en píxeles del juego** (un icono de 16×16 mide 16 en el canvas).

**Teclas** (`Code/Player/Input/InputService.cs`, sobre `Settings/Input/InputSystem_Actions.inputactions`):

| Mapa | Teclas |
|---|---|
| Player | WASD o flechas mover · clic izquierdo, clic derecho y Q, habilidades |
| UI | Escape cancelar, ratón |
| Debug | F1 trucos · F3 estadísticas de red · F6 cambiar de elemento (solo desarrollo) |
| Offer | 1, 2, 3 elegir · F volver a tirar · X descartar · C saltar · Tab ocultar la franja |

**Cuidado con.**
- `PlayerSkills` no lanza una habilidad si el ratón está sobre la interfaz (para que pulsar un botón no dispare). Por eso todo lo del HUD que **no se pulsa** lleva *Raycast Target* desactivado: si no, un texto o una barra bajo el cursor impediría lanzar. Lo que **sí** se pulsa (botones, filas de la franja de mejoras) lo lleva activado.
- El archivo `GameInputActions.cs` lo **genera Unity** a partir del `.inputactions`: no lo edites a mano; cambia las teclas en el editor de Input Actions.
- El panel de trucos y el laboratorio usan otro canvas (`DebugCanvas`, escalado a 1920×1080), no el de píxeles del juego.

### 4.17 Cámara

`Code/Camera/`: `LocalPlayerCameraTarget` (la cámara de Cinemachine sigue al jugador local), `ReactionCameraShake` + `ReactionShakeSettings` (temblor por reacción), `SnapToPixelGrid` (mantiene los textos sobre la rejilla para que no tiemblen) y `ResolutionTestRig` (solo la escena de pruebas). Los desplazamientos de hijos con sprite van en múltiplos de 0,0625 (1 píxel).

### 4.18 Herramientas de prueba

| Herramienta | Cómo se usa | Dónde |
|---|---|---|
| **Panel de trucos** | F1, solo en desarrollo. Generar enemigos, matar a todos, saltar o reiniciar oleadas, invulnerable, dañar o curar a todos, dar experiencia, dar reroll, **dar habilidad** (la siguiente de la biblioteca que no tienes: a un hueco libre o en lugar de la última) y **habilidades de prueba** (Estallido y Marca, para las pruebas de reacciones); enseña semilla y reanimaciones | `Code/Cheats/` (`DebugCheats` hace las cosas en el host, `DebugCheatsPanel` son los botones, `DebugCheatsSettings` los valores) |
| **Laboratorio de reacciones** | Botón «Laboratorio» del menú (solo desarrollo): poner auras y detonar sobre muñecos | `Code/Elements/ReactionLab/`, escena `ReactionLab` |
| **Estadísticas de red** | F3: ping y tráfico | `Code/Networking/Stats/` |
| **PerformanceLogger** | En builds de desarrollo: una línea por cada fotograma de más de 100 ms y un resumen cada 10 s | `Bootstrap` › `NetworkManager` |
| **CombatEventLogger** | Actívalo para contar golpes, muertes y reacciones por ventana | `Bootstrap` › `NetworkManager` (desactivado) |
| **Semilla fija y Log Rolls** | `Data/Core/RunRandom.asset` | 4.11 |
| **ResolutionTest** | Escena suelta para comparar resoluciones | `Scenes/ResolutionTest` |

Cualquier truco escribe `cheat_used` para el launcher: una partida con trucos no cuenta para las misiones.

### 4.19 Eventos del launcher

**Qué hace.** El juego escribe en su `Player.log` líneas `[PalitoEvent] …` con hechos («he provocado un Vapor», «he subido al nivel 3»), y Palito Launcher decide qué misiones de prueba se cumplen.

**Piezas.** `Code/Core/LauncherEvents/`: `LauncherEvents` (un método por evento), `LauncherEventWriter` (el formato), `LauncherEventReporter` (escucha lo que ya cuentan los servicios y lo convierte en eventos), `LauncherDiagnostics` (tirones, rendimiento, ping, errores), `LauncherAddressKind`.

**Cuidado con.** Es un **contrato**: nunca renombres ni quites un evento, un campo o un id. Añadir siempre se puede. Cada evento nuevo va también en [launcher-events.md](launcher-events.md) y en la copia del repo del launcher.

### 4.20 Estadísticas del jugador

**Qué hace.** Junta en un solo sitio los números de cada jugador que las mejoras pueden cambiar: vida máxima, velocidad, daño recibido, radio de recogida de orbes y daño, cadencia, alcance, balas por ataque y abanico del básico, duración de tus auras, el anillo de aura y el daño de tus habilidades.

**Piezas.** `Code/Player/Stats/`:

| Pieza | Qué es |
|---|---|
| `StatId` | La lista de estadísticas |
| `StatUnit` | Cómo cambia una: `Flat` suma al valor base, `Percent` multiplica |
| `StatBonus` | Un cambio concreto (estadística, unidad, valor y de qué mejora sale) |
| `PlayerStats` | En el jugador: sus cambios y el valor final de cada estadística |
| `PlayerStatSettings` + `StatLimit` | Los topes del catálogo (A.2), en `Data/Players/DefaultPlayerStats.asset` |
| `TimedStatBonus` | Un cambio que solo dura un rato: el cambio y sus ticks de inicio y fin |
| `TargetConditions` + `ConditionalBonus` | Cambios que solo cuentan en algunos golpes, según cómo está el objetivo (hoy: si tiene aura) |

**Dónde está montado.** `PlayerStats` en el prefab Player, con *Settings* = `DefaultPlayerStats`.

**Cómo funciona.**
1. **Los valores base no se mueven de sitio.** Cada sistema sigue teniendo los suyos en su asset (velocidad en `DefaultPlayerMovement`, cadencia en `DefaultAutoAttack`, vida en `DefaultPlayerHealth`, recogida en `DefaultOrbs`). Cuando necesita el valor, se lo pide a `PlayerStats` pasándole la base: `stats.Get(StatId.BasicRange, settings.Range)`.
2. **La cuenta** es siempre la misma: `(base + suma de Flat) × (1 + suma de Percent / 100)`, y después el tope. Los porcentajes se suman entre sí: dos +12 % y un −5 % dan +19 %.
3. **Quién la hace:** el host (que es la verdad y valida) y el dueño del jugador (que simula su movimiento y sus disparos). Los dos usan la misma función con los mismos cambios, así que llegan al mismo número.
4. **Qué viaja:** la lista de cambios es una `SyncList` que **solo recibe el dueño**: los demás no la necesitan. La única estadística que ven todos es la **vida máxima**, que `Health` sincroniza aparte para que todos vean `x/120`.
5. **Quién la usa:**

| Estadística | La lee | En qué PC |
|---|---|---|
| Vida máxima | `Health` (la pone `PlayerStats`) | Host, y llega a todos |
| Velocidad | `PlayerMovement` | Dueño |
| Daño recibido | `Health.TryTakeDamage` | Host |
| Radio de recogida | `ExperienceOrbs` | Host |
| Daño del básico | `PlayerAutoAttack.ApplyHit` | Host |
| Cadencia del básico | `PlayerAutoAttack` (cuándo dispara y el presupuesto de disparos del host) | Dueño y host |
| Alcance del básico | `PlayerAutoAttack` (a quién apunta y hasta dónde vuela la bala) | Dueño y host |
| Balas por ataque (base 1, máximo 4) y abanico en grados (base 0) | `PlayerAutoAttack` (la ráfaga, 4.5) | Dueño y host |
| Duración de tus auras fuertes y débiles (base en `DefaultAuras`, máximo el doble) | `AuraHolder.TryApply` (4.7) | Host |
| Anillo de aura de las habilidades: metros y enemigos de más (base 0) | `PlayerSkills` (4.6) | Host |
| Daño de las habilidades (base: el de cada habilidad) | `PlayerSkills.GetDamage` | Host |

6. **Cambios con condición** (Cazador de auras). Algunos cambios solo cuentan si el objetivo del golpe cumple algo. No se suman a los demás: se guardan aparte, **solo en el host** (son siempre de daño, y el daño lo decide el host), y se piden en cada golpe con `stats.Get(StatId.SkillDamage, base, condiciones)`, diciendo qué cumple ese objetivo. La condición se mira **antes** de que el golpe cambie al enemigo (regla 10 del motor): si la habilidad detona el aura, el bonus cuenta igual.
7. **Cambios temporales** (Tempo, Viento a favor). Duran unos segundos: se guardan con el **tick** en que empiezan y el tick en que acaban, en otra lista que también recibe solo el dueño (`TimedStatBonus`). Como el host y el dueño usan el mismo reloj de ticks, los dos dejan de contarlo a la vez sin mandarse nada más. Esta lista se manda en el siguiente tick, no cada 0,1 s como la otra: dura 2 s y cada tick de retraso se notaría.
   - **Predicción del dueño.** Si el cambio sale de algo que hace el propio dueño (lanzar una habilidad), el dueño no espera: lo aplica él mismo al momento (`PredictTimedBonus`) con el mismo tick que manda al host. Cuando llega la copia del host, la del dueño se quita y queda la del host, idéntica. Si en 1 s no llega (el host no aceptó el lanzamiento, o para el host la mejora aún estaba en su enfriamiento), se quita sola.
   - Lo que sale de una reacción (Viento a favor) no se predice: lo decide el host y llega con el ping.

**Qué ajustar.** Los topes, en `DefaultPlayerStats.asset`: un multiplicador mínimo y máximo respecto a la base (1,4 es +40 %) y un mínimo y máximo absolutos (la vida, entre 50 y 250). Una estadística sin fila no tiene tope.

**Cuidado con.**
- **Más alcance = la bala vuela más.** Si no, con Cañón largo apuntarías a enemigos a los que la bala no llega. La bala dura más en proporción (`Reach`), y el host comprueba los impactos con esa misma distancia.
- **Ganar vida máxima no cura**; perderla sí recorta la vida actual. Por eso Vitalidad lleva aparte su cura de 20.
- `StatId` y `StatUnit` se guardan como números en los assets y viajan por la red: añade al final, no renumeres.
- Los cambios se borran al reiniciar la partida (`PlayerUpgrades.ResetForNewRun` llama a `ClearBonuses`).
- F1 enseña tus estadísticas tal como las calcula tu PC: si no cambian al coger una mejora, el fallo está antes de que lleguen al dueño.

### 4.21 Efectos de las mejoras

**Qué hace.** Hace que cada mejora haga lo que dice, solo con datos: la ficha lleva una lista de **bloques**, y el host los aplica cuando un jugador la coge.

**Piezas.** `Code/Progression/Upgrades/Effects/`:

| Pieza | Qué es |
|---|---|
| `EffectBlock` | La base de todos los bloques. Cada bloque sabe hacer una cosa al coger la mejora (`Acquire`) |
| `StatModifierBlock` | Cambia una estadística para el resto de la partida |
| `ConditionalBlock` | Una lista de cambios de estadística que solo cuentan en golpes a objetivos que cumplen una condición (*Has Aura*). Hoy solo admite cambios de estadística, y solo el daño de habilidades mira condiciones |
| `AuraModifierBlock` | Cambia las auras que pones: cuánto duran (fuertes o débiles) y el anillo de aura alrededor de tus habilidades. También son estadísticas por dentro |
| `AttackModifierBlock` | Cambia cómo dispara el básico: balas de más y cuánto se abren. Por dentro son dos estadísticas, así que se acumula y tiene tope como cualquier otra |
| `OnEventBlock` | Cuando pasa algo, hace sus acciones. Eventos: *Acquired* (al cogerla), *Skill Cast* (lanzas una habilidad) y *Reaction* (una reacción en la que participas). Para *Reaction* filtra por papel (*Role*: el aura era tuya, la detonaste tú o cualquiera de las dos) y por dúo. Tiene su enfriamiento interno (*Cooldown*) y, para dar algo al compañero, uno por compañero (*Recipient Cooldown*) |
| `PlayerUpgradeEvents` | En el jugador. Avisa a sus mejoras de lo que pasa en la partida y guarda los enfriamientos internos de cada bloque para ese jugador (`Code/Progression/Upgrades/`) |
| `EffectEventInfo` | Lo que pasó: el evento, su tick y, en reacciones, el papel, si fue dúo y quién es el compañero |
| `ReactionRole`, `EffectTarget` | Las listas del papel en la reacción y de a quién va una acción (a ti o al compañero) |
| `TimedStatAction` | Cambia una estadística unos segundos |
| `EffectAction` | La base de las acciones: algo que pasa una vez |
| `RestoreAction` | Cura y devuelve recarga, a ti o al compañero del dúo |
| `ResourceAction` | Da tercios de reroll (dentro del tope de 2) |
| `EffectEvent` | La lista de eventos |
| `EffectContext` | Lo que recibe cada bloque: el jugador, sus estadísticas, su vida, la mejora |

En el editor: `Code/Core/Inspector/SubclassPickerAttribute.cs` y `Code/Editor/SubclassPickerDrawer.cs`, el botón para elegir el tipo de cada bloque.

**Cómo funciona.**
1. La lista de bloques es un campo `[SerializeReference]`: a diferencia de un campo normal, puede guardar **objetos de clases distintas** (un *Stat Modifier Block*, un *On Event Block*…). Unity no trae un botón para elegir la clase; lo añade `SubclassPickerDrawer`.
2. Al elegir una mejora, `PlayerUpgrades` (host) crea un `EffectContext` y llama a `Acquire` de cada bloque, en orden, una vez por acumulación.
3. Los resultados llegan a todos por lo que ya viaja: las estadísticas (al dueño), la vida, los rerolls, las recargas.
4. **Durante la partida**, `PlayerUpgradeEvents` recibe los eventos: los lanzamientos aceptados (de `PlayerSkills`) y las reacciones (de `ReactionService.Resolved`). Para cada mejora que tiene el jugador, busca sus *On Event Block* de ese evento, mira si el filtro encaja y si su enfriamiento interno ha terminado, y corre sus acciones. Una vez por evento, tenga las acumulaciones que tenga (ninguna de estas se acumula).
   - Los enfriamientos internos se cuentan en ticks desde el tick del evento. Están guardados por jugador: el bloque es del asset y lo comparten todos.
   - El dueño hace lo mismo con **su propio lanzamiento**, en modo predicción: solo actúan las acciones que saben predecir (`Predict`; hoy, `TimedStatAction`). Lo demás (curar, recargas) lo hace solo el host.

**Las mejoras que ya funcionan:**

| Mejora | Bloques |
|---|---|
| Cañón largo | Alcance +3 (Flat), cadencia +9 % (Percent) |
| Vitalidad | Vida máxima +20; al cogerla, cura 20 |
| Respiro | Al cogerla, cura 30 y +1/3 de reroll |
| Imán | Recogida +50 % |
| Pies ligeros | Velocidad +12 %, vida máxima −10 |
| Piel gruesa | Daño recibido −10 %, velocidad −5 % |
| Doble disparo | Ataque: +1 bala, abanico 12°; daño del básico −30 % (cada bala hace 7) |
| Auras tenaces | Aura: fuertes +1,5 s (6 → 7,5; con dos, 9) |
| Aura expansiva | Aura: anillo de +1 m alrededor de tus habilidades, hasta 8 enemigos más |
| Cazador de auras | Con condición *Has Aura*: daño de habilidades +15 % (Estallido 16 → 18, Marca 10 → 12) |
| Tempo | Al lanzar una habilidad (cada 4 s como mucho): intervalo del básico −20 % durante 2 s (0,6 → 0,48 s). El dueño lo predice |
| Viento a favor | Al detonar una reacción (cada 6 s como mucho): velocidad +25 % durante 2 s (5 → 6,25) |
| Relevo | Cuando otro jugador detona tu aura (cada 2 s como mucho): −1 s a todas tus recargas |
| Asistencia | Cuando detonas el aura de otro jugador: −1 s a todas sus recargas; cada compañero lo recibe de ti como mucho cada 2 s |

Con esto funcionan las 10 genéricas del Pre 3 y las 3 de prueba. Las 6 de personaje llegan con los personajes.

**Cuidado con.**
- **Un bloque solo corre en el host.** Si un bloque necesita que el dueño note algo al instante (más velocidad, otra cadencia), tiene que llegarle por sus estadísticas, no haciendo algo en el cliente.
- **Renombrar la clase de un bloque rompe las fichas que lo usan**: el asset guarda el nombre de la clase. Si hay que renombrar, añade `[UnityEngine.Scripting.APIUpdating.MovedFrom(false, null, null, "NombreViejo")]` a la clase.
- Un bloque nuevo es una clase `[Serializable]` que hereda de `EffectBlock` (o de `EffectAction`), con un constructor sin parámetros: el botón del Inspector la encuentra solo.
- **`PlayerUpgradeEvents` tiene que estar en el prefab Player**: sin él, las mejoras de evento no hacen nada (y no avisa).
- Al reiniciar la partida, el host borra los enfriamientos internos; el dueño no borra los suyos de predicción. Si reinicias justo después de lanzar con Tempo, la primera predicción puede no salir y Tempo llegará con el ping. Es esperado.
- Una reacción del jugador consigo mismo (solo, o pones el aura y la detonas tú) cuenta para los dos papeles, pero nunca es dúo.

---

## 5. Recetas: cómo hacer cosas nuevas

### 5.1 Un enemigo nuevo que se mueve y ataca como el perseguidor

1. Duplica `Prefabs/Enemies/Chaser.prefab` y cámbiale el sprite de `Visual`.
2. Crea su ficha: *Create › Coop2D › Enemies › Enemy Definition*, en `Data/Enemies/`. Ponle un **id nuevo en snake_case** (`fast_chaser`), su velocidad y su experiencia.
3. Si su vida es distinta, crea un *Health Settings* nuevo; si pega distinto, un *Contact Attack Settings* nuevo. Asígnalos en el prefab.
4. Añádelo a un horario de oleadas (5.2) o a la lista de enemigos de `Data/Cheats/DebugCheats.asset` para probarlo con F1.
5. **FishNet:** el prefab tiene que estar en la lista de prefabs de red. Normalmente se añade solo al crearlo; si al generarlo da error de prefab no registrado, revisa *DefaultPrefabObjects*.
6. Añade su id a la tabla de valores del launcher (`enemy`).

**Si se mueve o ataca de otra forma**, eso sí es código: un componente nuevo como `ChaserMovement` o `EnemyContactAttack`, con su propio asset de valores, que solo corre en el host. Fíjate en esos dos como plantilla.

### 5.2 Una oleada o un horario nuevo

Abre `Data/Enemies/Waves/ArenaTestWaves.asset` y añade una fila (segundo de inicio, prefab, cantidad, intervalo), o crea otro horario (*Create › Coop2D › Enemies › Wave Schedule*) y asígnalo al `WaveSpawner` de la escena. Pruébalo con F1 › «Siguiente oleada».

### 5.3 Una habilidad nueva con las formas que hay (círculo, línea, cono)

1. *Create › Coop2D › Skills › Skill Definition* en `Data/Skills/`, con el id del catálogo (`skills.json`) y el nombre del archivo en PascalCase del id (`TideSpark` para `tide_spark`).
2. Rellena elemento, si detona, recarga, forma y medidas, daño y empuje, y en *Visual* pon `Prefabs/Skills/SkillShapeVisual.prefab`.
3. Añádela a la lista *Skills* de `Data/Skills/SkillLibrary.asset` (al final). Si es del kit de un personaje, a sus *Own Skills* (o como *Starting Skill*); si es genérica, también a *Generic*.
4. Sus niveles 2 y 3, en *Levels*, con el texto de una línea que sale en la oferta y lo que cambia cada uno.
5. Para probarla sin esperar a que salga, F1 › «Dar habilidad» hasta que te toque.
6. Añade su id a la tabla `ability` de launcher-events.md.

Para un proyectil: *Shape* = Projectile, su velocidad, radio, *Impact Radius* (0 = golpe directo), *Pierce*, *Burst At End*, *Trap Duration* y *Projectile Prefab* = `Prefabs/Skills/SkillProjectile.prefab`.

Si necesita otra cosa (algo que se queda en el suelo, un retraso como Meteorito), es código: un caso nuevo en `SkillShape` y en `SkillArea`, o un sistema aparte para lo que dura (#158).

### 5.4 Cambiar una reacción

Edita su asset de `Data/Reactions/`. Para una pareja nueva, crea otro *Reaction Definition* y añádelo a `DefaultReactions.asset`. Pruébalo en el laboratorio. Un **elemento** nuevo pide: valor al final de `ElementType`, su *Element Definition* en `ElementLibrary`, sus filas de reacción y su id en el launcher.

### 5.5 Una mejora nueva

1. *Create › Coop2D › Progression › Upgrade* en `Data/Progression/Upgrades/`, con los datos del catálogo (`catalog.json` es la referencia). El nombre del archivo, en PascalCase del id (`DefVitality` para `def_vitality`).
2. Añádela al final de la lista de `DefaultUpgradePool.asset` (al final, para no cambiar lo que dan las semillas ya apuntadas).
3. Añade su id a la tabla `upgrade` de launcher-events.md.
4. Su efecto: en *Effects* de la ficha, pulsa **+** y elige el tipo de bloque con su botón (ver 4.21). Si es una estadística, un *Stat Modifier Block* con la estadística, la unidad y el valor del catálogo. Si los bloques que hay no bastan, hace falta un bloque nuevo (receta 5.12).

### 5.6 Un truco nuevo en F1

En `DebugCheats`: un método público que llama a un `[ServerRpc(RequireOwnership = false)]`, que comprueba `IsAllowed(caller)`, hace la cosa en el host y llama a `ReportCheat(caller, "id_nuevo")`. En `DebugCheatsPanel`: un campo `Button` opcional y su `AddListener` en `Awake`. Duplica un botón en `DebugCanvas/DebugCheatsPanel/Window`. Añade el id a la tabla `cheat` del launcher.

### 5.7 Una tecla nueva

Ábrela en el editor de `Settings/Input/InputSystem_Actions.inputactions` (doble clic), añade la acción y su tecla, guarda (Unity regenera `GameInputActions.cs`) y expónla en `InputService`. Léela en `Update` con `WasPressedThisFrame()` solo en el dueño, como `PlayerSkills`.

### 5.8 Un valor de ajuste nuevo

Nunca un número suelto en el código. Añádelo como `[SerializeField, Tooltip("…"), Min(…)] private float _nombre = valor;` en el ScriptableObject de su sistema, con una propiedad de solo lectura, y rellénalo en el asset. Si el sistema no tiene asset, crea uno con `[CreateAssetMenu(menuName = "Coop2D/…")]`.

### 5.9 Algo nuevo que pertenece a la partida

1. Un NetworkBehaviour en el objeto `Match` de la escena.
2. Si otros scripts lo necesitan, un `public static Tu Current` puesto en `OnStartNetwork` y quitado en `OnStopNetwork` (como `RunExperience`).
3. Su estado, en SyncVars si lo deben ver todos.
4. Un `ResetForNewRun()` (solo host) y su llamada en `MatchFlow.RestartMatch`, con un campo opcional en `MatchFlow`.
5. Una prueba en testing.md, incluida «reiniciar».

### 5.10 Un servicio nuevo de toda la sesión

Ver 4.2. Solo si de verdad vive toda la sesión (entre partidas); si es de la partida, 5.9.

### 5.11 Un evento nuevo del launcher

Método en `LauncherEvents` (con `///` que diga los valores), llamada desde donde pasa el hecho (casi siempre en el PC del jugador al que le pasa), fila en launcher-events.md y aviso para copiarlo al launcher.

### 5.12 Un bloque de efecto nuevo

1. Una clase `[Serializable]` en `Code/Progression/Upgrades/Effects/` que hereda de `EffectBlock` (algo que la mejora es mientras la tienes) o de `EffectAction` (algo que pasa una vez, dentro de un *On Event Block*).
2. Sus valores como `[SerializeField]` con *Tooltip*, y su efecto en `Acquire` o en `Run`, usando lo que trae el `EffectContext`. Solo el host lo ejecuta.
3. Si cambia algo que el dueño simula (moverse, disparar), que lo haga a través de una estadística (4.20), no directamente.
   - Si solo debe contar a veces (contra enemigos quemados, con poca vida…), no hace falta un bloque nuevo: añade la condición a `TargetConditions` (siguiente potencia de dos), haz que quien golpea la compruebe antes del golpe y úsala en un `ConditionalBlock`.
4. Pruébalo en la ficha de una mejora: el botón del Inspector ya lo enseña.
5. Si es una acción que el dueño debería notar al instante cuando él mismo provoca el evento, sobrescribe también `Predict` (mira `TimedStatAction`) y asegúrate de que el host la reemplaza al llegar.

### 5.13 Una estadística nueva

1. Un valor nuevo **al final** de `StatId`.
2. El sistema que la usa pide `stats.Get(StatId.Nueva, base)` donde hoy lee su valor base, en el PC que toque (host, dueño o los dos; mira la tabla de 4.20).
3. Si tiene tope en el catálogo, su fila en `DefaultPlayerStats.asset`.
4. Si la tienen que ver los demás jugadores (como la vida máxima), hace falta sincronizarla aparte.

### 5.14 Un evento nuevo para las mejoras

1. Un valor **al final** de `EffectEvent`.
2. Donde pasa (en el host), una llamada a `PlayerUpgradeEvents` del jugador con un `EffectEventInfo` y el tick: copia `RaiseSkillCast` o `OnReactionResolved`.
3. Si necesita filtros propios, añádelos a `OnEventBlock` y a su `Accepts`.
4. Si el dueño lo provoca y quieres que lo note al instante, una versión `Predict…` como `PredictSkillCast`.

### 5.15 Un personaje nuevo

1. *Create › Coop2D › Player › Character Definition* en `Data/Characters/`, con un **id nuevo en snake_case** (el del catálogo: `blade` para Filo), su nombre, su elemento, su habilidad inicial (*Starting Skill*) y el resto de su kit (*Own Skills*). Todas tienen que estar en `SkillLibrary`.
2. Añádelo a la lista *Characters* de `Data/Players/PlayerCharacters.asset`. Para probarlo, ponlo en un hueco de *By Slot*.
3. Sus mejoras propias llevan su id en *Character* y `char:<id>` en *Requires*.
4. Añade su id a la tabla `character` de launcher-events.md.

## 6. Quitar, mover y optimizar sin romper nada

**Cosas que no se pueden cambiar a la ligera** (rompen partidas guardadas, el launcher o las semillas):

| Qué | Por qué |
|---|---|
| Ids de personajes, enemigos, habilidades, reacciones, mejoras, trucos | Los usa el launcher y, en los personajes y las mejoras, viajan por la red y los piden los requisitos |
| Números de los enums que guardan los assets o viajan (`ElementType`, `UpgradeRarity`, `MatchPhase`…) | Añade siempre al final; cambiar el orden cambia lo que significa cada asset |
| Números de `RandomStream` y el orden de la lista del pool de mejoras | Cambian lo que da cada semilla |
| Nombres de campos `[SerializeField]` | Unity pierde el valor guardado. Si renombras, añade `[FormerlySerializedAs("_nombreViejo")]` |
| Eventos y campos del launcher | Es un contrato |

**Dependencias opcionales.** Muchas referencias son opcionales a propósito: `MatchFlow` funciona sin oleadas, sin cargas, sin experiencia o sin mejoras; un jugador sin `PlayerDowned` nunca cae; una escena sin `ReviveCharges` no tiene límite; sin `UpgradeOffers` nadie recibe ofertas. Para **quitar** un sistema de una escena, quita su componente y deja vacío el campo que lo apuntaba. Para **quitarlo del juego**, busca su clase en el proyecto (Ctrl+Shift+F) y sigue cada uso.

**Mover un sistema de sitio.** Los scripts se encuentran por su GUID (el `.meta`), no por su carpeta: mueve siempre **dentro de Unity** (o el `.cs` junto con su `.meta`) y no se rompe nada.

**Optimizar.** Ya se hace así, por si necesitas más:
- **Sondear cada poco en vez de cada fotograma:** derribados 0,1 s, orbes 0,1 s, enemigos buscan objetivo 0,25 s, fin de partida 0,25 s, cargas 0,5 s.
- **Listas reutilizadas** en vez de crear listas nuevas en cada búsqueda (fíjate en los `_candidates`).
- **Pools:** textos flotantes y orbes se reutilizan.
- **Topes:** 40 orbes, 40 enemigos vivos, 12 objetivos por habilidad, 6 nubes.
- **Mandar lo mínimo:** los orbes son una lista y no objetos de red; la reanimación manda una recta y no el progreso; las ofertas mandan ids.

Antes de optimizar, mide con `PerformanceLogger` o el Profiler de Unity: optimiza lo que de verdad cuesta.

## 7. Cómo mantener esta guía

- **Cada issue que añade o cambia un sistema actualiza su apartado** en el mismo commit (o en el siguiente commit de documentación). Si crea un sistema nuevo, se añade un apartado nuevo con el esquema de la sección 1 y su entrada en el contenido.
- Si cambia un valor por defecto que sale aquí, cámbialo también aquí.
- Si aprendemos algo de FishNet por las malas, va a [networking.md](networking.md), en «Errores que ya nos han pasado».
- Si una receta de la sección 5 se queda corta, amplíala la próxima vez que la uses.
