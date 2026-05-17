# 💣 Minesweeper

A clean, fully-featured Minesweeper clone built with **.NET MAUI** — tap to reveal, long-press to flag, and don't hit a mine.

![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS%20%7C%20Windows%20%7C%20Mac-3DDC84?logo=android&logoColor=white)
![Framework](https://img.shields.io/badge/.NET%20MAUI-11.0-512BD4?logo=dotnet&logoColor=white)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-MVVM-blue)
![Version](https://img.shields.io/badge/Version-1.0.0-orange)

---

## 📸 Screenshots

> _Coming soon_

---

## 🎮 How to Play

| Action | Gesture |
|---|---|
| Reveal a cell | Tap |
| Place / remove flag 🚩 | Long-press (or enable Flag Mode) |
| Toggle Flag Mode | Tap the 🚩 button |
| Restart the game | Tap the 🔄 button |
| Pan the board | Drag |
| Zoom in / out | Tap ➕ / ➖ |

Revealed cells show a number indicating how many of the 8 surrounding cells contain mines. Cells with no adjacent mines are automatically revealed in a flood fill. Reveal all safe cells to win — your first tap is always safe.

---

## ✨ Features

- **Difficulty presets** — Easy (9×9, 10 mines), Medium (16×16, 40 mines), Hard (16×30, 99 mines)
- **Custom mode** — configure rows (6–24), columns (6–30), and mine count freely
- **First-click safety** — mines are placed after your first tap, avoiding the clicked cell and its neighbors
- **Flood-fill auto-reveal** for empty cells
- **Flag Mode toggle** for one-handed flagging
- **Pan & zoom** — drag to scroll large boards, zoom in or out with ± buttons
- **4 themes** — Classic, Light, Retro, Neon
- **Best-time tracking** per difficulty, persisted locally
- **Stats dashboard** — games played, won, lost, current streak, best streak, win rate
- **Progress bar** showing percentage of safe cells cleared
- **Result overlay** with time, outcome, and new-best detection
- **Haptic & vibration feedback** on reveal, flag, win, and loss
- **One-tap restart** at any time

---

## 🏗️ Architecture

The project follows **MVVM** (Model-View-ViewModel) to keep game logic cleanly separated from the UI.

```
MinesweeperApp/
├── Models/
│   ├── Cell.cs                  # Cell state: revealed, flagged, mine, adjacency, theme colors
│   └── DifficultyLevel.cs       # Difficulty enum and GameSettings factory
├── ViewModels/
│   └── GameViewModel.cs         # Game logic, commands, stats, INotifyPropertyChanged
├── Controls/
│   └── MinesweeperBoardView.cs  # Custom GraphicsView canvas renderer with pan support
├── Services/
│   └── GameFeedback.cs          # Cross-platform haptic/audio feedback (partial class)
├── MainPage.xaml                # Main UI layout and bindings
├── MainPage.xaml.cs             # Code-behind (minimal)
└── MauiProgram.cs               # App entry point and DI setup
```

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/) |
| Language | C# 12 |
| UI Markup | XAML |
| Board Rendering | `GraphicsView` (custom canvas, viewport-culled) |
| Pattern | MVVM |
| Persistence | `Microsoft.Maui.Storage.Preferences` |
| Target | Android (API 24+), iOS 15+, macOS 17+, Windows 10+ |

---

## 🚀 Getting Started

### Prerequisites

Make sure the following are installed before building:

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 11.0 | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Android SDK | API 24+ | [Android Studio](https://developer.android.com/studio) |
| JDK | 17 | [Microsoft JDK 17](https://aka.ms/download-jdk/microsoft-jdk-17-windows-x64.msi) |
| IDE | VS Code + C# Dev Kit | [marketplace.visualstudio.com](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) |

### Installation

```bash
# Clone the repository
git clone https://github.com/shivcyh-0102/Minesweeper-App.git
cd Minesweeper-App

# Restore dependencies
dotnet restore

# Install MAUI workload (first time only)
dotnet workload install maui-android
```

### Running on Android

**Option A — Physical device (USB)**
```bash
# Enable Developer Mode and USB Debugging on your device, then:
dotnet build -t:Run -f net11.0-android
```

**Option B — Emulator**
```bash
# Start an AVD from Android Studio, then:
dotnet build -t:Run -f net11.0-android
```

**Option C — Build APK for sideloading**
```bash
dotnet publish -f net11.0-android -c Release
# APK is output to bin/Release/net11.0-android/publish/
```

---

## 🔧 Configuration

Custom game parameters are set from the difficulty selection screen in-app. For code-level defaults, see `Models/DifficultyLevel.cs`:

```csharp
DifficultyLevel.Easy   => 9×9,  10 mines
DifficultyLevel.Medium => 16×16, 40 mines
DifficultyLevel.Hard   => 16×30, 99 mines
DifficultyLevel.Custom => 6–24 rows, 6–30 cols, configurable mines
```

---

## 🗺️ Roadmap

- [ ] Animated mine reveal on loss
- [ ] Sound effects (platform audio implementation)
- [ ] Chord reveal (tap a numbered cell to auto-reveal neighbors when flags match)
- [ ] Screenshots and app store listing

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
