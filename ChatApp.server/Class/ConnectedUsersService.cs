using System.Collections.Concurrent;

namespace ChatApp.server.Class;

public class ConnectedUsersService
{
    private readonly ConcurrentDictionary<string, string> _activeUsers = new(); // username → connectionId
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new(); // connectionId → username
    /// <summary>
    /// Tries to add a user to the active users list. Returns false if the username is already taken.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="connectionId">Connection ID</param>
    /// <returns>True if the user was added, false otherwise</returns>
    public bool TryAddUser(string username, string connectionId)
    {
        if (_activeUsers.TryAdd(username, connectionId))
        {
            _connectionToUser[connectionId] = username;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Removes a user from the active users list based on their connection ID. If the connection ID is found, the associated username is also removed from the active users list.
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    public void RemoveUser(string connectionId)
    {
        if (_connectionToUser.TryRemove(connectionId, out var username))
        {
            _activeUsers.TryRemove(username, out _);
        }
    }
    /// <summary>
    /// Checks if a username is already taken by another active user. Returns true if the username is already in use, false otherwise.
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>True if the username is already taken, false otherwise</returns>
    public bool IsUsernameTaken(string username)
    {
        return _activeUsers.ContainsKey(username);
    }

    /// <summary>
    /// Gets the username associated with a given connection ID.
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <returns>Username if found, null otherwise</returns>
    public string? GetUsername(string connectionId)
    {
        _connectionToUser.TryGetValue(connectionId, out var username);
        return username;
    }
}
