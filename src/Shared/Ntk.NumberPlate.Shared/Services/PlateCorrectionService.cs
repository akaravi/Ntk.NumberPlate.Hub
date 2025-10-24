using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Ntk.NumberPlate.Shared.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Ntk.NumberPlate.Shared.Services;

/// <summary>
/// سرویس اصلاح و صاف کردن تصاویر پلاک
/// این سرویس مسئول تشخیص زاویه پلاک و اصلاح آن برای بهبود کیفیت OCR است
/// </summary>
public class PlateCorrectionService : IDisposable
{
    private bool _disposed;

    public PlateCorrectionService()
    {
        Debug.WriteLine("✅ سرویس اصلاح پلاک آماده شد");
    }

    /// <summary>
    /// اصلاح تصویر پلاک (Bitmap ورودی)
    /// این متد اصلی برای اصلاح تصاویر پلاک است
    /// </summary>
    /// <param name="plateImage">تصویر پلاک</param>
    /// <returns>تصویر اصلاح شده یا null در صورت خطا</returns>
    public Bitmap? CorrectPlate(Bitmap plateImage)
    {
        try
        {
            Debug.WriteLine("🔄 شروع اصلاح تصویر پلاک...");

            // تبدیل Bitmap به Mat برای پردازش با OpenCV
            using var mat = BitmapConverter.ToMat(plateImage);
            Debug.WriteLine($"📊 ابعاد تصویر ورودی: {mat.Width}x{mat.Height}");

            // مرحله 1: تبدیل به خاکستری برای پردازش بهتر
            using var grayPlate = ConvertToGrayscale(mat);
            Debug.WriteLine("✅ مرحله 1: تبدیل به خاکستری انجام شد");

            // مرحله 2: تشخیص خطوط در تصویر
            var lines = DetectLinesInPlate(grayPlate);
            if (lines == null || lines.Length == 0)
            {
                Debug.WriteLine("⚠️ هیچ خطی یافت نشد - بازگشت تصویر اصلی");
                return plateImage;
            }
            Debug.WriteLine($"✅ مرحله 2: {lines.Length} خط یافت شد");

            // مرحله 3: محاسبه زاویه چرخش
            var rotationAngle = CalculateRotationAngle(lines);
            Debug.WriteLine($"✅ مرحله 3: زاویه چرخش محاسبه شد: {rotationAngle:F2} درجه");

            // مرحله 4: بررسی نیاز به چرخش
            if (Math.Abs(rotationAngle) < 1.0)
            {
                Debug.WriteLine("📝 زاویه چرخش ناچیز - بازگشت تصویر اصلی");
                return plateImage;
            }

            // مرحله 5: چرخش تصویر
            var correctedImage = RotateImage(mat, rotationAngle);
            Debug.WriteLine("✅ مرحله 5: چرخش تصویر انجام شد");

            // مرحله 6: تنظیم برش برای بهبود نسبت ابعاد
            var finalImage = AdjustCropping(correctedImage);
            Debug.WriteLine("✅ مرحله 6: تنظیم برش انجام شد");

            // تبدیل نهایی به Bitmap
            var result = ConvertToBitmap(finalImage);
            Debug.WriteLine("✅ اصلاح پلاک با موفقیت انجام شد");

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در اصلاح پلاک: {ex.Message}");
            return plateImage; // در صورت خطا، تصویر اصلی را برگردان
        }
    }

