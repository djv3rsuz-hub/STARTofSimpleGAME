# STARTofSimpleGAME

A lightweight WPF game engine built with .NET 8, featuring 60fps rendering, controller support, acceleration physics, and a built-in debug console.

## Features

- **60fps game loop** with DeltaTime-based physics
- **Keyboard + Controller** support (Xbox, PS4/PS5 via DS4Windows)
- **Acceleration/Deceleration** movement physics
- **Debug Console** with live commands
- **Collision Debug** visualization (F3 toggle)
- **INI-based character settings** - edit in Notepad
- **Game settings** - configurable window, audio, gameplay, colors
- **Logger** - file + in-app log panel

## Controls

| Input | Action |
|-------|--------|
| W/A/S/D or Arrows | Move blue cube |
| F3 | Toggle collision debug |
| ESC | Exit game |
| Controller Left Stick / D-Pad | Move blue cube |

## Debug Console Commands

```
help              - Show all commands
collision / col   - Toggle collision debug (F3)
fps               - Toggle FPS display
objects / obj     - List all game objects
position / pos    - Show player position + velocity
speed <number>    - Set player move speed
accel <number>    - Set player acceleration
decel <number>    - Set player deceleration
clear             - Clear console
log <message>     - Write to log file
save              - Save settings to disk
reset             - Reset player to start position
```

## Configuration

### gamesettings.cfg
Auto-created on first run next to the exe. Controls window size, audio, gameplay toggles, colors.

```ini
[Display]
WindowWidth = 1600
WindowHeight = 900
GameScreenWidth = 1280
GameScreenHeight = 720

[Gameplay]
DefaultMoveSpeed = 350
ShowFps = True
ShowDebugInfo = True
ShowCollision = False

[Colors]
BackgroundColor = #FF000000
PlayerColor = #FF1E90FF
```

### Config/charsettings.ini
Define characters with position, size, speed, acceleration, color. Add new `[Sections]` to create more characters.

```ini
[PlayerCube]
Name = Blue Cube
StartX = 100
StartY = 310
Size = 80
MoveSpeed = 350
AccelerationSpeed = 1200
DecelerationSpeed = 900
Controllable = true
Color = DodgerBlue
ClampToScreen = true
```

Colors accept WPF named colors (`DodgerBlue`, `LimeGreen`, `Crimson`) or hex `#AARRGGBB`.

## Build

```bash
dotnet build --configuration Release
```

Requires .NET 8 SDK. NuGet dependency: `SharpDX.XInput 4.2.0` (controller support).

## Project Structure

```
STARTofSimpleWPFGame/
  App.xaml / App.xaml.cs          - Application entry
  MainWindow.xaml / .cs           - UI shell + debug console
  Game/
    GameObject.cs                 - Base class (Position, Velocity, Bounds, Collision)
    GameEngine.cs                 - 60fps game loop, rendering
    Cube.cs                       - Player/enemy cube with movement physics
  Input/
    InputManager.cs               - Keyboard + mouse state tracking
    ControllerManager.cs          - XInput controller polling (~120Hz)
  Config/
    IniParser.cs                  - INI file reader/writer
    CharSettings.cs               - Character data loader
    charsettings.ini              - Character definitions
  Settings/
    GameSettings.cs               - Game settings manager
  Logging/
    Logger.cs                     - File + Debug + UI logging
```

## License

Public domain. Use however you want.
