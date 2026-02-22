using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenDeepWiki.Chat.Providers.QQ;

/// <summary>
/// QQ WebSocket connection manager
/// Responsible for maintaining WebSocket connection with QQ Open Platform, handling authentication and heartbeat
/// </summary>
public class QQWebSocketManager : IDisposable
{
    private readonly ILogger<QQWebSocketManager> _logger;
    private readonly QQProviderOptions _options;
    private readonly HttpClient _httpClient;
    private readonly Func<Task<string>> _getAccessTokenAsync;
    
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _heartbeatCts;
    private CancellationTokenSource? _receiveCts;
    private Task? _heartbeatTask;
    private Task? _receiveTask;
    
    private string? _sessionId;
    private int _lastSequence;
    private int _heartbeatInterval;
    private int _reconnectAttempts;
    private bool _isConnected;
    private bool _isDisposed;
    
    /// <summary>
    /// Connection state change event
    /// </summary>
    public event EventHandler<QQConnectionStateChangedEventArgs>? ConnectionStateChanged;
    
    /// <summary>
    /// Message received event
    /// </summary>
    public event EventHandler<QQMessageReceivedEventArgs>? MessageReceived;
    
    /// <summary>
    /// Whether connected
    /// </summary>
    public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;
    
    /// <summary>
    /// Session ID
    /// </summary>
    public string? SessionId => _sessionId;
    
    public QQWebSocketManager(
        ILogger<QQWebSocketManager> logger,
        IOptions<QQProviderOptions> options,
        HttpClient httpClient,
        Func<Task<string>> getAccessTokenAsync)
    {
        _logger = logger;
        _options = options.Value;
        _httpClient = httpClient;
        _getAccessTokenAsync = getAccessTokenAsync;
    }
    