    /// <summary>
    /// اصلاح تصویر پلاک با تشخیص خط و چرخش
    /// این متد برای اصلاح پلاک‌های برش خورده از تصویر اصلی استفاده می‌شود
    /// </summary>
    /// <param name="imagePath">مسیر تصویر اصلی</param>
    /// <param name="boundingBox">محدوده پلاک در تصویر</param>
    /// <returns>تصویر اصلاح شده یا null در صورت خطا</returns>
    public Bitmap? CorrectPlateImage(string imagePath, BoundingBox boundingBox)
    {
        try
        {
            Debug.WriteLine($"🔄 شروع اصلاح پلاک از مسیر: {imagePath}");

            // بارگذاری تصویر اصلی
            using var originalImage = Cv2.ImRead(imagePath);
            if (originalImage.Empty())
            {
                Debug.WriteLine("❌ تصویر اصلی خالی است");
                return null;
            }
            Debug.WriteLine($"📊 ابعاد تصویر اصلی: {originalImage.Width}x{originalImage.Height}");

            // برش پلاک از تصویر اصلی
            var plateRect = new Rect(boundingBox.X, boundingBox.Y, boundingBox.Width, boundingBox.Height);
            using var plateImage = new Mat(originalImage, plateRect);
            Debug.WriteLine($"📊 ابعاد پلاک برش خورده: {plateImage.Width}x{plateImage.Height}");

            // تبدیل به خاکستری
            using var grayPlate = ConvertToGrayscale(plateImage);
            Debug.WriteLine("✅ تبدیل به خاکستری انجام شد");

            // تشخیص خطوط
            var lines = DetectLinesInPlate(grayPlate);
            if (lines == null || lines.Length == 0)
            {
                Debug.WriteLine("⚠️ هیچ خطی یافت نشد - بازگشت تصویر اصلی");
                return ConvertToBitmap(plateImage);
            }
            Debug.WriteLine($"✅ {lines.Length} خط یافت شد");

            // محاسبه زاویه چرخش
            var rotationAngle = CalculateRotationAngle(lines);
            Debug.WriteLine($"📊 زاویه چرخش: {rotationAngle:F2} درجه");

            // بررسی نیاز به چرخش
            if (Math.Abs(rotationAngle) < 0.5)
            {
                Debug.WriteLine("📝 زاویه خیلی کوچک است - بدون چرخش");
                var adjusted = AdjustCropping(grayPlate);
                return ConvertToBitmap(adjusted);
            }

            // چرخش تصویر
            var rotatedImage = RotateImage(grayPlate, rotationAngle);
            Debug.WriteLine("✅ چرخش تصویر انجام شد");

            // تنظیم برش
            var adjustedImage = AdjustCropping(rotatedImage);
            Debug.WriteLine("✅ تنظیم برش انجام شد");

            return ConvertToBitmap(adjustedImage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در اصلاح پلاک: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// مرحله 1: تبدیل تصویر به خاکستری
    /// این مرحله پیچیدگی محاسباتی را کاهش می‌دهد
    /// </summary>
    private Mat ConvertToGrayscale(Mat image)
    {
        var grayImage = new Mat();
        
        if (image.Channels() == 1)
        {
            // اگر قبلاً خاکستری است، کپی کن
            grayImage = image.Clone();
            Debug.WriteLine("📝 تصویر قبلاً خاکستری بود");
        }
        else
        {
            // تبدیل BGR به خاکستری
            Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
            Debug.WriteLine("📝 تصویر رنگی به خاکستری تبدیل شد");
        }

        return grayImage;
    }

    /// <summary>
    /// مرحله 2: تشخیص خطوط در تصویر پلاک
    /// این مرحله از الگوریتم Hough Line Transform استفاده می‌کند
    /// </summary>
    private LineSegmentPoint[]? DetectLinesInPlate(Mat plateImage)
    {
        try
        {
            Debug.WriteLine($"📊 شروع تشخیص خطوط در تصویر {plateImage.Width}x{plateImage.Height}...");

            // مرحله 2.1: اعمال فیلتر گاوسی برای کاهش نویز
            using var blurred = new Mat();
            Cv2.GaussianBlur(plateImage, blurred, new OpenCvSharp.Size(3, 3), 0);
            Debug.WriteLine("📝 فیلتر گاوسی 3x3 اعمال شد");

            // مرحله 2.2: تشخیص لبه‌ها با Canny Edge Detection
            using var edges = new Mat();
            Cv2.Canny(blurred, edges, 30, 100); // آستانه‌های پایین و بالا
            Debug.WriteLine("📝 تشخیص لبه‌ها با Canny انجام شد");

            // شمارش پیکسل‌های لبه برای ارزیابی کیفیت
            var edgePixels = Cv2.CountNonZero(edges);
            Debug.WriteLine($"📊 تعداد پیکسل‌های لبه: {edgePixels}");

            // مرحله 2.3: تشخیص خطوط با Hough Line Transform
            LineSegmentPoint[]? lines = null;

            // تلاش 1: پارامترهای اصلی (مطابق کد پایتون)
            lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 15, 50, 5);
            if (lines != null && lines.Length > 0)
            {
                Debug.WriteLine($"✅ روش 1 موفق: {lines.Length} خط یافت شد");
            }
            else
            {
                // تلاش 2: کاهش threshold و minLineLength برای حساسیت بیشتر
                Debug.WriteLine("🔄 تلاش با پارامترهای کمتر...");
                lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 10, 30, 10);

                if (lines != null && lines.Length > 0)
                {
                    Debug.WriteLine($"✅ روش 2 موفق: {lines.Length} خط یافت شد");
                }
                else
                {
                    // تلاش 3: پارامترهای خیلی آسان‌تر
                    Debug.WriteLine("🔄 تلاش با پارامترهای بسیار آسان...");
                    lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 5, 20, 15);

                    if (lines != null && lines.Length > 0)
                    {
                        Debug.WriteLine($"✅ روش 3 موفق: {lines.Length} خط یافت شد");
                    }
                }
            }

            if (lines == null || lines.Length == 0)
            {
                Debug.WriteLine("❌ هیچ خطی با هیچ پارامتری یافت نشد");
                return null;
            }

            Debug.WriteLine($"📊 تعداد کل خطوط یافت شده: {lines.Length}");

            // مرحله 2.4: محاسبه طول خطوط و مرتب‌سازی
            var lineLengths = new List<(LineSegmentPoint line, double length)>();
            foreach (var line in lines)
            {
                var length = Math.Sqrt(Math.Pow(line.P2.X - line.P1.X, 2) + Math.Pow(line.P2.Y - line.P1.Y, 2));
                lineLengths.Add((line, length));
            }

            // مرتب‌سازی بر اساس طول (کوچکترین اول)
            lineLengths.Sort((a, b) => a.length.CompareTo(b.length));

            // نمایش طولانی‌ترین 5 خط برای دیباگ
            var topLines = lineLengths.TakeLast(Math.Min(5, lineLengths.Count)).ToList();
            Debug.WriteLine("📊 طولانی‌ترین خطوط:");
            foreach (var (line, length) in topLines)
            {
                Debug.WriteLine($"  طول: {length:F2}, P1({line.P1.X},{line.P1.Y}) -> P2({line.P2.X},{line.P2.Y})");
            }

            return lineLengths.Select(x => x.line).ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در تشخیص خطوط: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// مرحله 3: محاسبه زاویه چرخش بر اساس خطوط
    /// این متد زاویه چرخش مورد نیاز را محاسبه می‌کند
    /// </summary>
    private double CalculateRotationAngle(LineSegmentPoint[] lines)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.WriteLine("⚠️ هیچ خطی برای محاسبه زاویه وجود ندارد");
            return 0;
        }

        // استفاده از طولانی‌ترین خط (آخرین خط در آرایه مرتب شده)
        var longestLine = lines[lines.Length - 1];
        Debug.WriteLine($"📝 استفاده از خط: P1({longestLine.P1.X},{longestLine.P1.Y}) -> P2({longestLine.P2.X},{longestLine.P2.Y})");

        // محاسبه تغییرات X و Y
        var deltaX = longestLine.P2.X - longestLine.P1.X;
        var deltaY = longestLine.P2.Y - longestLine.P1.Y;

        // بررسی خط عمودی
        if (Math.Abs(deltaX) < 0.001)
        {
            Debug.WriteLine("📝 خط عمودی تشخیص داده شد");
            return 0;
        }

        // محاسبه زاویه با استفاده از Atan (مطابق کد پایتون)
        var angle = Math.Atan(deltaY / (double)deltaX) * 180.0 / Math.PI;
        Debug.WriteLine($"📝 زاویه محاسبه شده: {angle:F2} درجه");

        return angle;
    }

