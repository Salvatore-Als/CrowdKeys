using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CrowdKeys.Services;

public class TwitchEventSubService : IDisposable
{
    private static readonly string WssUrl = BuildInfo.EventSubUrl;
    private static readonly string SubscriptionsUrl = BuildInfo.SubscriptionsUrl;

    private readonly HttpClient _http = new();
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private TaskCompletionSource<string>? _sessionIdTcs;
    private int _keepaliveTimeoutSeconds = 30;
    private DateTime _lastMessageTime = DateTime.UtcNow;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public event EventHandler<string>? RewardReceived;
    public event EventHandler? Disconnected;
    public event EventHandler<LogEntry>? LogAdded;

    private void AddLog(string message, string? color = null) =>
        LogAdded?.Invoke(this, new LogEntry { Message = message, CustomColor = color });

    public async Task ConnectAsync(string clientId, string accessToken, string userId, CancellationToken ct = default)
    {
        await DisconnectAsync();

        _sessionIdTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ws = new ClientWebSocket();
        _cts = new CancellationTokenSource();
        _lastMessageTime = DateTime.UtcNow;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        await _ws.ConnectAsync(new Uri(WssUrl), linked.Token);

        _ = ReceiveLoopAsync(clientId, accessToken, userId, _cts.Token);
        _ = WatchdogAsync(_cts.Token);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var welcomeLinked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var sessionId = await _sessionIdTcs.Task.WaitAsync(welcomeLinked.Token);

        await SubscribeAsync(sessionId, clientId, accessToken, userId, ct);
        AddLog("Socket connecté - en attente des récompenses.", "#00c853");
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        if (_ws?.State == WebSocketState.Open)
        {
            try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); }
            catch { }
        }
        _ws?.Dispose();
        _ws = null;
        _cts = null;
    }

    private async Task ReceiveLoopAsync(string clientId, string accessToken, string userId, CancellationToken ct)
    {
        var buffer = new byte[65536];
        var sb = new StringBuilder();

        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(buffer, ct);
                _lastMessageTime = DateTime.UtcNow;

                if (result.MessageType == WebSocketMessageType.Close) 
                    break;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage) 
                    continue;

                var json = sb.ToString();
                sb.Clear();

                var reconnectUrl = HandleMessage(json);
                if (reconnectUrl != null)
                {
                    AddLog("Reconnexion socket demandée par Twitch…", "#f0a500");
                    await ReconnectAsync(reconnectUrl, clientId, accessToken, userId, ct);
                    return;
                }
            }
        }
        catch (OperationCanceledException) 
        { 
            return; 
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested) 
                return;
            
            _cts?.Cancel();
            AddLog($"Erreur socket : {ex.Message}", "#e53935");
            Disconnected?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!ct.IsCancellationRequested)
        {
            _cts?.Cancel();
            AddLog("Socket déconnecté.", "#adadb8");
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task WatchdogAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);
                var elapsed = (DateTime.UtcNow - _lastMessageTime).TotalSeconds;

                if (elapsed > _keepaliveTimeoutSeconds + 5)
                {
                    AddLog($"Keepalive timeout - aucun message depuis {elapsed:0}s.", "#f0a500");
                    _cts?.Cancel();
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private string? HandleMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var messageType = doc.RootElement
                .GetProperty("metadata")
                .GetProperty("message_type")
                .GetString();

            switch (messageType)
            {
                case "session_welcome":
                    var session = doc.RootElement.GetProperty("payload").GetProperty("session");
                    var sessionId = session.GetProperty("id").GetString()!;

                    if (session.TryGetProperty("keepalive_timeout_seconds", out var kpProp) &&
                        kpProp.TryGetInt32(out var kp))
                        _keepaliveTimeoutSeconds = kp;
                    
                    _sessionIdTcs?.TrySetResult(sessionId);
                    break;

                case "notification":
                    var subType = doc.RootElement
                        .GetProperty("metadata").GetProperty("subscription_type").GetString();
                    if (subType == "channel.channel_points_custom_reward_redemption.add")
                    {
                        var title = doc.RootElement
                            .GetProperty("payload").GetProperty("event")
                            .GetProperty("reward").GetProperty("title").GetString();
                        
                        if (title is { Length: > 0 })
                            RewardReceived?.Invoke(this, title);
                    }
                    break;

                case "session_reconnect":
                    return doc.RootElement
                        .GetProperty("payload").GetProperty("session")
                        .GetProperty("reconnect_url").GetString();
            }
        }
        catch { }

        return null;
    }

    private async Task ReconnectAsync(string url, string clientId, string accessToken, string userId, CancellationToken ct)
    {
        var oldWs = _ws;
        _ws = new ClientWebSocket();

        await _ws.ConnectAsync(new Uri(url), ct);

        _lastMessageTime = DateTime.UtcNow;
        _ = ReceiveLoopAsync(clientId, accessToken, userId, ct);
        AddLog("Socket reconnecté (Twitch redirect).", "#00c853");

        try { 
            await oldWs!.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); 
        }
        catch { }
        oldWs?.Dispose();
    }

    private async Task SubscribeAsync(string sessionId, string clientId, string accessToken, string userId, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new
        {
            type = "channel.channel_points_custom_reward_redemption.add",
            version = "1",
            condition = new { broadcaster_user_id = userId },
            transport = new { method = "websocket", session_id = sessionId }
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, SubscriptionsUrl);
        req.Headers.Add("Authorization", $"Bearer {accessToken}");
        req.Headers.Add("Client-Id", clientId);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var resp = await _http.SendAsync(req, ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Token expiré.");

        resp.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _ws?.Dispose();
        _http.Dispose();
    }
}
