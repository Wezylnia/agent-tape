using System.Text.Json;
using AgentTape.Core.Abstractions;
using AgentTape.Core.Configuration;
using AgentTape.Core.Models;

namespace AgentTape.Core.Storage;

/// <summary>
/// Reads session data from the .agenttape/sessions directory layout.
/// </summary>
public sealed class FileSystemSessionReader : ISessionReader
{
    private readonly AgentTapeOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public FileSystemSessionReader(AgentTapeOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<IReadOnlyList<TapeSession>> ListAsync(CancellationToken cancellationToken)
    {
        var sessionsDir = Path.Combine(_options.AgentTapeDirectory, "sessions");
        if (!Directory.Exists(sessionsDir))
        {
            return Task.FromResult<IReadOnlyList<TapeSession>>(Array.Empty<TapeSession>());
        }

        var sessions = new List<TapeSession>();
        var dirs = Directory.GetDirectories(sessionsDir);

        foreach (var dir in dirs.OrderDescending())
        {
            var sessionJsonPath = Path.Combine(dir, "session.json");
            if (!File.Exists(sessionJsonPath))
                continue;

            try
            {
                var json = File.ReadAllText(sessionJsonPath);
                var session = JsonSerializer.Deserialize<TapeSession>(json, JsonOptions);
                if (session is not null)
                {
                    sessions.Add(session);
                }
            }
            catch
            {
                // Skip corrupt sessions
            }
        }

        return Task.FromResult<IReadOnlyList<TapeSession>>(sessions);
    }

    public Task<TapeSession?> FindAsync(string sessionId, CancellationToken cancellationToken)
    {
        var sessionJsonPath = Path.Combine(_options.AgentTapeDirectory, "sessions", sessionId, "session.json");
        if (!File.Exists(sessionJsonPath))
        {
            return Task.FromResult<TapeSession?>(null);
        }

        try
        {
            var json = File.ReadAllText(sessionJsonPath);
            var session = JsonSerializer.Deserialize<TapeSession>(json, JsonOptions);
            return Task.FromResult(session);
        }
        catch
        {
            return Task.FromResult<TapeSession?>(null);
        }
    }
}
