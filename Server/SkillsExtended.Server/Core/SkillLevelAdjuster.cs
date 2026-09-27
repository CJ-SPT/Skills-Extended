using SkillsExtended.Core.Editing;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace SkillsExtended.Core;

[Injectable(InjectionType.Singleton, OnLoadOrder.SaveCallbacks + 1)]
public class SkillLevelAdjuster
{
    private readonly ProfileEditor _editor;
    private readonly ISptLogger<SkillLevelAdjuster> _logger;

    public SkillLevelAdjuster(
        ISptLogger<SkillLevelAdjuster> logger,
        ProfileHelper profileHelper,
        SaveServer saveServer,
        FileUtil fileUtil,
        JsonUtil jsonUtil
    )
    {
        _logger = logger;
        _editor = new(new SptProfileStore(profileHelper, saveServer, fileUtil, jsonUtil));
    }

    public IReadOnlyList<ProfileChoice> GetProfiles() => _editor.Profiles();

    public ProfileSkillSnapshot? GetSnapshot(string id, string side) => _editor.Snapshot(id, side);

    public async Task<EditResult> ApplyAsync(
        ProfileSkillSnapshot baseline,
        IReadOnlyDictionary<string, double> changes
    )
    {
        var result = await _editor.ApplyAsync(baseline, changes);
        if (!result.Success)
            _logger.Warning($"[Skills Extended] {result.Message}");
        return result;
    }

    private sealed class SptProfileStore(
        ProfileHelper profiles,
        SaveServer saves,
        FileUtil files,
        JsonUtil json
    ) : IProfileSkillStore
    {
        public IReadOnlyList<ProfileChoice> Profiles() =>
            profiles
                .GetProfiles()
                .Select(p => new ProfileChoice(
                    p.Key.ToString(),
                    p.Value.ProfileInfo?.Username ?? "Unnamed profile"
                ))
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ToArray();

        private KeyValuePair<MongoId, SptProfile>? Find(string id)
        {
            foreach (var pair in profiles.GetProfiles())
                if (pair.Key.ToString() == id)
                    return pair;
            return null;
        }

        private static IEnumerable<CommonSkill>? CharacterSkills(SptProfile profile, string side) =>
            side switch
            {
                "Pmc" => profile.CharacterData?.PmcData?.Skills?.Common,
                "Scav" => profile.CharacterData?.ScavData?.Skills?.Common,
                _ => null,
            };

        public IReadOnlyList<LiveSkill>? Skills(string profileId, string side)
        {
            var profile = Find(profileId);
            return profile is null
                ? null
                : CharacterSkills(profile.Value.Value, side)
                    ?.Select(skill => new LiveSkill(
                        skill.Id.ToString(),
                        () => skill.Progress,
                        value => skill.Progress = value
                    ))
                    .ToArray();
        }

        public async Task Save(
            string profileId,
            string side,
            IReadOnlyDictionary<string, double> expected
        )
        {
            var pair =
                Find(profileId) ?? throw new InvalidOperationException("Profile no longer exists.");
            if (saves.IsProfileInvalidOrUnloadable(pair.Key))
                throw new InvalidOperationException("SPT cannot save this profile.");
            await saves.SaveProfileAsync(pair.Key, CancellationToken.None);
            // SaveProfileAsync may skip unchanged hashes. Read back the affected skills before reporting success.
            var text = await files.ReadFileAsync(
                Path.Combine("user", "profiles", profileId + ".json")
            );
            var persisted =
                json.Deserialize<SptProfile>(text)
                ?? throw new IOException("Saved profile could not be read back.");
            var skills = CharacterSkills(persisted, side)
                ?.ToDictionary(s => s.Id.ToString(), s => s.Progress);
            if (
                skills is null
                || expected.Any(p => !skills.TryGetValue(p.Key, out var value) || value != p.Value)
            )
                throw new IOException("Saved skill progress did not match the requested changes.");
        }
    }
}
