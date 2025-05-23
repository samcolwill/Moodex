# SamsGameLauncher

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

SamsGameLauncher is a video game management system and launcher for Windows, designed to help you organize, categorize, and launch both native and emulated games from a unified interface.

## Download

> **Prerequisite:** [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0/runtime)

- **Windows x64 (framework-dependent ZIP, ~5 MB)**  
  [SamsGameLauncher-v1.0.0-win-x64.zip](https://github.com/samcolwill/SamsGameLauncher/releases/download/v1.0.0/SamsGameLauncher-v1.0.0-win-x64.zip)  
  Unzip and run `SamsGameLauncher.exe`.

---

## Features

- **Game Library Management**: Organize your games into an easily browsable library. Supports native (PC) games, emulated games, and folder-based games.
- **Emulator Integration**: Associate emulators with games and launch them directly from the application. Emulated and folder-based games can be linked to their required emulators.
- **Genre Support**: Games can be categorized by genres such as Action, Adventure, Fighting, Platform, Puzzle, Racing, RPG, Shooter, Simulation, Sports, and Strategy.
- **Game Organization**: Move games between "active" and "archive" libraries/drives, with the application handling file moves and updating paths automatically.
- **Custom Grouping & Filtering**: Filter and group your game library for easier navigation.
- **Cover Art Support**: Automatically display cover art for your games if images are provided in the game folders.
- **Multi-Monitor Friendly**: Launch games or emulators on your chosen monitor, remembering your preferred monitor for future launches.
- **Settings and Persistence**: All configuration and library data is stored in JSON files, making it easy to back up or edit your library outside the app.

---

## Getting Started

### Prerequisites

- Windows OS
- .NET (WPF) compatible runtime (see `.csproj` for details)
- Emulators for non-native games (not included)
- Existing collection of PC games and/or ROMs for emulators

### Build & Run

1. **Clone the Repository**
   ```bash
   git clone https://github.com/samcolwill/SamsGameLauncher.git
   cd SamsGameLauncher
   ```

2. **Open in Visual Studio**
   - Double-click `SamsGameLauncher.sln` or open it from Visual Studio.

3. **Restore NuGet Packages**
   - Visual Studio should restore packages automatically; if not, right-click the solution and choose "Restore NuGet Packages".

4. **Build the Solution**
   - Build the project (`Build > Build Solution`).

5. **Run**
   - Press F5 or click "Start" to launch the application.

### Data Files

- On first run, the app creates or copies sample `emulators.json` and `games.json` into its data directory.
- You can edit these JSON files to add emulators or games manually, or use the built-in add/edit dialogs.

---

## Usage

- **Add a Game**: Use the "Add Game" dialog. Choose type (Native/Emulated/Folder-Based), fill in details, and select or link an emulator if needed.
- **Add an Emulator**: Use the "Add Emulator" dialog. Provide the executable path and default arguments (including `{RomPath}` or `{FolderPath}` placeholders).
- **Launch a Game**: Double-click a game or select and click "Run". The app will launch the appropriate executable or emulator with configured arguments.
- **Move/Archive Games**: Move games between storage locations (e.g., SSD to HDD) with a single click.
- **Edit or Delete**: Right-click or use dialogs to edit or remove games/emulators.

---

## Supported Game Types

- **Native Game**: PC game with a direct executable (EXE).
- **Emulated Game**: Console game ROM launched via an emulator (e.g., SNES, PS2, etc.).
- **Folder-Based Game**: Directory-based games, often used for certain emulators.

---

## Customization

- **Genres**: The following genres are supported by default:
  - Action, Adventure, Fighting, Platform, Puzzle, Racing, Role-playing, Shooter, Simulation, Sports, Strategy

- **Cover Art**: Place an image named after the game (e.g., `GameName.png`) in the same folder as the executable or ROM.

---

## Contributing

Pull requests and suggestions are welcome! Please open an issue or contact the maintainer for major changes.

---

## License

MIT License

---

For more details or to view the project source, visit: [SamsGameLauncher on GitHub](https://github.com/samcolwill/SamsGameLauncher)

_This README was generated based on available code and structure. For further details, check the codebase or open an issue._