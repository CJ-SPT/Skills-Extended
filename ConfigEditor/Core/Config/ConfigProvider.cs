using System.IO;
using System.Text.Json;
using System.Xml;
using ConfigEditor.Core.Models;

namespace ConfigEditor.Core.Config;

public static class ConfigProvider
{
	public static SkillsConfigModel SkillsConfig { get; private set; }
	
	private static readonly string BasePath = Path.Combine(AppContext.BaseDirectory, "config");
	private static readonly string SkillsConfigPath = Path.Combine(BasePath, "SkillsConfig.json");
	private static readonly string AdditionalWeaponsPath = Path.Combine(BasePath, "AdditionalWeapons.json");
	private static readonly string ServerConfigPath = Path.Combine(BasePath, "ServerConfig.json");
	private static readonly string SkillRewardsPath = Path.Combine(BasePath, "SkillRewards.json");
	
	public static void LoadConfigs()
	{
		SkillsConfig = LoadJson<SkillsConfigModel>(SkillsConfigPath)!;
	}

	public static async Task SaveConfigs()
	{
		await SaveJson(SkillsConfigPath, SkillsConfig);
	}
	
	private static T? LoadJson<T>(string path)
	{
		if (!File.Exists(path))
		{
			return default;
		}

		var options = new JsonSerializerOptions()
		{
			AllowTrailingCommas = true,
			IncludeFields = true,
			ReadCommentHandling = JsonCommentHandling.Skip
		};
		
		var text = File.ReadAllText(path);
		return JsonSerializer.Deserialize<T>(text, options);
	}

	private static async Task SaveJson<T>(string path, T value)
	{
		var options = new JsonSerializerOptions()
		{
			AllowTrailingCommas = true,
			IncludeFields = true,
			WriteIndented = true,
			ReadCommentHandling = JsonCommentHandling.Skip
		};

		await using var fs = new FileStream(path, FileMode.Create);
		await JsonSerializer.SerializeAsync(fs, value, options);
	}
}