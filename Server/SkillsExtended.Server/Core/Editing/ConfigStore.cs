using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SkillsExtended.Config;
using SkillsExtended.Models;

namespace SkillsExtended.Core.Editing;

public record ConfigSnapshot(SkillsConfig Skills, ServerConfig Server, string Revision,
    IReadOnlyDictionary<string, float>? NativeDefaults = null);

public enum EditStatus
{
    Success,
    Validation,
    Conflict,
    Persistence,
}

public record EditResult(EditStatus Status, string Message, ConfigSnapshot? Snapshot = null)
{
    public bool Success => Status == EditStatus.Success;
}

// Replaceable filesystem boundary for offline failure and rollback checks.
public interface IConfigFiles
{
    Task<string> Read(string path);
    Task Write(string path, string text);
    void Replace(string staged, string destination, string backup);
    void Restore(string backup, string destination);
    void Delete(string path);
}

public sealed class ConfigFiles : IConfigFiles
{
    public Task<string> Read(string path) => File.ReadAllTextAsync(path);

    public Task Write(string path, string text) => File.WriteAllTextAsync(path, text);

    public void Replace(string staged, string destination, string backup) =>
        File.Replace(staged, destination, backup);

    public void Restore(string backup, string destination) => File.Move(backup, destination, true);

    public void Delete(string path) => File.Delete(path);
}

public sealed class ConfigStore(string directory, IConfigFiles files)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static T Clone<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!;

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private string SkillsPath => Path.Combine(directory, "SkillsConfig.json");
    private string ServerPath => Path.Combine(directory, "ServerConfig.json");

    private static string Revision(string skills, string server) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(skills + "\0" + server)));

    public async Task<ConfigSnapshot> ReadSnapshotAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return await ReadCore();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ConfigSnapshot> ReadCore()
    {
        var skills = await files.Read(SkillsPath);
        var server = await files.Read(ServerPath);
        return new(
            JsonSerializer.Deserialize<SkillsConfig>(skills)
                ?? throw new InvalidDataException("Skills configuration is empty."),
            JsonSerializer.Deserialize<ServerConfig>(server)
                ?? throw new InvalidDataException("Server configuration is empty."),
            Revision(skills, server)
        );
    }

    public async Task<EditResult> SaveAsync(
        SkillsConfig skills,
        ServerConfig server,
        string expectedRevision,
        Action<ConfigSnapshot> publish
    )
    {
        var errors = ConfigRules.Validate(skills);
        errors.AddRange(ConfigRules.Validate(server));
        if (errors.Count > 0)
            return new(EditStatus.Validation, string.Join("\n", errors));
        // Detach before the first await; later edits cannot change this transaction.
        var skillsText = Serialize(skills);
        var serverText = Serialize(server);
        var committed = new ConfigSnapshot(
            Clone(skills),
            Clone(server),
            Revision(skillsText, serverText)
        );
        await _gate.WaitAsync();
        var suffix = "." + Guid.NewGuid().ToString("N");
        var paths = new[] { SkillsPath, ServerPath };
        var replaced = new List<string>();
        var keepBackups = false;
        try
        {
            if ((await ReadCore()).Revision != expectedRevision)
                return new(
                    EditStatus.Conflict,
                    "Configuration changed in another editor or on disk. Your draft is intact. Discard changes to reload the saved configuration before editing again."
                );
            await files.Write(SkillsPath + suffix + ".tmp", skillsText);
            await files.Write(ServerPath + suffix + ".tmp", serverText);
            if ((await ReadCore()).Revision != expectedRevision)
                return new(
                    EditStatus.Conflict,
                    "Configuration changed on disk while saving. Discard changes to reload it; your draft has been retained."
                );
            foreach (var path in paths)
            {
                files.Replace(path + suffix + ".tmp", path, path + suffix + ".bak");
                replaced.Add(path);
            }
            publish(committed);
            return new(
                EditStatus.Success,
                "Configuration saved. Restart connected game clients to load the new settings.",
                Clone(committed)
            );
        }
        catch (Exception ex)
        {
            var rollbackErrors = new List<string>();
            foreach (var path in replaced.AsEnumerable().Reverse())
            {
                try
                {
                    files.Restore(path + suffix + ".bak", path);
                }
                catch (Exception rollback)
                {
                    keepBackups = true;
                    rollbackErrors.Add($"{path + suffix + ".bak"}: {rollback.Message}");
                }
            }
            var recovery =
                rollbackErrors.Count == 0
                    ? "Original files restored; your draft is intact."
                    : "Recovery required. Retained backups: " + string.Join("; ", rollbackErrors);
            return new(
                EditStatus.Persistence,
                $"Could not save configuration: {ex.Message} {recovery}"
            );
        }
        finally
        {
            foreach (var path in paths)
            {
                try
                {
                    files.Delete(path + suffix + ".tmp");
                }
                catch
                { /* Cleanup must not mask the save result. */
                }
                if (!keepBackups)
                {
                    try
                    {
                        files.Delete(path + suffix + ".bak");
                    }
                    catch { }
                }
            }
            _gate.Release();
        }
    }
}
