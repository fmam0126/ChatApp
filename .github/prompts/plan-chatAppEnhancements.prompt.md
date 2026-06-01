## Plan: JWT Auth + DB Persistence + Chat History + Spectre.Console

**TL;DR** — Add proper JWT authentication (symmetric HMAC-SHA256) to both server and CLI to prevent duplicate usernames per-session; migrate from in-memory DB to SQLite for persistent message storage; auto-load recent chat history on CLI connect; and replace plain `Console` I/O with Spectre.Console for a rich terminal experience.

---

### Phase 1: Server — SQLite Persistence _(no dependencies)_

**Step 1.** Add `Microsoft.EntityFrameworkCore.Sqlite` NuGet package to `ChatApp.server.csproj`.

**Step 2.** Add a `ConnectionStrings:DefaultConnection` entry to `appsettings.json` pointing to a SQLite file (e.g., `Data Source=chatapp.db`).

**Step 3.** In `Program.cs`, replace `options.UseInMemoryDatabase("ChatDatabase")` with `options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))`.

**Step 4.** Ensure the DB is created on startup — call `context.Database.EnsureCreated()` at app startup (or use migrations). Add a simple migration or seed logic.

**Verification:** Run the server, confirm `chatapp.db` file is created. Call `POST /ChatMessages` via Swagger and verify the row persists across server restarts.

---

### Phase 2: Server — JWT Authentication Overhaul _(depends on Phase 1)_

**Step 5.** Add a `JwtSettings` section to `appsettings.json` with `SecretKey` (at least 32 chars), `Issuer`, `Audience`, and `ExpirationMinutes`.

**Step 6.** In `Program.cs`, rework the JWT auth setup:

- Remove the placeholder `Authority` URL and external-authority assumption.
- Configure `AddJwtBearer` to use `TokenValidationParameters` with symmetric `IssuerSigningKey` (from the secret key in config), validate issuer/audience/lifetime.
- Keep the `OnMessageReceived` event hook for reading tokens from SignalR query strings.
- Remove the duplicate `AddAuthentication` call (currently called twice — once with `BearerTokenDefaults` and once with JWT — consolidate into ONE call chain).

**Step 7.** Create a `TokenService` class (in `Class/` or a new `Services/` folder):

- Method `GenerateToken(User user)` → returns a JWT string with claims: `sub` (user ID), `unique_name` (username), `iat`, `exp`.
- Uses `Microsoft.IdentityModel.Tokens` and `System.IdentityModel.Tokens.Jwt`.
- Register as a singleton in DI.

**Step 8.** Create a `/auth/login` POST endpoint in `AuthController`:

- Accepts `{ "username": "..." }` DTO.
- Checks if username is already taken by an active SignalR connection (inject a `ConnectedUsersService` — a singleton `ConcurrentDictionary<string, string>` mapping `username → connectionId`).
- If username is in use by an active connection → return `409 Conflict` with message "Username already taken".
- If username is new: find or create a `User` entity in DB, generate a JWT via `TokenService`, return `{ "token": "...", "username": "...", "userId": ... }`.
- Delete the existing `GET /auth/token` endpoint (it just reads an existing token — not needed).

**Step 9.** Create `ConnectedUsersService` (singleton):

- `ConcurrentDictionary<string, string> ActiveUsers` (username → connectionId).
- Methods: `TryAddUser(username, connectionId) → bool`, `RemoveUser(connectionId)`, `GetUsername(connectionId) → string?`.
- Register in DI.

**Verification:** Use Swagger to call `POST /auth/login` with a username → get a JWT. Call again with same username before connecting via SignalR → get 409. Connect via SignalR with the token → username is tracked. Disconnect → username freed.

---

### Phase 3: Server — Message Persistence in SignalR Hub _(depends on Phase 2)_

**Step 10.** Inject `ChatContext` and `ConnectedUsersService` into `ChatHub` via constructor.

**Step 11.** Override `OnConnectedAsync`:

- Extract username from `Context.User?.FindFirst("unique_name")?.Value`.
- Call `ConnectedUsersService.TryAddUser(username, Context.ConnectionId)`.
- If duplicate → throw `HubException` ("Username already taken") to reject connection.
- Broadcast a system message: `"X has joined the chat"` via `Clients.All.SendAsync("ReceiveMessage", "System", "...")`.

**Step 12.** Override `OnDisconnectedAsync`:

- Remove user from `ConnectedUsersService`.
- Broadcast: `"X has left the chat"`.

**Step 13.** Modify `SendMessage(string user, string message)`:

