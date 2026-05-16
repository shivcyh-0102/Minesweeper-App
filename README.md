# 💣 Minesweeper

A clean, fully-featured Minesweeper clone built with **.NET MAUI** for Android — tap to reveal, double-tap to flag, and don't hit a mine.

![Platform](https://img.shields.io/badge/Platform-Android-3DDC84?logo=android&logoColor=white)
![Framework](https://img.shields.io/badge/.NET%20MAUI-9.0-512BD4?logo=dotnet&logoColor=white)
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
| Place / remove flag 🚩 | Hold |
| Restart the game | Tap the 🔄 button |

Revealed cells show a number indicating how many of the 8 surrounding cells contain mines. Cells with no adjacent mines are automatically revealed in a flood fill. Flag all mines and reveal all safe cells to win.

---

## ✨ Features

- 9×9 grid with 10 randomly placed mines
- Flood-fill auto-reveal for empty cells
- Mine counter and flagging system
- Win and loss detection with end-game overlay
- One-tap restart at any time

---

## 🏗️ Architecture

The project follows **MVVM** (Model-View-ViewModel) to keep game logic cleanly separated from the UI.

```
MinesweeperApp/
├── Models/
│   └── Cell.cs              # Cell state: revealed, flagged, mine, adjacency count
├── ViewModels/
│   └── GameViewModel.cs     # Game logic, commands, INotifyPropertyChanged
├── Views/
│   └── MainPage.xaml        # Grid layout and gesture recognizers
│   └── MainPage.xaml.cs     # Code-behind (minimal)
└── MauiProgram.cs           # App entry point and DI setup
```

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| UI Framework | [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/) |
| Language | C# 12 |
| UI Markup | XAML |
| Pattern | MVVM |
| Target | Android (API 21+) |

---

## 🚀 Getting Started

### Prerequisites

Make sure the following are installed before building:

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 9.0 (Preview) | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Android SDK | API 21+ | [Android Studio](https://developer.android.com/studio) |
| JDK | 17 | [Microsoft JDK 17](https://aka.ms/download-jdk/microsoft-jdk-17-windows-x64.msi) |
| IDE | VS Code + C# Dev Kit | [marketplace.visualstudio.com](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) |

### Installation

```bash
# Clone the repository
git clone https://github.com/your-username/minesweeper-maui.git
cd minesweeper-maui

# Restore dependencies
dotnet restore

# Install MAUI workload (first time only)
dotnet workload install maui-android
```

### Running on Android

**Option A — Physical device (USB)**
```bash
# Enable Developer Mode and USB Debugging on your device, then:
dotnet build -t:Run -f net9.0-android
```

**Option B — Emulator**
```bash
# Start an AVD from Android Studio, then:
dotnet build -t:Run -f net9.0-android
```

**Option C — Build APK for sideloading**
```bash
dotnet publish -f net9.0-android -c Release
# APK is output to bin/Release/net9.0-android/publish/
```

---

## 🔧 Configuration

Game constants are defined at the top of `GameViewModel.cs` and can be adjusted freely:

```csharp
private const int Rows      = 9;
private const int Cols      = 9;
private const int MineCount = 10;
```

---

## 🗺️ Roadmap

- [ ] Difficulty presets (Beginner / Intermediate / Expert)
- [ ] First-click safety guarantee (no mine on first tap)
- [ ] Best-time leaderboard with local persistence
- [ ] Haptic feedback on flag placement
- [ ] Animated mine reveal on loss

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
