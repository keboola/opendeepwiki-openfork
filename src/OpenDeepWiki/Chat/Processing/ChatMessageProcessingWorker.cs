using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenDeepWiki.Chat.Abstractions;
using OpenDeepWiki.Chat.Callbacks;
using OpenDeepWiki.Chat.Execution;
using OpenDeepWiki.Chat.Queue;
using OpenDeepWiki.Chat.Sessions;
using System.Threading.Channels;

namespace OpenDeepWiki.Chat.Processing;

/// <summary>
/// Message processing background service
/// Responsible for dequeuing messages, processing them, and sending callbacks
/// Requirements: 10.1, 10.2, 10.3
/// </summary>
public class ChatMessageProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatMessageProcessingWorker> _logger;
    private readonly ChatProcessingOptions _options;

    private readonly Channel<QueuedMessage> _channel;

    public ChatMessageProcessingWorker(
        IServiceProvider serviceProvider,
        ILogger<ChatMessageProcessingWorker> logger,
        IOptions<ChatProcessingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;

        var capacity = Math.Max(1, _options.MaxConcurrency) * 100;
        _channel = Channel.CreateBounded<QueuedMessage>(new BoundedChannelOptions(capacity)
        {
            SingleWriter = false,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var concurrency = Math.Max(1, _options.MaxConcurrency);

        _logger.LogInformation("Message processing Worker started, concurrency: {Concurrency}", concurrency);

        var producer = ProduceMessagesAsync(stoppingToken);

        var consumers = Enumerable.Range(0, concurrency)
            .Select(_ => ConsumeMessagesAsync(stoppingToken))
            .ToArray();

        await Task.WhenAll(consumers.Prepend(producer));

        _logger.LogInformation("Message processing Worker stopped");
    }

    private async Task ProduceMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var msg = await messageQueue.DequeueAsync(stoppingToken);
                if (msg != null)
                    await _channel.Writer.WriteAsync(msg, stoppingToken);
                else
                    await Task.Delay(_options.PollingIntervalMs, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore cancellation
        }
        finally
        {
            _channel.Writer.TryComplete();
        }
    }

    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
        var sessionManager = scope.ServiceProvider.GetRequiredService<ISessionManager>();
        var agentExecutor = scope.ServiceProvider.GetRequiredService<IAgentExecutor>();
        var messageCallback = scope.ServiceProvider.GetRequiredService<IMessageCallback>();

        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _logger.LogDebug("Starting to process message: {MessageId}, type: {Type}",
            message.Id, message.Type);

            try
            {
                await ProcessMessageAsync(
                    message,
                    messageQueue,
                    sessionManager,
                    agentExecutor,
                    messageCallback,
                    stoppingToken);

                await messageQueue.CompleteAsync(message.Id, stoppingToken);
                _logger.LogDebug("Message processing complete: {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message processing failed: {MessageId}", message.Id);
                await HandleMessageFailureAsync(message, messageQueue, ex.Message, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(
        QueuedMessage queuedMessage,
        IMessageQueue messageQueue,
        ISessionManager sessionManager,
        IAgentExecutor agentExecutor,
        IMessageCallback messageCallback,
        CancellationToken stoppingToken)
    {
        switch (queuedMessage.Type)
        {
            case QueuedMessageType.Incoming:
                await ProcessIncomingMessageAsync(
                    queuedMessage, sessionManager, agentExecutor, messageCallback, stoppingToken);
                break;

            case QueuedMessageType.Outgoing:
                await ProcessOutgoingMessageAsync(queuedMessage, messageCallback, stoppingToken);
                break;

            case QueuedMessageType.Retry:
                // Retry messages are processed as their original type
                await ProcessOutgoingMessageAsync(queuedMessage, messageCallback, stoppingToken);
                break;

            default:
                _logger.LogWarning("Unknown message type: {Type}", queuedMessage.Type);
                break;
        }
    }

    private async Task ProcessIncomingMessageAsync(
        QueuedMessage queuedMessage,
        ISessionManager sessionManager,
        IAgentExecutor agentExecutor,
        IMessageCallback messageCallback,
        CancellationToken stoppingToken)
    {
        // Get or create session
        var session = await sessionManager.GetOrCreateSessionAsync(
            queuedMessage.Message.SenderId,
            queuedMessage.Message.Platform,
            stoppingToken);

        // Add user message to session history
        session.AddMessage(queuedMessage.Message);

        // Execute Agent processing
        var response = await agentExecutor.ExecuteAsync(
            queuedMessage.Message, session, stoppingToken);

        // Reply target: use ReceiverId (channel) when available, fall back to SenderId (DM)
        var replyTarget = !string.IsNullOrEmpty(queuedMessage.Message.ReceiverId)
            ? queuedMessage.Message.ReceiverId
            : queuedMessage.Message.SenderId;

        if (response.Success && response.Messages.Any())
        {
            // Send response messages
            foreach (var responseMessage in response.Messages)
            {
                // Propagate metadata from original message (e.g. thread_ts for Slack threading)
                var messageToSend = responseMessage;
                if (queuedMessage.Message.Metadata != null && responseMessage.Metadata == null)
                {
                    messageToSend = new ChatMessage
                    {
                        MessageId = responseMessage.MessageId,
                        SenderId = responseMessage.SenderId,
                        ReceiverId = responseMessage.ReceiverId,
                        Content = responseMessage.Content,
                        MessageType = responseMessage.MessageType,
                        Platform = responseMessage.Platform,
                        Timestamp = responseMessage.Timestamp,
                        Metadata = new Dictionary<string, object>(queuedMessage.Message.Metadata)
                    };
                }

                var sendResult = await messageCallback.SendAsync(
                    queuedMessage.Message.Platform,
                    replyTarget,
                    messageToSend,
                    stoppingToken);

                if (!sendResult.Success)
                {
                    _logger.LogWarning("Response message send failed: {ErrorMessage}", sendResult.ErrorMessage);
                }

                // Add Agent response to session history
                session.AddMessage(responseMessage);
            }
        }
        else if (!response.Success)
        {
            // Send error message to user
            var errorMessage = new ChatMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                SenderId = "system",
                ReceiverId = replyTarget,
                Content = response.ErrorMessage ?? "An error occurred while processing your message, please try again later.",
                MessageType = ChatMessageType.Text,
                Platform = queuedMessage.Message.Platform,
                Timestamp = DateTimeOffset.UtcNow,
                Metadata = queuedMessage.Message.Metadata != null
                    ? new Dictionary<string, object>(queuedMessage.Message.Metadata)
                    : null
            };

            await messageCallback.SendAsync(
                queuedMessage.Message.Platform,
                replyTarget,
                errorMessage,
                stoppingToken);
        }

        // Update session
        await sessionManager.UpdateSessionAsync(session, stoppingToken);
    }

    private async Task ProcessOutgoingMessageAsync(
        QueuedMessage queuedMessage,
        IMessageCallback messageCallback,
        CancellationToken stoppingToken)
    {
        var result = await messageCallback.SendAsync(
            queuedMessage.Message.Platform,
            queuedMessage.TargetUserId,
            queuedMessage.Message,
            stoppingToken);

        if (!result.Success && result.ShouldRetry)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Message send failed");
        }
    }

    private async Task HandleMessageFailureAsync(
        QueuedMessage queuedMessage,
        IMessageQueue messageQueue,
        string reason,
        CancellationToken stoppingToken)
    {
        if (queuedMessage.RetryCount < _options.MaxRetryCount)
        {
            // Add to retry queue
            var delaySeconds = CalculateRetryDelay(queuedMessage.RetryCount);
            await messageQueue.RetryAsync(queuedMessage.Id, delaySeconds, stoppingToken);
            _logger.LogInformation("Message added to retry queue: {MessageId}, delay: {Delay}s",
                queuedMessage.Id, delaySeconds);
        }
        else
        {
            // Move to dead letter queue
            await messageQueue.FailAsync(queuedMessage.Id, reason, stoppingToken);
            _logger.LogWarning("Message moved to dead letter queue: {MessageId}, reason: {Reason}",
                queuedMessage.Id, reason);
        }
    }

    /// <summary>
    /// Calculate retry delay (exponential backoff)
    /// </summary>
    private int CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff: 30s, 60s, 120s, ...
        return _options.BaseRetryDelaySeconds * (int)Math.Pow(2, retryCount);
    }
}
