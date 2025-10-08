using System.Text;
using Newtonsoft.Json;
using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Node.Service.Services;

/// <summary>
/// سرویس ارتباط با Hub Server
/// </summary>
public class HubCommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HubCommunicationService> _logger;
    private DateTime _lastHeartbeat = DateTime.MinValue;

    public HubCommunicationService(ILogger<HubCommunicationService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<bool> RegisterNodeAsync(NodeConfiguration config)
    {
        try
        {
            var url = $"{config.HubServerUrl}/api/node/register";

            var data = new
            {
                NodeId = config.NodeId,
                NodeName = config.NodeName
            };

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(config.ApiToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiToken);
            }

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"نود با موفقیت در Hub ثبت شد: {config.NodeName}");
                return true;
            }
            else
            {
                _logger.LogWarning($"خطا در ثبت نود: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ارتباط با Hub برای ثبت نود");
            return false;
        }
    }

    public async Task SendHeartbeatAsync(NodeConfiguration config)
    {
        // ارسال heartbeat فقط هر 30 ثانیه
        if ((DateTime.Now - _lastHeartbeat).TotalSeconds < 30)
            return;

        try
        {
            var url = $"{config.HubServerUrl}/api/node/heartbeat/{config.NodeId}";
            var response = await _httpClient.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                _lastHeartbeat = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ارسال heartbeat");
        }
    }

    public async Task<bool> SendDetectionAsync(VehicleDetectionData detection, NodeConfiguration config)
    {
        int retryCount = 0;

        while (retryCount < config.MaxRetryAttempts)
        {
            try
            {
                var url = $"{config.HubServerUrl}/api/vehicledetection/submit";

                using var formData = new MultipartFormDataContent();

                // افزودن JSON داده‌های تشخیص
                var detectionJson = JsonConvert.SerializeObject(detection);
                formData.Add(new StringContent(detectionJson, Encoding.UTF8, "application/json"), "detection");

                // افزودن تصویر
                if (!string.IsNullOrEmpty(detection.ImageFileName))
                {
                    var imagePath = Path.Combine(config.LocalImagePath,
                        detection.DetectionTime.ToString("yyyy-MM-dd"),
                        detection.ImageFileName);

                    if (File.Exists(imagePath))
                    {
                        var imageBytes = await File.ReadAllBytesAsync(imagePath);
                        formData.Add(new ByteArrayContent(imageBytes), "image", detection.ImageFileName);
                    }
                }

                var response = await _httpClient.PostAsync(url, formData);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"تشخیص با موفقیت به Hub ارسال شد: {detection.PlateNumber}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"خطا در ارسال تشخیص به Hub: {response.StatusCode}");
                    retryCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطا در ارسال تشخیص (تلاش {retryCount + 1})");
                retryCount++;
            }

            if (retryCount < config.MaxRetryAttempts)
            {
                await Task.Delay(2000 * retryCount); // تاخیر تصاعدی
            }
        }

        return false;
    }
}


