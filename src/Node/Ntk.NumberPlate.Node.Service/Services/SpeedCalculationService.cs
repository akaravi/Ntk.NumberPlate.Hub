namespace Ntk.NumberPlate.Node.Service.Services;

/// <summary>
/// سرویس محاسبه سرعت خودرو
/// </summary>
public class SpeedCalculationService
{
    private readonly Dictionary<string, DateTime> _lastDetectionTimes = new();
    private readonly ILogger<SpeedCalculationService> _logger;

    public SpeedCalculationService(ILogger<SpeedCalculationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// محاسبه سرعت بر اساس زمان گذر از دو نقطه
    /// </summary>
    /// <param name="plateNumber">شماره پلاک</param>
    /// <param name="currentTime">زمان فعلی</param>
    /// <param name="distance">فاصله بین دو نقطه تشخیص (متر)</param>
    /// <returns>سرعت به کیلومتر بر ساعت</returns>
    public Task<double> CalculateSpeedAsync(string plateNumber, DateTime currentTime, double distance)
    {
        try
        {
            if (_lastDetectionTimes.TryGetValue(plateNumber, out DateTime lastTime))
            {
                var timeSpan = currentTime - lastTime;

                // اگر بیشتر از 10 ثانیه گذشته باشد، احتمالا خودروی دیگری است
                if (timeSpan.TotalSeconds > 10)
                {
                    _lastDetectionTimes[plateNumber] = currentTime;
                    return Task.FromResult(0.0);
                }

                // محاسبه سرعت: سرعت = فاصله / زمان
                // فاصله به متر، زمان به ثانیه، خروجی به کیلومتر بر ساعت
                double timeInHours = timeSpan.TotalHours;
                double distanceInKm = distance / 1000.0;
                double speed = distanceInKm / timeInHours;

                _lastDetectionTimes[plateNumber] = currentTime;

                _logger.LogInformation($"سرعت محاسبه شد: {plateNumber} = {speed:F1} km/h");

                return Task.FromResult(speed);
            }
            else
            {
                // اولین بار این پلاک را می‌بینیم
                _lastDetectionTimes[plateNumber] = currentTime;
                return Task.FromResult(0.0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در محاسبه سرعت");
            return Task.FromResult(0.0);
        }
    }

    /// <summary>
    /// پاک کردن داده‌های قدیمی (بالای 1 دقیقه)
    /// </summary>
    public void CleanupOldData()
    {
        var cutoffTime = DateTime.Now.AddMinutes(-1);
        var oldKeys = _lastDetectionTimes
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldKeys)
        {
            _lastDetectionTimes.Remove(key);
        }

        if (oldKeys.Any())
        {
            _logger.LogInformation($"{oldKeys.Count} رکورد قدیمی پاک شد");
        }
    }
}


