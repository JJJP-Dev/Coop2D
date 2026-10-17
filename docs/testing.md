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

Con el host y dos clientes, en local y otra vez con latencia. El muñeco de `Arena_Test` tiene 50 de vida, cada disparo le quita 10 y revive al segundo de morir. Los perseguidores tienen 30 y desaparecen al morir; hasta que lleguen las oleadas (#50) hay cuatro colocados en la arena. Para las pruebas del muñeco, acabad antes con ellos.

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
| Sale el perseguido | Con perseguidores detrás de un cliente, ese cliente sale de la partida. | En menos de medio segundo van a por otro jugador, sin errores. |

## 8. Prueba entre varios PCs

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
