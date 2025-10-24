using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Ntk.NumberPlate.Shared.Models;
using Ntk.NumberPlate.Shared.Services.Ocr;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Ntk.NumberPlate.Shared.Services;

/// <summary>
/// سرویس تشخیص حروف OCR برای پلاک‌های خودرو
/// این سرویس مسئول تشخیص و استخراج متن از تصاویر پلاک است
/// </summary>
public class PlateDetectionOCRService : IDisposable
{
    private readonly NodeConfiguration _configuration;
    private readonly IOcrEngine _ocrEngine;
    private bool _disposed;

    public PlateDetectionOCRService(NodeConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        Debug.WriteLine("🔄 شروع مقداردهی سرویس OCR...");

        // ایجاد موتور OCR مناسب بر اساس تنظیمات
        _ocrEngine = CreateOcrEngine(_configuration.OcrMethod);

        Debug.WriteLine($"✅ سرویس OCR با موتور '{_ocrEngine.EngineName}' آماده شد");
    }

    /// <summary>
    /// ایجاد موتور OCR بر اساس روش انتخابی
    /// این متد موتور مناسب را بر اساس تنظیمات انتخاب می‌کند
    /// </summary>
    private IOcrEngine CreateOcrEngine(OcrMethod method)
    {
        try
        {
            Debug.WriteLine($"🔄 ایجاد موتور OCR با روش: {method}");

            // فقط از YOLO OCR استفاده می‌کنیم
            if (method != OcrMethod.Yolo)
            {
                Debug.WriteLine($"⚠️ فقط YOLO OCR پشتیبانی می‌شود - تغییر روش از {method} به Yolo");
                method = OcrMethod.Yolo;
            }

            Debug.WriteLine("📝 انتخاب موتور YOLO OCR");
            
            // بررسی وجود مسیر مدل YOLO
            if (string.IsNullOrEmpty(_configuration.YoloOcrModelPath))
            {
                throw new InvalidOperationException("مسیر مدل YOLO مشخص نشده است. لطفاً مسیر مدل YOLO را در تنظیمات مشخص کنید.");
            }

            // ایجاد موتور YOLO با تنظیمات
            var yoloEngine = new YoloOcrEngine(
                _configuration.YoloOcrModelPath,
                _configuration.OcrConfidenceThreshold
            );

            // بررسی آمادگی موتور
            if (!yoloEngine.IsReady)
            {
                throw new InvalidOperationException($"موتور YOLO آماده نیست. بررسی کنید که فایل مدل در مسیر '{_configuration.YoloOcrModelPath}' وجود دارد");
            }

            Debug.WriteLine($"✅ موتور YOLO آماده شد: {_configuration.YoloOcrModelPath}");
            return yoloEngine;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در ایجاد موتور OCR: {ex.Message}");
            throw new InvalidOperationException($"خطا در ایجاد موتور YOLO OCR: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// تشخیص متن پلاک از تصویر (Bitmap)
    /// این متد اصلی برای تشخیص متن از تصاویر پلاک است
    /// </summary>
    /// <param name="plateImage">تصویر پلاک</param>
    /// <returns>نتیجه OCR</returns>
    public OcrResult RecognizePlate(Bitmap plateImage)
    {
        if (plateImage == null)
        {
            Debug.WriteLine("❌ تصویر پلاک null است");
            return CreateErrorResult("تصویر پلاک null است");
        }

        Debug.WriteLine($"🔄 شروع تشخیص OCR روی تصویر {plateImage.Width}x{plateImage.Height}...");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // اجرای تشخیص با موتور انتخابی
            var result = _ocrEngine.Recognize(plateImage);

            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            // ثبت نتایج
            LogOcrResult(result);

            Debug.WriteLine("✅ تشخیص OCR تکمیل شد");
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Debug.WriteLine($"❌ خطا در تشخیص OCR: {ex.Message}");

            return new OcrResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Method = _configuration.OcrMethod,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// تشخیص متن پلاک از تصویر (Mat)
    /// این متد برای تصاویر OpenCV استفاده می‌شود
    /// </summary>
    /// <param name="plateImage">تصویر پلاک</param>
    /// <returns>نتیجه OCR</returns>
    public OcrResult RecognizePlate(Mat plateImage)
    {
        if (plateImage == null || plateImage.Empty())
        {
            Debug.WriteLine("❌ تصویر پلاک خالی است");
            return CreateErrorResult("تصویر پلاک خالی است");
        }

        Debug.WriteLine($"🔄 شروع تشخیص OCR روی Mat {plateImage.Width}x{plateImage.Height}...");

        try
        {
            // تبدیل Mat به Bitmap
            using var bitmap = plateImage.ToBitmap();
            Debug.WriteLine("✅ تبدیل Mat به Bitmap انجام شد");
            
            return RecognizePlate(bitmap);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در تبدیل Mat به Bitmap: {ex.Message}");
            return CreateErrorResult($"خطا در تبدیل تصویر: {ex.Message}");
        }
    }

    /// <summary>
    /// تشخیص متن از مسیر فایل
    /// این متد برای پردازش فایل‌های تصویری استفاده می‌شود
    /// </summary>
    /// <param name="imagePath">مسیر فایل تصویر</param>
    /// <returns>نتیجه OCR</returns>
    public OcrResult RecognizePlateFromFile(string imagePath)
    {
        try
        {
            Debug.WriteLine($"🔄 شروع تشخیص OCR از فایل: {imagePath}");

            // بررسی وجود فایل
            if (!File.Exists(imagePath))
            {
                Debug.WriteLine($"❌ فایل یافت نشد: {imagePath}");
                return CreateErrorResult($"فایل یافت نشد: {imagePath}");
            }

            // بارگذاری تصویر
            using var image = new Bitmap(imagePath);
            Debug.WriteLine($"📊 تصویر بارگذاری شد: {image.Width}x{image.Height}");

            // تشخیص متن
            return RecognizePlate(image);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در تشخیص از فایل: {ex.Message}");
            return CreateErrorResult($"خطا در بارگذاری فایل: {ex.Message}");
        }
    }

    /// <summary>
    /// تشخیص متن با تنظیمات سفارشی
    /// این متد امکان تنظیم پارامترهای تشخیص را فراهم می‌کند
    /// </summary>
    /// <param name="plateImage">تصویر پلاک</param>
    /// <param name="preprocess">آیا پیش‌پردازش انجام شود</param>
    /// <param name="confidenceThreshold">آستانه اعتماد</param>
    /// <returns>نتیجه OCR</returns>
    public OcrResult RecognizePlateWithSettings(Bitmap plateImage, bool preprocess = true, float confidenceThreshold = 0.5f)
    {
        try
        {
            Debug.WriteLine($"🔄 شروع تشخیص OCR با تنظیمات سفارشی (preprocess={preprocess}, threshold={confidenceThreshold})");

            Bitmap processedImage = plateImage;

            // پیش‌پردازش تصویر در صورت نیاز
            if (preprocess)
            {
                Debug.WriteLine("📝 انجام پیش‌پردازش تصویر...");
                // در اینجا می‌توانید سرویس بهینه‌سازی تصویر را فراخوانی کنید
                // processedImage = _imageOptimizationService.OptimizeImage(plateImage);
            }

            // تشخیص متن
            var result = RecognizePlate(processedImage);

            // اعمال آستانه اعتماد سفارشی
            if (result.Confidence < confidenceThreshold)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = $"اعتماد کمتر از آستانه ({confidenceThreshold:P0})";
                Debug.WriteLine($"⚠️ اعتماد {result.Confidence:P0} کمتر از آستانه {confidenceThreshold:P0}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در تشخیص با تنظیمات سفارشی: {ex.Message}");
            return CreateErrorResult($"خطا در تشخیص سفارشی: {ex.Message}");
        }
    }

    /// <summary>
    /// ایجاد نتیجه خطا
    /// این متد برای ایجاد نتیجه خطا استفاده می‌شود
    /// </summary>
    private OcrResult CreateErrorResult(string errorMessage)
    {
        return new OcrResult
        {
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            Method = _configuration.OcrMethod,
            Confidence = 0
        };
    }

    /// <summary>
    /// ثبت نتایج OCR
    /// این متد نتایج تشخیص را ثبت و نمایش می‌دهد
    /// </summary>
    private void LogOcrResult(OcrResult result)
    {
        if (result.IsSuccessful)
        {
            Debug.WriteLine($"🔍 OCR موفق:");
            Debug.WriteLine($"   موتور: {_ocrEngine.EngineName}");
            Debug.WriteLine($"   متن: '{result.Text}'");
            Debug.WriteLine($"   اعتماد: {result.Confidence:P0}");
            Debug.WriteLine($"   زمان: {result.ProcessingTimeMs}ms");
        }
        else
        {
            Debug.WriteLine($"❌ OCR ناموفق:");
            Debug.WriteLine($"   موتور: {_ocrEngine.EngineName}");
            Debug.WriteLine($"   خطا: {result.ErrorMessage}");
            Debug.WriteLine($"   زمان: {result.ProcessingTimeMs}ms");
        }
    }

    /// <summary>
    /// دریافت اطلاعات موتور فعلی
    /// این متد اطلاعات موتور OCR فعال را برمی‌گرداند
    /// </summary>
    public string GetEngineInfo()
    {
        var status = _ocrEngine.IsReady ? "آماده" : "غیرآماده";
        var method = "YOLO OCR"; // فقط YOLO OCR پشتیبانی می‌شود

        var info = $"{_ocrEngine.EngineName} ({method}) - وضعیت: {status}";
        Debug.WriteLine($"📊 اطلاعات موتور: {info}");
        
        return info;
    }

    /// <summary>
    /// دریافت آمار عملکرد OCR
    /// این متد آمار عملکرد موتور OCR را برمی‌گرداند
    /// </summary>
    public OcrPerformanceStats GetPerformanceStats()
    {
        return new OcrPerformanceStats
        {
            EngineName = _ocrEngine.EngineName,
            Method = _ocrEngine.Method,
            IsReady = _ocrEngine.IsReady,
            Configuration = _configuration.OcrMethod
        };
    }

    /// <summary>
    /// تست عملکرد موتور OCR
    /// این متد عملکرد موتور را با تصویر نمونه تست می‌کند
    /// </summary>
    /// <param name="testImage">تصویر تست</param>
    /// <returns>نتیجه تست</returns>
    public OcrTestResult TestEngine(Bitmap testImage)
    {
        try
        {
            Debug.WriteLine("🔄 شروع تست عملکرد موتور OCR...");

            var stopwatch = Stopwatch.StartNew();
            var result = RecognizePlate(testImage);
            stopwatch.Stop();

            var testResult = new OcrTestResult
            {
                IsSuccessful = result.IsSuccessful,
                Text = result.Text,
                Confidence = result.Confidence,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = result.ErrorMessage,
                EngineName = _ocrEngine.EngineName
            };

            Debug.WriteLine($"📊 نتیجه تست: موفق={testResult.IsSuccessful}, زمان={testResult.ProcessingTimeMs}ms");
            return testResult;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در تست موتور: {ex.Message}");
            return new OcrTestResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                EngineName = _ocrEngine.EngineName
            };
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Debug.WriteLine("🔄 پاکسازی سرویس OCR");
        _ocrEngine?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// کلاس برای ذخیره آمار عملکرد OCR
/// </summary>
public class OcrPerformanceStats
{
    public string EngineName { get; set; } = string.Empty;
    public OcrMethod Method { get; set; }
    public bool IsReady { get; set; }
    public OcrMethod Configuration { get; set; }
}

/// <summary>
/// کلاس برای ذخیره نتیجه تست OCR
/// </summary>
public class OcrTestResult
{
    public bool IsSuccessful { get; set; }
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string EngineName { get; set; } = string.Empty;
}