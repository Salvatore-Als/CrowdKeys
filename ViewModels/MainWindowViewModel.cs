using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrowdKeys.Models;
using CrowdKeys.Services;
using static CrowdKeys.Models.StepType;

namespace CrowdKeys.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CrowdKeys", "settings.json");

    private readonly TwitchAuthService _auth = new();
    private readonly TwitchEventSubService _eventSub = new();
    private readonly RedemptionService _redemption;

    // ── Connection ────────────────────────────────────────────────────────────

    private static readonly string ClientId = BuildInfo.ClientId;

    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _statusColor = "#3d3d4a";
    [ObservableProperty] private string _connectButtonText = "Connecter";
    [ObservableProperty] private string _activeButtonText = "ACTIF";

    // Stored tokens - not bound to UI
    private string _accessToken = "";
    private string _refreshToken = "";
    private string _userId = "";
    private CancellationTokenSource? _retryCts;
    private bool _initialized;

    [ObservableProperty] private bool _isLoadingRewards;

    // ── Bindings ──────────────────────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<RedemptionBinding> _bindings = [];
    [ObservableProperty] private RedemptionBinding? _selectedBinding;
    [ObservableProperty] private ObservableCollection<string> _availableRewards = [];
    [ObservableProperty] private ObservableCollection<string> _filteredAvailableRewards = [];
    [ObservableProperty] private string? _selectedNewReward;

    // ── Available keys ────────────────────────────────────────────────────────

    public static readonly IReadOnlyList<string> AvailableKeys =
    [
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
        "SPACE","ENTER","TAB","ESC",
        "LEFT","UP","RIGHT","DOWN",
        "NUMPAD0","NUMPAD1","NUMPAD2","NUMPAD3","NUMPAD4",
        "NUMPAD5","NUMPAD6","NUMPAD7","NUMPAD8","NUMPAD9",
    ];

    public static string AppVersion => BuildInfo.Version;

    // ── Log ───────────────────────────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<LogEntry> _log = [];

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainWindowViewModel()
    {
        _redemption = new RedemptionService(new CrossPlatformKeySimulator());

        _redemption.LogAdded += (_, entry) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => Log.Insert(0, entry));

        _eventSub.LogAdded += (_, entry) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => Log.Insert(0, entry));

        _eventSub.RewardReceived += async (_, name) =>
            await _redemption.OnRewardReceivedAsync(name);

        _eventSub.Disconnected += (_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                IsConnected = false;
                StatusColor = "#3d3d4a";
                ConnectButtonText = "Connecter";
                ClearBindingOrphans();
                _ = AutoRetryAsync();
            });

        LoadSettings();
        SyncBindings();
        _initialized = true;
    }

    // ── Partial callbacks ─────────────────────────────────────────────────────

    partial void OnIsActiveChanged(bool value)
    {
        _redemption.IsActive = value;
        ActiveButtonText = value ? "ACTIF" : "PAUSÉ";
        if (_initialized)
            AddToLog(value ? "Récompenses actives." : "Récompenses en pause.", value ? "#00c853" : "#f0a500");
        SaveSettings();
    }

    partial void OnSelectedBindingChanged(RedemptionBinding? value) => SaveSettings();

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ToggleConnect()
    {
        if (IsConnected)
        {
            _retryCts?.Cancel();
            _retryCts = null;
            await _eventSub.DisconnectAsync();
            IsConnected = false;
            StatusColor = "#3d3d4a";
            ConnectButtonText = "Connecter";
            AddToLog("Déconnecté.", "#adadb8");
            ClearBindingOrphans();

            return;
        }

        _retryCts?.Cancel();
        _retryCts = null;

        try
        {
            StatusColor = "#f0a500";
            ConnectButtonText = "Connexion…";

            if (string.IsNullOrWhiteSpace(_accessToken))
                await RunDeviceFlowAsync();

            try
            {
                await ConnectWithTokenAsync();
            }
            catch (UnauthorizedAccessException)
            {
                _accessToken = "";
                _refreshToken = "";
                SaveSettings();
                AddToLog("Session expirée - nouvelle authentification requise.", "#f0a500");
            
                await RunDeviceFlowAsync();
                await ConnectWithTokenAsync();
            }
        }
        catch (OperationCanceledException)
        {
            AddToLog("Authentification annulée.", "#e53935");
            ResetConnectionState();
        }
        catch (Exception ex)
        {
            AddToLog($"Erreur : {ex.Message}", "#e53935");
            ResetConnectionState();
        }
    }

    private async Task ConnectWithTokenAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            throw new UnauthorizedAccessException("Aucun token - authentification requise.");
        }

        string userId;
        try
        {
            userId = await _auth.GetUserIdAsync(ClientId, _accessToken, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            (_accessToken, _refreshToken) = await _auth.RefreshTokenAsync(ClientId, _refreshToken, ct);
            SaveSettings();

            userId = await _auth.GetUserIdAsync(ClientId, _accessToken, ct);
        }

        _userId = userId;

        try
        {
            await _eventSub.ConnectAsync(ClientId, _accessToken, userId, ct);
        }
        catch (UnauthorizedAccessException)
        {
            (_accessToken, _refreshToken) = await _auth.RefreshTokenAsync(ClientId, _refreshToken, ct);
            SaveSettings();

            await _eventSub.ConnectAsync(ClientId, _accessToken, userId, ct);
        }

        IsConnected = true;
        StatusColor = "#00c853";
        ConnectButtonText = "Déconnecter";
        SaveSettings();

        await RefreshRewardsInternalAsync(ct);
    }

    private async Task RunDeviceFlowAsync(CancellationToken ct = default)
    {
        ConnectButtonText = "En attente du code…";
        AddToLog("Démarrage de l'authentification Twitch…", "#adadb8");

        (_accessToken, _refreshToken) = await _auth.StartDeviceFlowAsync(
            ClientId,
            userCode => AddToLog($"Entrez ce code sur twitch.tv/activate : {userCode}", "#9147ff"),
            ct);

        SaveSettings();
        AddToLog("Authentification Twitch réussie.", "#00c853");
        ConnectButtonText = "Connexion…";
    }

    private async Task AutoRetryAsync()
    {
        if (string.IsNullOrWhiteSpace(_accessToken)) 
            return;

        _retryCts?.Cancel();
        _retryCts = new CancellationTokenSource();
        var ct = _retryCts.Token;
        const int maxAttempts = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var delaySecs = (int)Math.Pow(2, attempt);
            AddToLog($"Reconnexion dans {delaySecs}s… (tentative {attempt}/{maxAttempts})", "#f0a500");

            try 
            { 
                await Task.Delay(TimeSpan.FromSeconds(delaySecs), ct); 
            }
            catch (OperationCanceledException) 
            { 
                return; 
            }

            if (ct.IsCancellationRequested) 
                return;

            try
            {
                StatusColor = "#f0a500";
                ConnectButtonText = "Reconnexion…";
                await ConnectWithTokenAsync(ct);
            
                return;
            }
            catch (OperationCanceledException) 
            { 
                return; 
            }
            catch (UnauthorizedAccessException)
            {
                _accessToken = "";
                _refreshToken = "";

                SaveSettings();
                AddToLog("Session expirée - cliquez sur Connecter pour vous reconnecter.", "#e53935");

                IsConnected = false;
                StatusColor = "#3d3d4a";
                ConnectButtonText = "Connecter";

                ClearBindingOrphans();

                return;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusColor = "#3d3d4a";
                ConnectButtonText = "Connecter";
                AddToLog($"Tentative {attempt} échouée : {ex.Message}", "#e53935");
            }
        }

        AddToLog("Reconnexion abandonnée après 5 tentatives.", "#e53935");
    }

    private void AddToLog(string message, string? color = null) =>
        Log.Insert(0, new LogEntry { Message = message, CustomColor = color });

    private void ResetConnectionState()
    {
        IsConnected = false;
        StatusColor = "#3d3d4a";
        ConnectButtonText = "Connecter";
    }

    [RelayCommand]
    private void AddBinding()
    {
        if (string.IsNullOrWhiteSpace(SelectedNewReward))
        {
            return;
        }

        var binding = new RedemptionBinding { RewardName = SelectedNewReward };
        CheckBindingOrphan(binding);
        Bindings.Add(binding);
        SelectedBinding = binding;
        SelectedNewReward = null;
        SyncBindings();
        UpdateFilteredRewards();
        SaveSettings();
    }

    [RelayCommand]
    private async Task RefreshRewards()
    {
        if (string.IsNullOrEmpty(_userId) || string.IsNullOrEmpty(_accessToken))
            return;

        await RefreshRewardsInternalAsync(CancellationToken.None);
    }

    private async Task RefreshRewardsInternalAsync(CancellationToken ct)
    {
        IsLoadingRewards = true;
        try
        {
            var rewards = await _auth.GetChannelRewardsAsync(ClientId, _accessToken, _userId, ct);
            AvailableRewards.Clear();
            foreach (var r in rewards)
            {
                AvailableRewards.Add(r);
            }
        
            UpdateBindingOrphans();
            UpdateFilteredRewards();
            AddToLog($"{AvailableRewards.Count} reward(s) chargé(s).", "#adadb8");
        }
        catch (Exception ex)
        {
            AddToLog($"Erreur chargement rewards : {ex.Message}", "#e53935");
        }
        finally
        {
            IsLoadingRewards = false;
        }
    }

    private void UpdateBindingOrphans()
    {
        foreach (var binding in Bindings)
        {
            CheckBindingOrphan(binding);
        }
    }

    private void CheckBindingOrphan(RedemptionBinding binding)
    {
        if (AvailableRewards.Count == 0)
        {
            binding.IsOrphaned = false;
            return;
        }

        binding.IsOrphaned = !AvailableRewards.Any(r => r.Equals(binding.RewardName, StringComparison.OrdinalIgnoreCase));
    }

    private void ClearBindingOrphans()
    {
        AvailableRewards.Clear();
        FilteredAvailableRewards.Clear();

        foreach (var binding in Bindings)
        {
            binding.IsOrphaned = false;
        }
    }

    private void UpdateFilteredRewards()
    {
        var used = Bindings
            .Select(b => b.RewardName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        FilteredAvailableRewards.Clear();

        foreach (var r in AvailableRewards)
        {
            if (!used.Contains(r))
                FilteredAvailableRewards.Add(r);
        }
    }

    [RelayCommand]
    private void DeleteBinding(RedemptionBinding? b)
    {
        if (b is null)
            return;

        if (SelectedBinding == b)
            SelectedBinding = null;

        Bindings.Remove(b);
        SyncBindings();
        UpdateFilteredRewards();
        SaveSettings();
    }

    [RelayCommand]
    private void AddKeyStep()
    {
        if (SelectedBinding is null) 
            return;
        
        SelectedBinding.Steps.Add(new KeyStep { Type = StepType.Key });
        SaveSettings();
    }

    [RelayCommand]
    private void AddPauseStep()
    {
        if (SelectedBinding is null) 
            return;
        
        SelectedBinding.Steps.Add(new KeyStep { Type = StepType.Pause, DurationMs = 500 });
        SaveSettings();
    }

    [RelayCommand]
    private void AddMouseClickStep()
    {
        if (SelectedBinding is null) 
            return;
        
        SelectedBinding.Steps.Add(new KeyStep { Type = StepType.MouseClick });
        SaveSettings();
    }

    [RelayCommand]
    private void AddMouseScrollStep()
    {
        if (SelectedBinding is null) 
            return;

        SelectedBinding.Steps.Add(new KeyStep { Type = StepType.MouseScroll, ScrollAmount = 3 });
        SaveSettings();
    }

    [RelayCommand]
    private void AddMouseMoveStep()
    {
        if (SelectedBinding is null) 
            return;

        SelectedBinding.Steps.Add(new KeyStep { Type = StepType.MouseMove });
        SaveSettings();
    }

    [RelayCommand]
    private void DeleteStep(KeyStep? step)
    {
        if (step is null || SelectedBinding is null) 
            return;
        
        SelectedBinding.Steps.Remove(step);
        SaveSettings();
    }

    public void MoveStep(KeyStep source, KeyStep target)
    {
        if (SelectedBinding is null) 
            return;
        
        var steps = SelectedBinding.Steps;
        var si = steps.IndexOf(source);
        var ti = steps.IndexOf(target);

        if (si < 0 || ti < 0 || si == ti) 
            return;
        
        steps.Move(si, ti);
        SaveSettings();
    }

    [RelayCommand]
    private void ClearLog() => Log.Clear();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SyncBindings() => _redemption.UpdateBindings(Bindings);

    private void LoadSettings()
    {
        if (!File.Exists(SettingsPath)) 
            return;
        
        try
        {
            var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
            if (s is null) 
                return;

            _accessToken = s.AccessToken;
            _refreshToken = s.RefreshToken;
            IsActive = s.IsActive;
            Bindings = new ObservableCollection<RedemptionBinding>(s.Bindings);
        }
        catch { }
    }

    // ── Auto-save ─────────────────────────────────────────────────────────────

    partial void OnBindingsChanged(ObservableCollection<RedemptionBinding> value)
    {
        foreach (var binding in value)
            WatchBinding(binding);

        value.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is null) 
                return;
            
            foreach (RedemptionBinding binding in e.NewItems)
                WatchBinding(binding);
        };
    }

    private void WatchBinding(RedemptionBinding binding)
    {
        binding.PropertyChanged += (_, e) =>
        {
            SaveSettings();
            if (e.PropertyName == nameof(RedemptionBinding.RewardName))
            {
                CheckBindingOrphan(binding);
                UpdateFilteredRewards();
            }
        };
        foreach (var step in binding.Steps)
            step.PropertyChanged += (_, _) => SaveSettings();

        binding.Steps.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is null) 
                return;
            
            foreach (KeyStep step in e.NewItems)
                step.PropertyChanged += (_, _) => SaveSettings();
        };
    }

    private void SaveSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(
            new AppSettings
            {
                AccessToken = _accessToken,
                RefreshToken = _refreshToken,
                IsActive = IsActive,
                Bindings = [.. Bindings],
            },
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
