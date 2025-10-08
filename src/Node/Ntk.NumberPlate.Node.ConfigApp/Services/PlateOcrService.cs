using System;
using System.Diagnostics;
using System.Drawing;
using Ntk.NumberPlate.Node.ConfigApp.Services.Ocr;
using Ntk.NumberPlate.Shared.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Ntk.NumberPlate.Node.ConfigApp.Services
{
    /// <summary>
    /// سرویس OCR برای تشخیص نوشته‌های پلاک با پشتیبانی از روش‌های مختلف
    /// </summary>
    public class PlateOcrService : IDisposable
    {
        private readonly NodeConfiguration _configuration;
        private readonly IOcrEngine _ocrEngine;
        private bool _disposed;

        public PlateOcrService(NodeConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // ایجاد موتور OCR مناسب بر اساس تنظیمات
            _ocrEngine = CreateOcrEngine(_configuration.OcrMethod);

            Debug.WriteLine($"✅ PlateOcrService با موتور '{_ocrEngine.EngineName}' آماده شد");
        }

        /// <summary>
        /// ایجاد موتور OCR بر اساس روش انتخابی
        /// </summary>
        private IOcrEngine CreateOcrEngine(OcrMethod method)
        {
            try
            {
                switch (method)
                {
                    case OcrMethod.Simple:
                        return new SimpleOcrEngine();

                    case OcrMethod.Yolo:
                        if (string.IsNullOrEmpty(_configuration.YoloOcrModelPath))
                        {
                            Debug.WriteLine("⚠️ مسیر مدل YOLO مشخص نشده - استفاده از روش ساده");
                            Debug.WriteLine("💡 لطفاً مسیر مدل YOLO را در تنظیمات مشخص کنید");
                            return new SimpleOcrEngine();
                        }

                        try
                        {
                            var yoloEngine = new YoloOcrEngine(
                                _configuration.YoloOcrModelPath,
                                _configuration.OcrConfidenceThreshold
                            );

                            if (!yoloEngine.IsReady)
                            {
                                Debug.WriteLine("⚠️ موتور YOLO آماده نیست - استفاده از روش ساده");
                                Debug.WriteLine($"💡 بررسی کنید که فایل مدل در مسیر '{_configuration.YoloOcrModelPath}' وجود دارد");
                                yoloEngine.Dispose();
                                return new SimpleOcrEngine();
                            }

                            Debug.WriteLine($"✅ موتور YOLO آماده شد: {_configuration.YoloOcrModelPath}");
                            return yoloEngine;
                        }
                        catch (Exception yoloEx)
                        {
                            Debug.WriteLine($"❌ خطا در ایجاد موتور YOLO: {yoloEx.Message}");
                            Debug.WriteLine("🔄 استفاده از روش ساده به عنوان جایگزین");
                            return new SimpleOcrEngine();
                        }

                    case OcrMethod.IronOcr:
                        var ironEngine = new IronOcrEngine();

                        if (!ironEngine.IsReady)
                        {
                            Debug.WriteLine("⚠️ موتور IronOCR آماده نیست - استفاده از روش ساده");
                            ironEngine.Dispose();
                            return new SimpleOcrEngine();
                        }

                        return ironEngine;

                    default:
                        Debug.WriteLine($"⚠️ روش OCR نامعتبر: {method} - استفاده از روش ساده");
                        return new SimpleOcrEngine();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ خطا در ایجاد موتور OCR: {ex.Message}");
                Debug.WriteLine("🔄 استفاده از موتور ساده به عنوان جایگزین");
                return new SimpleOcrEngine();
            }
        }

        /// <summary>
        /// تشخیص متن پلاک از تصویر (Bitmap)
        /// </summary>
        /// <param name="plateImage">تصویر پلاک</param>
        /// <returns>نتیجه OCR</returns>
        public OcrResult RecognizePlate(Bitmap plateImage)
        {
            if (plateImage == null)
            {
                return CreateErrorResult("تصویر پلاک null است");
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = _ocrEngine.Recognize(plateImage);

                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                LogResult(result);

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
        /// </summary>
        /// <param name="plateImage">تصویر پلاک</param>
        /// <returns>نتیجه OCR</returns>
        public OcrResult RecognizePlate(Mat plateImage)
        {
            if (plateImage == null || plateImage.Empty())
            {
                return CreateErrorResult("تصویر پلاک خالی است");
            }

            try
            {
                // تبدیل Mat به Bitmap
                using var bitmap = plateImage.ToBitmap();
                return RecognizePlate(bitmap);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ خطا در تبدیل Mat به Bitmap: {ex.Message}");
                return CreateErrorResult($"خطا در تبدیل تصویر: {ex.Message}");
            }
        }

        /// <summary>
        /// ایجاد نتیجه خطا
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
        /// ثبت log نتیجه OCR
        /// </summary>
        private void LogResult(OcrResult result)
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
        /// </summary>
        public string GetEngineInfo()
        {
            var status = _ocrEngine.IsReady ? "آماده" : "غیرآماده";
            var method = _ocrEngine.Method switch
            {
                OcrMethod.Simple => "Simple OCR",
                OcrMethod.Yolo => "YOLO OCR",
                OcrMethod.IronOcr => "IronOCR",
                _ => "نامشخص"
            };

            return $"{_ocrEngine.EngineName} ({method}) - وضعیت: {status}";
        }

        public void Dispose()
        {
            if (_disposed) return;

            Debug.WriteLine("🔄 پاکسازی PlateOcrService");

            _ocrEngine?.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
