using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
                    StatusText = "En attente de l'autorisation sur Twitch…";
                }));

            LoginSucceeded?.Invoke(this, (at, rt));
        }
        catch (OperationCanceledException)
        {
            StatusText = "Authentification annulée.";
        }
        catch (Exception ex)
        {
            StatusText = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
            HasCode      = false;
            DeviceCode   = "";
        }
    }
}