    /// <summary>
    /// مرحله 4: چرخش تصویر با حفظ تمام محتوا
    /// این متد تصویر را با زاویه مشخص می‌چرخاند
    /// </summary>
    private Mat RotateImage(Mat image, double angle)
    {
        try
        {
            Debug.WriteLine($"🔄 شروع چرخش تصویر با زاویه {angle:F2} درجه...");

            var (height, width) = (image.Height, image.Width);
            var center = new Point2f(width / 2.0f, height / 2.0f);

            // ایجاد ماتریس چرخش
            var rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            Debug.WriteLine("📝 ماتریس چرخش ایجاد شد");

            // محاسبه اندازه جدید برای جلوگیری از بریده شدن
            var radians = Math.Abs(angle) * Math.PI / 180.0;
            var sin = Math.Abs(Math.Sin(radians));
            var cos = Math.Abs(Math.Cos(radians));

            var newWidth = (int)(height * sin + width * cos);
            var newHeight = (int)(height * cos + width * sin);

            Debug.WriteLine($"📊 اندازه اصلی: {width}x{height}, اندازه جدید: {newWidth}x{newHeight}");

            // تنظیم ماتریس برای مرکز جدید
            rotationMatrix.At<double>(0, 2) += (newWidth / 2.0) - center.X;
            rotationMatrix.At<double>(1, 2) += (newHeight / 2.0) - center.Y;

            // اعمال چرخش با اندازه جدید
            var rotated = new Mat();
            Cv2.WarpAffine(image, rotated, rotationMatrix, new OpenCvSharp.Size(newWidth, newHeight),
                InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(0, 0, 0));

            Debug.WriteLine("✅ چرخش تصویر انجام شد");
            return rotated;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در چرخش تصویر: {ex.Message}");
            return image.Clone();
        }
    }

