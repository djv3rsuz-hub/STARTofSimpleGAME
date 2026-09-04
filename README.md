# STARTofSimpleGAME

A WPF-based game engine and RPG framework built with .NET 8. Currently in **active development (v0.0.1 → v0.5.0 alpha)** — building toward a full RPG system with deep stat mechanics, VFX, equipment, skills, and progression systems.

## Current State

**v0.0.1 → v0.1.0 — Core Engine & Foundation** *(current)*

| System | Status |
|--------|--------|
| 60fps game loop with DeltaTime physics | Done |
| Keyboard + Controller (Xbox/PS4/PS5) | Done |
| Acceleration/Deceleration movement | Done |
| AABB collision + resolution | Done |
| INI-based character config | Done |
| 12 RPG stats per character | Done |
| Debug console (30+ commands) | Done |
| JSON save/load system (5 slots) | Done |
| Tab-based UI with anime-style icons | Done |
| Custom tooltip system | Done |
| VFX system (Fire, Lightning, Laser, Smoke, Water) | Done |
| Scene lighting effects | Done |
| Window size presets (720p/900p/1080p) | Done |

## Roadmap

### v0.2.0 — Equipment & Inventory
- Equipment slots (Weapon, Armor, Accessory, etc.)
- Item database with rarity tiers
- Stat modifiers from gear (flat, %, scaling)
- Inventory UI with drag-and-drop
- Equipment comparison tooltips

### v0.3.0 — Skills & Abilities
- Skill tree / class system (Paragon-like branching)
- Active skills with cooldowns and mana costs
- Passive skills with stat bonuses
- Skill synergies and combo system
- Skill UI with hotbar

### v0.4.0 — Quests & Progression
- Quest system with objectives, rewards, chains
- XP and leveling with scaling curves
- Prestige / Paragon system for endgame
- Achievement tracking
- Quest log UI

### v0.5.0 — Advanced Combat & Resistance
- Damage types: Physical, Fire, Ice, Lightning, Poison, etc.
- Resistance system (per damage type)
- Critical hits, dodge, parry, counter mechanics
- Elemental interactions (weakness, immunity, absorption)
- Enemy AI with aggro, formations, abilities

### v0.6.0+ — Polish & Content
- Procedural generation (dungeons, loot tables)
- Multiple character classes with unique stat scaling
- Crafting system
- Trading/economy
- Sound effects and music integration
- Particle-based environmental effects (rain, snow, fog)
- Optical distortion effects (heat haze, water ripples, shockwaves)

## Features (Current)

### RPG Stats System
Each character has 12 configurable stats, all float-based (0.00–1.00):

| Stat | Description |
|------|-------------|
| HP | Health points |
| Defence | Damage reduction |
| Attack | Physical damage |
| AttackSpeed | Attack multiplier |
| Resistance | Elemental mitigation |
| DodgeChance | Evasion chance |
| CriticalChance | Crit probability |
| CriticalDamage | Crit damage multiplier |
| MainDamage | Primary damage scaling |
| SkillDamage | Skill damage scaling |
| ParryDamage | Parry counter damage |
| CounterDamage | Counter-attack damage |

### VFX System
Modular particle-based effects with presets:

- **Fire** — Standard, Big, Inferno, Torch presets
- **Lightning** — Branching bolts with flash effects
- **Laser** — Pulsing beams with glow layers
- **Smoke** — Standard, Cloud, Dark, Steam presets
- **Water** — Fountain, Drip, Ocean with splash physics

### Save/Load System
- 5 save slots with JSON serialization
- Captures: cube positions, velocity, stats, dash state, settings snapshot
- F5 = Quick Save, F9 = Quick Load
- Console: `save [slot]`, `load [slot]`, `saves`, `delsave <slot>`

### Debug Console
30+ commands for live tweaking:

```
help, collision, fps, objects, position, speed, accel, decel,
quality, vsync, shadows, volume, window, save, load, saves,
delsave, vfx fire, vfx lightning, vfx laser, vfx smoke,
vfx water, vfx clear, vfx info, log, reset, options, clear
```

