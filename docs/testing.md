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
| Versión en el menú | Entra en Multiplayer. | Se ve la versión del juego (`v0.0.4`, con ` dev` en las builds de desarrollo). |
| Recordar la IP | Únete a `127.0.0.1`, cierra el juego y vuelve a abrirlo. | El campo de IP ya tiene `127.0.0.1`. |
| Versión distinta | En la ventana del host, cambia *Version* en *Project Settings > Player* (por ejemplo, `0.0.4-test`) y hospeda; un Player de MPPM se une. Devuélvela al terminar. | El cliente no entra: toast «Versión distinta: el host tiene la 0.0.4-test y tú la 0.0.4», y los botones se reactivan. El host avisa una vez de que lo rechazó. No sale «Conectado» ni «El host ha cerrado la partida». |

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

Con el host y dos clientes, en local y otra vez con latencia. El muñeco de `Arena_Test` tiene 50 de vida, cada disparo le quita 10 y revive al segundo de morir. Los perseguidores tienen 60 y desaparecen al morir, y llegan en oleadas desde los bordes de la arena. Para las pruebas del muñeco, desmarca antes el componente `WaveSpawner` de `Arena_Test` y guarda; vuelve a marcarlo al terminar.

Algunas pruebas cambian ajustes durante el Play. Los cambios en un ScriptableObject se quedan guardados al parar, así que devuélvelos a su valor al terminar.

En las pruebas de persecución, nadie debe disparar: antes de dar Play, pon a 0 el *Range* de `DefaultAutoAttack` y guarda, para que los Players de MPPM también lo carguen. Devuélvelo a 6 al terminar.

Desde el #51 los perseguidores hacen daño al tocar. En las pruebas de persecución y de oleadas que no van de recibir daño, pulsa antes «Invulnerable» en el panel de trucos de cada ventana.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Vida sincronizada | En el panel de trucos (F1), pulsa «Dañar a todos» y luego «Curar a todos». | Todas las vidas bajan 10 y vuelven al máximo, igual en todas las ventanas. |
| Disparo al momento | Con latencia, un cliente se acerca al muñeco. | Su proyectil sale en cuanto el muñeco entra en rango, sin esperar al host. Las demás ventanas también lo ven salir de ese jugador. |
| Sin daño doble | Disparad al muñeco desde el host y desde un cliente. | Baja de 10 en 10 en todas las ventanas, cae en 0 y revive al segundo. |
| Cadencia | Con un cliente disparando, sube en el host el *Attack Interval* de `DefaultAutoAttack` a 2. Devuélvelo a 0,6. | El host avisa de disparos rechazados por ir demasiado rápido, y el muñeco solo baja con los que acepta. Son los únicos avisos esperados. |
| Golpe fuera del recorrido | Con el host lejos y un cliente disparando, mueve el muñeco unas casillas en la vista *Scene* del host. | El host avisa de golpes rechazados por estar lejos del recorrido, y la vida no baja. Son los únicos avisos esperados. |
| Eventos de golpe y muerte | En `Bootstrap`, activa el componente `CombatEventLogger` del objeto NetworkManager y guarda la escena. Disparad al muñeco hasta que caiga varias veces y alejaos de él. | En cada ventana sale una línea por golpe y, por cada caída, «Death» justo después de su quinto golpe. Los contadores coinciden en todas las ventanas, y «(mine)» solo aparece en la ventana del jugador que disparó. |
| Eventos de «Dañar a todos» | Con el logger activo, pulsa «Dañar a todos» en el panel de trucos. | Cada ventana registra un golpe por cada jugador y cada enemigo, de «no one». |
| Eventos al entrar tarde | Con el logger activo, haz caer el muñeco y une después a otro cliente. | El logger del nuevo empieza en 1: no recibe golpes anteriores. Desactiva el componente antes del commit. |
| Persecución | Entrad los tres y separaos por la arena. | Cada perseguidor va hacia el jugador más cercano y cambia de objetivo cuando otro le queda más cerca. Se mueven igual en todas las ventanas; en los clientes, con un poco de retraso. |
| Sin amontonarse | Quedaos quietos y dejad que os alcancen. | Se paran junto a vuestros pies y se reparten a vuestro alrededor, sin juntarse en un punto ni temblar. |
| Sin choques con jugadores | Camina a través de un perseguidor y del muñeco. | Los atraviesas. Los perseguidores sí chocan entre ellos y con las paredes. |
| Muerte de un perseguidor | Disparad a un perseguidor. | Cae al tercer disparo y desaparece en todas las ventanas, sin avisos. Con el logger activo, su «Death» sale en todas. |
| Daño por contacto | Con *Range* a 0, deja que un perseguidor te alcance y quédate quieto. | Tu vida baja 10 cada segundo, igual en todas las ventanas. Con varios encima, como mucho 10 cada medio segundo. |
| Contacto con latencia | Repite la prueba anterior con un cliente y 100 ms de latencia. | Igual que sin latencia, sin avisos. Si se aleja corriendo, deja de recibir daño en cuanto el host lo ve fuera de alcance. |
| Contacto e invulnerable | Pulsa «Invulnerable» y deja que te alcancen. | La vida no baja. |
| Disparo anticipado | Con *Range* en 6, un jugador se deja perseguir y otro, quieto a un lado, dispara a los perseguidores que pasan. Para comparar, repítelo con *Lead Factor* a 0 en `DefaultAutoAttack` y devuélvelo a 1. | Con 1, sus proyectiles salen por delante del perseguidor y casi todos aciertan, también con latencia. Con 0 fallan casi todos. |
| Sale el perseguido | Con perseguidores detrás de un cliente, ese cliente sale de la partida. | En menos de medio segundo van a por otro jugador, sin errores. |
| Oleadas | Hospeda, une a un cliente y jugad los 3 minutos con *Range* en 6. | Los enemigos salen por los puntos de los bordes, lejos de los jugadores, en tandas que crecen con el tiempo. Pasados los 3 minutos no sale ninguno más. Todo igual en todas las ventanas. |
| Tope de enemigos | Con *Range* a 0, deja correr las oleadas sin matar a nadie. Luego vuelve a ponerlo en 6. | Nunca hay más de 40 enemigos vivos: cuenta los `Chaser(Clone)` de la escena de la arena en la *Hierarchy* del host. Al matar alguno, salen los que esperaban. |
| Oleadas al entrar tarde | Une a un cliente a mitad de partida. | Ve a todos los enemigos vivos en su sitio, y las oleadas siguen igual para todos. |
| Oleadas al volver a hospedar | El host sale y vuelve a hospedar. | Las oleadas empiezan de cero. |