    /// <summary>
    /// مرحله 5: تنظیم برش تصویر برای بهبود نسبت ابعاد
    /// این متد تصویر را برای تشخیص بهتر برش می‌دهد
    /// </summary>
    private Mat AdjustCropping(Mat rotatedImage)
    {
        try
        {
            Debug.WriteLine("🔄 شروع تنظیم برش تصویر...");

            var (height, width) = (rotatedImage.Height, rotatedImage.Width);

            // محاسبه ارتفاع هدف (یک سوم عرض - افزایش ارتفاع برای حفظ اطلاعات بیشتر)
            var targetHeight = width / 3;
            Debug.WriteLine($"📝 ارتفاع هدف: {targetHeight} (یک سوم عرض)");

            // محاسبه برش از بالا و پایین
            var cropHeight = (height - targetHeight) / 2;

            // اطمینان از اینکه مقادیر مثبت هستند
            cropHeight = Math.Max(0, cropHeight);
            targetHeight = Math.Min(targetHeight, height);

            Debug.WriteLine($"📝 برش از بالا و پایین: {cropHeight} پیکسل");

            // برش تصویر
            var rect = new Rect(0, cropHeight, width, targetHeight);
            var result = new Mat(rotatedImage, rect);

            Debug.WriteLine($"📊 ابعاد نهایی: {result.Width}x{result.Height}");
            Debug.WriteLine("✅ تنظیم برش انجام شد");

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در تنظیم برش: {ex.Message}");
            return rotatedImage.Clone();
        }
    }

    /// <summary>
    /// تبدیل Mat به Bitmap
    /// این متد تصویر OpenCV را به Bitmap .NET تبدیل می‌کند
    /// </summary>
    private Bitmap? ConvertToBitmap(Mat mat)
    {
        try
        {
            if (mat.Empty())
            {
                Debug.WriteLine("❌ تصویر خالی است");
                return null;
            }

            // اگر تصویر خاکستری است، به RGB تبدیل کن
            Mat rgbMat;
            if (mat.Channels() == 1)
            {
                rgbMat = new Mat();
                Cv2.CvtColor(mat, rgbMat, ColorConversionCodes.GRAY2BGR);
                Debug.WriteLine("📝 تصویر خاکستری به RGB تبدیل شد");
            }
            else
            {
                rgbMat = mat.Clone();
                Debug.WriteLine("📝 تصویر رنگی کپی شد");
            }

            // تبدیل به Bitmap
            var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(rgbMat);
            rgbMat.Dispose();

            Debug.WriteLine("✅ تبدیل به Bitmap انجام شد");
            return bitmap;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ خطا در تبدیل به Bitmap: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Debug.WriteLine("🔄 پاکسازی سرویس اصلاح پلاک");
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
