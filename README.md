# CrowdKeys

**[crowdkeys.dev](https://crowdkeys.dev)** — v1.1.0

Maps Twitch channel point rewards to keyboard/mouse actions on your PC. When a viewer redeems a reward, the app automatically executes the configured action sequence.

## How it works

1. Sign in via **OAuth Device Flow** (no password entered in the app)
2. The app subscribes to your channel via **Twitch EventSub WebSocket** (`channel.channel_points_custom_reward_redemption.add`)
3. On each redemption, it finds the binding matching the reward name
4. It executes the steps in order: keys, clicks, scroll, mouse move, pauses, screen effects

### Available step types

| Type | Description |
|------|-------------|
| **Key** | Keyboard combo (Ctrl/Shift/Alt/Win + key), repeatable with optional delay |
| **Pause** | Wait in milliseconds |
| **Mouse click** | Left / right / middle click, repeatable |
| **Mouse scroll** | Scroll up or down by a number of ticks |
| **Mouse move** | Relative pixel movement with optional speed |
| **Screen effect** | Full-screen visual effect (mirror, blur, glitch, scanlines…) for a set duration |

### Available screen effects (Windows only)

Horizontal Mirror, Split Screen x2/x4, Blur, Screen Shake, Vertical Flip, Invert Colors, Grayscale, Pixelate, Zoom x1.6, RGB Aberration, Glitch, CRT Scanlines, Zoom Pulse.

### Multi-language

UI available in **French**, **English**, **German** and **Italian**. Language can be changed at the bottom of the main window.

### Multi-account

Multiple Twitch accounts are supported. Each account's settings are saved separately in `%APPDATA%\CrowdKeys\profiles\<userId>.json`. The last used account is stored in `%APPDATA%\CrowdKeys\config.json`.

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A **Twitch Client ID** (app registered on [dev.twitch.tv](https://dev.twitch.tv/console/apps))
  - Application type: **Other**
  - Category: **Developer Tool**
  - No OAuth redirect URL needed (device flow)

---

## Setup

Copy `build.config.private.props.exemple` to `build.config.private.props` and fill in your Client ID:

```bash
cp build.config.private.props.exemple build.config.private.props
```

```xml
<Project>
  <PropertyGroup>
    <TwitchClientId>YOUR_CLIENT_ID_HERE</TwitchClientId>
  </PropertyGroup>
</Project>
```

> `build.config.private.props` is git-ignored. Never commit your Client ID.

---

## Build

```bash
# Restore dependencies
dotnet restore

# Run in development
dotnet run

# Build release
dotnet build -c Release

# Publish self-contained single-file executable (Windows x64)
dotnet publish -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true
```

Output: `bin/Release/net8.0/win-x64/publish/`.

---

## Tech stack

- **Avalonia 12** — cross-platform UI (MVVM, compiled bindings)
- **CommunityToolkit.Mvvm** — source generators for `ObservableProperty` / `RelayCommand`
- **Twitch EventSub WebSocket** — real-time event reception
- **OAuth Device Flow** — auth without local HTTP redirect (scope: `channel:read:redemptions`)
- **SkiaSharp** — screen effect rendering
- **DXGI Desktop Duplication / GDI** — Windows screen capture for visual effects
