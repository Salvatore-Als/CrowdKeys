# CrowdKeys

**[crowdkeys.dev](https://crowdkeys.dev)** — v1.1.0

CrowdKeys maps Twitch channel point rewards to keyboard/mouse actions on your PC. When a viewer redeems a reward, the app executes the configured action sequence automatically — no third-party software or browser extension needed.

---

## Features

### Reward bindings
Create one binding per reward. Each binding holds an ordered list of steps that execute sequentially when the reward is redeemed. Bindings can be individually enabled or disabled, and each can have an optional description.

### Step types

| Type | Description |
|------|-------------|
| **Key** | Keyboard combo (Ctrl / Shift / Alt / Win + key). Repeatable with optional delay between presses. |
| **Pause** | Wait a fixed number of milliseconds before the next step. |
| **Mouse click** | Left, right, or middle click. Repeatable. |
| **Mouse scroll** | Scroll up or down by a configurable number of ticks. |
| **Mouse move** | Relative pixel movement from current cursor position. Optional speed (ms). |
| **Screen effect** | Full-screen visual effect rendered on top of all windows for a set duration. Windows only. |

### Screen effects (Windows only)

14 effects available: Horizontal Mirror, Split Screen ×2, Split Screen ×4, Blur, Screen Shake, Vertical Flip, Invert Colors, Grayscale, Pixelate, Zoom ×1.6, RGB Aberration, Glitch, CRT Scanlines, Zoom Pulse.

Effects use DXGI Desktop Duplication (falls back to GDI). The overlay window is excluded from capture to avoid feedback loops.

### Activity log
Real-time log of all connection events, reward redemptions, and errors. Clearable.

### Auto-reconnect
On disconnection, the app retries automatically with exponential backoff (up to 5 attempts). Handles expired tokens and re-authentication transparently.

### Pause / Resume
Pause the listener without disconnecting. Resume reconnects to the EventSub stream.

### Multi-language
UI available in **English**, **French**, **German**, and **Italian**. Language switches live — no restart needed. Selector available on both the login screen and the main window.

### Multi-account
Multiple Twitch accounts supported. Each account's bindings and tokens are stored separately. The last used account is restored on startup.

---

## Authentication

Sign in via **OAuth Device Flow**:
1. Click **Connect** — the app requests a device code from Twitch
2. A code appears in the UI — enter it at [twitch.tv/activate](https://www.twitch.tv/activate)
3. The app polls for approval and connects automatically

No password is ever entered in the app. Required scope: `channel:read:redemptions`.

---

## Data storage

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\CrowdKeys\` |
| macOS | `~/Library/Application Support/CrowdKeys/` |

- `config.json` — last used account ID
- `profiles/<userId>.json` — tokens and bindings per account

---

## Requirements

- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (or use the self-contained build)
- A **Twitch Client ID** — register an app on [dev.twitch.tv](https://dev.twitch.tv/console/apps)
  - Type: **Other** / Category: **Developer Tool**
  - No OAuth redirect URL needed

---

## Building from source

Copy `build.config.private.props.exemple` to `build.config.private.props` and add your Client ID:

```xml
<Project>
  <PropertyGroup>
    <TwitchClientId>YOUR_CLIENT_ID_HERE</TwitchClientId>
  </PropertyGroup>
</Project>
```

> `build.config.private.props` is git-ignored. Never commit your Client ID.

```bash
# Run in development
dotnet run

# Publish self-contained single-file exe (Windows x64)
dotnet publish -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true
```

Output: `bin/Release/net8.0/win-x64/publish/CrowdKeys.exe`

---

## Tech stack

| | |
|---|---|
| **Avalonia 12** | Cross-platform UI — MVVM, compiled bindings |
| **CommunityToolkit.Mvvm** | `ObservableProperty` / `RelayCommand` source generators |
| **Twitch EventSub WebSocket** | Real-time reward redemption events |
| **OAuth Device Flow** | Token-based auth without a local HTTP server |
| **SkiaSharp** | Screen effect rendering |
| **DXGI Desktop Duplication** | High-performance Windows screen capture |
| **GDI BitBlt** | Fallback screen capture for older GPU drivers |
