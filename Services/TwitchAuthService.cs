using System.Diagnostics;
using System.Text.Json;

namespace CrowdKeys.Services;

public class TwitchAuthService : IDisposable
{
    private static readonly string DeviceUrl = BuildInfo.DeviceUrl;
    private static readonly string TokenUrl = BuildInfo.TokenUrl;
    private const string Scope = "channel:read:redemptions";

    private readonly HttpClient _http = new();

    public async Task<(string accessToken, string refreshToken)> StartDeviceFlowAsync(
        string clientId,
        Action<string> onCodeReady,
        CancellationToken ct = default)
    {
        var resp = await _http.PostAsync(DeviceUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["scopes"] = Scope,
            }), ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var deviceCode = doc.RootElement.GetProperty("device_code").GetString()!;
        var userCode = doc.RootElement.GetProperty("user_code").GetString()!;
        var verificationUri = doc.RootElement.GetProperty("verification_uri").GetString()!;
        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        var interval = doc.RootElement.TryGetProperty("interval", out var iv) ? iv.GetInt32() : 5;

        Process.Start(new ProcessStartInfo(verificationUri) { UseShellExecute = true });
        onCodeReady(userCode);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(expiresIn));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        while (!linked.Token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval), linked.Token);

            var pollResp = await _http.PostAsync(TokenUrl,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["device_code"] = deviceCode,
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                }), linked.Token);

            if (pollResp.IsSuccessStatusCode)
                return ParseTokenResponse(await pollResp.Content.ReadAsStringAsync(linked.Token));

            using var errDoc = JsonDocument.Parse(await pollResp.Content.ReadAsStringAsync(linked.Token));
            var msg = errDoc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;

            switch (msg)
            {
                case "authorization_pending": 
                    continue;
                case "slow_down": interval += 5; 
                    continue;
                default: 
                    throw new Exception($"Authentification échouée : {msg}");
            }
        }

        throw new OperationCanceledException("Authentification expirée ou annulée.", linked.Token);
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(
        string clientId, string refreshToken, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync(TokenUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            }), ct);

        if ((int)resp.StatusCode is 400 or 401)
            throw new UnauthorizedAccessException("Refresh token invalide ou expiré.");

        resp.EnsureSuccessStatusCode();
        return ParseTokenResponse(await resp.Content.ReadAsStringAsync(ct));
    }

    public async Task<IReadOnlyList<string>> GetChannelRewardsAsync(
        string clientId, string accessToken, string userId, CancellationToken ct = default)
    {
        var url = $"https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id={userId}";
    
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        req.Headers.Add("Client-Id", clientId);

        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var titles = new List<string>();

        foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var title = item.GetProperty("title").GetString();
            if (title is not null)
                titles.Add(title);
        }

        return titles;
    }

    public async Task<(string id, string login)> GetUserInfoAsync(string clientId, string accessToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        req.Headers.Add("Client-Id", clientId);

        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var user = doc.RootElement.GetProperty("data")[0];
        return (user.GetProperty("id").GetString()!, user.GetProperty("login").GetString()!);
    }

    private static (string accessToken, string refreshToken) ParseTokenResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return (
            doc.RootElement.GetProperty("access_token").GetString()!,
            doc.RootElement.GetProperty("refresh_token").GetString()!
        );
    }

    public void Dispose() => _http.Dispose();
}