    /// <summary>
    /// Connect to QQ WebSocket gateway
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(QQWebSocketManager));
        
        if (IsConnected)
        {
            _logger.LogWarning("WebSocket is already connected");
            return;
        }
        
        try
        {
            // Get WebSocket gateway URL
            var gatewayUrl = await GetGatewayUrlAsync(cancellationToken);
            if (string.IsNullOrEmpty(gatewayUrl))
            {
                throw new InvalidOperationException("Failed to get WebSocket gateway URL");
            }
            
            _logger.LogInformation("Connecting to QQ WebSocket gateway: {Url}", gatewayUrl);
            
            // Create WebSocket connection
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(gatewayUrl), cancellationToken);
            
            _isConnected = true;
            _reconnectAttempts = 0;
            
            OnConnectionStateChanged(QQConnectionState.Connected);
            
            // Start receive task
            _receiveCts = new CancellationTokenSource();
            _receiveTask = ReceiveLoopAsync(_receiveCts.Token);
            
            _logger.LogInformation("Connected to QQ WebSocket gateway");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to QQ WebSocket gateway");
            _isConnected = false;
            OnConnectionStateChanged(QQConnectionState.Disconnected);
            throw;
        }
    }
    
    /// <summary>
    /// Disconnect
    /// </summary>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            return;
        
        try
        {
            // Stop heartbeat
            StopHeartbeat();
            
            // Stop receiving
            _receiveCts?.Cancel();
            
            // Close WebSocket
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
            
            _isConnected = false;
            OnConnectionStateChanged(QQConnectionState.Disconnected);
            
            _logger.LogInformation("Disconnected from QQ WebSocket gateway");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disconnecting from QQ WebSocket gateway");
        }
    }
    
    /// <summary>
    /// Send authentication request
    /// </summary>
    public async Task IdentifyAsync(int intents, CancellationToken cancellationToken = default)
    {
        var token = await _getAccessTokenAsync();
        
        var identifyPayload = new QQWebhookEvent
        {
            OpCode = QQOpCode.Identify,
            Data = new QQEventData()
        };
        
        // Build authentication data
        var identifyData = new
        {
            token = $"QQBot {token}",
            intents = intents,
            shard = new[] { 0, 1 }
        };
        
        var payload = new
        {
            op = QQOpCode.Identify,
            d = identifyData
        };
        
        await SendAsync(JsonSerializer.Serialize(payload), cancellationToken);
        _logger.LogDebug("Sent identify request with intents: {Intents}", intents);
    }
    
    /// <summary>
    /// Send resume connection request
    /// </summary>
    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_sessionId))
        {
            _logger.LogWarning("Cannot resume without session ID, will re-identify");
            return;
        }
        
        var token = await _getAccessTokenAsync();
        
        var resumeData = new
        {
            token = $"QQBot {token}",
            session_id = _sessionId,
            seq = _lastSequence
        };
        
        var payload = new
        {
            op = QQOpCode.Resume,
            d = resumeData
        };
        
        await SendAsync(JsonSerializer.Serialize(payload), cancellationToken);
        _logger.LogDebug("Sent resume request for session: {SessionId}", _sessionId);
    }
    
    #region Private methods
    
    /// <summary>
    /// Get WebSocket gateway URL
    /// </summary>
    private async Task<string?> GetGatewayUrlAsync(CancellationToken cancellationToken)
    {
        var token = await _getAccessTokenAsync();
        var baseUrl = _options.UseSandbox ? _options.SandboxApiBaseUrl : _options.ApiBaseUrl;
        var url = $"{baseUrl}/gateway";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("QQBot", token);
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get gateway URL: {StatusCode} - {Content}", response.StatusCode, content);
            return null;
        }
        
        var gatewayResponse = JsonSerializer.Deserialize<QQGatewayResponse>(content);
        return gatewayResponse?.Url;
    }
    
    /// <summary>
    /// Message receive loop
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket?.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket closed by server: {Status} - {Description}",
                        result.CloseStatus, result.CloseStatusDescription);
                    await HandleDisconnectionAsync(cancellationToken);
                    break;
                }
                
                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                
                if (result.EndOfMessage)
                {
                    var message = messageBuilder.ToString();
                    messageBuilder.Clear();
                    
                    await ProcessMessageAsync(message, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Receive loop cancelled");
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "WebSocket error in receive loop");
            await HandleDisconnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in receive loop");
        }
    }
    
    /// <summary>
    /// Process received message
    /// </summary>
    private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<QQWebhookEvent>(message);
            if (payload == null) return;
            
            // Update sequence number
            if (payload.Sequence.HasValue)
            {
                _lastSequence = payload.Sequence.Value;
            }
            
            switch (payload.OpCode)
            {
                case QQOpCode.Hello:
                    await HandleHelloAsync(message, cancellationToken);
                    break;
                    
                case QQOpCode.HeartbeatAck:
                    _logger.LogDebug("Received heartbeat ACK");
                    break;
                    
                case QQOpCode.Dispatch:
                    await HandleDispatchAsync(payload, message, cancellationToken);
                    break;
                    
                case QQOpCode.Reconnect:
                    _logger.LogWarning("Server requested reconnect");
                    await HandleDisconnectionAsync(cancellationToken);
                    break;
                    
                case QQOpCode.InvalidSession:
                    _logger.LogWarning("Invalid session, will re-identify");
                    _sessionId = null;
                    await HandleDisconnectionAsync(cancellationToken);
                    break;
                    
                default:
                    _logger.LogDebug("Received unknown opcode: {OpCode}", payload.OpCode);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {Message}", message);
        }
    }
    
    /// <summary>
    /// Handle Hello message
    /// </summary>
    private async Task HandleHelloAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            // Parse heartbeat interval
            using var doc = JsonDocument.Parse(message);
            if (doc.RootElement.TryGetProperty("d", out var data) &&
                data.TryGetProperty("heartbeat_interval", out var interval))
            {
                _heartbeatInterval = interval.GetInt32();
            }
            else
            {
                _heartbeatInterval = _options.HeartbeatInterval;
            }
            
            _logger.LogDebug("Received Hello, heartbeat interval: {Interval}ms", _heartbeatInterval);
            
            // Start heartbeat
            StartHeartbeat();
            
            // Send authentication or resume
            if (!string.IsNullOrEmpty(_sessionId))
            {
                await ResumeAsync(cancellationToken);
            }
            else
            {
                // Subscribe to public domain message events by default
                // 1 << 30 = Group chat @ message
                // 1 << 25 = C2C direct message
                var intents = (1 << 30) | (1 << 25);
                await IdentifyAsync(intents, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Hello message");
        }
    }
    
    /// <summary>
    /// Handle Dispatch message
    /// </summary>
    private Task HandleDispatchAsync(QQWebhookEvent payload, string rawMessage, CancellationToken cancellationToken)
    {
        var eventType = payload.EventType;
        
        if (eventType == QQEventType.Ready)
        {
            // Parse Ready data to get session_id
            try
            {
                using var doc = JsonDocument.Parse(rawMessage);
                if (doc.RootElement.TryGetProperty("d", out var data) &&
                    data.TryGetProperty("session_id", out var sessionId))
                {
                    _sessionId = sessionId.GetString();
                    _logger.LogInformation("Ready! Session ID: {SessionId}", _sessionId);
                    OnConnectionStateChanged(QQConnectionState.Ready);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Ready event");
            }
        }
        else if (eventType == QQEventType.Resumed)
        {
            _logger.LogInformation("Session resumed successfully");
            OnConnectionStateChanged(QQConnectionState.Ready);
        }
        else
        {
            // Trigger message received event
            OnMessageReceived(eventType ?? string.Empty, rawMessage);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Handle disconnection
    /// </summary>
    private async Task HandleDisconnectionAsync(CancellationToken cancellationToken)
    {
        _isConnected = false;
        OnConnectionStateChanged(QQConnectionState.Disconnected);
        
        // Attempt to reconnect
        if (_reconnectAttempts < _options.MaxReconnectAttempts)
        {
            _reconnectAttempts++;
            var delay = _options.ReconnectInterval * _reconnectAttempts;
            
            _logger.LogInformation("Attempting to reconnect ({Attempt}/{Max}) in {Delay}ms",
                _reconnectAttempts, _options.MaxReconnectAttempts, delay);
            
            await Task.Delay(delay, cancellationToken);
            
            try
            {
                OnConnectionStateChanged(QQConnectionState.Reconnecting);
                await ConnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnection attempt {Attempt} failed", _reconnectAttempts);
            }
        }
        else
        {
            _logger.LogError("Max reconnection attempts reached, giving up");
            OnConnectionStateChanged(QQConnectionState.Failed);
        }
    }
    
    /// <summary>
    /// Start heartbeat
    /// </summary>
    private void StartHeartbeat()
    {
        StopHeartbeat();
        
        _heartbeatCts = new CancellationTokenSource();
        _heartbeatTask = HeartbeatLoopAsync(_heartbeatCts.Token);
        
        _logger.LogDebug("Heartbeat started with interval: {Interval}ms", _heartbeatInterval);
    }
    
    /// <summary>
    /// Stop heartbeat
    /// </summary>
    private void StopHeartbeat()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _heartbeatCts = null;
        _heartbeatTask = null;
    }
    
    /// <summary>
    /// Heartbeat loop
    /// </summary>
    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_heartbeatInterval, cancellationToken);
                
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await SendHeartbeatAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Heartbeat loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in heartbeat loop");
        }
    }
    
    /// <summary>
    /// Send heartbeat
    /// </summary>
    private async Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        var payload = new
        {
            op = QQOpCode.Heartbeat,
            d = _lastSequence > 0 ? (int?)_lastSequence : null
        };
        
        await SendAsync(JsonSerializer.Serialize(payload), cancellationToken);
        _logger.LogDebug("Sent heartbeat, last sequence: {Sequence}", _lastSequence);
    }
    
    /// <summary>
    /// Send message
    /// </summary>
    private async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        if (_webSocket?.State != WebSocketState.Open)
        {
            _logger.LogWarning("Cannot send message, WebSocket is not open");
            return;
        }
        
        var bytes = Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }
    
    /// <summary>
    /// Trigger connection state change event
    /// </summary>
    private void OnConnectionStateChanged(QQConnectionState state)
    {
        ConnectionStateChanged?.Invoke(this, new QQConnectionStateChangedEventArgs(state));
    }
    
    /// <summary>
    /// Trigger message received event
    /// </summary>
    private void OnMessageReceived(string eventType, string rawMessage)
    {
        MessageReceived?.Invoke(this, new QQMessageReceivedEventArgs(eventType, rawMessage));
    }
    
    #endregion
    
    #region IDisposable
    
    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        
        StopHeartbeat();
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        
        _webSocket?.Dispose();
        
        GC.SuppressFinalize(this);
    }
    
    #endregion
}


/// <summary>
/// QQ connection state
/// </summary>
public enum QQConnectionState
{
    /// <summary>
    /// Disconnected
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// Connecting
    /// </summary>
    Connecting,
    
    /// <summary>
    /// Connected
    /// </summary>
    Connected,
    
    /// <summary>
    /// Ready (authenticated)
    /// </summary>
    Ready,
    
    /// <summary>
    /// Reconnecting
    /// </summary>
    Reconnecting,
    
    /// <summary>
    /// Connection failed
    /// </summary>
    Failed
}

/// <summary>
/// Connection state change event arguments
/// </summary>
public class QQConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// New state
    /// </summary>
    public QQConnectionState State { get; }
    
    public QQConnectionStateChangedEventArgs(QQConnectionState state)
    {
        State = state;
    }
}

/// <summary>
/// Message received event arguments
/// </summary>
public class QQMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// Event type
    /// </summary>
    public string EventType { get; }
    
    /// <summary>
    /// Raw message
    /// </summary>
    public string RawMessage { get; }
    
    public QQMessageReceivedEventArgs(string eventType, string rawMessage)
    {
        EventType = eventType;
        RawMessage = rawMessage;
    }
}
