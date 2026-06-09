# CrowdKeys

**[crowdkeys.dev](https://crowdkeys.dev)**

Associe des récompenses de points de chaîne Twitch à des actions clavier/souris sur votre PC. Quand un viewer rachète une récompense, l'application exécute automatiquement la séquence d'actions configurée.

## Fonctionnement

1. Vous vous connectez via **OAuth Device Flow** (aucun mot de passe saisi dans l'app)
2. L'app s'abonne à votre chaîne via **Twitch EventSub WebSocket** (`channel.channel_points_custom_reward_redemption.add`)
3. À chaque rachat, elle cherche un binding dont le nom correspond à la récompense
4. Elle exécute les étapes dans l'ordre : touches, clics, scroll, déplacement, pauses

### Types d'étapes disponibles

| Type | Description |
|------|-------------|
| **Touche** | Combinaison clavier (Ctrl/Shift/Alt/Win + touche), répétable avec délai optionnel |
| **Pause** | Attente en millisecondes |
| **Clic souris** | Clic gauche / droit / molette, répétable |
| **Scroll souris** | Scroll haut ou bas d'un nombre de crans |
| **Déplacement souris** | Déplacement relatif en pixels, avec vitesse optionnelle |

### Multi-compte

L'application supporte plusieurs comptes Twitch. Les paramètres de chaque compte sont sauvegardés séparément dans `%APPDATA%\CrowdKeys\settings_<userId>.json`. Le dernier compte utilisé est mémorisé dans `%APPDATA%\CrowdKeys\global_config.json`.

---

## Prérequis

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Un **Client ID Twitch** (application enregistrée sur [dev.twitch.tv](https://dev.twitch.tv/console/apps))
  - Type d'application : **Autre**
  - Catégorie : **Outil de développement**
  - Aucune URL de redirection OAuth n'est nécessaire (flux device)

---

## Configuration

Copiez `build.config.private.props.exemple` en `build.config.private.props` et renseignez votre Client ID :

```bash
cp build.config.private.props.exemple build.config.private.props
```

```xml
<Project>
  <PropertyGroup>
    <TwitchClientId>VOTRE_CLIENT_ID_ICI</TwitchClientId>
  </PropertyGroup>
</Project>
```

> `build.config.private.props` est ignoré par git. Ne commitez jamais votre Client ID.

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

- **Avalonia 12** — UI cross-platform (MVVM, bindings compilés)
- **CommunityToolkit.Mvvm** — source generators pour `ObservableProperty` / `RelayCommand`
- **Twitch EventSub WebSocket** — réception des événements en temps réel
- **OAuth Device Flow** — authentification sans redirection HTTP locale (scope : `channel:read:redemptions`)
