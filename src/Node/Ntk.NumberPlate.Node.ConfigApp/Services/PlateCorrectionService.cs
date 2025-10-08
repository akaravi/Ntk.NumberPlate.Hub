using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Ntk.NumberPlate.Shared.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Ntk.NumberPlate.Node.ConfigApp.Services
{
    /// <summary>
    /// سرویس اصلاح و صاف کردن تصاویر پلاک
    /// </summary>
    public class PlateCorrectionService
    {
        /// <summary>
        /// اصلاح تصویر پلاک (Bitmap ورودی)
        /// </summary>
        /// <param name="plateImage">تصویر پلاک</param>
        /// <returns>تصویر اصلاح شده یا null در صورت خطا</returns>
        public Bitmap? CorrectPlate(Bitmap plateImage)
        {
            try
            {
                // تبدیل Bitmap به Mat
                using var mat = BitmapConverter.ToMat(plateImage);

                // تبدیل به خاکستری
                using var grayPlate = new Mat();
                Cv2.CvtColor(mat, grayPlate, ColorConversionCodes.BGR2GRAY);

                // تشخیص خطوط
                var lines = FindLongestLine(grayPlate);
                if (lines == null || lines.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("هیچ خطی یافت نشد - بازگشت تصویر اصلی");
                    return plateImage;
                }

                // محاسبه زاویه چرخش
                var longestLine = lines[lines.Length - 1];
                var rotationAngle = FindLineAngle(longestLine);

                System.Diagnostics.Debug.WriteLine($"تعداد خطوط: {lines.Length}, زاویه محاسبه شده: {rotationAngle:F2} درجه");

                // اگر زاویه خیلی کم است، نیازی به چرخش نیست
                if (Math.Abs(rotationAngle) < 1.0)
                {
                    System.Diagnostics.Debug.WriteLine("زاویه چرخش ناچیز - بازگشت تصویر اصلی");
                    return plateImage;
                }

                // چرخش تصویر
                var center = new Point2f(mat.Width / 2.0f, mat.Height / 2.0f);
                using var rotationMatrix = Cv2.GetRotationMatrix2D(center, rotationAngle, 1.0);
                using var rotatedImage = new Mat();
                Cv2.WarpAffine(mat, rotatedImage, rotationMatrix, mat.Size());

                // تبدیل به Bitmap و برگرداندن
                return BitmapConverter.ToBitmap(rotatedImage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در اصلاح پلاک: {ex.Message}");
                return plateImage; // در صورت خطا، تصویر اصلی را برگردان
            }
        }

        /// <summary>
        /// اصلاح تصویر پلاک با تشخیص خط و چرخش
        /// </summary>
        /// <param name="imagePath">مسیر تصویر اصلی</param>
        /// <param name="boundingBox">محدوده پلاک در تصویر</param>
        /// <returns>تصویر اصلاح شده یا null در صورت خطا</returns>
        public Bitmap? CorrectPlateImage(string imagePath, BoundingBox boundingBox)
        {
            try
            {
                // بارگذاری تصویر
                using var originalImage = Cv2.ImRead(imagePath);
                if (originalImage.Empty())
                    return null;

                // برش پلاک از تصویر اصلی
                var plateRect = new Rect(boundingBox.X, boundingBox.Y, boundingBox.Width, boundingBox.Height);
                using var plateImage = new Mat(originalImage, plateRect);

                // تبدیل به خاکستری
                using var grayPlate = new Mat();
                Cv2.CvtColor(plateImage, grayPlate, ColorConversionCodes.BGR2GRAY);

                // تشخیص خطوط
                var lines = FindLongestLine(grayPlate);
                if (lines == null || lines.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("هیچ خطی یافت نشد - بازگشت تصویر اصلی");
                    return ConvertToBitmap(plateImage);
                }

                // محاسبه زاویه چرخش (استفاده از آخرین خط = طولانی‌ترین خط)
                var longestLine = lines[lines.Length - 1];
                var rotationAngle = FindLineAngle(longestLine);

                System.Diagnostics.Debug.WriteLine($"تعداد خطوط: {lines.Length}, زاویه محاسبه شده: {rotationAngle:F2} درجه");
                System.Diagnostics.Debug.WriteLine($"خط انتخاب شده: P1({longestLine.P1.X},{longestLine.P1.Y}) -> P2({longestLine.P2.X},{longestLine.P2.Y})");

                // اگر زاویه خیلی کوچک است، چرخش ندهیم
                if (Math.Abs(rotationAngle) < 0.5)
                {
                    System.Diagnostics.Debug.WriteLine("زاویه خیلی کوچک است - بدون چرخش");
                    var adjusted = AdjustCropping(grayPlate);
                    return ConvertToBitmap(adjusted);
                }

                // چرخش تصویر
                var rotatedImage = RotateImage(grayPlate, rotationAngle);

                // تنظیم برش
                var adjustedImage = AdjustCropping(rotatedImage);

                return ConvertToBitmap(adjustedImage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در اصلاح پلاک: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// تشخیص خطوط در تصویر پلاک (مطابق کد پایتون)
        /// </summary>
        private LineSegmentPoint[]? FindLongestLine(Mat plateImage)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"اندازه تصویر پلاک: {plateImage.Width}x{plateImage.Height}");

                // اعمال فیلتر گاوسی - kernel_size = 3
                using var blurred = new Mat();
                Cv2.GaussianBlur(plateImage, blurred, new OpenCvSharp.Size(3, 3), 0);

                // تشخیص لبه‌ها - کاهش آستانه برای حساسیت بیشتر و تشخیص دقیق‌تر هاشورها
                using var edges = new Mat();
                Cv2.Canny(blurred, edges, 30, 100);

                // ذخیره تصویر لبه‌ها برای دیباگ
                var edgePixels = Cv2.CountNonZero(edges);
                System.Diagnostics.Debug.WriteLine($"تعداد پیکسل‌های لبه: {edgePixels}");

                // تلاش با پارامترهای مختلف
                LineSegmentPoint[]? lines = null;

                // تلاش 1: پارامترهای اصلی پایتون
                lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 15, 50, 5);
                if (lines != null && lines.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"روش 1 موفق: {lines.Length} خط یافت شد");
                }
                else
                {
                    // تلاش 2: کاهش threshold و minLineLength
                    System.Diagnostics.Debug.WriteLine("تلاش با پارامترهای کمتر...");
                    lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 10, 30, 10);

                    if (lines != null && lines.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"روش 2 موفق: {lines.Length} خط یافت شد");
                    }
                    else
                    {
                        // تلاش 3: پارامترهای خیلی آسان‌تر
                        System.Diagnostics.Debug.WriteLine("تلاش با پارامترهای بسیار آسان...");
                        lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 5, 20, 15);

                        if (lines != null && lines.Length > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"روش 3 موفق: {lines.Length} خط یافت شد");
                        }
                    }
                }

                if (lines == null || lines.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("هیچ خطی با هیچ پارامتری یافت نشد - احتمالاً تصویر لبه کافی ندارد");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"تعداد کل خطوط یافت شده: {lines.Length}");

                // محاسبه طول خطوط و مرتب‌سازی
                var lineLengths = new List<(LineSegmentPoint line, double length)>();
                foreach (var line in lines)
                {
                    var length = Math.Sqrt(Math.Pow(line.P2.X - line.P1.X, 2) + Math.Pow(line.P2.Y - line.P1.Y, 2));
                    lineLengths.Add((line, length));
                }

                // مرتب‌سازی بر اساس طول (کوچکترین اول، مطابق کد پایتون)
                lineLengths.Sort((a, b) => a.length.CompareTo(b.length));

                // نمایش طولانی‌ترین 5 خط برای دیباگ
                var topLines = lineLengths.TakeLast(Math.Min(5, lineLengths.Count)).ToList();
                System.Diagnostics.Debug.WriteLine("طولانی‌ترین خطوط:");
                foreach (var (line, length) in topLines)
                {
                    System.Diagnostics.Debug.WriteLine($"  طول: {length:F2}, P1({line.P1.X},{line.P1.Y}) -> P2({line.P2.X},{line.P2.Y})");
                }

                return lineLengths.Select(x => x.line).ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در تشخیص خطوط: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// محاسبه زاویه خط (مطابق کد پایتون)
        /// </summary>
        private double FindLineAngle(LineSegmentPoint line)
        {
            var deltaX = line.P2.X - line.P1.X;
            var deltaY = line.P2.Y - line.P1.Y;

            if (Math.Abs(deltaX) < 0.001) // خط عمودی
                return 0;

            // استفاده از Atan (نه Atan2) مطابق کد پایتون
            var angle = Math.Atan(deltaY / (double)deltaX) * 180.0 / Math.PI;
            return angle;
        }

        /// <summary>
        /// چرخش تصویر با حفظ تمام محتوا
        /// </summary>
        private Mat RotateImage(Mat image, double angle)
        {
            try
            {
                var (height, width) = (image.Height, image.Width);
                var center = new Point2f(width / 2.0f, height / 2.0f);

                // ماتریس چرخش
                var rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);

                // محاسبه اندازه جدید برای جلوگیری از بریده شدن
                var radians = Math.Abs(angle) * Math.PI / 180.0;
                var sin = Math.Abs(Math.Sin(radians));
                var cos = Math.Abs(Math.Cos(radians));

                var newWidth = (int)(height * sin + width * cos);
                var newHeight = (int)(height * cos + width * sin);

                // تنظیم ماتریس برای مرکز جدید
                rotationMatrix.At<double>(0, 2) += (newWidth / 2.0) - center.X;
                rotationMatrix.At<double>(1, 2) += (newHeight / 2.0) - center.Y;

                System.Diagnostics.Debug.WriteLine($"اندازه اصلی: {width}x{height}, اندازه جدید: {newWidth}x{newHeight}");

                // اعمال چرخش با اندازه جدید
                var rotated = new Mat();
                Cv2.WarpAffine(image, rotated, rotationMatrix, new OpenCvSharp.Size(newWidth, newHeight),
                    InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(0, 0, 0));

                return rotated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در چرخش تصویر: {ex.Message}");
                return image.Clone();
            }
        }

        /// <summary>
        /// تنظیم برش تصویر
        /// </summary>
        private Mat AdjustCropping(Mat rotatedImage)
        {
            try
            {
                var (height, width) = (rotatedImage.Height, rotatedImage.Width);

                // محاسبه ارتفاع هدف (یک سوم عرض - افزایش ارتفاع برای حفظ اطلاعات بیشتر)
                var targetHeight = width / 3;

                // محاسبه برش از بالا و پایین
                var cropHeight = (height - targetHeight) / 2;

                // اطمینان از اینکه مقادیر مثبت هستند
                cropHeight = Math.Max(0, cropHeight);
                targetHeight = Math.Min(targetHeight, height);

                // برش تصویر
                var rect = new Rect(0, cropHeight, width, targetHeight);
                return new Mat(rotatedImage, rect);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در تنظیم برش: {ex.Message}");
                return rotatedImage.Clone();
            }
        }

        /// <summary>
        /// تبدیل Mat به Bitmap
        /// </summary>
        private Bitmap? ConvertToBitmap(Mat mat)
        {
            try
            {
                if (mat.Empty())
                    return null;

                // اگر تصویر خاکستری است، به RGB تبدیل کن
                Mat rgbMat;
                if (mat.Channels() == 1)
                {
                    rgbMat = new Mat();
                    Cv2.CvtColor(mat, rgbMat, ColorConversionCodes.GRAY2BGR);
                }
                else
                {
                    rgbMat = mat.Clone();
                }

                // تبدیل به Bitmap
                var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(rgbMat);
                rgbMat.Dispose();

                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در تبدیل به Bitmap: {ex.Message}");
                return null;
            }
        }
    }
}
