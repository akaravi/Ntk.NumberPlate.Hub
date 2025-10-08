using Newtonsoft.Json;
using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Node.Service.Services;

public class ConfigurationService
{
    private const string ConfigFileName = "node-config.json";
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
    }

    public NodeConfiguration? LoadConfiguration()
    {
        try
        {
            if (!File.Exists(ConfigFileName))
            {
                _logger.LogWarning($"فایل تنظیمات یافت نشد: {ConfigFileName}");
                return null;
            }

            var json = File.ReadAllText(ConfigFileName);
            var config = JsonConvert.DeserializeObject<NodeConfiguration>(json);

            _logger.LogInformation("تنظیمات با موفقیت بارگذاری شد");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بارگذاری تنظیمات");
            return null;
        }
    }

    public bool SaveConfiguration(NodeConfiguration config)
    {
        try
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFileName, json);

            _logger.LogInformation("تنظیمات با موفقیت ذخیره شد");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ذخیره تنظیمات");
            return false;
        }
    }
}


