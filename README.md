# Goo-napped

A 2D top-down stealth puzzle game about a clear-aura slime who is thrown out of his kingdom and must steal his friends back from an alchemist's laboratory. Split yourself, copy the aura of the friend you want to rescue, and slip past the guards without being seen.

Made for **GitHub Game Off 2026**, theme **SHIFT**. Working title, the final name is still open.

- **Engine:** Unity 6 (6000.3.28f1), Universal Render Pipeline 2D
- **Art:** pixel art, 3D-rendered sprites made in Blender
- **Platform:** Windows (keyboard)
- **Status:** game-jam build, playtest and balance ongoing

---

## Play

Download the Windows build from the itch.io page (link to be added after publishing), unzip it, and run `GooNapped.exe`.

### Controls

| Key | Action |
|---|---|
| WASD / Arrow keys | Move (small hopping slime movement) |
| E | Eat essence, use things, interact (swap with a jar, rescue a friend) |
| Space | Split off a piece (max 2 pieces); merge when touching another piece |
| Shift | Switch control to another piece, or to a friend in the vent |
| C | Hide inside an object (you cannot move while hidden) |
| F | Call / wait for a rescued friend; use a friend's power in the vent |
| Q | Spit out the last essence you ate |
| R | Retry from the last vent checkpoint |
| Esc | Pause |

### How a level works

1. Find the experiment room. There is no marker; follow the clues in the environment (aura pipes, glowing jars, dripping goo).
2. Eat essences. Each one adds mass and effects. **The last essence you eat decides your aura.**
3. Split. A piece whose mass equals a friend's size goes into that friend's jar as a copy, and the friend is freed.
4. Lead the friend to a vent without being caught. Vents are safe, act as checkpoints, and are the level exit.

Being spotted means the guards chase you. Break line of sight and hide to escape. Being caught sends you back to the last vent.

---

## Development

### Requirements
- Unity **6000.3.28f1** (install through Unity Hub)
- **Git LFS** (`git lfs install`). Sprites, `.blend` files, and audio are stored with LFS.

### Open the project
1. Clone the repository and open the folder in Unity Hub.
2. Open `Assets/_Game/Scenes/Boot/Boot.unity` and press Play. The main menu starts the game flow: Intro → L00 tutorial → L01 … L11, with the friend hideout between levels, and the Ending.

### Build for Windows
Use the **GooNapped → Build** menu in the editor:
- **Windows Rilis** (release) → `Builds/Windows/`
- **Windows Dev (dengan log)** (development, with log file and in-game log overlay) → `Builds/Windows-Dev/`

The build script adds every scene in the game flow automatically, so you do not need to edit Build Settings. Builds are ignored by git.

Smoke test: run `smoke_test.bat` in a build folder. It loads every scene and quits, then writes results to `goo-napped_log.txt`.

### Project layout

```
Assets/
  _Game/                 all game-owned assets
    Art/                 sprites, materials, shaders (PixelToonLit), animations, tiles, fonts
    Audio/               music, SFX
    Data/                ScriptableObjects: essences, friend recipes, story
    Prefabs/             characters, enemies, props, levels, UI, systems
    Scenes/              Boot, Menus, Story, Levels (V2 = current levels), Sandbox (test scenes)
    Scripts/
      Runtime/           game code (asmdef GooNapped.Runtime)
      Editor/            editor tools and build script (asmdef GooNapped.Editor)
    Tests/               EditMode and PlayMode tests
  ThirdParty/            third-party assets and their licenses
  Settings/              URP, renderer, input actions
art/                     art sources (Blender files, render tools, palette)
docs/                    team documentation (in Indonesian)
```

### Tests
Run tests from **Window → General → Test Runner**. EditMode and PlayMode suites are under `Assets/_Game/Tests/`.

---

## Credits

Full list in [CREDITS.txt](CREDITS.txt). It is also included in every build.

- **Team:** design, art, level design, story, and audio by the team; gameplay architecture, code, and tooling with AI assistance under the team's direction.
- **Music and sound:** Freesound.org, CC0 and CC BY 4.0 (see CREDITS.txt for each author and link).
- **Font:** Pixelify Sans, SIL Open Font License 1.1.

## License

TBD. The team has not chosen a license yet. Until one is added, all rights are reserved by the team, and third-party assets keep their own licenses.

---

## Team documentation (Indonesian)

| Document | Contents |
|---|---|
| [docs/desain-v2.md](docs/desain-v2.md) | Game design v2: lore, mechanics, levels, enemies |
| [docs/backlog.md](docs/backlog.md) | Feature progress |
| [docs/arsitektur.md](docs/arsitektur.md) | Code architecture |
| [docs/cara-edit-unity.md](docs/cara-edit-unity.md) | How to edit levels, essences, guards, and sound in Unity |
| [docs/panduan-blender-3d.md](docs/panduan-blender-3d.md) | Blender model to 48 px sprite pipeline |
| [docs/panduan-art-desainer.md](docs/panduan-art-desainer.md) | 2D art rules |
| [docs/ai-vs-tim.md](docs/ai-vs-tim.md) | Who does what on the team |
| [CLAUDE.md](CLAUDE.md), [art/CLAUDE.md](art/CLAUDE.md) | Design decisions and technical rules |
