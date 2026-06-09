# CrowdKeys

Associe des récompenses de points de chaîne Twitch à des actions clavier/souris sur votre PC. Quand un viewer rachète une récompense, l'application exécute automatiquement la séquence d'actions configurée.

## Fonctionnement

1. Vous vous connectez via OAuth (flux device - aucun mot de passe saisi dans l'app)
2. L'app écoute votre chaîne via **Twitch EventSub WebSocket**
3. À chaque rachat, elle cherche un binding dont le nom correspond
4. Elle exécute les étapes dans l'ordre : touches, clics, scroll, déplacement, pauses

### Types d'étapes disponibles

| Type | Description |
|------|-------------|
| **Touche** | Combinaison clavier (Ctrl/Shift/Alt/Win + touche), répétable |
| **Pause** | Attente en millisecondes |
| **Clic souris** | Clic gauche / droit / molette, répétable |
| **Scroll souris** | Scroll haut ou bas d'un nombre de crans |
| **Déplacement souris** | Déplacement relatif en pixels, avec vitesse optionnelle |

Les paramètres sont sauvegardés automatiquement dans `%APPDATA%\CrowdKeys\settings.json`.

---

## Prérequis

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Un **Client ID Twitch** (application enregistrée sur [dev.twitch.tv](https://dev.twitch.tv/console/apps))
  - Type d'application : **Autre**
  - Catégorie : **Outil de développement**
  - Aucune URL de redirection OAuth n'est nécessaire (flux device)

---

## Configuration

Renseignez votre Client ID dans `build.config.props` avant de compiler :

```xml
<Project>
  <PropertyGroup>
    <AppVersion>1.0.0</AppVersion>
    <TwitchClientId>VOTRE_CLIENT_ID_ICI</TwitchClientId>
    <EventSubWssUrl>wss://eventsub.wss.twitch.tv/ws</EventSubWssUrl>
  </PropertyGroup>
</Project>
```

---

## Build

```bash
# Restaurer les dépendances
dotnet restore

# Lancer en mode développement
dotnet run

# Compiler en release
dotnet build -c Release

# Publier un exécutable autonome (Windows x64)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

L'exécutable publié se trouve dans `bin/Release/net8.0/win-x64/publish/`.

---

## Stack technique

- **Avalonia 12** - UI cross-platform (MVVM, bindings compilés)
- **CommunityToolkit.Mvvm** - source generators pour ObservableProperty / RelayCommand
- **Twitch EventSub WebSocket** - réception des événements en temps réel
- **OAuth Device Flow** - authentification sans redirection HTTP locale
