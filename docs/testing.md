# Lista de pruebas

Pruebas que repetimos antes de cada pull request que toque la red, los jugadores o los menús. En la descripción del PR, apunta cuáles has pasado y qué ha fallado.

## Preparación

- **En el editor:** abre **solo** `Bootstrap` y pulsa Play, para arrancar igual que la build. Para varios jugadores, activa los Players 2-4 en *Window > Multiplayer > Multiplayer Play Mode*.
- **Con la build:** abre dos copias. Una hospeda y la otra se une a `127.0.0.1`.
- **Consola:** en el editor, la pestaña *Console*. En la build, la tecla `º` (a la izquierda del 1) abre la consola del juego.
- **Con latencia:** en `Bootstrap`, en el *TransportManager* del NetworkManager, activa *Latency Simulator* con *Latency* 100. Da unos 200 ms de ida y vuelta, que se ven con F3. Desactívalo antes del commit.

Al terminar, la consola debe estar limpia en todas las ventanas: sin errores y sin avisos nuevos.

## 1. Arranque

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| El menú aparece | Abre el juego. | Se ve el menú principal. |
| Volver con Escape | Entra en Multiplayer y pulsa Escape. | Vuelves al menú principal. En el menú principal, Escape no hace nada. |

## 2. Conexión

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Hospedar | Pulsa Host. | Toast "Conectado". Apareces en la Arena. |
| Unirse con el botón | Con un host abierto, escribe `127.0.0.1` y pulsa Join. | Toast "Conectado". Apareces en la Arena. |
| Unirse con Enter | Igual, pero pulsa Enter en el campo de IP. | Igual que con el botón. |
| Enter repetido | Mientras conecta, pulsa Enter varias veces. | Un solo intento: un solo `New State: Connecting` en la consola. |
| IP no válida | Escribe `abc` o `1.2.3` y pulsa Join. | Toast "IP no válida". No intenta conectar. |
| Host inexistente | Únete a `127.0.0.1` sin ningún host abierto. | A los 10 s, un solo toast "No se encontró el host". Los botones se reactivan. |
| Puerto ocupado | Con un host abierto, pulsa Host en otra ventana tres veces. | Tres toasts "No se pudo hospedar: el puerto está ocupado". |

## 3. Salir

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Sale un cliente | Un cliente pulsa el botón de salir. | Él vuelve al menú con "Has salido de la partida". Los demás siguen jugando y su personaje desaparece. |
| Cierra el host | El host pulsa salir. | Cada cliente vuelve al menú con un solo "El host ha cerrado la partida". |
| Entrar y salir | Hospeda y sal tres veces seguidas; luego únete y sal tres veces. | Todo funciona igual la tercera vez que la primera. |

## 4. Jugadores

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Puntos de aparición | Hospeda y une a otros tres. | Cada jugador aparece en un punto distinto. |
| Colores | Mira las cuatro ventanas. | Cada jugador tiene su color bajo los pies, y es el mismo en todas las ventanas. |
| Entrar tarde | Une a dos, juega un rato y une al cuarto. | El cuarto ve a todos con su color correcto. |
| Reutilizar huecos | Sale el jugador 2 y vuelve a entrar. | Recupera el mismo punto y el mismo color. |
| Volver a hospedar | El host sale y vuelve a hospedar. | Vuelve a ser el jugador 1, en el primer punto. |
| Sin empujones | Camina a través de otro jugador. | Os atravesáis. |

## 5. Movimiento y cámara

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Cada uno mueve el suyo | Muévete en cada ventana. | Cada ventana mueve solo a su jugador. |
| La cámara sigue al propio | Muévete en cada ventana. | Cada cámara sigue a su jugador y nunca a otro. Al empezar, la cámara ya está centrada, sin deslizarse. |
| Sin temblor | Muévete en las cuatro direcciones. | El propio jugador se ve estable en todas, también en vertical. |
| Los demás al pararse | Otro jugador anda y se para de golpe. | Se para sin pasarse de largo ni recular. |

## 6. Panel de red

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Solo en partida | Mira el menú y luego entra en una partida. | En el menú no se ve el panel. En la partida sí. |
| Datos | Mira el panel en el host y en un cliente. | Rol y número de jugador correctos, el número de jugadores de la partida y los FPS. |
| Alguien sale | Sale un jugador. | Al resto les baja el número de jugadores en menos de un segundo. |
| F3 | Pulsa F3 en el host y en un cliente. | El panel se oculta y vuelve a aparecer. |

## 7. Combate

