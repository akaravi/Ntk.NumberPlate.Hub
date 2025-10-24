using System;
using System.Diagnostics;
using System.Drawing;
using Ntk.NumberPlate.Shared.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Ntk.NumberPlate.Shared.Services;

/// <summary>
/// سرویس بهینه‌سازی تصویر برای بهبود کیفیت تصاویر پلاک
/// این سرویس شامل عملیات پیش‌پردازش، افزایش کنتراست، کاهش نویز و بهبود وضوح است
/// </summary>
public class ImageOptimizationService : IDisposable
{
    private bool _disposed;

    public ImageOptimizationService()
    {
        Debug.WriteLine("✅ سرویس بهینه‌سازی تصویر آماده شد");
    }

    /// <summary>
    /// بهینه‌سازی تصویر پلاک با استفاده از تکنیک‌های پیشرفته پردازش تصویر
    /// </summary>
    /// <param name="inputImage">تصویر ورودی</param>
    /// <returns>تصویر بهینه‌سازی شده</returns>
    public Bitmap OptimizeImage(Bitmap inputImage)
    {
        try
        {
            Debug.WriteLine("🔄 شروع بهینه‌سازی تصویر...");

            // تبدیل Bitmap به Mat برای پردازش با OpenCV
            using var mat = BitmapConverter.ToMat(inputImage);
            Debug.WriteLine($"📊 ابعاد تصویر ورودی: {mat.Width}x{mat.Height}");

            // مرحله 1: تبدیل به خاکستری برای کاهش پیچیدگی
            using var grayImage = ConvertToGrayscale(mat);
            Debug.WriteLine("✅ مرحله 1: تبدیل به خاکستری انجام شد");

            // مرحله 2: کاهش نویز با فیلتر گاوسی
            using var denoisedImage = ReduceNoise(grayImage);
            Debug.WriteLine("✅ مرحله 2: کاهش نویز انجام شد");

            // مرحله 3: افزایش کنتراست با CLAHE
            using var enhancedImage = EnhanceContrast(denoisedImage);
            Debug.WriteLine("✅ مرحله 3: افزایش کنتراست انجام شد");

            // مرحله 4: تیز کردن تصویر
            using var sharpenedImage = SharpenImage(enhancedImage);
            Debug.WriteLine("✅ مرحله 4: تیز کردن تصویر انجام شد");

            // مرحله 5: تنظیم روشنایی و کنتراست
            using var adjustedImage = AdjustBrightnessContrast(sharpenedImage);
            Debug.WriteLine("✅ مرحله 5: تنظیم روشنایی و کنتراست انجام شد");

            // تبدیل نهایی به Bitmap
            var result = BitmapConverter.ToBitmap(adjustedImage);
            Debug.WriteLine("✅ بهینه‌سازی تصویر با موفقیت انجام شد");

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در بهینه‌سازی تصویر: {ex.Message}");
            return inputImage; // در صورت خطا، تصویر اصلی را برگردان
        }
    }

    /// <summary>
    /// مرحله 1: تبدیل تصویر رنگی به خاکستری
    /// این مرحله پیچیدگی محاسباتی را کاهش می‌دهد و تمرکز را روی ساختار تصویر می‌گذارد
    /// </summary>
    private Mat ConvertToGrayscale(Mat inputImage)
    {
        var grayImage = new Mat();
        
        // بررسی تعداد کانال‌های تصویر
        if (inputImage.Channels() == 1)
        {
            // اگر تصویر قبلاً خاکستری است، کپی کن
            grayImage = inputImage.Clone();
            Debug.WriteLine("📝 تصویر قبلاً خاکستری بود");
        }
        else if (inputImage.Channels() == 3)
        {
            // تبدیل BGR به خاکستری با فرمول استاندارد
            Cv2.CvtColor(inputImage, grayImage, ColorConversionCodes.BGR2GRAY);
            Debug.WriteLine("📝 تصویر رنگی به خاکستری تبدیل شد");
        }
        else if (inputImage.Channels() == 4)
        {
            // تبدیل BGRA به خاکستری
            Cv2.CvtColor(inputImage, grayImage, ColorConversionCodes.BGRA2GRAY);
            Debug.WriteLine("📝 تصویر RGBA به خاکستری تبدیل شد");
        }
        else
        {
            // حالت پیش‌فرض: کپی تصویر
            grayImage = inputImage.Clone();
            Debug.WriteLine("📝 تصویر با فرمت نامشخص - کپی شد");
        }

        return grayImage;
    }

