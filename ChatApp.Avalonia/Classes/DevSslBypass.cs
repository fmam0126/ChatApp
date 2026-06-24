namespace ChatApp.Avalonia.Services;

/// <summary>
/// Development-only helper that bypasses SSL certificate validation.
/// Required for local development with self-signed certificates (e.g., Traefik).
/// REMOVE before any production deployment.
/// </summary>
public static class DevSslBypass
{
    /// <summary>
    /// Returns an HttpClientHandler that accepts all certificates.
    /// Only use for local development.
    /// </summary>
    public static HttpClientHandler CreateHandler()
    {
        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
#endif
        return handler;
    }
}