Con el host y dos clientes, en local y otra vez con latencia. El muñeco de `Arena_Test` tiene 50 de vida, cada disparo le quita 10 y revive al segundo de morir. Los perseguidores tienen 30 y desaparecen al morir, y llegan en oleadas desde los bordes de la arena. Para las pruebas del muñeco, desmarca antes el componente `WaveSpawner` de `Arena_Test` y guarda; vuelve a marcarlo al terminar.

Algunas pruebas cambian ajustes durante el Play. Los cambios en un ScriptableObject se quedan guardados al parar, así que devuélvelos a su valor al terminar.

En las pruebas de persecución, nadie debe disparar: antes de dar Play, pon a 0 el *Range* de `DefaultAutoAttack` y guarda, para que los Players de MPPM también lo carguen. Devuélvelo a 6 al terminar.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Vida sincronizada | En el host, pulsa F4 y luego F5. | Todas las vidas bajan 10 y vuelven al máximo, igual en todas las ventanas. |
| Disparo al momento | Con latencia, un cliente se acerca al muñeco. | Su proyectil sale en cuanto el muñeco entra en rango, sin esperar al host. Las demás ventanas también lo ven salir de ese jugador. |
| Sin daño doble | Disparad al muñeco desde el host y desde un cliente. | Baja de 10 en 10 en todas las ventanas, cae en 0 y revive al segundo. |
| Cadencia | Con un cliente disparando, sube en el host el *Attack Interval* de `DefaultAutoAttack` a 2. Devuélvelo a 0,6. | El host avisa de disparos rechazados por ir demasiado rápido, y el muñeco solo baja con los que acepta. Son los únicos avisos esperados. |
| Golpe fuera del recorrido | Con el host lejos y un cliente disparando, mueve el muñeco unas casillas en la vista *Scene* del host. | El host avisa de golpes rechazados por estar lejos del recorrido, y la vida no baja. Son los únicos avisos esperados. |
| Eventos de golpe y muerte | En `Bootstrap`, activa el componente `CombatEventLogger` del objeto NetworkManager y guarda la escena. Disparad al muñeco hasta que caiga varias veces y alejaos de él. | En cada ventana sale una línea por golpe y, por cada caída, «Death» justo después de su quinto golpe. Los contadores coinciden en todas las ventanas, y «(mine)» solo aparece en la ventana del jugador que disparó. |
| Eventos con F4 | Con el logger activo, pulsa F4 en el host. | Cada ventana registra un golpe por cada jugador y cada enemigo, de «no one». |
| Eventos al entrar tarde | Con el logger activo, haz caer el muñeco y une después a otro cliente. | El logger del nuevo empieza en 1: no recibe golpes anteriores. Desactiva el componente antes del commit. |
| Persecución | Entrad los tres y separaos por la arena. | Cada perseguidor va hacia el jugador más cercano y cambia de objetivo cuando otro le queda más cerca. Se mueven igual en todas las ventanas; en los clientes, con un poco de retraso. |
| Sin amontonarse | Quedaos quietos y dejad que os alcancen. | Se paran junto a vuestros pies y se reparten a vuestro alrededor, sin juntarse en un punto ni temblar. |
| Sin choques con jugadores | Camina a través de un perseguidor y del muñeco. | Los atraviesas. Los perseguidores sí chocan entre ellos y con las paredes. |
| Muerte de un perseguidor | Disparad a un perseguidor. | Cae al tercer disparo y desaparece en todas las ventanas, sin avisos. Con el logger activo, su «Death» sale en todas. |
| Disparo anticipado | Con *Range* en 6, un jugador se deja perseguir y otro, quieto a un lado, dispara a los perseguidores que pasan. Para comparar, repítelo con *Lead Factor* a 0 en `DefaultAutoAttack` y devuélvelo a 1. | Con 1, sus proyectiles salen por delante del perseguidor y casi todos aciertan, también con latencia. Con 0 fallan casi todos. |
| Sale el perseguido | Con perseguidores detrás de un cliente, ese cliente sale de la partida. | En menos de medio segundo van a por otro jugador, sin errores. |
| Oleadas | Hospeda, une a un cliente y jugad los 3 minutos con *Range* en 6. | Los enemigos salen por los puntos de los bordes, lejos de los jugadores, en tandas que crecen con el tiempo. Pasados los 3 minutos no sale ninguno más. Todo igual en todas las ventanas. |
| Tope de enemigos | Con *Range* a 0, deja correr las oleadas sin matar a nadie. Luego vuelve a ponerlo en 6. | Nunca hay más de 40 enemigos vivos: cuenta los `Chaser(Clone)` de la escena de la arena en la *Hierarchy* del host. Al matar alguno, salen los que esperaban. |
| Oleadas al entrar tarde | Une a un cliente a mitad de partida. | Ve a todos los enemigos vivos en su sitio, y las oleadas siguen igual para todos. |
| Oleadas al volver a hospedar | El host sale y vuelve a hospedar. | Las oleadas empiezan de cero. |

