# Coop 2D (working title)

A small 2D pixel-art co-op roguelite where players combine elements to trigger reactions they can't pull off alone.

> **Design motto:** "You do this, I do that."

This project is first and foremost a training ground: the goal is to learn how to make real games, not to finish fast.

---

## Requirements

| Tool | Version |
| --- | --- |
| Unity | **6000.3.TODO** (Unity 6.3 LTS) — everyone uses the exact same version |
| Render pipeline | URP, Universal 2D template |
| Networking | FishNet 4.7.3R |
| Art | Aseprite |
| Code editor | Rider or Visual Studio Community |

**Do not use Unity update releases (6.5, 6.6...).** If we upgrade, we all upgrade together, on a separate branch.

## Getting started

1. Install the exact Unity version above from Unity Hub, with the **Windows Build Support** module.
2. Clone this repository.
3. Open the project folder from Unity Hub.
4. Open `Assets/_Project/Scenes/Bootstrap.unity` and press Play.

### Testing multiplayer

- **On one PC:** use Multiplayer Play Mode (*Window > Multiplayer > Multiplayer Play Mode*) to run extra players inside the editor.
- **Between PCs:** connect through our VPN (TODO: Tailscale / ZeroTier). One player hosts, the others join using the host's VPN IP.

## Project structure

Everything we make lives in `Assets/_Project`. Third-party packages stay outside it and are never edited.

```
Assets/
  _Project/
    Art/        sprites, tiles, icons, VFX, UI, palette
    Audio/
    Code/       one folder per system (Core, Networking, Player, Combat, ...)
    Data/       ScriptableObjects with design data
    Prefabs/
    Scenes/
    Settings/   input actions, rendering, import presets
  FishNet/      package files, do not edit
  Sandbox/
    <YourName>/ your personal test scenes, never included in builds
```

## Conventions

**Everything in the repository is in English:** code, comments, folders, files, assets, scenes, branches, commits and pull requests. Design documents and team chat are in Spanish.

### Code

- `PascalCase` for classes, methods and files; `camelCase` for variables; `_camelCase` for private fields.
- One class per file, with the same name as the file.
- **No magic numbers.** Health, damage, cooldowns and other tuning values live in ScriptableObjects under `Data/`.

### Art

- File names follow `type_name_variant`, lowercase: `chr_mage_idle.aseprite`, `enm_slime_walk.aseprite`, `ico_fire.aseprite`, `tile_floor_stone.aseprite`.
- Tiles are 16×16. Characters are 16×16 or 16×24. Bosses are 32×32 or 48×48.
- Only colors from the shared palette in `Art/Palette`.
- Draw at real size, with no antialiasing. Same canvas size for every animation of a character, with the pivot at the feet.
- Animations use Aseprite tags (`idle`, `walk`, `attack`...).
- No external or AI-generated assets. Our own placeholders are always fine.

### Scenes

- Only one person edits a shared scene at a time. Announce it in Discord before you start and when you finish.
- Everything else is done in prefabs or in your Sandbox folder.

## Git workflow

- `main` is protected. Everything goes in through a pull request.
- One branch per task: `feature/health-bar`, `fix/enemy-spawn`, `art/slime-walk`.
- Every pull request includes one sentence on **what it does** and **how to test it**.
- Every pull request needs approval from someone else, including the lead's.
- Tasks are tracked as issues in GitHub Projects: *To do → In progress → In review → Done*.

## Definition of done

A task is done when:

- It works on both host and client.
- It produces no errors in the console.
- It has been reviewed and merged.
- Whoever made it can explain it.

## AI rule

We write our own code and make our own art.

| Allowed | Not allowed |
| --- | --- |
| Explaining concepts, errors and compiler messages | Writing code that goes into the project |
| Comparing approaches and their pros and cons | Pasting generated code, even "just to try it" |
| Reviewing code we wrote and pointing out problems | Getting the full solution to a task |
| Suggesting exercises to practice a concept | Generating art, sprites or effects |

**Quick test:** if you can't explain something you wrote line by line, it doesn't go into `main`.

## Ideas

New ideas go to the **Version 2** list in the design document, not into the current version.
