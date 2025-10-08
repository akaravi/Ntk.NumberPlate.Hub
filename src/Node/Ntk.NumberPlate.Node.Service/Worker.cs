using Ntk.NumberPlate.Node.Service.Services;

namespace Ntk.NumberPlate.Node.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ConfigurationService _configService;
    private readonly YoloDetectionService _detectionService;
    private readonly HubCommunicationService _hubService;
    private readonly SpeedCalculationService _speedService;

    public Worker(
        ILogger<Worker> logger,
        ConfigurationService configService,
        YoloDetectionService detectionService,
        HubCommunicationService hubService,
        SpeedCalculationService speedService)
    {
        _logger = logger;
        _configService = configService;
        _detectionService = detectionService;
        _hubService = hubService;
        _speedService = speedService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("سرویس Node شروع به کار کرد");

        // بارگذاری تنظیمات
        var config = _configService.LoadConfiguration();
        if (config == null)
        {
            _logger.LogError("فایل تنظیمات یافت نشد. لطفا ابتدا از نرم‌افزار پیکربندی استفاده کنید.");
            return;
        }

        // ثبت نود در Hub
        await _hubService.RegisterNodeAsync(config);

        // راه‌اندازی سرویس تشخیص
        await _detectionService.InitializeAsync(config);

        _logger.LogInformation($"نود {config.NodeName} آماده است");

        // حلقه اصلی
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // دریافت فریم و تشخیص پلاک
                var detection = await _detectionService.ProcessNextFrameAsync();

                if (detection != null)
                {
                    // محاسبه سرعت
                    if (config.EnableSpeedDetection)
                    {
                        detection.Speed = await _speedService.CalculateSpeedAsync(
                            detection.PlateNumber,
                            detection.DetectionTime,
                            config.DetectionDistance);

                        detection.IsSpeedViolation = detection.Speed > config.SpeedLimit;
                        detection.SpeedLimit = config.SpeedLimit;
                    }

                    // ارسال به Hub
                    if (config.AutoSendEnabled)
                    {
                        await _hubService.SendDetectionAsync(detection, config);
                    }

                    _logger.LogInformation($"تشخیص جدید: {detection.PlateNumber}, سرعت: {detection.Speed:F1} km/h");
                }

                // ارسال heartbeat هر 30 ثانیه
                await _hubService.SendHeartbeatAsync(config);

                // تاخیر بین پردازش‌ها
                await Task.Delay(1000 / config.ProcessingFps, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در پردازش فریم");
                await Task.Delay(5000, stoppingToken); // تاخیر در صورت خطا
            }
        }

        _logger.LogInformation("سرویس Node متوقف شد");
    }
}


