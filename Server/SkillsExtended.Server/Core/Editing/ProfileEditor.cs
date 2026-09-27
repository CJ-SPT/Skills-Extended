using System.Globalization;

namespace SkillsExtended.Core.Editing;

public record ProfileChoice(string Id, string Name);

public record ProfileSkillSnapshot(
    string ProfileId,
    string Side,
    Dictionary<string, double> Progress
);

public record LiveSkill(string Id, Func<double> Read, Action<double> Write);

public interface IProfileSkillStore
{
    IReadOnlyList<ProfileChoice> Profiles();
    IReadOnlyList<LiveSkill>? Skills(string profileId, string side);
    Task Save(string profileId, string side, IReadOnlyDictionary<string, double> expected);
}

public sealed class ProfileDraft(ProfileSkillSnapshot snapshot)
{
    public ProfileSkillSnapshot Baseline { get; } =
        new(snapshot.ProfileId, snapshot.Side, new(snapshot.Progress));
    public Dictionary<string, double> Changes { get; } = [];
    public Dictionary<string, string> Inputs { get; } = [];
    public Dictionary<string, string> Errors { get; } = [];
    public bool Dirty => Changes.Count > 0 || Errors.Count > 0;

    public int Level(string id) =>
        (int)
            Math.Clamp(
                Math.Floor(Changes.GetValueOrDefault(id, Baseline.Progress[id]) / 100),
                0,
                51
            );

    public void Edit(string id, string text)
    {
        if (!Baseline.Progress.ContainsKey(id))
            return;
        Inputs[id] = text;
        if (
            !int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var level)
            || level < 0
            || level > 51
        )
        {
            Errors[id] = "Enter a whole level from 0 to 51.";
            return;
        }
        Errors.Remove(id);
        // Restoring the original displayed level also restores its fractional progress.
        if (level == Math.Clamp(Math.Floor(Baseline.Progress[id] / 100), 0, 51))
            Changes.Remove(id);
        else
            Changes[id] = level * 100d;
    }
}

public sealed class ProfileEditor(IProfileSkillStore store)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public IReadOnlyList<ProfileChoice> Profiles() => store.Profiles();

    public ProfileSkillSnapshot? Snapshot(string id, string side)
    {
        if (side is not ("Pmc" or "Scav"))
            return null;
        var skills = store.Skills(id, side);
        return skills is null ? null : new(id, side, skills.ToDictionary(s => s.Id, s => s.Read()));
    }

    public async Task<EditResult> ApplyAsync(
        ProfileSkillSnapshot baseline,
        IReadOnlyDictionary<string, double> requested
    )
    {
        var changes = new Dictionary<string, double>(requested);
        if (
            baseline.Side is not ("Pmc" or "Scav")
            || changes.Any(p =>
                !baseline.Progress.ContainsKey(p.Key)
                || !double.IsFinite(p.Value)
                || p.Value < 0
                || p.Value > 5100
                || p.Value % 100 != 0
            )
        )
            return new(
                EditStatus.Validation,
                "Choose an existing skill and a whole level from 0 to 51."
            );
        if (changes.Count == 0)
            return new(EditStatus.Success, "No profile changes to apply.");
        await _gate.WaitAsync();
        try
        {
            var live = store.Skills(baseline.ProfileId, baseline.Side)?.ToDictionary(s => s.Id);
            if (live is null)
                return new(
                    EditStatus.Conflict,
                    "The profile or character no longer exists. Reload the selection."
                );
            foreach (var id in changes.Keys)
                if (!live.TryGetValue(id, out var skill) || skill.Read() != baseline.Progress[id])
                    return new(
                        EditStatus.Conflict,
                        $"{id} changed since this page was loaded. Your draft is intact. Discard and reload before trying again."
                    );
            var original = changes.Keys.ToDictionary(id => id, id => live[id].Read());
            try
            {
                foreach (var (id, progress) in changes)
                    live[id].Write(progress);
                await store.Save(baseline.ProfileId, baseline.Side, changes);
                return new(
                    EditStatus.Success,
                    $"Saved {changes.Count} skill changes for {baseline.Side}. Start the game client to load them."
                );
            }
            catch (Exception ex)
            {
                var restored = new Dictionary<string, double>();
                foreach (var (id, progress) in original)
                {
                    // Do not undo an unrelated update that arrived during an awaited save.
                    if (live[id].Read() == changes[id])
                    {
                        live[id].Write(progress);
                        restored[id] = progress;
                    }
                }
                string recovery;
                try
                {
                    // Also resets SPT's save hash after a failed write, making a retry effective.
                    await store.Save(baseline.ProfileId, baseline.Side, restored);
                    recovery = "Original affected values were restored and saved.";
                }
                catch (Exception rollback)
                {
                    recovery =
                        $"Affected in-memory values were restored, but disk recovery failed: {rollback.Message}. Resolve the storage error before playing.";
                }
                return new(
                    EditStatus.Persistence,
                    $"Profile save failed: {ex.Message}. {recovery} Your draft is intact."
                );
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
