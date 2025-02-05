using System.IO;
using System.Text.Json;
using System.Xml;
using ConfigEditor.Core.Models;

namespace ConfigEditor.Core.Config;

public static class ConfigProvider
{
	public static SkillsConfigModel? SkillsConfig { get; private set; }
	public static AdditionalWeaponsData? AdditionalWeapons { get; private set; }
	
	private static readonly string BasePath = Path.Combine(AppContext.BaseDirectory, "config");
	private static readonly string SkillsConfigPath = Path.Combine(BasePath, "SkillsConfig.json");
	private static readonly string AdditionalWeaponsPath = Path.Combine(BasePath, "AdditionalWeapons.json");
	private static readonly string ServerConfigPath = Path.Combine(BasePath, "ServerConfig.json");
	private static readonly string SkillRewardsPath = Path.Combine(BasePath, "SkillRewards.json");
	
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		AllowTrailingCommas = true,
		IncludeFields = true,
		WriteIndented = true,
		ReadCommentHandling = JsonCommentHandling.Skip
	};
	
	public static async Task LoadConfigs()
	{
		SkillsConfig = await LoadJson<SkillsConfigModel>(SkillsConfigPath);

		if (Path.Exists(AdditionalWeaponsPath))
		{
			AdditionalWeapons = await LoadJson<AdditionalWeaponsData>(AdditionalWeaponsPath);
		}
		else
		{
			await CreateAdditionalWeaponsData();
		}
	}

	public static async Task SaveConfigs(bool weaponsModified = false)
	{
		await SaveJson(SkillsConfigPath, SkillsConfig);
	}
	
	private static async Task<T?> LoadJson<T>(string path)
	{
		if (!File.Exists(path))
		{
			return default;
		}
		
		await using var fs = new FileStream(path, FileMode.Open);
		return await JsonSerializer.DeserializeAsync<T>(fs, JsonOptions);
	}

	private static async Task SaveJson<T>(string path, T value)
	{ 
		await using var fs = new FileStream(path, FileMode.Create);
		await JsonSerializer.SerializeAsync(fs, value, JsonOptions);
	}

	public static async Task CreateAdditionalWeaponsData()
	{
		if (File.Exists(AdditionalWeaponsPath)) return;
		
		var stream = File.Create(AdditionalWeaponsPath);
		await JsonSerializer.SerializeAsync(
			stream, 
			AdditionalWeapons ?? new AdditionalWeaponsData { AdditionalEasternWeapons = [], AdditionalNatoWeapons = [] }
			);
	}
}