    /// <summary>
    /// مرحله 2: کاهش نویز با فیلتر گاوسی
    /// این مرحله نویزهای تصادفی را کاهش می‌دهد و کیفیت تصویر را بهبود می‌بخشد
    /// </summary>
    private Mat ReduceNoise(Mat inputImage)
    {
        var denoisedImage = new Mat();
        
        // اعمال فیلتر گاوسی با kernel size 3x3
        // این اندازه برای تصاویر پلاک مناسب است
        Cv2.GaussianBlur(inputImage, denoisedImage, new OpenCvSharp.Size(3, 3), 0);
        
        Debug.WriteLine("📝 فیلتر گاوسی 3x3 اعمال شد");
        
        return denoisedImage;
    }

    /// <summary>
    /// مرحله 3: افزایش کنتراست با CLAHE (Contrast Limited Adaptive Histogram Equalization)
    /// این تکنیک کنتراست محلی را بهبود می‌بخشد بدون ایجاد over-enhancement
    /// </summary>
    private Mat EnhanceContrast(Mat inputImage)
    {
        var enhancedImage = new Mat();
        
        // ایجاد CLAHE با پارامترهای بهینه برای تصاویر پلاک
        var clahe = Cv2.CreateCLAHE();
        clahe.ClipLimit = 2.0; // محدودیت کلیپ برای جلوگیری از over-enhancement
        clahe.TilesGridSize = new OpenCvSharp.Size(8, 8); // اندازه تایل‌ها
        
        // اعمال CLAHE
        clahe.Apply(inputImage, enhancedImage);
        
        Debug.WriteLine("📝 CLAHE با clip limit 2.0 و tile size 8x8 اعمال شد");
        
        return enhancedImage;
    }

    /// <summary>
    /// مرحله 4: تیز کردن تصویر با فیلتر Unsharp Mask
    /// این مرحله وضوح تصویر را بهبود می‌بخشد و جزئیات را برجسته می‌کند
    /// </summary>
    private Mat SharpenImage(Mat inputImage)
    {
        var sharpenedImage = new Mat();
        
        // ایجاد kernel تیز کردن (Unsharp Mask)
        // این kernel مرکز را تقویت می‌کند و لبه‌ها را برجسته می‌کند
        var kernel = new Mat(3, 3, MatType.CV_32F, new float[]
        {
            0, -1, 0,
            -1, 5, -1,
            0, -1, 0
        });
        
        // اعمال فیلتر تیز کردن
        Cv2.Filter2D(inputImage, sharpenedImage, -1, kernel);
        
        Debug.WriteLine("📝 فیلتر تیز کردن Unsharp Mask اعمال شد");
        
        return sharpenedImage;
    }

    /// <summary>
    /// مرحله 5: تنظیم روشنایی و کنتراست
    /// این مرحله تصویر را برای تشخیص بهتر آماده می‌کند
    /// </summary>
    private Mat AdjustBrightnessContrast(Mat inputImage)
    {
        var adjustedImage = new Mat();
        
        // تنظیم روشنایی (alpha) و کنتراست (beta)
        // alpha > 1: افزایش کنتراست
        // beta: تغییر روشنایی
        double alpha = 1.2; // افزایش کنتراست 20%
        int beta = 10; // افزایش روشنایی 10 واحد
        
        // اعمال فرمول: new_pixel = alpha * pixel + beta
        inputImage.ConvertTo(adjustedImage, -1, alpha, beta);
        
        Debug.WriteLine($"📝 روشنایی و کنتراست تنظیم شد (alpha={alpha}, beta={beta})");
        
        return adjustedImage;
    }

