using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Skoleoverblikket.Api.Services;

public sealed class UvmTimetableService(IWebHostEnvironment env, ILogger<UvmTimetableService> logger)
{
	private static Dictionary<string, Dictionary<int, double>>? _timetal;
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
	private static readonly Lock _lock = new();

	public Dictionary<string, Dictionary<int, double>> Load()
	{
		if (_timetal is not null)
		{
			return _timetal;
		}

		lock (_lock)
		{
			if (_timetal is not null)
			{
				return _timetal;
			}

			try
			{
				var dataPath = Path.Combine(env.ContentRootPath, "Data", "uvm-timetal");
				if (!Directory.Exists(dataPath))
				{
					dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "uvm-timetal");
				}

				var files = Directory.GetFiles(dataPath, "*.json").OrderDescending().ToArray();
				if (files.Length == 0)
				{
					logger.LogWarning("No UVM timetal JSON files found in {Path}", dataPath);
					return [];
				}

				var now = DateTime.UtcNow;
				var schoolYearStart = now.Month >= 8 ? now.Year : now.Year - 1;
				var targetName = $"{schoolYearStart}-{schoolYearStart + 1}.json";
				var match = files.FirstOrDefault(f => Path.GetFileName(f) == targetName) ?? files[0];

				if (!File.Exists(match))
				{
					logger.LogWarning("UVM timetal file not found: {File}", match);
					return [];
				}

				var json = File.ReadAllText(match);
				var loaded = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, double>>>(json, JsonOptions);
				if (loaded is null)
				{
					logger.LogWarning("UVM timetal file deserialized to null: {File}", match);
					return [];
				}

				_timetal = loaded;
				return _timetal;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load UVM timetal data");
				return [];
			}
		}
	}
}