- Extract the real username from `Context.User` claims (ignore the `user` parameter to prevent impersonation — or keep it but validate it matches the token's username).
- Create a `ChatMessage` entity: `Content = message`, `SenderId = userId from claims`, `Created = DateTime.UtcNow`.
- Save to `_context.SaveChangesAsync()`.
- Broadcast: `Clients.All.SendAsync("ReceiveMessage", username, message)`.

**Verification:** Connect two CLI instances with different usernames. Send messages. Verify they appear in DB (check SQLite file). Verify duplicate username is rejected. Verify join/leave system messages.

---

### Phase 4: Server — Chat History Endpoint Enhancement _(depends on Phase 3)_

**Step 14.** Update `GET /ChatMessages` in `ChatMessagesController`:

- Add optional query param `?count=50` (default 50, max 200).
- Return messages ordered by `Created` **descending** (most recent first), then reverse client-side if needed.
- Include sender username by `.Include(m => m.Sender)` or a join — add a navigation property `User Sender` to the `ChatMessage` model.
- Return a DTO (not the raw entity) that includes `Id`, `Content`, `Created`, `SenderId`, `SenderName`.
- Add `[Authorize]` attribute to require a valid JWT.

**Step 15.** Add navigation property to `ChatMessage` model:

- `public User Sender { get; set; } = null!;`
- The `SenderId` foreign key already exists.

**Verification:** Call `GET /ChatMessages?count=10` with a valid JWT → get JSON array with sender names, ordered by most recent first.

---

### Phase 5: CLI — Spectre.Console Migration _(can run parallel with Phase 1–4)_

**Step 16.** Add `Spectre.Console` NuGet package to `ChatApp.CliClient.csproj`.

**Step 17.** Create a `SpectreDisplay` helper class (in `Classes/` or a new `Display/` folder):

- `RenderMessage(string username, string message, DateTime timestamp)` → uses a colored `Panel`:
  - Username in bold with a color derived from the username (hash → color).
  - Timestamp in grey/dim.
  - System messages in blue/dim italic.
- `RenderHistory(IEnumerable<ChatMessage> messages)` → uses a `Table` with columns: Time, User, Message.
- `ShowStatus(string text, Action action)` → wraps an action with `AnsiConsole.Status()` spinner.
- `Prompt(string text)` → wraps `AnsiConsole.Prompt(new TextPrompt<string>(text))`.
- `ShowError(string message)` → red markup.
- `ShowInfo(string message)` → blue markup.
- `ShowSuccess(string message)` → green markup.

**Step 18.** Refactor `Program.cs`:

- Remove the large block of commented-out DI/Host/HttpClient code.
- Replace all `Console.WriteLine` / `Console.ReadLine` with `SpectreDisplay` methods.
- Add a welcome banner using `FigletText` or a simple `Rule`.
- Connection status shown with a spinner.

**Verification:** Run the CLI — confirm styled output, colored usernames, formatted panels for messages.

---

### Phase 6: CLI — JWT Auth Flow _(depends on Phase 2 + Phase 5)_

**Step 19.** Remove hardcoded `UserName` and `AcessToken` from `appsettings.json` (keep `ServerUrl`). The token will be acquired dynamically.

**Step 20.** Add an `AuthService` class to the CLI:

- `Task<string?> LoginAsync(string serverUrl, string username)`:
  - POSTs to `{serverUrl}/auth/login` with JSON `{ "username" }`.
  - On 409 → returns null (username taken), caller prompts for a different name.
  - On 200 → deserializes the token, returns it.
  - On error → throws with descriptive message.

**Step 21.** Update `Program.cs` startup flow:

1. Show welcome banner.
2. Prompt for username via `SpectreDisplay.Prompt`.
3. Call `AuthService.LoginAsync` in a loop until a unique name is chosen.
4. Store the returned JWT token in memory.
5. Pass token to SignalR `HubConnectionBuilder` via `options.AccessTokenProvider`.
6. Also store token for REST API calls (chat history).

**Step 22.** Update `Settings.cs`:

- Make `UserName` and `AcessToken` optional (not `required`), or remove them entirely.
- Fix the typo: `AcessToken` → `AccessToken` (or just remove it).

**Verification:** Run CLI → prompt for username → receive JWT → connect with that JWT. Try a second CLI instance with the same name → rejected, prompted to pick another.

---

### Phase 7: CLI — Chat History on Connect _(depends on Phase 4 + Phase 6)_

**Step 23.** Revive and update the `ChatClient` class (HTTP-based, previously unused):

- Modify `GetMessagesAsync()` to accept a `count` parameter and pass the JWT in an `Authorization: Bearer` header.
- Use `SpectreDisplay.ShowStatus` to show a spinner while fetching.

**Step 24.** In `Program.cs`, after successful SignalR connection, load chat history:

- Call `chatClient.GetMessagesAsync(count: 50)`.
- Display via `SpectreDisplay.RenderHistory(messages)`.
- Then enter the live chat loop.

**Step 25.** Register `ChatClient` and `HttpClient` properly:

- Create an `HttpClient` with `BaseAddress` set to the server URL and default `Authorization` header with the JWT.
- Use this for both history loading and potential future REST calls.

**Verification:** Connect → see recent messages displayed in a formatted table before the live chat prompt appears.

---

### Phase 8: Polish & Cleanup _(depends on all previous phases)_

**Step 26.** Server-side input sanitization:

- In `ChatHub.SendMessage`, trim and sanitize messages (strip control characters, limit length to e.g., 2000 chars).
- Validate username format (alphanumeric, length 3–30).

**Step 27.** Update `TODO.txt` to reflect completed items.

**Step 28.** Test end-to-end:

- Server starts → SQLite DB created.
- CLI 1 connects with "Alice" → gets token → sees history → sends messages.
- CLI 2 tries "Alice" → rejected → picks "Bob" → sees join message from Alice → both exchange messages.
- Both see messages persisted in DB and visible in history.
- Server restart → messages survive, users can reconnect with fresh tokens.

---

### Relevant Files (to be modified)

| File                                                   | Action                                                                        |
| ------------------------------------------------------ | ----------------------------------------------------------------------------- |
| `ChatApp.server/ChatApp.server.csproj`                 | Add `Microsoft.EntityFrameworkCore.Sqlite`                                    |
| `ChatApp.server/appsettings.json`                      | Add `ConnectionStrings`, `JwtSettings` sections                               |
| `ChatApp.server/Program.cs`                            | Rework JWT config, switch to SQLite, add services                             |
| `ChatApp.server/Models/ChatMessage.cs`                 | Add `Sender` navigation property                                              |
| `ChatApp.server/Models/User.cs`                        | May need no changes (already has Id + Name)                                   |
| `ChatApp.server/Hubs/ChatHub.cs`                       | Inject DbContext + ConnectedUsersService, persist messages, track connections |
| `ChatApp.server/Controllers/AuthController.cs`         | Replace with `POST /auth/login`, remove `GET /auth/token`                     |
| `ChatApp.server/Controllers/ChatMessagesController.cs` | Add `[Authorize]`, DTO with sender name, `?count=` param, `.Include()`        |
| `ChatApp.server/DTO/ChatMessageDTO.cs`                 | Add response DTO class (or modify existing)                                   |
| `ChatApp.server/Class/` (new files)                    | `TokenService.cs`, `ConnectedUsersService.cs`, DTOs                           |
| `ChatApp.CliClient/ChatApp.CliClient.csproj`           | Add `Spectre.Console`                                                         |
| `ChatApp.CliClient/appsettings.json`                   | Remove `UserName`/`AcessToken`, keep `ServerUrl`                              |
| `ChatApp.CliClient/Program.cs`                         | Major refactor: auth flow, Spectre display, history loading, cleanup          |
| `ChatApp.CliClient/Models/Settings.cs`                 | Remove or make optional `UserName`/`AcessToken`                               |
| `ChatApp.CliClient/Classes/ChatClient.cs`              | Revive with JWT header support, history fetching                              |
| `ChatApp.CliClient/Classes/` (new files)               | `SpectreDisplay.cs`, `AuthService.cs`                                         |

### Verification

1. **Automated:** Build both projects — `dotnet build` must succeed with zero errors.
2. **Automated:** Run server — `dotnet run` — confirm SQLite file created, Swagger UI loads, `/auth/login` returns 200 with token.
3. **Manual:** Connect CLI → pick unique name → see history → send messages → second CLI with same name rejected → second CLI with different name works → both see each other's messages.
4. **Manual:** Restart server → reconnect CLI → old messages still visible in history.
5. **Manual:** Verify Spectre.Console styling: colored usernames, formatted panels, spinner during connect/load.

### Decisions

- **SQLite** for zero-setup persistence (over SQL Server).
- **Symmetric HMAC-SHA256** JWT signing (simpler than asymmetric for this use case).
- **Per-session username uniqueness** — usernames are freed on disconnect (chat-room style, not permanent accounts).
- **Auto-load last 50 messages on connect** (no `/history` command — keeps it simple).
- **Server-issued tokens only** — the hardcoded token in `appsettings.json` is removed; all tokens come from the `/auth/login` endpoint.
- **SignalR hub uses token claims** for identity — the `user` parameter in `SendMessage` is validated against the JWT to prevent impersonation.

### Further Considerations

1. **Token refresh** — Not implemented in this plan. Tokens expire based on `ExpirationMinutes`. When a token expires, the SignalR connection will drop. A future enhancement could add refresh tokens or auto-re-login on disconnect.
2. **Pagination** — Only a simple `?count=` parameter. Full cursor/offset pagination is excluded from this plan.
3. **Private messages / rooms** — Excluded; this plan keeps the broadcast-to-all model.
