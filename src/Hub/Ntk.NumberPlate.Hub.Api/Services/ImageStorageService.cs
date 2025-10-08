namespace Ntk.NumberPlate.Hub.Api.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly string _basePath;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(IConfiguration configuration, ILogger<ImageStorageService> logger)
    {
        _basePath = configuration["ImageStorage:BasePath"] ?? "wwwroot/images";
        _logger = logger;

        // ایجاد پوشه در صورت عدم وجود
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation($"پوشه ذخیره‌سازی تصاویر ایجاد شد: {_basePath}");
        }
    }

    public async Task<string> SaveImageAsync(byte[] imageData, string fileName)
    {
        try
        {
            // ایجاد پوشه بر اساس تاریخ
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var folderPath = Path.Combine(_basePath, dateFolder);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // مسیر کامل فایل
            var filePath = Path.Combine(folderPath, fileName);

            await File.WriteAllBytesAsync(filePath, imageData);

            _logger.LogInformation($"تصویر ذخیره شد: {fileName}");

            // بازگشت مسیر نسبی
            return Path.Combine(dateFolder, fileName).Replace("\\", "/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"خطا در ذخیره تصویر: {fileName}");
            throw;
        }
    }

    public async Task<byte[]?> GetImageAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_basePath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"فایل یافت نشد: {fileName}");
                return null;
            }

            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"خطا در خواندن تصویر: {fileName}");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_basePath, fileName);

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                _logger.LogInformation($"تصویر حذف شد: {fileName}");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"خطا در حذف تصویر: {fileName}");
            return false;
        }
    }
}