## 8. Elementos y auras

Con el host y dos clientes. Hasta que lleguen los personajes (#162), cada jugador juega el elemento de su hueco: J1 fuego, J2 agua, J3 electricidad y J4 fuego.

Para las pruebas del muñeco, desmarca el componente `WaveSpawner` de `Arena_Test` y sube *Max Health* de `TrainingDummyHealth` a 500, para que no muera a cada momento. Al terminar, vuelve a marcar el spawner y deja la vida en 50.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Elemento por hueco | Hospeda y une a dos clientes. | Cada ventana recibe un solo toast con su elemento: «Tu elemento: Fuego» en el host, «Agua» y «Electricidad» en los clientes. |
| Cambiar de elemento | En un cliente, pulsa F6 tres veces, esperando a cada toast. | Su elemento pasa a Electricidad, Fuego y Agua, con un toast por pulsación. Las demás ventanas no reciben ninguno. |
| Volver a entrar | Ese cliente sale y vuelve a entrar. | Recibe otra vez el elemento de su hueco, no el que tenía. |
| Aura del básico | J1 dispara al muñeco y luego se aleja. | Sobre el muñeco sale «Fuego (débil) · J1», igual en todas las ventanas y con J1 en su color. Unos 2 s después de su último disparo, desaparece. |
| El básico no tapa auras | J1 dispara al muñeco y se aleja. Justo después, J2 se acerca y dispara. | El aura sigue siendo «Fuego · J1» aunque J2 le dé. Cuando caduca, la siguiente bala de J2 la cambia a «Agua (débil) · J2». |
| Refresco y autoría | Con F6, pon a J2 en fuego. J1 y J2 disparan a la vez al muñeco. | El aura sigue siendo de fuego y no caduca. El «J» va cambiando al último que la refresca. |
| Aura al morir | Devuelve la vida del muñeco a 50 y mátalo con un aura puesta. | Al revivir, no tiene aura. |
| Auras en perseguidores | Con el spawner marcado, disparad a los perseguidores. | Cada uno muestra el aura de quien le dio primero y desaparece al morir, sin avisos. |
| Aura al entrar tarde | Con el muñeco marcado, une a otro cliente. | El nuevo ve el aura y su dueño desde el principio. |

## 9. Habilidades

Con el host y dos clientes. Hasta que lleguen los personajes (#162), todos tienen las mismas dos habilidades de prueba, del color de su elemento: **Estallido** con el clic izquierdo (círculo de 1,5) y **Marca** con el derecho (círculo de 2). Las dos tienen 7 de alcance y 7 s de recarga. La Q queda reservada para el tercer hueco. Quien lanza ve su círculo al instante; el host comprueba el lanzamiento y aplica el daño y el aura fuerte.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Lanzar | Apunta a la arena y haz clic izquierdo. | Sale un círculo del color de tu elemento donde apuntas, y se desvanece enseguida. |
| La segunda habilidad | Haz clic derecho. | Sale un círculo más grande. |
| Alcance | Apunta mucho más lejos de 7 y lanza. | El círculo cae a 7 de ti, en la dirección del cursor. |
| Recarga | Haz clic izquierdo varias veces seguidas. | Solo sale un círculo cada 7 s. El clic derecho tiene su propia recarga. |
| Sobre la interfaz | Pon el ratón sobre el botón de salir y haz clic derecho. | No sale ningún círculo. |
| Sigue a tu elemento | Pulsa F6 y lanza otra vez. | El círculo sale del color del elemento nuevo. |
| Hueco vacío | Pulsa Q. | No pasa nada, y la consola sigue limpia. |
| Lo ven todos | Lanza Estallido desde un cliente. | El círculo sale en todas las ventanas, del color de quien lanza. En las demás, un poco más tarde. |
| Daño | Lanza Estallido sobre el muñeco. | Baja 16 en todas las ventanas. Con el logger activo, el golpe sale «(mine)» solo en la ventana de quien lanza. |
| Aura fuerte | Lanza Marca sobre el muñeco sin aura. | Sale «… (fuerte) · Jn» en todas las ventanas y dura unos 4 s. |
| Fuerte sobre débil | Deja que tu básico marque el muñeco y lanza Estallido sobre él. | El aura pasa de débil a fuerte, con tu elemento. |
| Otro elemento | Con el muñeco marcado por J1 (fuego), J2 (agua) lanza Estallido sobre él. | El aura sigue siendo «Fuego · J1» y el muñeco recibe el daño. La reacción llega con #64. |
| Varios objetivos | Con las oleadas en marcha, lanza Marca sobre un grupo de perseguidores. | Todos los que toca reciben daño y aura. |
| Sin fuego amigo | Lanza Estallido sobre otro jugador. | No recibe daño ni aura. |
| Recarga en el host | En el host, sube la *Cooldown* de `TestBurst` a 20. Un cliente lanza Estallido cada 7 s. Devuélvela a 7. | El host avisa de lanzamientos rechazados antes de su recarga, y esos no hacen daño. Son los únicos avisos esperados. |
| Con latencia | Repite «Daño» y «Varios objetivos» con latencia, contra perseguidores en movimiento. | Los golpes cuentan y la consola del host sigue limpia. |

## 10. Reacciones

Con el host y dos clientes, el `WaveSpawner` desmarcado y la vida del muñeco a 500, como en la sección 8. J1 es fuego, J2 agua y J3 electricidad. Solo Estallido detona; Marca y el básico solo dejan aura. Lanzad las habilidades desde unos 7 de distancia, fuera del alcance del básico (6), para que vuestro básico no deje su aura justo después.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Reacción | J1 marca el muñeco con el básico y se aleja. Antes de 2 s, J2 lanza Estallido sobre él. | El aura de fuego desaparece en todas las ventanas y no queda aura de agua. |
| Sin detonar | Igual, pero J2 lanza Marca. | El aura sigue siendo «Fuego · J1». |
| El básico no detona | J1 marca el muñeco y J2 le dispara con el básico. | El aura sigue siendo «Fuego · J1». |
| Débil o fuerte | J1 marca el muñeco con Marca (aura fuerte) y J2 lanza Estallido. | El aura también desaparece: la reacción consume las dos. |
| Mismo elemento | Con F6, pon a J2 en fuego. J1 marca y J2 lanza Estallido. | No hay reacción: el aura pasa a «Fuego (fuerte) · J2». |
| Bloqueo | En el host, sube *Lockout Duration* de `DefaultReactions` a 5. Provoca una reacción, J1 vuelve a marcar enseguida y, antes de 5 s, J3 lanza Estallido. Devuelve el valor a 0,5. | La segunda vez no hay reacción: el fuego de J1 se queda. |
| En solitario | J1 lanza Marca sobre el muñeco, pulsa F6 para pasar a agua y lanza Estallido. | El aura de fuego desaparece: también reaccionas con tu propia aura. |
| Evento de reacción | Activa el `CombatEventLogger` y repite «Reacción». | Cada ventana registra una sola «Reaction», con `vapor_fire_water`, el aura de J1, la detonación de J2 y «Duo». Solo las ventanas de J1 y J2 añaden «(mine)». La «Reaction» sale antes del «Hit» del Estallido. |
| Las seis variantes | Con F6, provoca cada pareja en los dos órdenes: fuego→agua, agua→fuego, agua→electricidad, electricidad→agua, electricidad→fuego y fuego→electricidad. | Cada una registra su variante: `vapor_fire_water`, `vapor_water_fire`, `electrocution_water_electric`, `electrocution_electric_water`, `overload_electric_fire` y `overload_fire_electric`. |
| Reacción sin dúo | Con el logger, repite «En solitario». | La «Reaction» sale sin «Duo». |
| Varios enemigos | Con el spawner marcado, J1 marca un grupo de perseguidores con Marca y J2 lanza Estallido sobre ellos. | Una «Reaction» por cada perseguidor marcado que toca el Estallido, igual en todas las ventanas. |

Los efectos de cada variante, con el spawner marcado y contra perseguidores, porque el muñeco no se mueve y no puede ir más lento, quedarse quieto ni ser empujado. Con el logger activo, el daño de la reacción sale como golpes de quien detona.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Vapor (fuego → agua) | J1 marca con Marca un grupo de perseguidores y J2 lanza Estallido sobre ellos. | Cada uno, y los que estén a menos de 2, reciben 15. Durante 3 s, los que siguen en esa zona van más lentos, y al salir recuperan la velocidad. |
| Vapor (agua → fuego) | Al revés: J2 marca y J1 detona. | 30 al aparecer y una ralentización más suave, de 1,5 s. |
| Electrocución (agua → electricidad) | J2 marca con Marca un grupo. J3 lanza Estallido sobre el borde del grupo, de forma que toque solo a uno. | Ese recibe 30 y hasta 3 perseguidores mojados cercanos reciben 20 cada uno, saltando de uno a otro. Esos siguen con su aura de agua. |
| Electrocución (electricidad → agua) | J3 marca y J2 detona. | 25 a todos los enemigos a menos de 2 del objetivo, sin cadena. |
| Sobrecarga (electricidad → fuego) | J3 marca y J1 detona. | 35 a menos de 1,5, y todos salen empujados una casilla hacia fuera. El del centro se aleja de J1. |
| Sobrecarga (fuego → electricidad) | J1 marca y J3 detona. | 35 a menos de 1,5, y se quedan quietos medio segundo, sin empuje. |
| Sin fuego amigo | Provoca una Sobrecarga junto a otro jugador. | Al jugador no le pasa nada. |
| Empuje corto | Dentro de una nube de Vapor, provoca una Sobrecarga (electricidad → fuego). | Los empujados siguen dentro de la nube: el empuje (1) es menor que su radio (2). |
| Se ve la explosión | Provoca cada reacción. | Sale un círculo del color de la reacción, igual en todas las ventanas: cian el Vapor, amarillo la Electrocución y naranja la Sobrecarga. Su tamaño es el de su daño en área. |
| Se ve la nube | Provoca los dos Vapor. | Queda una nube cian translúcida del tamaño de la zona lenta, que dura lo mismo que ella: 3 s o 1,5 s. |
| Se ve la cadena | Repite «Electrocución (agua → electricidad)». | Una línea amarilla en zigzag salta de enemigo en enemigo, uno por fotograma, en todas las ventanas. |

## 11. Textos flotantes

Con el host y dos clientes. Todos los valores salen de los estilos de `Data/UI/FloatingText` y de cada reacción.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Números del básico | Dispara al muñeco. | Números blancos que aparecen con un pequeño pop, suben y se desvanecen en algo más de medio segundo. Los disparos seguidos al mismo enemigo se suman en un solo número. |
| Números de habilidad | Lanza Estallido sobre el muñeco. | Un número más grande, del color de tu elemento. |
| Daño de la reacción | Provoca una Sobrecarga sobre un grupo. | Un número claro por cada enemigo alcanzado, aparte del de la habilidad. |
| Titular de dúo | J1 marca y J2 detona. | Sobre el enemigo sale el nombre en grande, por ejemplo «VAPOR!», con un pop fuerte y un degradado del color de la reacción. Sube y se desvanece en algo más de 1 s. Debajo, «J1 + J2  ¡DÚO!», con cada jugador en su color. Igual en todas las ventanas. |
| Titular en solitario | Repite «En solitario» de la sección 10. | Debajo sale «J1  EN SOLITARIO», en gris. |
| Varias a la vez | J1 marca un grupo con Marca y J2 detona varios de una vez. | Las reacciones iguales y cercanas se juntan en un titular que cuenta, por ejemplo «VAPOR! ×3». Si dos titulares se pisarían, el segundo sube un poco. |
| Sin amontonarse | Con las oleadas, jugad un minuto entre todos. | Nunca hay más de 24 textos a la vez. Cuando faltan huecos, desaparecen antes los números del básico de los demás que los tuyos y que los titulares. |
| Todo en datos | Durante el Play, cambia en `ReactionHeadline` el tamaño o la duración. Devuélvelo después. | Los titulares nuevos cambian sin tocar código. |

## 12. Prueba entre varios PCs

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Firewall | Hospeda por primera vez con la build. | Windows pide permiso de red. Acéptalo en redes privadas. |
| Red local | Únete desde otro PC de casa con la IP local del host (`ipconfig`, línea *Dirección IPv4*). | Funciona igual que en un solo PC. |
| Internet | Únete desde fuera con la IP pública del host (requiere el port-forward). | Funciona. Apunta en el issue el ping de cada cliente y cualquier tirón. |

El ping del panel se mide en ticks de 33 ms y se actualiza una vez por segundo. En local saldrá entre 0 y 33 ms, y en el host siempre unos 33. Compáralo entre vuestras pruebas, no con el ping de otros juegos.

Si la build se cierra de golpe, pide a quien la jugaba el archivo:

```
%USERPROFILE%\AppData\LocalLow\PalitoGames\Coop2D\Player.log
```
