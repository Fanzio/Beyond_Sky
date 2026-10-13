# Beyond Sky

Beyond Sky is a **2D side-scrolling arcade game**, developed with **Unity in C#**. The player pilots a plane through three environments — **city, sky and space** — avoiding obstacles, collecting resources and facing enemies and bosses.

The project was created as the final assignment for the Davante game programming course.

**The game assets were generated using AI. The C# code was developed through a combination of manual implementation and AI-assisted generation.**

## Features

- Continuous world scrolling with level sections selected according to the environment and difficulty.
- Progression through city, sky and space.
- Gradually increasing scrolling speed.
- Touch and mouse controls for movement, dodging and shooting.
- Dodge manoeuvre with temporary invulnerability and a cooldown.
- Collectible coins, shields and ammunition.
- Additional ammunition earned through close calls with obstacles.
- Enemies with different behaviours: tracking, bursts, charges, missiles, mines and lasers.
- Bosses selected from the variants available for each environment.
- Option to continue in endless mode after completing the progression.
- Score based on distance travelled and coins collected.
- Cosmetic skin shop using in-game coins.
- Local saving of high scores, coins, skins and preferences.
- Initial tutorial, pause menu and results screen.
- Separate settings for music, sound effects and vibration.
- Parallax backgrounds, animations and visual effects.

## Controls

- **Hold in the upper part of the left zone**: climb.
- **Hold in the lower part of the left zone**: descend.
- **Double-tap in the same left zone**: perform a dodge.
- **Tap the right zone**: fire, if ammunition is available.

On PC, the same controls work with the left mouse button: hold for movement, double-click to dodge and click in the right zone to fire.

## Repository structure

```text
Beyond_Sky/
├── Assets/
│   ├── _Project/
│   │   ├── Art/            Graphical assets
│   │   ├── Audio/          Audio assets
│   │   ├── Editor/         Editor tools
│   │   ├── Prefabs/        Reusable game elements
│   │   ├── Scenes/         MainMenu and Game
│   │   └── Scripts/        C# source code organised by system
│   ├── Settings/           Rendering configuration
│   └── TextMesh Pro/       TextMeshPro assets
├── Packages/               Unity dependency configuration
├── ProjectSettings/        Project settings
├── Enunciato_Progetto_finale.pdf
├── GDD_Beyond_Sky.docx
├── .gitattributes
└── .gitignore
```

## Requirements

- **Unity 2022.3.62f3**
- **Git**
- **Git LFS**

The project uses **Universal Render Pipeline**, **Unity Input System** and **TextMeshPro**. Dependencies are defined in `Packages/manifest.json`.

## Downloading the project

Graphical assets, audio and some other binary files are managed through **Git LFS**. To obtain the complete files, install Git LFS and clone the repository:

```sh
git lfs install
git clone https://github.com/Fanzio/Beyond_Sky.git
cd Beyond_Sky
git lfs pull
```

The repository ZIP archive may contain LFS pointers instead of the actual assets. Cloning with Git LFS is the recommended method.

## Opening and running the project

1. Add the project to Unity Hub by selecting the folder containing `Assets`, `Packages` and `ProjectSettings`.
2. Open the project with **Unity 2022.3.62f3**.
3. Wait for asset importing and dependency restoration to finish.
4. Open `Assets/_Project/Scenes/MainMenu.unity`.
5. Press **Play** and start a game from the menu.

The gameplay scene is `Assets/_Project/Scenes/Game.unity`.

Building for Android requires the **Android Build Support**, **Android SDK & NDK Tools** and **OpenJDK** modules, available through Unity Hub. In the build scene list, `MainMenu` must come before `Game`.

## Game logic

The level is assembled from prefabricated sections, selected according to the current environment and difficulty. Sections are reused when they leave the gameplay area. Enemies, projectiles, mines and missiles also use object pooling systems.

The plane moves vertically while the world scrolls horizontally. Dodging grants temporary invulnerability; the shield absorbs one hit before being consumed. Close calls with obstacles contribute towards replenishing ammunition.

Boss encounters begin after travelling a set distance within the environment. Encounters are completed by surviving until the timer expires; hitting the boss reduces the remaining duration. Completing the encounter allows progression to the next environment.

After completing the progression in space, the player can continue in endless mode with further boss encounters.

## Data and customisation

High scores, accumulated coins, owned skins, the selected skin and preferences are saved locally using `PlayerPrefs`.

Skins change the plane’s appearance without altering its gameplay characteristics. The catalogue includes six variants: Classic, Military, Ice, High Tech, Golden and Cosmo.

Settings allow adjustment of music and sound effects, enabling or disabling vibration, and deletion of saved high scores, coins and skins.

## Documentation

- [Game Design Document](GDD_Beyond_Sky.docx)
- [Final project assignment](Enunciato_Progetto_finale.pdf)