## 8. Elementos y auras

Con el host y dos clientes. Hasta la sala de espera (#173), el personaje sale del hueco y con él el elemento: J1 Brasa (fuego), J2 Marea (agua), J3 Voltio (electricidad) y J4 Brasa (fuego).

Para las pruebas del muñeco, desmarca el componente `WaveSpawner` de `Arena_Test` y sube *Max Health* de `TrainingDummyHealth` a 500, para que no muera a cada momento. Al terminar, vuelve a marcar el spawner y deja la vida en 50.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Elemento por personaje | Hospeda y une a dos clientes. | Cada ventana recibe un solo toast con su personaje y su elemento: «Juegas con Brasa · Fuego» en el host, «Juegas con Marea · Agua» y «Juegas con Voltio · Electricidad» en los clientes. |
| Cambiar de elemento | En un cliente, pulsa F6 tres veces, esperando a cada toast. | Su elemento pasa a Electricidad, Fuego y Agua, con un toast «Tu elemento: …» por pulsación. Las demás ventanas no reciben ninguno. |
| Volver a entrar | Ese cliente sale y vuelve a entrar. | Recibe otra vez el elemento de su personaje, no el que tenía. |
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
| Aura fuerte | Lanza Marca sobre el muñeco sin aura. | Sale «… (fuerte) · Jn» en todas las ventanas y dura unos 6 s. |
| Fuerte sobre débil | Deja que tu básico marque el muñeco y lanza Estallido sobre él. | El aura pasa de débil a fuerte, con tu elemento. |
| Otro elemento | Con el muñeco marcado por J1 (fuego), J2 (agua) lanza Estallido sobre él. | El aura sigue siendo «Fuego · J1» y el muñeco recibe el daño. La reacción llega con #64. |
| Varios objetivos | Con las oleadas en marcha, lanza Marca sobre un grupo de perseguidores. | Todos los que toca reciben daño y aura. |
| Sin fuego amigo | Lanza Estallido sobre otro jugador. | No recibe daño ni aura. |
| Recarga en el host | En el host, sube la *Cooldown* de `TestBurst` a 20. Un cliente lanza Estallido cada 7 s. Devuélvela a 7. | El host avisa de lanzamientos rechazados antes de su recarga, y esos no hacen daño. Son los únicos avisos esperados. |
| Con latencia | Repite «Daño» y «Varios objetivos» con latencia, contra perseguidores en movimiento. | Los golpes cuentan y la consola del host sigue limpia. |
| Recarga en el HUD | Lanza Estallido y mira su hueco en la barra de habilidades. | El relleno se vacía en 7 s y el número baja de 7 a 1. Al acabar puedes volver a lanzarla, ni antes ni después. Igual en el host y en un cliente con latencia. |
| Hueco vacío en el HUD | Mira el tercer hueco (Q). | Se ve atenuado, sin número. |
| Destello al golpear | Dispara al muñeco y a un perseguidor, y pulsa «Dañar a todos». | Cada golpe pone blanco el sprite un instante, en todas las ventanas, también a los jugadores. Cambia *Hit Flash Duration* de `DefaultFeedback` a 0,5 para verlo bien y devuélvelo a 0,05. |

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
| Temblor de cámara | Cerca de tu jugador, provoca una reacción en solitario y luego un dúo. Después, varias a la vez sobre un grupo. | La pantalla tiembla un instante, algo más con el dúo, en las ventanas que tienen la reacción a la vista. Con varias a la vez tiembla una sola vez. Los valores salen de `Data/Camera/ReactionShake`. |

## 11. Textos flotantes

Con el host y dos clientes. Todos los valores salen de los estilos de `Data/UI/FloatingText` y de cada reacción.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Números del básico | Dispara al muñeco. | Números blancos que aparecen con un pequeño pop, suben y se desvanecen en algo más de medio segundo. Los disparos seguidos al mismo enemigo se suman en un solo número. |
| Números de habilidad | Lanza Estallido sobre el muñeco. | Un número más grande, del color de tu elemento. |
| Daño de la reacción | Provoca una Sobrecarga sobre un grupo. | Un número claro por cada enemigo alcanzado, a la derecha y algo más abajo que el de la habilidad, sin pisarse con él. |
| Titular de dúo | J1 marca y J2 detona. | Sobre el enemigo sale el nombre en grande, por ejemplo «VAPOR!», con un pop fuerte, un degradado del color de la reacción, un contorno negro grueso y una sombra negra dura hacia abajo a la derecha. Sube y se desvanece en algo más de 1 s. Debajo, «J1 + J2  ¡DÚO!», con cada jugador en su color. Igual en todas las ventanas. |
| Titular en solitario | Repite «En solitario» de la sección 10. | Debajo sale «J1  EN SOLITARIO», en gris. |
| Varias a la vez | J1 marca un grupo con Marca y J2 detona varios de una vez. | Las reacciones iguales y cercanas se juntan en un titular que cuenta, por ejemplo «VAPOR! ×3». Si dos titulares se pisarían, el segundo sube un poco. |
| Sin amontonarse | Con las oleadas, jugad un minuto entre todos. | Nunca hay más de 24 textos a la vez. Cuando faltan huecos, desaparecen antes los números del básico de los demás que los tuyos y que los titulares. |
| Todo en datos | Durante el Play, cambia en `ReactionHeadline` el tamaño o la duración. Devuélvelo después. | Los titulares nuevos cambian sin tocar código. |
| Contorno y sombra | Durante el Play, cambia el *Thickness* del contorno en el material `PixelifySans-Bold SDF - Headline`. | Cambian al momento los titulares que hay en pantalla. Al salir del Play el cambio se queda, porque es un asset. |
| En la build | Provoca una reacción en una build. | El titular tiene la misma sombra que en el editor. |

## 12. Panel de trucos

Con el host y un cliente. Funciona en el editor y en las builds con *Development Build*. Los valores salen de `Data/Cheats/DebugCheats`.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Abrir y cerrar | Pulsa F1 en cualquier ventana, y otra vez. | El panel se abre solo en esa ventana y se cierra con el segundo F1. Hacer clic en sus botones no lanza habilidades. |
| Generar un enemigo | Desde el cliente, pulsa «Generar Chaser» varias veces. | Cada vez sale un Chaser a 4 unidades del jugador que lo pidió, en una dirección al azar. Sale en todas las ventanas y persigue al jugador más cercano. |
| Generar en tandas | Pulsa «Generar 10 Chaser». | Salen 10 a la vez alrededor de quien lo pidió. |
| Matar a todos | Con varios enemigos vivos, pulsa «Matar a todos». | Mueren todos a la vez, con su número, en todas las ventanas. Los muñecos y los jugadores no cambian. |
| El tope sigue bien | Llena el tope de enemigos de las oleadas y pulsa «Matar a todos». | Siguen saliendo los que esperaban sitio. |
| Invulnerable | En el cliente, pulsa «Invulnerable: no» y luego «Dañar a todos». | El botón pasa a «Invulnerable: sí» y la vida de ese jugador dice «invulnerable» en todas las ventanas. Su vida no baja y la de los demás sí. Al pulsarlo otra vez vuelve a «no». |
| Dañar y curar a todos | Pulsa «Dañar a todos» y luego «Curar a todos». | Bajan 10 las vidas de jugadores, enemigos y muñecos, y vuelven al máximo, en todas las ventanas. |
| Siguiente oleada | Con las oleadas activas, pulsa «Siguiente oleada» varias veces. | Quien lo pulsa ve «Saltas a la oleada de M:SS» y empiezan a salir esos enemigos. Lo que quedaba de las oleadas anteriores ya no sale. Tras la última, el aviso dice «No quedan más oleadas». |
| Reiniciar oleadas | Con las oleadas a medias, o ya terminadas, pulsa «Reiniciar oleadas». | Mueren todos los enemigos, quien lo pulsa ve «Oleadas reiniciadas» y vuelve a salir la primera oleada, como al empezar la partida. |
| Build de lanzamiento | Haz una build sin *Development Build* y pulsa F1. | No pasa nada: el panel no existe. |

## 13. Prueba entre varios PCs

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Firewall | Hospeda por primera vez con la build. | Windows pide permiso de red. Acéptalo en redes privadas. |
| Red local | Únete desde otro PC de casa con la IP local del host (`ipconfig`, línea *Dirección IPv4*). | Funciona igual que en un solo PC. |
| Internet | Únete desde fuera con la IP pública del host (requiere el port-forward). | Funciona. Apunta en el issue el ping de cada cliente y cualquier tirón. |

El ping del panel se mide en ticks de 33 ms y se actualiza una vez por segundo. En local saldrá entre 0 y 33 ms, y en el host siempre unos 33. Compáralo entre vuestras pruebas, no con el ping de otros juegos.

En las builds de desarrollo, `PerformanceLogger` (en el objeto NetworkManager de `Bootstrap`) escribe en el log de cada jugador:

- `[Perf] Hitch`: cada fotograma de más de 100 ms. Lleva los enemigos que había, los pasos de física y los ticks de red de ese fotograma, si hubo recolección de basura, los golpes, muertes y reacciones que llegaron, y los bytes recibidos.
- `[Perf] Summary`: cada 10 s. Lleva los FPS, el peor fotograma, los enemigos, proyectiles y objetos que hay, los eventos y el tráfico por segundo, y la memoria.
- `[DebugCheats]`: cada truco del panel y quién lo usó (solo en el log del host).

Si alguien nota tirones o congelaciones, apuntad la hora y pedid el log de todos: cada línea lleva la hora del PC, así que se pueden cruzar.

Si la build se cierra de golpe, pide a quien la jugaba el archivo:

```
%USERPROFILE%\AppData\LocalLow\PalitoGames\Coop2D\Player.log
```

## 14. Laboratorio de reacciones

Solo en el editor y en builds de desarrollo. Desde Multiplayer, pulsa «Laboratorio» en vez de Host; los demás se unen como siempre. Hay tres muñecos que no se mueven, y el panel aplica auras y detonaciones como si fueran del jugador que pulsa. El ataque básico de todos empieza apagado, para que no marque los muñecos; el botón «Ataque básico» lo enciende y apaga para todos.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Las 9 combinaciones | Para cada elemento de aura (fuego, agua, electricidad), pon un aura fuerte en un muñeco y detónala con cada uno de los tres elementos. Pulsa «Reiniciar» entre una y otra. | Con el mismo elemento: «el aura se refresca y no hay reacción». Con los otros dos, sale la variante de la tabla: `vapor_fire_water` con aura de fuego y agua, `vapor_water_fire` al revés, y así con Electrocución y Sobrecarga. |
| El básico no detona | Pon un aura débil de fuego y luego otra débil de agua. | «Ignorado»: el aura débil no detona ni sustituye a otra. |
| Bloqueo tras reaccionar | Provoca una reacción y, enseguida, pon otra aura y detónala. | La segunda detonación sale «Ignorado» si llega dentro de 0,5 s. |
| Dúo | J1 pone el aura y J2 la detona en el mismo muñeco. | La línea de la reacción dice «aura de J1, detona J2 · ¡DÚO!» en las dos ventanas. |
| Cambiar un valor | Sube el *Damage* de `VaporFireWater` y repite su combinación. Devuélvelo después. | El muñeco pierde la vida nueva. |
| Reiniciar | Pulsa «Reiniciar» con muñecos dañados y con aura. | Vida llena y sin aura, en todas las ventanas. |
| Ataque básico | Acércate a un muñeco con el básico apagado; luego pulsa «Ataque básico» en la otra ventana. | Apagado, nadie dispara y el muñeco no recibe aura. Al encenderlo, los dos jugadores disparan y la etiqueta cambia en las dos ventanas. Al volver a `Arena_Test`, el básico funciona como siempre. |

## 15. Eventos del launcher

El juego escribe en `Player.log` una línea `[PalitoEvent]` por cada hecho que lee Palito Launcher. El contrato está en [launcher-events.md](launcher-events.md). Se escriben en todas las builds, también en las de lanzamiento.

Con la build, juega una partida corta con un host y un cliente y busca las líneas en el log de cada uno:

```powershell
Select-String -Path "$env:USERPROFILE\AppData\LocalLow\PalitoGames\Coop2D\Player.log" -Pattern '^\[PalitoEvent\] '
```

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Arranque y cierre | Abre la build y ciérrala con normalidad. | La primera línea es `session_start` con `seq=1` y `launcher_session=none`; la última, `session_end`. Matando el proceso desde el Administrador de tareas no hay `session_end`. |
| Números | Mira varias líneas. | `seq` sube de uno en uno sin huecos, y `t` usa punto decimal también en Windows en español. |
| Conexión | Pulsa Host, y en el cliente únete con Enter a `127.0.0.1`. Prueba también `abc`. | En el host, `host_started result=ok`. En el cliente, `join_attempt target=loopback via=enter` y `join_result result=ok target=loopback`; con `abc`, `target=invalid` y `result=invalid_ip`. |
| Entrar en la partida | Une al cliente con la partida empezada hace más de 10 s. | `match_joined role=client … late=true seed=… character=tide` en el cliente, con la misma semilla que el `match_joined` del host (que dice `character=ember`), y justo después `element_set element=water source=character`; en el host, `player_joined slot=2 players=2`. |
| Elemento tras F6 y reinicio | Pulsa F6, pierde y reinicia. | Tras el `match_joined` nuevo, `element_set … source=debug_key`: el elemento sigue siendo el de F6. |
| Reacción agrupada | Marca tres perseguidores juntos con fuego y detónalos con un Estallido de agua. | Una sola línea `reaction variant=vapor_fire_water … targets=3`, con `me=detonator` en una ventana y `me=aura` en la otra si el aura era del otro jugador. |
| Muertes | Mata un perseguidor con el básico y otro con «Matar a todos». | `enemy_killed enemy=chaser by=me source=basic` y `enemy_killed enemy=chaser by=none source=cheat`. |
| Trucos | Pulsa un truco del panel en el cliente. | `cheat_used … by=me` en el cliente y `by=ally` en el host. |
| Oleadas | Usa «Siguiente oleada». | `wave_started index=…` en las dos ventanas. |
| Salir | El cliente pulsa salir. | En el cliente, `match_left reason=quit`; en el host, `player_left slot=2 players=1`. |
| El host cierra | El host pulsa salir; repítelo cerrando la ventana del host. | En el cliente, toast «El host ha cerrado la partida» y `match_left reason=host_closed` las dos veces. |
| Se corta la red | Con un cliente en la partida, mata el proceso del host desde el Administrador de tareas. | Al rato, en el cliente, toast «Se ha perdido la conexión con el host» y `match_left reason=disconnected`. |
| Derribado y reanimado | En el host, deja que derriben al cliente y reanímalo con el host. Después, derriba al host y levántalo con «Curar a todos». | En el cliente: `player_downed slot=2 me=true` y `player_revived slot=2 me=true by=ally left=1`; en el host, `me=false`, `by=me` y el mismo `left`. Con el truco, `player_revived slot=1 … by=none left=1` en las dos ventanas: no gasta. |
| Fin de oleadas derribado | Quédate derribado cuando acaben los 3 minutos de oleadas. | `waves_finished … alive=false` en tu ventana y `alive=true` en la de quien sigue en pie. |
| Consola limpia | Repasa la consola del editor tras una partida. | Ningún aviso de `LauncherEventWriter`: si sale, esa línea no cumple el contrato. |

## 16. Semilla

Todo lo aleatorio de la partida sale de la semilla del host (#75). Se ajusta en `Assets/_Project/Data/Core/RunRandom.asset`: deja *Fixed Seed* vacío y *Log Rolls* desmarcado antes del commit.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Semilla nueva | Con *Fixed Seed* vacío, hospeda, sal y vuelve a hospedar. | Cada vez sale `[RunRandomService] Run seed` con 8 caracteres distintos. |
| Misma semilla, mismos números | Escribe `3A9F01C2` en *Fixed Seed*, marca *Log Rolls* y hospeda dos veces. | Las dos veces, `[RunRandomService] Seed 3A9F01C2 preview · waves 47 78 4 · rooms 22 5 85 · offers J1 34 46 24`. |
| Semilla mal escrita | Escribe `hola` en *Fixed Seed* y hospeda. | Un aviso de que no es una semilla válida y una semilla nueva. Ningún error. |
| Solo en desarrollo | Con *Fixed Seed* escrito, haz una build sin *Development Build* y hospeda dos veces. | En el `Player.log`, cada partida tiene una semilla distinta. |
| Oleadas con la misma semilla | Con `3A9F01C2` y *Log Rolls*, hospeda solo, quédate quieto en tu punto de aparición y espera a que salgan 5 enemigos. Sal, vuelve a hospedar y repite. | Las dos veces, las líneas `[WaveSpawner] Spawn roll` empiezan por `0.561`, `0.427`, `0.560`, `0.618` y `0.785`, y van a los mismos puntos en el mismo orden. |
| Oleadas con jugadores que se mueven | Repite la prueba anterior con un cliente que se mueve por la Arena. | Las tiradas son las mismas en el mismo orden. Algún punto puede cambiar, porque depende de dónde estén los jugadores. |
| Todos ven la misma semilla | Hospeda, une a dos clientes y abre el panel de trucos (F1) en las tres ventanas. | Las tres dicen «Semilla: …» con los mismos 8 caracteres que `[RunRandomService] Run seed` en el host, y cada ventana escribe `[MatchSeed] Match seed` con ese valor. |
| Semilla al entrar tarde | Une a un cliente con la partida empezada hace un rato, con 100 ms de latencia. | Ve la misma semilla que los demás nada más entrar. |
| Semilla nueva al volver a hospedar | El host sale y vuelve a hospedar, con *Fixed Seed* vacío. | El panel muestra la semilla nueva en todas las ventanas. |

## 17. Derribado y reanimación

Con el host y dos clientes, en local y otra vez con 100 ms de latencia. Con *Range* a 0 en `DefaultAutoAttack` los perseguidores llegan antes; también puedes bajar la vida con «Dañar a todos» (10 cada vez). Si caen todos, la partida se queda parada hasta el #52: sal con «Curar a todos».

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Caer | Deja que los perseguidores te bajen a 0. | En todas las ventanas tu cuerpo se aclara y encima pone «Derribado». No te mueves, no disparas ni lanzas habilidades, y los perseguidores dejan de pegarte y van a por otro jugador. |
| Caer con latencia | Repite con un cliente y 100 ms. | Igual. Como mucho da un paso o sale un disparo justo al caer, y el host no avisa de nada. |
| Levantarse con trucos | Con un jugador derribado, pulsa «Curar a todos». | Se levanta con la vida llena en todas las ventanas y vuelve a moverse y disparar. |
| Derribado al entrar tarde | Con un jugador derribado, une a otro cliente. | El nuevo lo ve derribado desde el principio. |
| Reanimar | Con un jugador derribado, ponte a menos de 1,5 casillas y quédate cerca. | En todas las ventanas sube «Reanimando N %» sin saltos. La primera reanimación de la partida tarda 3 s; se levanta con 50 de vida y vuelve a moverse y disparar. |
| Alejarse | Empieza a reanimar y aléjate antes de los 3 s. Vuelve después. | Al alejarte vuelve a «Derribado». Al volver, el porcentaje empieza de cero. |
| Reanimar con latencia | Un cliente con 100 ms reanima a otro. | El porcentaje sube igual de suave en todas las ventanas y se levanta en todas a la vez (el cliente lento puede verlo un instante después). |
| Golpe a quien reanima | Reanima tú solo y deja que un perseguidor te toque, o pulsa «Dañar a todos». | El porcentaje vuelve a «Derribado»: se pierde. Al pasar 1 s sin golpes, vuelve a empezar de cero. |
| Golpe con dos reanimando | Reanimad dos y que golpeen solo a uno. | El porcentaje sigue, más despacio, mientras el otro siga cerca. Al pasar 1 s sin golpes, el golpeado vuelve a acelerarlo. |
| Invulnerable no corta | Reanima con «Invulnerable» activo y deja que te toquen. | Los golpes no cuentan y la reanimación sigue. |
| Margen al levantarse | Reanima a alguien rodeado de perseguidores. | Durante 1 s su vida no baja; después vuelve a recibir daño. |
| Un derribado no reanima | Con dos jugadores derribados uno junto al otro. | Ninguno levanta al otro. |
| Ajustar con datos | Sube *Duration* de `DefaultRevive` a 6 y repite «Reanimar» en una partida nueva. Devuélvelo a 3. | La primera tarda 6 s. |
| Cada vez más lenta | Con 4 jugadores (3 cargas), reanima tres veces seguidas con un solo compañero. Cronometra. | Tardan 3, 4 y 5 s. |
| Varios a la vez | Con 3 jugadores, derriba a uno y reanimadlo los otros dos a la vez, en la primera reanimación de una partida. Repite con uno solo y luego con los dos, empezando uno y llegando el otro a la mitad. | Con dos tarda 2 s (×1,5); con uno, 3 s. Si llega el segundo a la mitad, el porcentaje acelera desde donde iba, sin saltar. Si se va uno, frena sin perder lo hecho. |
| Cargas con 2 jugadores | Host y un cliente. Abre F1 y derribaos y reanimaos por turnos. | El panel dice «Reanimaciones: 2/2», luego 1/2 y 0/2, igual en las dos ventanas. Al caer otra vez, la etiqueta dice «Sin reanimaciones», el compañero no puede levantarte y «Curar a todos» sí (no gasta). |
| Cargas con 4 jugadores | Host y tres clientes. | «Reanimaciones: 3/3» en las cuatro ventanas. |
| Entra o sale alguien | Con 2 jugadores, gasta una carga (1/2); une a un tercero y luego que salga. | Al entrar pasa a 2/3; al salir sigue en 2/3. |
| Cortar no gasta | Empieza a reanimar y aléjate antes de terminar. | Las cargas no bajan. |

## 18. Fin de partida y reinicio

Con el host y dos clientes, en local y otra vez con 100 ms de latencia. Para acabar rápido, pon *Range* a 0 en `DefaultAutoAttack` y deja que os derriben, o usad «Dañar a todos» varias veces.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Caen todos | Dejad que derriben a los tres. | Al caer el último, en todas las ventanas sale «Habéis caído» con las mismas oleadas y el mismo tiempo. Dejan de salir enemigos y los que hay se quedan quietos. En el host, `[MatchFlow] Every player is downed`. |
| Queda uno en pie | Que derriben a dos y el tercero siga vivo, sin cargas. | No sale la pantalla. Al caer el tercero, sí. |
| Salir desde la pantalla | Con la pantalla de fin, un cliente pulsa «Salir». | Vuelve al menú como con el botón de salir de siempre. |
| Entrar con la partida acabada | Con la pantalla de fin, une a otro cliente. Repite con un cliente que sale y vuelve a entrar. | Aparece derribado (cuerpo aclarado, «Sin reanimaciones» o «Derribado») y ve la pantalla de fin con los mismos datos. Ningún enemigo lo persigue ni le pega, y nadie escribe `player_downed` por él. Al reiniciar, se levanta con los demás. |
| Evento de fin | Mira los logs al caer todos. | Un solo `match_over result=defeat match_time=… waves=…`, igual en todas las ventanas. |
| Solo el host reinicia | Con la pantalla de fin abierta, mira las tres ventanas. | El host ve «Reiniciar» y «Salir»; los clientes, «Esperando al host…» y «Salir». |
| Reiniciar | El host pulsa «Reiniciar». | En todas las ventanas: se cierra la pantalla, todos de pie con 100 de vida en su punto de aparición (los demás saltan allí, sin deslizarse), sin enemigos, el reloj vuelve a 0:00 y la primera oleada sale como al empezar. F1 muestra una semilla nueva, igual en todas, y las reanimaciones completas (2/2 con dos jugadores, 3/3 con tres). En el host, `[MatchFlow] Match restarted (1)`. |
| Tres veces seguidas | Perded y reiniciad tres veces seguidas, una de ellas con 100 ms de latencia. | Todo igual la tercera vez que la primera. En la *Hierarchy* del host no queda ningún `Chaser(Clone)` justo después de reiniciar, y la consola sigue limpia. |
| Nadie se levanta por truco | Mira los logs al reiniciar. | Ningún `player_revived` al reiniciar: levantarse ahí no es una reanimación. |
| Reinicio para el launcher | Mira los logs al reiniciar. | En cada ventana, `match_left reason=restart` y enseguida `match_joined` con la semilla nueva y `match_time=0`. Un truco usado antes del reinicio no aparece después. |
| Entrar tras reiniciar | Reinicia y une a otro cliente. | Entra en la partida nueva, con la misma semilla que los demás. |

## 19. Experiencia y niveles

Con el host y dos clientes, en local y otra vez con 100 ms de latencia. «Dar experiencia» (F1) suma 10 al grupo.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Primer nivel con todos | Hospeda solo y une a dos clientes en los primeros segundos. Pulsa «Dar experiencia» tres veces. | Con 3 jugadores el primer nivel pide 19 (10 × 1,9). Al tercer clic se sube al nivel 2 con 11 de 34. Igual en todas las ventanas. |
| Varios niveles de golpe | Sube *Give Xp Amount* a 100 en `DebugCheats` y pulsa una vez. Devuélvelo a 10. | Sube varios niveles a la vez, con el sobrante en la barra. En los logs, una línea `level_up` por cada nivel. |
| Cambiar de jugadores a mitad de nivel | Con experiencia en la barra, une a otro cliente. | El nivel actual sigue pidiendo lo mismo; el siguiente ya cuenta al nuevo. |
| Entrar tarde | Une a un cliente con el grupo en nivel 3. | Ve el mismo nivel y la misma barra. No escribe `level_up` de los niveles anteriores. |
| Reiniciar | Pierde y reinicia. | Nivel 1 y barra vacía en todas las ventanas. |
| Ajustar con datos | Cambia *Base Xp* de `DefaultXpCurve` a 20 en una partida nueva. Devuélvelo a 10. | El primer nivel pide 20 con un jugador. |
| Orbes al morir | Matad perseguidores lejos y acercaos después a donde cayeron. | La barra no sube hasta que un jugador en pie pasa a menos de 1,5 casillas; entonces sube 1 por orbe, igual en todas las ventanas. |
| Un orbe, una vez | Dos jugadores se acercan a la vez al mismo orbe, uno con 100 ms. | La barra sube 1, no 2. |
| Derribado no recoge | Un jugador derribado sobre orbes. | No los recoge; sí quien llegue de pie. |
| Tope de 40 | Con *Range* de `DefaultAutoAttack` en 6, «Generar 10 Chaser» y «Matar a todos» cinco veces sin recoger nada. | Nunca hay más de 40 orbes en el suelo (cuenta los `ExperienceOrb(Clone)` activos bajo `Match` en la *Hierarchy*); los que se suman a otro se ven más grandes. Al recogerlos todos, la barra sube 50. |
| Reiniciar sin orbes | Deja orbes en el suelo, pierde y reinicia. | No queda ninguno y la barra empieza vacía. |
| Se ven en todas | Matad perseguidores. | Cada uno deja un orbe donde cayó, en el mismo sitio en todas las ventanas. |
| Atracción suave | Pasa cerca de varios orbes, también con 100 ms. | Vuelan hacia ti acelerando y desaparecen al llegar, sin saltos. En las otras ventanas vuelan hacia ti, no hacia ellos. |
| Orbes al entrar tarde | Deja orbes en el suelo y une a otro cliente. | Los ve todos en su sitio, una sola vez cada uno. |
| Sin orbes fantasma | Con host y dos clientes, recoged muchos orbes por turnos durante un par de oleadas, cada uno en una zona, y derribaos y reanimaos entre medias. Mira la ventana del host. | Un orbe recogido desaparece en todas las ventanas, también en la del host. Nunca queda en una ventana un orbe que no se puede recoger. |
| Sale quien los recoge | Un cliente recoge orbes y sale justo después. | Los orbes en vuelo desaparecen sin errores. |

## 20. Resolución base (#42)

Escena `ResolutionTest`, que no está en la build: ábrela sola y pulsa Play. En la vista *Game*, elige la resolución fija de cada prueba (*Free Aspect* › `+` para añadir 1920×1080 y 2560×1440) con la escala a 1×. Si tu monitor es más pequeño, haz una build de desarrollo con solo esta escena y ábrela a pantalla completa en cada monitor que tengáis.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Se lee a 1080p | A 1920×1080, alterna 1 y 2. | Con las dos se distinguen los 4 personajes y los enemigos en movimiento; apunta con cuál se lee mejor y con cuál se ve mejor el grupo. |
| Se lee a 1440p | A 2560×1440, alterna 1 y 2 y luego pulsa C. | 640×360 llena la pantalla (×4). 480×270 da ×5 y, sin recorte, se ve algo más de arena; con recorte, bandas negras. |
| Espacio para 4 | Con 30 enemigos, mira si el grupo ve venir a los enemigos antes de que lleguen. | Apunta en el issue con qué resolución hay sitio para reaccionar. |
| Personajes más grandes | A 1920×1080 con 640×360, pulsa S para ver los sprites ×1,5 y ×2. Compara con 480×270 a ×1. | Con ×1,5 un personaje de 24 px se ve como uno de 36, y con ×2 como uno de 48; la vista sigue siendo de 40×22 casillas. Apunta qué tamaño se siente cerca sin tapar el grupo con 30 enemigos. |

## 21. Mejoras y ofertas (#78 y #77)

Arena_Test. Para las pruebas de semilla, en `Data/Core/RunRandom.asset` pon *Fixed Seed* `3A9F01C2` y activa *Log Rolls*; vacía *Fixed Seed* al acabar. «Dar experiencia» (F1) sube niveles y «Dar reroll» (F1) da un reroll a todos. Lo que hace cada mejora se prueba en la sección 22.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Primera oferta | Hospeda solo y sube al nivel 2. | Se abre la franja abajo, encima de la barra de habilidades, con «Nivel 2» y tres filas. Te sigues moviendo y el básico sigue disparando. |
| Misma semilla, mismas ofertas | Con la semilla fija, solo, sube de nivel y elige siempre la 1 hasta el nivel 6. Hazlo dos veces. | Las dos veces la consola dice lo mismo: `nivel 2 (nueva): econ_magnet, basic_tempo, aura_lingering`, `nivel 3: econ_magnet, basic_twin_shot, def_thick_skin`, `nivel 4: mov_tailwind, basic_long_barrel, stat_aura_hunter`, `nivel 5: mov_swift, basic_twin_shot, def_thick_skin`, `nivel 6: mov_swift, aura_lingering, basic_twin_shot`. (Cambian si cambia la lista del pool: se recalculan con `offers_sim.py`.) |
| Builds distintas | Sin semilla fija, dos partidas eligiendo lo que quieras. | Ofertas y mejoras distintas. |
| Plaza A | Elige Auras tenaces en el nivel 2. | En el nivel 3 sale primero otra vez, con «2/2». En el 4 ya no sale. |
| Rara garantizada | Llega al nivel 5. | La segunda fila es Doble disparo (la única rara genérica del Pre 3) si no la tienes. |
| Épica desde el 5 | Mira las ofertas de los niveles 2 a 4. | Nunca sale Aura expansiva (épica) antes del nivel 5. En el 10 sale seguro si no la tienes. |
| Requisitos | Sube hasta el nivel 12 eligiendo cualquier cosa. | Nunca salen Muro vivo, Llama eterna, Remolino, Corazón de marea, Sobretensión ni Bobina detonante: aún no tienen efecto (llegan con el #162), y una mejora sin efecto no sale. Cuando no queda nada, sale Respiro. |
| Cooperación | Con host y un cliente, subid al nivel 3. | Relevo y Asistencia pueden salir desde el nivel 3. Solo, nunca. |
| Cada uno la suya | Con host y dos clientes, misma semilla, subid al nivel 2. | Cada ventana tiene su oferta. Con la semilla fija y dos jugadores, J1 ve `econ_magnet, basic_tempo, aura_lingering` y J2 `mov_swift, basic_twin_shot, def_vitality`. |
| Cola | Pulsa «Dar experiencia» varias veces seguidas sin elegir. | La cabecera dice «+N en cola». Al elegir, sale al instante la siguiente con su nivel. |
| Ocultar | Con una oferta abierta, pulsa Tab. | Se oculta la franja y aparece «Mejoras: N · Tab». Tab la vuelve a abrir con las mismas cartas. |
| Elegir con clic | Haz clic en una fila. | La elige, sin lanzar la habilidad del clic izquierdo. |
| Reroll | Pulsa «Dar reroll» y luego F. | Tres opciones nuevas para el mismo nivel; «volver a tirar (0)». Sin rerolls, F no hace nada. |
| Tope de rerolls | Pulsa «Dar reroll» cuatro veces. | Nunca pasa de 2. Saltar (C) da «+1/3», pero el total nunca pasa de 2 rerolls por partida. |
| Rerolls por tiempo | Deja correr la partida hasta 4:00 y 8:00. | Cada vez, un reroll más para cada jugador (`[UpgradeOffers] Timed reroll`). |
| Descartar | Pulsa X y luego 2. | Esa opción cambia por otra y «descartar (0)». No vuelve a salir en toda la partida. Una segunda X no hace nada. |
| Respiro | Agota el pool (nivel 12 o más, solo) y elige Respiro. | Suma «+1/3» de reroll. No se puede descartar. |
| Invulnerable al elegir | Elige con enemigos encima, y otra vez antes de 10 s. | La primera vez, 1 s sin recibir daño; la segunda, no. |
| Eligiendo | Con host y cliente, abre la franja en el cliente. | En las dos ventanas, «Eligiendo» encima del cliente. Al cerrar o elegir, desaparece. |
| Derribado elige | Déjate derribar con ofertas en cola. | Puedes seguir eligiendo. |
| Con ping | Con 100 ms de latencia, elige, rerollea y descarta en un cliente. | La franja se abre al instante. Tras pulsar, las filas se atenúan hasta que llega la respuesta; nunca se elige dos veces. La mejora elegida coincide en host y cliente (`upgrade_picked` en el log del cliente). |
| Entrar tarde | Con el grupo en nivel 4, une a un cliente. | Recibe tres ofertas en cola (niveles 2, 3 y 4) y los rerolls por tiempo ya dados. |
| Reiniciar | Con mejoras y ofertas en cola, pierde y reinicia. | Se cierra la franja; nadie tiene mejoras, rerolls a 0 y descartes a 1. |
| Fin de partida | Con la franja abierta, que caigan todos. | La franja se oculta mientras está la pantalla de fin. |
| Launcher | Elige, rerollea, descarta y salta. | En el log de quien elige: `upgrade_picked upgrade=… rarity=… level=…` y `offer_action action=reroll`, `banish upgrade=…` y `skip`. «Dar reroll» escribe `cheat_used cheat=give_reroll`. |

## 22. Estadísticas y efectos de las mejoras (#157 y #161)

Arena_Test. Abre F1: la línea de estadísticas enseña las tuyas tal como las calcula tu PC. Para que salga una mejora concreta, sube niveles con «Dar experiencia» y rerollea con «Dar reroll»; en solitario el pool es corto y salen pronto.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Sin mejoras, como antes | Empieza una partida. | F1: vida máx. 100, velocidad 5, básico 10 ×1 cada 0.6 s, alcance 6, daño recibido ×1, recogida 1.5. Todo se juega igual que antes. |
| Cañón largo | Cógela. | Alcance 9 y cadencia 0.65 s. Disparas a enemigos más lejanos y la bala llega hasta ellos (en tu ventana y en las de los demás). |
| Vitalidad | Pierde vida y cógela. | Vida máx. 120 y +20 de vida al momento; la etiqueta de vida dice `x/120` en todas las ventanas. Tres veces: 160. |
| Pies ligeros | Cógela dos veces. | Velocidad 5.6 y luego 6.2; vida máx. 90 y 80 (si tenías más vida que el nuevo máximo, baja a él). Te mueves más rápido en tu ventana y los demás te ven igual de rápido. |
| Piel gruesa | Cógela y deja que te golpee un perseguidor. | Daño recibido ×0.9: cada golpe de 10 quita 9. Velocidad ×0.95. |
| Imán | Cógela. | Recogida 2.25: los orbes vuelan hacia ti desde más lejos. Solo a ti; un compañero sin Imán sigue con 1.5. |
| Respiro | Agota el pool y elígelo herido. | +30 de vida (sin pasar del máximo) y +1/3 de reroll. Derribado no cura. |
| Se suman | Coge Pies ligeros dos veces y Piel gruesa una. | Los porcentajes se suman: velocidad 5 × (1 + 0,24 − 0,05) = 5.95. |
| Doble disparo | Cógela (sale como rara garantizada en el nivel 5). Deja que se acerquen varios perseguidores. | F1: `Básico 7 ×2`. Cada ataque suelta dos balas abiertas en V estrecha. Si las dos tocan al mismo enemigo, solo una le quita vida (7, no 14); con dos enemigos juntos, cada uno recibe la suya. Los demás jugadores ven las dos balas. |
| Doble disparo con ping | Con 100 ms, un cliente coge Doble disparo y dispara a un grupo. | En el host, cada enemigo baja 7 por ráfaga como mucho; sin `Rejected a shot` ni `Rejected a hit` (salvo, quizá, uno justo al cogerla). |
| Auras tenaces | Antes de cogerla, lanza Marca (clic derecho) sobre un perseguidor y cuenta cuánto dura su aura. Cógela y repite. | Antes 6 s; después 7,5 s. Con las dos acumulaciones, 9 s. El aura del básico (débil) sigue durando 3,5 s. Con dos jugadores, solo duran más las auras de quien la tiene. |
| Aura expansiva | Cógela. Lanza Marca en el centro de un grupo, con enemigos justo fuera del círculo. | Los de dentro reciben daño y aura; los que están hasta 1 m fuera reciben solo el aura, sin daño. Como mucho 8 de más. |
| Aura expansiva no detona | Pon un aura de otro elemento a un enemigo (otro jugador o `ReactionLab`) y lanza Estallido de forma que ese enemigo quede justo fuera del círculo. | No hay reacción en ese enemigo y su aura no cambia. Dentro del círculo sí detona como siempre. |
| Aura expansiva con ping | Con 100 ms, un cliente con Aura expansiva lanza Marca. | El anillo funciona igual; sin avisos `Rejected` en el host. |
| Cazador de auras | Cógela. Lanza Estallido sobre un perseguidor sin aura y, cuando reaparezca, sobre uno al que hayas marcado antes con el básico. | Sin aura: −16. Con aura: −18 (el texto flotante lo enseña). Si el Estallido detona una reacción, también −18: el aura contaba antes del golpe. Marca: 10 sin aura, 12 con aura. El básico no cambia. |
| Tempo | Cógela. Mira F1 y lanza Estallido. | La cadencia pasa a `cada 0.48 s` al momento y vuelve a 0.6 a los 2 s. Si vuelves a lanzar antes de 4 s (desde el primer lanzamiento), no se repite. |
| Tempo con ping | Con 150 ms, un cliente con Tempo lanza Estallido. | En su F1, 0.48 al instante (sin esperar al host) y vuelta a 0.6 a los 2 s; sin parpadeos. En el host, ningún `Rejected a shot … too fast`. |
| Viento a favor | Cógela. Marca a un enemigo con otro elemento (o en `ReactionLab`) y detónalo con Estallido. | Velocidad 6.25 durante 2 s; luego 5. Otra reacción antes de 6 s no la repite. Poner el aura que otro detona no la activa. |
| Relevo | Dos jugadores con elementos distintos. J1 coge Relevo, lanza sus dos habilidades y pone aura; J2 la detona. | Las recargas de J1 bajan 1 s al momento (en su HUD). Otra reacción antes de 2 s no le quita más. Si J1 detona su propia aura, no pasa nada. |
| Asistencia | J2 coge Asistencia. J1 lanza, pone aura y J2 la detona. | Las recargas de J1 (no las de J2) bajan 1 s. Si J2 detona otra aura de J1 antes de 2 s, J1 no recibe más. |
| Suelo de recarga | J1 con Relevo lanza Estallido (7 s) y J2 detona tres auras suyas seguidas, cada 2 s. | La recarga de J1 nunca termina antes de 4,2 s tras lanzar. Lanzar justo al acabar no da `Rejected … before its cooldown ended` en el host. |
| Reiniciar con temporales | Con Tempo activo (0.48), pierde y reinicia. | F1 vuelve a 0.6 y las recargas se pueden usar. |
| Reiniciar | Con varias mejoras, pierde y reinicia. | F1 vuelve a los valores de la primera fila. |
| Cliente con ping | Con 100 ms, un cliente coge Cañón largo y Pies ligeros. | Lo nota al llegar la respuesta del host y sus disparos lejanos cuentan (sin `Rejected a hit` en la consola del host). |
| Entrar tarde | Un jugador con mejoras; une a otro cliente. | El nuevo ve la vida máxima correcta del primero (`x/120`). |
| Editor | Abre `Data/Progression/Upgrades/DefVitality.asset`. | En *Effects* hay dos bloques: *Stat Modifier Block* (Max Health, Flat, 20) y *On Event Block* (Acquired) con un *Restore Action* de 20. El botón de cada uno abre la lista de bloques. |

## 23. Personajes (#162)

Arena_Test, con el host y tres clientes. Hasta la sala de espera (#173) el personaje sale del hueco, y hasta que lleguen los kits todos tienen Estallido y Marca.

| Prueba | Pasos | Resultado esperado |
|---|---|---|
| Personaje por hueco | Hospeda y une a tres clientes. | J1 Brasa (fuego), J2 Marea (agua), J3 Voltio (electricidad) y J4 Brasa (fuego): lo dice el toast de cada ventana. Disparos, auras y círculos salen del color de ese elemento en todas las ventanas. |
| Habilidades del personaje | En cada ventana, mira la barra de habilidades y lanza las dos. | Estallido en el clic izquierdo, Marca en el derecho y la Q vacía, como antes. Sin avisos en la consola del host. |
| Entrar tarde | Con la partida empezada, une a otro cliente con 100 ms de latencia. | Recibe el personaje de su hueco y ve a los demás con el elemento correcto desde el principio. |
| Volver al hueco | J2 sale y vuelve a entrar. | Vuelve a ser Marea. |
| Datos | En `Data/Players/PlayerCharacters.asset`, pon Voltio en el hueco 0 (*By Slot*, elemento 0) y hospeda. Devuélvelo a Brasa. | El host juega Voltio, con electricidad. |
| Mejoras de personaje | Sube hasta el nivel 12 con cada personaje. | No sale ninguna de las 6 de personaje (no tienen efecto todavía). Con la semilla `3A9F01C2`, las ofertas son las mismas que en la sección 21. |
| Reiniciar | Pierde y reinicia. | Cada uno sigue con su personaje y su elemento. |
