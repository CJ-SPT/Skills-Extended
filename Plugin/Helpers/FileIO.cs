using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SkillsExtended.Helpers
{
    public static class FileIO
    {
        private static string _profileDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Profiles");

        public static void SaveProfileToDisk(Profile profile)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_profileDirectory, profile.ProfileId + ".json"));

            if (Directory.Exists(_profileDirectory))
            {
                string json = JsonConvert.SerializeObject(profile, Formatting.Indented);

                File.WriteAllText(fullPath, json);

                Plugin.Log.LogDebug($"Profile {profile.ProfileId} saved to disk");
                return;
            }

            Plugin.Log.LogDebug($"Error saving profile. Path does not exist.");
        }

        public static Profile LoadProfileFromDisk(string profileId)
        {
            var fullPath = Path.GetFullPath(Path.Combine(_profileDirectory, profileId + ".json"));

            Plugin.Log.LogDebug($"{fullPath}");

            if (File.Exists(fullPath))
            {
                string json = File.ReadAllText(fullPath);

                var profile = JsonConvert.DeserializeObject<Profile>(json);

                Plugin.Log.LogDebug($"Loaded {profileId} from disk.");

                return profile;
            }

            Plugin.Log.LogDebug($"Error loading {profileId} from disk.");
            return null;
        }
    }
}
