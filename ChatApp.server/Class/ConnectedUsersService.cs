using System.Collections.Concurrent;

namespace ChatApp.server.Class;

public class ConnectedUsersService
{
    private readonly ConcurrentDictionary<string, string> _activeUsers = new(); // username → connectionId
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new(); // connectionId → username

    public bool TryAddUser(string username, string connectionId)
    {
        if (_activeUsers.TryAdd(username, connectionId))
        {
            _connectionToUser[connectionId] = username;
            return true;
        }
        return false;
    }

    public void RemoveUser(string connectionId)
    {
        if (_connectionToUser.TryRemove(connectionId, out var username))
        {
            _activeUsers.TryRemove(username, out _);
        }
    }

    public bool IsUsernameTaken(string username)
    {
        return _activeUsers.ContainsKey(username);
    }

    public string? GetUsername(string connectionId)
    {
        _connectionToUser.TryGetValue(connectionId, out var username);
        return username;
    }
}
