# Moodex

Moodex is a Windows game‑launcher framework. It manages one library that includes both PC games and titles launched through emulators.

Core pieces:

- **Emulators Manager** – register emulator executables and arguments.
- **Games Manager** – add games to the library, track completion and record acheviements.
- **Script & Controller Configuration** - customise input for specific games.

Design notes:

- Lightweight: files stay in your folders; small per‑game manifests store metadata.
- Portable layout: simple folders; easy to move or back up to a configurable archive.

## Get started

1) Install [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0/runtime)
2) Build and run:

```bash
git clone https://github.com/samcolwill/Moodex.git
cd Moodex
dotnet run -c Debug
```

Point Moodex at your library and add emulators/games as needed.

---

[MIT](LICENSE)

