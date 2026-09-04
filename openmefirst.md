# SimpleWPFGame - Project Guide

> Open this file first before making any changes to the project.

## Quick Start

```bash
dotnet build -c Debug    # Always build debug
dotnet run               # Launch game
```

**Default Controls:** WASD = Move, Space/B = Dash, F3 = Collision Debug, F5 = Quick Save, F9 = Quick Load, ESC = Options

---

## Project Structure

```
STARTofSimpleWPFGame/
├── Config/                 # Character data (INI-based, user-editable)
│   ├── CharSettings.cs     # INI parser + CharacterData model
│   ├── CharacterStats.cs   # RPG stats + AI action toggles
│   ├── charsettings.ini    # All cube stats (edit in Notepad)
│   └── IniParser.cs        # Generic INI file reader
├── Game/                   # Core engine
│   ├── GameEngine.cs       # 60fps loop, collision, rendering
│   ├── GameObject.cs       # Base class (position, bounds, collision)
│   └── Cube.cs             # Player/enemy/NPC with movement + dash + stats
├── Input/                  # Controller + keyboard/mouse
│   ├── ControllerManager.cs  # XInput via SharpDX (120Hz polling)
│   └── InputManager.cs     # Keyboard + mouse state tracking
├── Logging/
│   └── Logger.cs           # File + UI logging
├── SaveSystem/             # Save/Load system
│   ├── SaveData.cs         # Serializable data models
│   └── SaveManager.cs      # Save/Load/Apply logic
├── Settings/
│   └── GameSettings.cs     # All settings (auto-creates gamesettings.cfg)
├── UI/                     # UI components
│   ├── TooltipManager.cs   # Custom cursor-following tooltip
│   └── TooltipDescriptions.cs  # All tooltip text (centralized)
├── MainWindow.xaml         # Window layout + options menu
├── MainWindow.xaml.cs      # All UI logic + console commands
├── App.xaml                # Button styles (GameButton)
└── STARTofSimpleWPFGame.csproj
```

---

## Architecture Principles

### 1. Singleton Pattern
Used for managers that need global access:
```csharp
GameSettings.Instance    // Settings (load/save)
GameEngine.Instance      // Game loop + rendering
InputManager.Instance    // Keyboard/mouse state
ControllerManager.Instance // Gamepad state
TooltipManager.Instance  // Tooltip display
SaveManager.Instance     // Save/load system
```

### 2. Data-Driven Design
All character stats live in `charsettings.ini`, not hardcoded:
```ini
[PlayerCube]
Size = 60
MoveSpeed = 720
HP = 1.00
Attack = 0.15
DashEnabled = true
```
Edit in Notepad, restart game to apply.

### 3. Event-Driven UI
UI updates on a 30fps timer, not every frame:
```csharp
_uiUpdateTimer.Interval = TimeSpan.FromMilliseconds(33);
_uiUpdateTimer.Tick += UpdateUI;
```

### 4. Stats as Float (0.00 format)
All RPG stats use float 0.00 = 0%, 1.00 = 100%:
```csharp
Stats.HP = 1.00f;        // 100% health
Stats.Attack = 0.15f;    // 15% attack
Stats.CriticalDamage = 1.50f;  // 150% crit multiplier
```

---

## How to Add New Features

### Add a New Stat
1. Add property to `Config/CharacterStats.cs`:
   ```csharp
   public float NewStat { get; set; } = 0.00f;
   ```
2. Add to `Config/charsettings.ini` under each cube:
   ```ini
   NewStat = 0.05
   ```
3. Add loader in `Config/CharSettings.cs`:
   ```csharp
   NewStat = _ini.GetFloat(sectionName, "NewStat", 0.00f)
   ```
4. Add to `Cube.cs` Stats property if needed
5. Add display in `MainWindow.xaml` left sidebar
6. Add tooltip in `UI/TooltipDescriptions.cs`
7. Add to `SaveSystem/SaveData.cs` StatsSaveData

### Add a New Console Command
1. Add help text in `RunConsoleCommand` case `"help":`
2. Add case in the switch statement:
   ```csharp
   case "mycommand":
       // do something
       ConsoleWrite("Result", "#FF00FF88");
       break;
   ```

### Add a New AI Action Toggle
1. Add to `Config/CharacterStats.cs` CharacterActionToggles:
   ```csharp
   public bool NewActionEnabled { get; set; } = true;
   ```
2. Add to `charsettings.ini`:
   ```ini
   NewActionEnabled = true
   ```
3. Add loader in `Config/CharSettings.cs`
4. Check toggle in `Cube.cs` before allowing the action
5. Add UI display in left sidebar

### Add a New Options Menu Item
1. Add RowDefinition in `MainWindow.xaml` options panel
2. Add Grid with label + control (Button/Slider)
3. Add ToolTip from `TooltipDescriptions.cs`
4. Add click/changed handler in `MainWindow.xaml.cs`
5. Add save/load in `GameSettings.cs`

---

## Key Files to Know

| File | What it does | When to edit |
|------|-------------|--------------|
| `charsettings.ini` | Character stats, speeds, sizes | Changing cube properties |
| `GameSettings.cs` | All game settings | Adding new settings |
| `Cube.cs` | Movement, dash, rendering | Changing cube behavior |
| `GameEngine.cs` | Game loop, collision | Changing game rules |
| `MainWindow.xaml` | UI layout | Changing UI |
| `MainWindow.xaml.cs` | UI logic, console | Adding commands/features |
| `TooltipDescriptions.cs` | All tooltip text | Adding/changing tooltips |
| `SaveData.cs` | Save file format | Adding new save fields |
| `SaveManager.cs` | Save/load logic | Changing save behavior |

---

## Save/Load System

### How it works
- **F5** = Quick Save to slot 1 (JSON file in `Saves/` folder)
- **F9** = Quick Load from slot 1
- Console: `save [slot]`, `load [slot]`, `saves`, `delsave <slot>`
- Saves full game state: positions, velocities, stats, settings, dash state
- Pause game before saving for clean snapshot

### Save file format (JSON)
```json
{
  "version": "1.0",
  "saveTimestamp": "2026-09-04 21:30:00",
  "gameTime": 125.5,
  "player": { "posX": 100, "posY": 310, "stats": { "hp": 1.0, ... } },
  "enemy": { ... },
  "greenCube": { ... },
  "settings": { "windowWidth": 1920, ... }
}
```

---

## Common Pitfalls

1. **`InputManager` name conflict** - Use fully qualified `System.Windows.Input` in MainWindow.xaml.cs
2. **`Vector.Normalize` is void** - Use `v / v.Length` instead
3. **Always build Debug** - `<Configuration>` defaults to Debug in csproj
4. **INI changes require restart** - Settings load once at startup
5. **Window resize** - Uses Win32 `SetWindowPos`, not WPF resize

---

## Performance Notes

- Game loop: 60fps via DispatcherTimer (1ms interval)
- UI updates: 30fps (33ms interval)
- Controller polling: 120Hz (separate timer)
- Rendering: Cached pens/brushes, quality-tiered shadows
- Collision: AABB with resolution, no spatial partitioning (3 objects is fine)

---

## Git Workflow

```bash
git add -A
git commit -m "Description of changes"
git push
```

Always build and verify before committing. Never commit secrets or keys.
