using System.Security.Claims;
using ChatApp.server.Class;
using ChatApp.server.Hubs;
using ChatApp.server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ChatApp.Server.Tests.Hubs;

public class ChatHubTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ConnectedUsersService _connectedUsers;
    private readonly ChatContext _dbContext;
    private readonly ChatMetrics _metrics;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockAll;
    private readonly ChatHub _hub;

    public ChatHubTests()
    {
        // Use SQLite in-memory for real relational behavior
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ChatContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new ChatContext(options);
        _dbContext.Database.EnsureCreated();

        _connectedUsers = new ConnectedUsersService();
        _metrics = new ChatMetrics();

        _mockAll = new Mock<IClientProxy>();
        _mockAll.Setup(m => m.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockClients = new Mock<IHubCallerClients>();
        _mockClients.Setup(c => c.All).Returns(_mockAll.Object);

        _hub = new ChatHub(_dbContext, _connectedUsers, _metrics)
        {
            Clients = _mockClients.Object,
            Context = CreateMockContext("testuser", "1", "conn-test-1").Object
        };
    }
    /// <summary>
    /// Creates a mocked HubCallerContext for use in tests. The mocked context contains a
    /// ClaimsPrincipal populated with the provided username and userId, and returns the specified
    /// connectionId from the ConnectionId property.
    /// </summary>
    /// <param name="username">The user name to include in the mocked ClaimsPrincipal.</param>
    /// <param name="userId">The unique identifier to include as the NameIdentifier claim.</param>
    /// <param name="connectionId">The SignalR connection id to set on the mocked context.</param>
    /// <returns>A Mock of HubCallerContext with User and ConnectionId configured.</returns>
    private static Mock<HubCallerContext> CreateMockContext(string username, string userId, string connectionId)
    {
        var mockContext = new Mock<HubCallerContext>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new("unique_name", username),
            new(ClaimTypes.NameIdentifier, userId),
        };
        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);

        mockContext.Setup(c => c.User).Returns(principal);
        mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
        return mockContext;
    }

    [Fact]
    public async Task OnConnectedAsync_AddsUserToService()
    {
        // Act
        await _hub.OnConnectedAsync();
        
        // Assert 
        Assert.True(_connectedUsers.IsUsernameTaken("testuser"));
        Assert.Equal("testuser", _connectedUsers.GetUsername("conn-test-1"));
    }

    [Fact]
    public async Task OnConnectedAsync_BroadcastsJoinMessage()
    {
        
        await _hub.OnConnectedAsync();

        _mockAll.Verify(
            m => m.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(args =>
                    (string)args[0] == "System" &&
                    ((string)args[1]).Contains("testuser") &&
                    ((string)args[1]).Contains("joined")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_DuplicateUsername_ThrowsHubException()
    {
        _connectedUsers.TryAddUser("testuser", "existing-conn");

        await Assert.ThrowsAsync<HubException>(() => _hub.OnConnectedAsync());
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesUserAndBroadcasts()
    {
        _connectedUsers.TryAddUser("testuser", "conn-test-1");

        await _hub.OnDisconnectedAsync(null);

        Assert.False(_connectedUsers.IsUsernameTaken("testuser"));

        _mockAll.Verify(
            m => m.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(args =>
                    (string)args[0] == "System" &&
                    ((string)args[1]).Contains("testuser") &&
                    ((string)args[1]).Contains("left")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_NonexistentUser_DoesNotThrow()
    {
        // Should not throw; just no broadcast
        await _hub.OnDisconnectedAsync(null);

        _mockAll.Verify(
            m => m.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendMessage_ValidMessage_PersistsAndBroadcasts()
    {
        // Seed the user first (SQLite enforces FK)
        _dbContext.Users.Add(new User { Id = 42, Name = "sender" });
        _dbContext.SaveChanges();

        var hub = new ChatHub(_dbContext, _connectedUsers, _metrics)
        {
            Clients = _mockClients.Object,
            Context = CreateMockContext("sender", "42", "conn-send-1").Object
        };

        await hub.SendMessage("Hello everybody!");

        // Verify persisted
        var messages = _dbContext.ChatMessages.ToList();
        Assert.Single(messages);
        Assert.Equal("Hello everybody!", messages[0].Content);
        Assert.Equal(42, messages[0].SenderId);

        // Verify broadcast
        _mockAll.Verify(
            m => m.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(args =>
                    (string)args[0] == "sender" &&
                    (string)args[1] == "Hello everybody!"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_EmptyOrWhitespace_DoesNotPersist()
    {
        _dbContext.Users.Add(new User { Id = 1, Name = "sender" });
        _dbContext.SaveChanges();

        var hub = new ChatHub(_dbContext, _connectedUsers, _metrics)
        {
            Clients = _mockClients.Object,
            Context = CreateMockContext("sender", "1", "conn-1").Object
        };

        await hub.SendMessage("   ");

        Assert.Empty(_dbContext.ChatMessages.ToList());
    }

    [Fact]
    public async Task SendMessage_ExceedsMaxLength_Truncates()
    {
        _dbContext.Users.Add(new User { Id = 1, Name = "sender" });
        _dbContext.SaveChanges();

        var hub = new ChatHub(_dbContext, _connectedUsers, _metrics)
        {
            Clients = _mockClients.Object,
            Context = CreateMockContext("sender", "1", "conn-1").Object
        };

        var longMessage = new string('A', 2500);
        await hub.SendMessage(longMessage);

        var msg = _dbContext.ChatMessages.Single();
        Assert.Equal(2000, msg.Content.Length);
    }

    public void Dispose()
    {
        _connection.Dispose();
        _dbContext.Dispose();
    }
}