### UI System
- Left sidebar with anime-style icon tabs (Stats, New Game, Save, Load, Options, Exit)
- Inline options panel (graphics, audio, window size, gameplay)
- Save/Load slot selection
- Custom cursor-following tooltips
- Game screen: 1600x900 (configurable)

## Controls

| Input | Action |
|-------|--------|
| W/A/S/D | Move blue cube (player) |
| F3 | Toggle collision debug |
| F5 | Quick Save |
| F9 | Quick Load |
| ESC | Open options / close panels |
| Controller Left Stick | Move blue cube |
| Controller Start | Open options |

## Configuration

### gamesettings.cfg
Window, audio, gameplay, and graphics settings. Auto-created on first run.

### Config/charsettings.ini
Per-character config with movement params + RPG stats + AI toggles:

```ini
[PlayerCube]
Name = Blue Cube
StartX = 100
StartY = 310
Size = 60
MoveSpeed = 720
AccelerationSpeed = 9000
DecelerationSpeed = 0
Controllable = true
Color = DodgerBlue
ClampToScreen = true
DashEnabled = true

# RPG Stats (0.00 = 0%, 1.00 = 100%)
HP = 1.00
Defence = 0.80
Attack = 0.90
AttackSpeed = 1.20
Resistance = 0.70
DodgeChance = 0.15
CriticalChance = 0.25
CriticalDamage = 1.50
MainDamage = 0.85
SkillDamage = 0.80
ParryDamage = 0.60
CounterDamage = 0.70
```

## Project Structure

```
STARTofSimpleWPFGame/
├── App.xaml / App.xaml.cs              — Application entry + styles
├── MainWindow.xaml / .cs               — UI shell, tabs, console, VFX init
├── Game/
│   ├── GameObject.cs                   — Base class (Position, Velocity, Bounds)
│   ├── GameEngine.cs                   — 60fps loop, collision, rendering
│   └── Cube.cs                         — Character with stats, actions, movement
├── Input/
│   ├── InputManager.cs                 — Keyboard + mouse state
│   └── ControllerManager.cs            — XInput polling (~120Hz)
├── Config/
│   ├── IniParser.cs                    — INI file reader/writer
│   ├── CharSettings.cs                 — Character data loader
│   ├── CharacterStats.cs               — Stats + action toggles
│   └── charsettings.ini                — Character definitions
├── Settings/
│   └── GameSettings.cs                 — Game settings manager
├── Logging/
│   └── Logger.cs                       — File + Debug + UI logging
├── SaveSystem/
│   ├── SaveManager.cs                  — JSON save/load engine
│   └── SaveData.cs                     — Serializable data models
├── UI/
│   ├── GameIcons.cs                    — Anime-style icon factory
│   ├── TooltipManager.cs               — Custom cursor tooltips
│   └── TooltipDescriptions.cs          — Tooltip text constants
├── VFX/
│   ├── Particle.cs                     — Base particle + VFXEffect
│   ├── VFXSystem.cs                    — Effect manager + pooling
│   ├── FireEffect.cs                   — Fire with presets
│   ├── LightningEffect.cs              — Branching bolts
│   ├── LaserEffect.cs                  — Pulsing beams
│   ├── SmokeEffect.cs                  — Smoke/cloud particles
│   └── WaterEffect.cs                  — Fountain/splash physics
├── Saves/                              — Save slot JSON files
├── openmefirst.md                      — Developer docs
└── STARTofSimpleWPFGame.csproj         — .NET 8, SharpDX.XInput
```

## Build

```bash
dotnet build --configuration Debug
```

Requires .NET 8 SDK. NuGet: `SharpDX.XInput 4.2.0`

## Design Philosophy

- **Plug-and-play systems** — every feature is modular and expandable
- **Data-driven** — stats, configs, saves all externalized (INI/JSON)
- **Performance-first** — cached brushes, quality-tiered rendering, object pooling
- **Developer-friendly** — live console, debug visualization, hot-reload configs
- **Structured scaling** — stats, skills, equipment all designed to scale together

## License

Public domain. Use however you want.