    /// <summary>
    /// بهینه‌سازی تصویر با تنظیمات سفارشی
    /// </summary>
    /// <param name="inputImage">تصویر ورودی</param>
    /// <param name="brightness">مقدار روشنایی (-100 تا 100)</param>
    /// <param name="contrast">مقدار کنتراست (0.1 تا 3.0)</param>
    /// <param name="sharpen">فعال/غیرفعال کردن تیز کردن</param>
    /// <returns>تصویر بهینه‌سازی شده</returns>
    public Bitmap OptimizeImageWithSettings(Bitmap inputImage, int brightness = 0, double contrast = 1.0, bool sharpen = true)
    {
        try
        {
            Debug.WriteLine($"🔄 شروع بهینه‌سازی با تنظیمات سفارشی (brightness={brightness}, contrast={contrast}, sharpen={sharpen})");

            using var mat = BitmapConverter.ToMat(inputImage);
            using var grayImage = ConvertToGrayscale(mat);
            using var denoisedImage = ReduceNoise(grayImage);
            using var enhancedImage = EnhanceContrast(denoisedImage);

            Mat processedImage;
            if (sharpen)
            {
                processedImage = SharpenImage(enhancedImage);
                Debug.WriteLine("📝 تیز کردن فعال شد");
            }
            else
            {
                processedImage = enhancedImage.Clone();
                Debug.WriteLine("📝 تیز کردن غیرفعال شد");
            }

            // اعمال تنظیمات روشنایی و کنتراست سفارشی
            var adjustedImage = new Mat();
            processedImage.ConvertTo(adjustedImage, -1, contrast, brightness);

            Debug.WriteLine($"📝 تنظیمات سفارشی اعمال شد (brightness={brightness}, contrast={contrast})");

            var result = BitmapConverter.ToBitmap(adjustedImage);
            processedImage.Dispose();

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در بهینه‌سازی سفارشی: {ex.Message}");
            return inputImage;
        }
    }

    /// <summary>
    /// دریافت اطلاعات آماری تصویر
    /// </summary>
    /// <param name="image">تصویر</param>
    /// <returns>اطلاعات آماری</returns>
    public ImageStatistics GetImageStatistics(Bitmap image)
    {
        try
        {
            using var mat = BitmapConverter.ToMat(image);
            using var grayImage = ConvertToGrayscale(mat);

            // محاسبه میانگین و انحراف معیار
            Cv2.MeanStdDev(grayImage, out var mean, out var stdDev);

            // محاسبه هیستوگرام
            var histogram = new Mat();
            Cv2.CalcHist(new Mat[] { grayImage }, new int[] { 0 }, new Mat(), histogram, 1, new int[] { 256 }, new Rangef[] { new Rangef(0, 256) });

            var stats = new ImageStatistics
            {
                Width = image.Width,
                Height = image.Height,
                MeanBrightness = mean.Val0,
                StandardDeviation = stdDev.Val0,
                AspectRatio = (double)image.Width / image.Height
            };

            Debug.WriteLine($"📊 آمار تصویر: {stats.Width}x{stats.Height}, میانگین: {stats.MeanBrightness:F2}, انحراف: {stats.StandardDeviation:F2}");

            return stats;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در محاسبه آمار تصویر: {ex.Message}");
            return new ImageStatistics();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Debug.WriteLine("🔄 پاکسازی سرویس بهینه‌سازی تصویر");
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// کلاس برای ذخیره آمار تصویر
/// </summary>
public class ImageStatistics
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double MeanBrightness { get; set; }
    public double StandardDeviation { get; set; }
    public double AspectRatio { get; set; }
}
