using System.ComponentModel;
using System.Globalization;

namespace CrowdKeys.Localization;

public sealed record LanguageOption(string Code, string Label)
{
    public override string ToString() => Label;
}

public sealed class Loc : INotifyPropertyChanged
{
    public static readonly Loc Instance = new();
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _lang = "fr";
    public string CurrentLang => _lang;

    public static readonly IReadOnlyList<LanguageOption> Languages =
    [
        new("fr", "🇫🇷 Français"),
        new("en", "🇬🇧 English"),
        new("de", "🇩🇪 Deutsch"),
        new("it", "🇮🇹 Italiano"),
    ];

    public string this[string key]
    {
        get
        {
            if (_strings.TryGetValue(_lang, out var dict) && dict.TryGetValue(key, out var val))
                return val;
            
            if (_strings.TryGetValue("fr", out var fb) && fb.TryGetValue(key, out var fbVal))
                return fbVal;
            
            return $"[{key}]";
        }
    }

    public void SetLanguage(string lang)
    {
        if (_lang == lang)
            return;

        _lang = lang;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLang)));
    }

    private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["fr"] = new()
        {
            // Login
            ["Login_Subtitle"]      = "Connectez votre chaîne Twitch pour commencer.",
            ["Login_EnterCode"]     = "Entrez ce code sur twitch.tv/activate",
            ["Login_Button"]        = "Se connecter avec Twitch",
            ["Login_WaitingAuth"]   = "En attente de l'autorisation sur Twitch…",
            ["Copyright"]         = "Copyright © 2026 Kriax. Tous droits réservés.",

            // Status
            ["Status_Paused"]     = "En pause",
            ["Status_Disconnected"] = "Déconnecté",

            // Buttons
            ["Btn_Disconnect"]    = "Déconnecter",
            ["Btn_Connect"]       = "Se connecter",
            ["Btn_Connecting"]    = "Connexion…",
            ["Btn_Reconnecting"]  = "Reconnexion…",
            ["Btn_WaitingCode"]   = "En attente du code…",
            ["Btn_Pause"]         = "Pause",
            ["Btn_Resume"]        = "Reprendre",

            // Bindings panel
            ["Bindings_Title"]    = "BINDINGS",
            ["Bindings_Refresh"]  = "Actualiser la liste des rewards",
            ["Bindings_Steps"]    = "étape(s)",
            ["Bindings_Orphan"]   = "Reward inexistant",
            ["Bindings_Active"]   = "ACTIF",
            ["Bindings_Inactive"] = "INACTIF",
            ["Bindings_Select"]   = "Sélectionnez un reward…",
            ["Bindings_Add"]      = "Ajouter",

            // Activity log panel
            ["Log_Title"]         = "ACTIVITÉ",
            ["Log_Empty"]         = "Aucune activité récente.",
            ["Log_Clear"]         = "Effacer",

            // Step editor
            ["Step_Title"]        = "SÉQUENCE DE TOUCHES",
            ["Step_Description"]  = "Description du binding (optionnel)…",
            ["Step_SelectBinding"] = "Sélectionnez un binding",
            ["Step_TypeKey"]      = "TOUCHE",
            ["Step_TypePause"]    = "PAUSE",
            ["Step_TypeClick"]    = "CLIC SOURIS",
            ["Step_TypeScroll"]   = "MOLETTE",
            ["Step_TypeMove"]     = "DÉPLACEMENT",
            ["Step_TypeEffect"]   = "EFFET ÉCRAN",
            ["Step_TypeHold"]     = "MAINTIEN TOUCHE",
            ["Step_Iterations"]   = "ITÉRATIONS",
            ["Step_DelayBetween"] = "DÉLAI ENTRE (MS) • 0 = INSTANTANÉ",
            ["Step_Duration"]     = "DURÉE (MS)",
            ["Step_Button"]       = "BOUTON",
            ["Step_BtnLeft"]      = "GAUCHE",
            ["Step_BtnRight"]     = "DROIT",
            ["Step_BtnMiddle"]    = "MILIEU",
            ["Step_Direction"]    = "DIRECTION",
            ["Step_ScrollUp"]     = "⬆ HAUT",
            ["Step_ScrollDown"]   = "⬇ BAS",
            ["Step_Quantity"]     = "QUANTITÉ",
            ["Step_Speed"]        = "VITESSE (MS) • 0 = INSTANTANÉ",
            ["Step_Effect"]       = "EFFET",
            ["Step_AddKey"]       = "Touche",
            ["Step_AddPause"]     = "Pause",
            ["Step_AddClick"]     = "Clic",
            ["Step_AddScroll"]    = "Molette",
            ["Step_AddMove"]      = "Déplacer",
            ["Step_AddEffect"]    = "Effet écran",
            ["Step_AddHold"]      = "Maintien",
            ["Step_HoldDuration"] = "DURÉE MAINTIEN (MS)",
            ["Step_ModeNormal"]   = "NORMAL",
            ["Step_ModeHeld"]     = "MAINTENU",
            ["Step_KeyPlaceholder"] = "Touche…",

            // Monitor selector
            ["Monitor_Screen"]    = "Écran",
            ["Monitor_Label"]     = "Écran :",

            // Screen effects
            ["Effect_Mirror"]     = "Miroir Horizontal",
            ["Effect_Split2"]     = "Split Écran x2",
            ["Effect_Split4"]     = "Split Écran x4",
            ["Effect_Blur"]       = "Flou (Blur)",
            ["Effect_Shake"]      = "Screen Shake",
            ["Effect_Flip"]       = "Flip Vertical",
            ["Effect_Invert"]     = "Inversion Couleurs",
            ["Effect_Grayscale"]  = "Noir & Blanc",
            ["Effect_Pixelate"]   = "Pixelisé",
            ["Effect_ZoomIn"]     = "Zoom x1.6",
            ["Effect_Chroma"]     = "Aberration RGB",
            ["Effect_Glitch"]     = "Glitch",
            ["Effect_Scanlines"]  = "Scanlines CRT",
            ["Effect_ZoomPulse"]  = "Zoom Pulsé",

            // Log messages
            ["Log_ConnectRetry"]           = "Connexion initiale échouée - nouvelle tentative…",
            ["Log_SessionExpiredConnect"]   = "Session expirée — cliquez sur Se connecter pour vous reconnecter.",
            ["Log_ConnectFailed"]           = "Connexion initiale échouée : {0}",
            ["Log_SessionExpiredReauth"]    = "Session expirée - nouvelle authentification requise.",
            ["Log_AuthCancelled"]           = "Authentification annulée.",
            ["Log_Error"]                   = "Erreur : {0}",
            ["Log_Disconnected"]            = "Déconnecté.",
            ["Log_ListenerPaused"]          = "Listener en pause.",
            ["Log_ResumeError"]             = "Erreur lors de la reprise : {0}",
            ["Log_AuthStarted"]             = "Démarrage de l'authentification Twitch…",
            ["Log_EnterCode"]               = "Entrez ce code sur twitch.tv/activate : {0}",
            ["Log_AuthSuccess"]             = "Authentification Twitch réussie.",
            ["Log_Reconnecting"]            = "Reconnexion dans {0}s… (tentative {1}/{2})",
            ["Log_AttemptFailed"]           = "Tentative {0} échouée : {1}",
            ["Log_ReconnectAbandoned"]      = "Reconnexion abandonnée après 5 tentatives.",
            ["Log_RewardsLoaded"]           = "{0} reward(s) chargé(s).",
            ["Log_RewardsError"]            = "Erreur chargement rewards : {0}",
            ["Log_PreviewClosed"]           = "⚠ La fenêtre de prévisualisation a été fermée. Les effets écran ne seront pas visibles dans l'app.",
            ["Log_EffectBypassed"]          = "⚠ Effet ignoré : la fenêtre de prévisualisation est fermée.",
        },

        ["en"] = new()
        {
            ["Login_Subtitle"]      = "Connect your Twitch channel to get started.",
            ["Login_EnterCode"]     = "Enter this code at twitch.tv/activate",
            ["Login_Button"]        = "Connect with Twitch",
            ["Login_WaitingAuth"]   = "Waiting for Twitch authorization…",
            ["Copyright"]         = "Copyright © 2026 Kriax. All rights reserved.",

            ["Status_Paused"]     = "Paused",
            ["Status_Disconnected"] = "Disconnected",

            ["Btn_Disconnect"]    = "Disconnect",
            ["Btn_Connect"]       = "Connect",
            ["Btn_Connecting"]    = "Connecting…",
            ["Btn_Reconnecting"]  = "Reconnecting…",
            ["Btn_WaitingCode"]   = "Waiting for code…",
            ["Btn_Pause"]         = "Pause",
            ["Btn_Resume"]        = "Resume",

            ["Bindings_Title"]    = "BINDINGS",
            ["Bindings_Refresh"]  = "Refresh rewards list",
            ["Bindings_Steps"]    = "step(s)",
            ["Bindings_Orphan"]   = "Reward not found",
            ["Bindings_Active"]   = "ACTIVE",
            ["Bindings_Inactive"] = "INACTIVE",
            ["Bindings_Select"]   = "Select a reward…",
            ["Bindings_Add"]      = "Add",

            ["Log_Title"]         = "ACTIVITY",
            ["Log_Empty"]         = "No recent activity.",
            ["Log_Clear"]         = "Clear",

            ["Step_Title"]        = "KEY SEQUENCE",
            ["Step_Description"]  = "Binding description (optional)…",
            ["Step_SelectBinding"] = "Select a binding",
            ["Step_TypeKey"]      = "KEY",
            ["Step_TypePause"]    = "PAUSE",
            ["Step_TypeClick"]    = "MOUSE CLICK",
            ["Step_TypeScroll"]   = "SCROLL",
            ["Step_TypeMove"]     = "MOVE",
            ["Step_TypeEffect"]   = "SCREEN EFFECT",
            ["Step_TypeHold"]     = "KEY HOLD",
            ["Step_Iterations"]   = "ITERATIONS",
            ["Step_DelayBetween"] = "DELAY BETWEEN (MS) • 0 = INSTANT",
            ["Step_Duration"]     = "DURATION (MS)",
            ["Step_Button"]       = "BUTTON",
            ["Step_BtnLeft"]      = "LEFT",
            ["Step_BtnRight"]     = "RIGHT",
            ["Step_BtnMiddle"]    = "MIDDLE",
            ["Step_Direction"]    = "DIRECTION",
            ["Step_ScrollUp"]     = "⬆ UP",
            ["Step_ScrollDown"]   = "⬇ DOWN",
            ["Step_Quantity"]     = "AMOUNT",
            ["Step_Speed"]        = "SPEED (MS) • 0 = INSTANT",
            ["Step_Effect"]       = "EFFECT",
            ["Step_AddKey"]       = "Key",
            ["Step_AddPause"]     = "Pause",
            ["Step_AddClick"]     = "Click",
            ["Step_AddScroll"]    = "Scroll",
            ["Step_AddMove"]      = "Move",
            ["Step_AddEffect"]    = "Screen effect",
            ["Step_AddHold"]      = "Hold",
            ["Step_HoldDuration"] = "HOLD DURATION (MS)",
            ["Step_ModeNormal"]   = "Normal",
            ["Step_ModeHeld"]     = "HELD",
            ["Step_KeyPlaceholder"] = "Key…",

            ["Monitor_Screen"]    = "Screen",
            ["Monitor_Label"]     = "Screen:",

            ["Effect_Mirror"]     = "Horizontal Mirror",
            ["Effect_Split2"]     = "Split Screen x2",
            ["Effect_Split4"]     = "Split Screen x4",
            ["Effect_Blur"]       = "Blur",
            ["Effect_Shake"]      = "Screen Shake",
            ["Effect_Flip"]       = "Vertical Flip",
            ["Effect_Invert"]     = "Invert Colors",
            ["Effect_Grayscale"]  = "Grayscale",
            ["Effect_Pixelate"]   = "Pixelate",
            ["Effect_ZoomIn"]     = "Zoom x1.6",
            ["Effect_Chroma"]     = "RGB Aberration",
            ["Effect_Glitch"]     = "Glitch",
            ["Effect_Scanlines"]  = "CRT Scanlines",
            ["Effect_ZoomPulse"]  = "Zoom Pulse",

            // Log messages
            ["Log_ConnectRetry"]           = "Initial connection failed - retrying…",
            ["Log_SessionExpiredConnect"]   = "Session expired — click Connect to reconnect.",
            ["Log_ConnectFailed"]           = "Initial connection failed: {0}",
            ["Log_SessionExpiredReauth"]    = "Session expired - new authentication required.",
            ["Log_AuthCancelled"]           = "Authentication cancelled.",
            ["Log_Error"]                   = "Error: {0}",
            ["Log_Disconnected"]            = "Disconnected.",
            ["Log_ListenerPaused"]          = "Listener paused.",
            ["Log_ResumeError"]             = "Error while resuming: {0}",
            ["Log_AuthStarted"]             = "Starting Twitch authentication…",
            ["Log_EnterCode"]               = "Enter this code at twitch.tv/activate: {0}",
            ["Log_AuthSuccess"]             = "Twitch authentication successful.",
            ["Log_Reconnecting"]            = "Reconnecting in {0}s… (attempt {1}/{2})",
            ["Log_AttemptFailed"]           = "Attempt {0} failed: {1}",
            ["Log_ReconnectAbandoned"]      = "Reconnection abandoned after 5 attempts.",
            ["Log_RewardsLoaded"]           = "{0} reward(s) loaded.",
            ["Log_RewardsError"]            = "Error loading rewards: {0}",
            ["Log_PreviewClosed"]           = "⚠ Preview window was closed. Screen effects won't be visible in the app.",
            ["Log_EffectBypassed"]          = "⚠ Effect skipped: preview window is closed.",
        },

        ["de"] = new()
        {
            ["Login_Subtitle"]      = "Verbinden Sie Ihren Twitch-Kanal, um loszulegen.",
            ["Login_EnterCode"]     = "Geben Sie diesen Code auf twitch.tv/activate ein",
            ["Login_Button"]        = "Mit Twitch verbinden",
            ["Login_WaitingAuth"]   = "Warte auf Twitch-Autorisierung…",
            ["Copyright"]         = "Copyright © 2026 Kriax. Alle Rechte vorbehalten.",

            ["Status_Paused"]     = "Pausiert",
            ["Status_Disconnected"] = "Getrennt",

            ["Btn_Disconnect"]    = "Trennen",
            ["Btn_Connect"]       = "Verbinden",
            ["Btn_Connecting"]    = "Verbinde…",
            ["Btn_Reconnecting"]  = "Erneut verbinden…",
            ["Btn_WaitingCode"]   = "Warte auf Code…",
            ["Btn_Pause"]         = "Pause",
            ["Btn_Resume"]        = "Fortsetzen",

            ["Bindings_Title"]    = "BINDINGS",
            ["Bindings_Refresh"]  = "Belohnungsliste aktualisieren",
            ["Bindings_Steps"]    = "Schritt(e)",
            ["Bindings_Orphan"]   = "Belohnung nicht gefunden",
            ["Bindings_Active"]   = "AKTIV",
            ["Bindings_Inactive"] = "INAKTIV",
            ["Bindings_Select"]   = "Belohnung auswählen…",
            ["Bindings_Add"]      = "Hinzufügen",

            ["Log_Title"]         = "AKTIVITÄT",
            ["Log_Empty"]         = "Keine aktuelle Aktivität.",
            ["Log_Clear"]         = "Leeren",

            ["Step_Title"]        = "TASTENFOLGE",
            ["Step_Description"]  = "Beschreibung (optional)…",
            ["Step_SelectBinding"] = "Binding auswählen",
            ["Step_TypeKey"]      = "TASTE",
            ["Step_TypePause"]    = "PAUSE",
            ["Step_TypeClick"]    = "MAUSKLICK",
            ["Step_TypeScroll"]   = "SCROLLEN",
            ["Step_TypeMove"]     = "BEWEGEN",
            ["Step_TypeEffect"]   = "BILDSCHIRMEFFEKT",
            ["Step_TypeHold"]     = "TASTE HALTEN",
            ["Step_Iterations"]   = "ITERATIONEN",
            ["Step_DelayBetween"] = "VERZÖGERUNG (MS) • 0 = SOFORT",
            ["Step_Duration"]     = "DAUER (MS)",
            ["Step_Button"]       = "TASTE",
            ["Step_BtnLeft"]      = "LINKS",
            ["Step_BtnRight"]     = "RECHTS",
            ["Step_BtnMiddle"]    = "MITTE",
            ["Step_Direction"]    = "RICHTUNG",
            ["Step_ScrollUp"]     = "⬆ OBEN",
            ["Step_ScrollDown"]   = "⬇ UNTEN",
            ["Step_Quantity"]     = "MENGE",
            ["Step_Speed"]        = "GESCHWINDIGKEIT (MS) • 0 = SOFORT",
            ["Step_Effect"]       = "EFFEKT",
            ["Step_AddKey"]       = "Taste",
            ["Step_AddPause"]     = "Pause",
            ["Step_AddClick"]     = "Klick",
            ["Step_AddScroll"]    = "Scrollen",
            ["Step_AddMove"]      = "Bewegen",
            ["Step_AddEffect"]    = "Bildschirmeffekt",
            ["Step_AddHold"]      = "Halten",
            ["Step_HoldDuration"] = "HALTEDAUER (MS)",
            ["Step_ModeNormal"]   = "Normal",
            ["Step_ModeHeld"]     = "HALTEN",
            ["Step_KeyPlaceholder"] = "Taste…",

            ["Monitor_Screen"]    = "Bildschirm",
            ["Monitor_Label"]     = "Bildschirm:",

            ["Effect_Mirror"]     = "Horizontaler Spiegel",
            ["Effect_Split2"]     = "Split Bildschirm x2",
            ["Effect_Split4"]     = "Split Bildschirm x4",
            ["Effect_Blur"]       = "Weichzeichner",
            ["Effect_Shake"]      = "Bildschirmzittern",
            ["Effect_Flip"]       = "Vertikaler Flip",
            ["Effect_Invert"]     = "Farben invertieren",
            ["Effect_Grayscale"]  = "Schwarzweiß",
            ["Effect_Pixelate"]   = "Verpixelt",
            ["Effect_ZoomIn"]     = "Zoom x1.6",
            ["Effect_Chroma"]     = "RGB-Aberration",
            ["Effect_Glitch"]     = "Glitch",
            ["Effect_Scanlines"]  = "CRT-Scanlines",
            ["Effect_ZoomPulse"]  = "Zoom-Puls",

            // Log messages
            ["Log_ConnectRetry"]           = "Erstverbindung fehlgeschlagen - erneuter Versuch…",
            ["Log_SessionExpiredConnect"]   = "Sitzung abgelaufen — klicken Sie auf Verbinden, um die Verbindung wiederherzustellen.",
            ["Log_ConnectFailed"]           = "Erstverbindung fehlgeschlagen: {0}",
            ["Log_SessionExpiredReauth"]    = "Sitzung abgelaufen - neue Authentifizierung erforderlich.",
            ["Log_AuthCancelled"]           = "Authentifizierung abgebrochen.",
            ["Log_Error"]                   = "Fehler: {0}",
            ["Log_Disconnected"]            = "Getrennt.",
            ["Log_ListenerPaused"]          = "Listener pausiert.",
            ["Log_ResumeError"]             = "Fehler beim Fortsetzen: {0}",
            ["Log_AuthStarted"]             = "Twitch-Authentifizierung wird gestartet…",
            ["Log_EnterCode"]               = "Geben Sie diesen Code auf twitch.tv/activate ein: {0}",
            ["Log_AuthSuccess"]             = "Twitch-Authentifizierung erfolgreich.",
            ["Log_Reconnecting"]            = "Erneut verbinden in {0}s… (Versuch {1}/{2})",
            ["Log_AttemptFailed"]           = "Versuch {0} fehlgeschlagen: {1}",
            ["Log_ReconnectAbandoned"]      = "Verbindungswiederherstellung nach 5 Versuchen aufgegeben.",
            ["Log_RewardsLoaded"]           = "{0} Belohnung(en) geladen.",
            ["Log_RewardsError"]            = "Fehler beim Laden der Belohnungen: {0}",
            ["Log_PreviewClosed"]           = "⚠ Vorschaufenster wurde geschlossen. Bildschirmeffekte sind in der App nicht sichtbar.",
            ["Log_EffectBypassed"]          = "⚠ Effekt übersprungen: Vorschaufenster ist geschlossen.",
        },

        ["it"] = new()
        {
            ["Login_Subtitle"]      = "Collegate il vostro canale Twitch per iniziare.",
            ["Login_EnterCode"]     = "Inserite questo codice su twitch.tv/activate",
            ["Login_Button"]        = "Connetti con Twitch",
            ["Login_WaitingAuth"]   = "In attesa dell'autorizzazione Twitch…",
            ["Copyright"]         = "Copyright © 2026 Kriax. Tutti i diritti riservati.",

            ["Status_Paused"]     = "In pausa",
            ["Status_Disconnected"] = "Disconnesso",

            ["Btn_Disconnect"]    = "Disconnetti",
            ["Btn_Connect"]       = "Connetti",
            ["Btn_Connecting"]    = "Connessione…",
            ["Btn_Reconnecting"]  = "Riconnessione…",
            ["Btn_WaitingCode"]   = "In attesa del codice…",
            ["Btn_Pause"]         = "Pausa",
            ["Btn_Resume"]        = "Riprendi",

            ["Bindings_Title"]    = "BINDINGS",
            ["Bindings_Refresh"]  = "Aggiorna la lista delle ricompense",
            ["Bindings_Steps"]    = "fase/i",
            ["Bindings_Orphan"]   = "Ricompensa non trovata",
            ["Bindings_Active"]   = "ATTIVO",
            ["Bindings_Inactive"] = "INATTIVO",
            ["Bindings_Select"]   = "Seleziona una ricompensa…",
            ["Bindings_Add"]      = "Aggiungi",

            ["Log_Title"]         = "ATTIVITÀ",
            ["Log_Empty"]         = "Nessuna attività recente.",
            ["Log_Clear"]         = "Cancella",

            ["Step_Title"]        = "SEQUENZA TASTI",
            ["Step_Description"]  = "Descrizione del binding (opzionale)…",
            ["Step_SelectBinding"] = "Seleziona un binding",
            ["Step_TypeKey"]      = "TASTO",
            ["Step_TypePause"]    = "PAUSA",
            ["Step_TypeClick"]    = "CLIC MOUSE",
            ["Step_TypeScroll"]   = "ROTELLA",
            ["Step_TypeMove"]     = "SPOSTA",
            ["Step_TypeEffect"]   = "EFFETTO SCHERMO",
            ["Step_TypeHold"]     = "TASTO TENUTO",
            ["Step_Iterations"]   = "ITERAZIONI",
            ["Step_DelayBetween"] = "RITARDO TRA (MS) • 0 = ISTANTANEO",
            ["Step_Duration"]     = "DURATA (MS)",
            ["Step_Button"]       = "PULSANTE",
            ["Step_BtnLeft"]      = "SINISTRA",
            ["Step_BtnRight"]     = "DESTRA",
            ["Step_BtnMiddle"]    = "CENTRALE",
            ["Step_Direction"]    = "DIREZIONE",
            ["Step_ScrollUp"]     = "⬆ SU",
            ["Step_ScrollDown"]   = "⬇ GIÙ",
            ["Step_Quantity"]     = "QUANTITÀ",
            ["Step_Speed"]        = "VELOCITÀ (MS) • 0 = ISTANTANEO",
            ["Step_Effect"]       = "EFFETTO",
            ["Step_AddKey"]       = "Tasto",
            ["Step_AddPause"]     = "Pausa",
            ["Step_AddClick"]     = "Clic",
            ["Step_AddScroll"]    = "Rotella",
            ["Step_AddMove"]      = "Sposta",
            ["Step_AddEffect"]    = "Effetto schermo",
            ["Step_AddHold"]      = "Tieni",
            ["Step_HoldDuration"] = "DURATA PRESSIONE (MS)",
            ["Step_ModeNormal"]   = "NORMALE",
            ["Step_ModeHeld"]     = "TIENI PREMUTO",
            ["Step_KeyPlaceholder"] = "Tasto…",

            ["Monitor_Screen"]    = "Schermo",
            ["Monitor_Label"]     = "Schermo:",

            ["Effect_Mirror"]     = "Specchio Orizzontale",
            ["Effect_Split2"]     = "Schermo Diviso x2",
            ["Effect_Split4"]     = "Schermo Diviso x4",
            ["Effect_Blur"]       = "Sfocatura",
            ["Effect_Shake"]      = "Schermo Tremante",
            ["Effect_Flip"]       = "Flip Verticale",
            ["Effect_Invert"]     = "Inversione Colori",
            ["Effect_Grayscale"]  = "Bianco e Nero",
            ["Effect_Pixelate"]   = "Pixelato",
            ["Effect_ZoomIn"]     = "Zoom x1.6",
            ["Effect_Chroma"]     = "Aberrazione RGB",
            ["Effect_Glitch"]     = "Glitch",
            ["Effect_Scanlines"]  = "Scanlines CRT",
            ["Effect_ZoomPulse"]  = "Zoom Pulsante",

            // Log messages
            ["Log_ConnectRetry"]           = "Connessione iniziale fallita - nuovo tentativo…",
            ["Log_SessionExpiredConnect"]   = "Sessione scaduta — clicca su Connetti per riconnetterti.",
            ["Log_ConnectFailed"]           = "Connessione iniziale fallita: {0}",
            ["Log_SessionExpiredReauth"]    = "Sessione scaduta - nuova autenticazione richiesta.",
            ["Log_AuthCancelled"]           = "Autenticazione annullata.",
            ["Log_Error"]                   = "Errore: {0}",
            ["Log_Disconnected"]            = "Disconnesso.",
            ["Log_ListenerPaused"]          = "Listener in pausa.",
            ["Log_ResumeError"]             = "Errore durante la ripresa: {0}",
            ["Log_AuthStarted"]             = "Avvio autenticazione Twitch…",
            ["Log_EnterCode"]               = "Inserite questo codice su twitch.tv/activate: {0}",
            ["Log_AuthSuccess"]             = "Autenticazione Twitch riuscita.",
            ["Log_Reconnecting"]            = "Riconnessione in {0}s… (tentativo {1}/{2})",
            ["Log_AttemptFailed"]           = "Tentativo {0} fallito: {1}",
            ["Log_ReconnectAbandoned"]      = "Riconnessione abbandonata dopo 5 tentativi.",
            ["Log_RewardsLoaded"]           = "{0} ricompensa/e caricata/e.",
            ["Log_RewardsError"]            = "Errore caricamento ricompense: {0}",
            ["Log_PreviewClosed"]           = "⚠ La finestra di anteprima è stata chiusa. Gli effetti schermo non saranno visibili nell'app.",
            ["Log_EffectBypassed"]          = "⚠ Effetto ignorato: la finestra di anteprima è chiusa.",
        },
    };
}
