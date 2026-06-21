using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrowdKeys.Localization;
using LocSingleton = CrowdKeys.Localization.Loc;
using CrowdKeys.Services;

namespace CrowdKeys.ViewModels;

public partial class LoginWindowViewModel : ViewModelBase
{
    private static readonly string ClientId = BuildInfo.ClientId;
    private readonly TwitchAuthService _auth = new();

    [ObservableProperty] private bool   _isConnecting;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _deviceCode = "";
    [ObservableProperty] private bool   _hasCode;

    public event EventHandler<(string AccessToken, string RefreshToken)>? LoginSucceeded;

    public LanguageOption SelectedLanguage
    {
        get => LocSingleton.Languages.FirstOrDefault(l => l.Code == LocSingleton.Instance.CurrentLang)
               ?? LocSingleton.Languages[0];
        set
        {
            if (value is null) 
                return;
            
            LocSingleton.Instance.SetLanguage(value.Code);
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private async Task Connect()
    {
        IsConnecting = true;
        StatusText   = "";
        HasCode      = false;
        DeviceCode   = "";

        try
        {
            var (at, rt) = await _auth.StartDeviceFlowAsync(
                ClientId,
                code => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    DeviceCode = code;
                    HasCode    = true;
                    StatusText = LocSingleton.Instance["Login_WaitingAuth"];
                }));

            LoginSucceeded?.Invoke(this, (at, rt));
        }
        catch (OperationCanceledException)
        {
            StatusText = LocSingleton.Instance["Log_AuthCancelled"];
        }
        catch (Exception ex)
        {
            StatusText = string.Format(LocSingleton.Instance["Log_Error"], ex.Message);
        }
        finally
        {
            IsConnecting = false;
            HasCode      = false;
            DeviceCode   = "";
        }
    }
}
