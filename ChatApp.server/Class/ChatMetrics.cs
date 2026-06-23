using System.Diagnostics.Metrics;

namespace ChatApp.server.Class;

/// <summary>
/// OpenTelemetry metrics for SignalR chat message flow and connections.
/// </summary>
public class ChatMetrics
{
    private readonly Meter _meter;

    public Counter<long> MessagesSent { get; }
    public Counter<long> SystemMessages { get; }
    public Counter<long> Connections { get; }
    public Counter<long> Disconnections { get; }

    public ChatMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("ChatApp.SignalR", "1.0.0");

        MessagesSent = _meter.CreateCounter<long>(
            "chat.messages.sent",
            description: "Total user messages sent through SignalR");

        SystemMessages = _meter.CreateCounter<long>(
            "chat.messages.system",
            description: "System messages (join/leave announcements)");

        Connections = _meter.CreateCounter<long>(
            "chat.connections.total",
            description: "Total SignalR connections established");

        Disconnections = _meter.CreateCounter<long>(
            "chat.disconnections.total",
            description: "Total SignalR disconnections");
    }

    /// <summary>
    /// Creates an observable gauge that reports the current number of active connections.
    /// </summary>
    public void CreateActiveConnectionsGauge(Func<int> activeCountProvider)
    {
        _meter.CreateObservableGauge(
            "chat.connections.active",
            activeCountProvider,
            description: "Current number of active SignalR connections");
    }
}
