# Coop 2D (título provisional)

Un roguelite cooperativo 2D en pixel art donde los jugadores combinan elementos para provocar reacciones que solos no conseguirían.

> **Frase de diseño:** "Haz esto y yo hago aquello."

Este proyecto es, ante todo, un campo de entrenamiento: el objetivo es aprender a hacer juegos de verdad, no terminar rápido.

---

## Requisitos

| Herramienta | Versión |
| --- | --- |
| Unity | **6000.3.TODO** (Unity 6.3 LTS). Todos usamos exactamente la misma versión |
| Render pipeline | URP, plantilla Universal 2D |
| Red | FishNet 4.7.3R |
| Arte | Aseprite |
| Editor de código | Rider o Visual Studio Community |

**No usar las versiones de actualización de Unity (6.5, 6.6...).** Si actualizamos, actualizamos todos a la vez y en una rama aparte.

## Cómo empezar

1. Instala la versión exacta de Unity desde Unity Hub, con el módulo **Windows Build Support**.
2. Clona este repositorio.
3. Abre la carpeta del proyecto desde Unity Hub.
4. Abre `Assets/_Project/Scenes/Bootstrap.unity` y pulsa Play.

### Probar el multijugador

- **En un solo PC:** usa Multiplayer Play Mode (*Window > Multiplayer > Multiplayer Play Mode*) para abrir más jugadores dentro del editor.
- **Entre varios PCs:** el host abre el puerto UDP de Tugboat en su router (port-forward) y los demás se unen con su IP pública. Si el operador del host usa CGNAT, el plan B es una VPN (Tailscale o ZeroTier).

## Estructura del proyecto

Todo lo nuestro vive en `Assets/_Project`. Los paquetes externos quedan fuera y no se editan nunca.

```
Assets/
  _Project/
    Art/        sprites, tiles, iconos, VFX, UI, paleta
    Audio/
    Code/       una carpeta por sistema (Core, Networking, Player, Combat...)
    Data/       ScriptableObjects con datos de diseño
    Prefabs/
    Scenes/
    Settings/   input actions, rendering, presets de importación
  FishNet/      archivos del paquete, no editar
  Sandbox/
    <TuNombre>/ tus escenas de prueba; nunca entran en las builds
```

## Convenciones

### Idioma

- **En inglés:** código, carpetas, archivos, comentarios, assets, escenas, ramas, commits y títulos de pull requests e issues.
- **En español:** este README, los documentos de diseño, las descripciones de issues y PRs, y las conversaciones del equipo.

### Código

- `PascalCase` para clases, métodos y archivos; `camelCase` para variables; `_camelCase` para campos privados.
- Una clase por archivo, con el mismo nombre que el archivo.
- **Nada de números mágicos.** Vida, daño, cooldowns y demás valores de ajuste van en ScriptableObjects dentro de `Data/`.

### Arte

- Los archivos siguen el formato `type_name_variant`, en minúsculas: `chr_mage_idle.aseprite`, `enm_slime_walk.aseprite`, `ico_fire.aseprite`, `tile_floor_stone.aseprite`.
- Tiles de 16×16. Personajes de 16×16 o 16×24. Jefes de 32×32 o 48×48.
- Solo colores de la paleta común, guardada en `Art/Palette`.
- Se dibuja a tamaño real y sin antialiasing. Mismo tamaño de lienzo en todas las animaciones de un personaje, con el pivot en los pies.
- Las animaciones usan tags de Aseprite (`idle`, `walk`, `attack`...).
- Nada de assets externos ni generados con IA. Los placeholders propios siempre valen.

### Escenas

- Una escena compartida solo la edita una persona a la vez. Avisa en Discord cuando empieces y cuando termines.
- Todo lo demás se trabaja en prefabs o en tu carpeta de Sandbox.

## Flujo de Git

- Una rama por tarea: `feature/health-bar`, `fix/enemy-spawn`, `art/slime-walk`.
- Cada pull request incluye una frase de **qué hace** y **cómo probarlo**.
- Todo pull request necesita la aprobación de otra persona, también los de quien lidera.
- Las tareas se siguen como issues en GitHub Projects: *To do → In progress → In review → Done*.

## Cuándo una tarea está hecha

Una tarea está hecha cuando:

- Funciona en host y en cliente.
- No da errores en la consola.
- Está revisada y fusionada.
- Quien la hizo sabe explicarla.

## Regla de la IA

La regla depende del rol, y el arte lo hacemos siempre nosotros.

**Quien está aprendiendo** (Fushigod, GrandMaster53):

| Permitido | No permitido |
| --- | --- |
| Explicar conceptos, errores y mensajes del compilador | Escribir código que entra en el proyecto |
| Comparar enfoques y sus pros y contras | Pegar código generado, aunque sea "para probar" |
| Revisar código ya escrito y señalar problemas | Que te dé la solución completa de una tarea |
| Proponer ejercicios para practicar un concepto | Generar arte, sprites o efectos |

**Programador principal** (Palito): puede usar código generado con IA, con estas condiciones:

- Solo entra en `main` si sabe explicar cada línea.
- Pasa por pull request y revisión igual que el resto, en PRs pequeños.
- Cumple las convenciones del proyecto: nombres, datos en ScriptableObjects, autoridad del host.
- Nunca arte generado.

**Prueba rápida, para todos:** si no puedes explicar línea a línea algo que entra en `main`, no entra.

## Ideas nuevas

Las ideas nuevas van a la **lista de ideas** del documento de diseño, no a la versión actual.
