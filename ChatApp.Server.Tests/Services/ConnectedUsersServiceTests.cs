using ChatApp.server.Class;

namespace ChatApp.Server.Tests.Services;

public class ConnectedUsersServiceTests
{
    private readonly ConnectedUsersService _service;

    public ConnectedUsersServiceTests()
    {
        _service = new ConnectedUsersService();
    }

    [Fact]
    public void TryAddUser_NewUser_ReturnsTrue()
    {
        var result = _service.TryAddUser("cena", "conn-1");

        Assert.True(result);
    }

    [Fact]
    public void TryAddUser_DuplicateUsername_ReturnsFalse()
    {
        _service.TryAddUser("cena", "conn-1");

        var result = _service.TryAddUser("cena", "conn-2");

        Assert.False(result);
    }

    [Fact]
    public void IsUsernameTaken_ExistingUser_ReturnsTrue()
    {
        _service.TryAddUser("bob", "conn-1");

        Assert.True(_service.IsUsernameTaken("bob"));
    }

    [Fact]
    public void IsUsernameTaken_NonexistentUser_ReturnsFalse()
    {
        Assert.False(_service.IsUsernameTaken("nobody"));
    }

    [Fact]
    public void GetUsername_ExistingConnection_ReturnsUsername()
    {
        _service.TryAddUser("charlie", "conn-42");

        var username = _service.GetUsername("conn-42");

        Assert.Equal("charlie", username);
    }

    [Fact]
    public void GetUsername_NonexistentConnection_ReturnsNull()
    {
        var username = _service.GetUsername("nonexistent");

        Assert.Null(username);
    }

    [Fact]
    public void RemoveUser_ExistingConnection_RemovesFromBothDictionaries()
    {
        _service.TryAddUser("dave", "conn-99");

        _service.RemoveUser("conn-99");

        Assert.False(_service.IsUsernameTaken("dave"));
        Assert.Null(_service.GetUsername("conn-99"));
    }

    [Fact]
    public void RemoveUser_NonexistentConnection_DoesNotThrow()
    {
        // Should not throw
        _service.RemoveUser("nonexistent");
    }

    [Fact]
    public void TryAddUser_TwoDifferentUsers_Succeed()
    {
        var r1 = _service.TryAddUser("cena", "conn-1");
        var r2 = _service.TryAddUser("bob", "conn-2");

        Assert.True(r1);
        Assert.True(r2);
        Assert.True(_service.IsUsernameTaken("cena"));
        Assert.True(_service.IsUsernameTaken("bob"));
        Assert.Equal("cena", _service.GetUsername("conn-1"));
        Assert.Equal("bob", _service.GetUsername("conn-2"));
    }

    [Fact]
    public void RemoveUser_OnlyRemovesTargetedUser()
    {
        _service.TryAddUser("cena", "conn-1");
        _service.TryAddUser("bob", "conn-2");

        _service.RemoveUser("conn-1");

        Assert.False(_service.IsUsernameTaken("cena"));
        Assert.True(_service.IsUsernameTaken("bob"));
    }
}
