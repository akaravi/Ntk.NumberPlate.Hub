using System;
using System.Drawing;
using Ntk.NumberPlate.Shared.Services;
using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Node.ConfigApp.Examples
{
    /// <summary>
    /// مثال‌های استفاده از سرویس OCR
    /// </summary>
    public class OcrServiceExample
    {
        /// <summary>
        /// مثال 1: استفاده از روش ساده
        /// </summary>
        public void Example_SimpleOcr()
        {
            Console.WriteLine("=== مثال 1: استفاده از روش ساده ===\n");

            // تنظیمات
            var config = new NodeConfiguration
            {
                OcrMethod = OcrMethod.Yolo
            };

            // ایجاد سرویس
            using var ocrService = new PlateDetectionOCRService(config);

            // فرض کنیم تصویر پلاک را داریم
            // var plateImage = new Bitmap("plate.jpg");

            // تشخیص متن
            // var result = ocrService.RecognizePlate(plateImage);

            // نمایش نتیجه
            // if (result.IsSuccessful)
            // {
            //     Console.WriteLine($"✅ متن پلاک: {result.Text}");
            //     Console.WriteLine($"📊 اعتماد: {result.Confidence:P0}");
            //     Console.WriteLine($"⏱️ زمان: {result.ProcessingTimeMs}ms");
            // }
            // else
            // {
            //     Console.WriteLine($"❌ خطا: {result.ErrorMessage}");
            // }
        }

        /// <summary>
        /// مثال 2: استفاده از روش YOLO
        /// </summary>
        public void Example_YoloOcr()
        {
            Console.WriteLine("=== مثال 2: استفاده از روش YOLO ===\n");

            // تنظیمات
            var config = new NodeConfiguration
            {
                OcrMethod = OcrMethod.Yolo,
                YoloOcrModelPath = "models/plate-ocr.onnx",
                OcrConfidenceThreshold = 0.5f
            };

            // ایجاد سرویس
            using var ocrService = new PlateDetectionOCRService(config);

            // استفاده مشابه مثال قبل
            Console.WriteLine("✅ سرویس YOLO OCR آماده است");
        }

        /// <summary>
        /// مثال 3: استفاده از روش IronOCR
        /// </summary>
        public void Example_IronOcr()
        {
            Console.WriteLine("=== مثال 3: استفاده از روش IronOCR ===\n");

            // تنظیمات
            var config = new NodeConfiguration
            {
                OcrMethod = OcrMethod.Yolo
            };

            // ایجاد سرویس
            using var ocrService = new PlateDetectionOCRService(config);

            // استفاده مشابه مثال‌های قبل
            Console.WriteLine("✅ سرویس IronOCR آماده است");
        }

        /// <summary>
        /// مثال 4: تغییر روش OCR در زمان اجرا
        /// </summary>
        public void Example_SwitchOcrMethod()
        {
            Console.WriteLine("=== مثال 4: تغییر روش OCR ===\n");

            var config = new NodeConfiguration();

            // تست با روش ساده
            config.OcrMethod = OcrMethod.Yolo;
            using (var ocrService1 = new PlateDetectionOCRService(config))
            {
                Console.WriteLine("🔧 استفاده از روش ساده");
                // var result1 = ocrService1.RecognizePlate(plateImage);
            }

            // تست با روش YOLO
            config.OcrMethod = OcrMethod.Yolo;
            config.YoloOcrModelPath = "models/plate-ocr.onnx";
            using (var ocrService2 = new PlateDetectionOCRService(config))
            {
                Console.WriteLine("🔧 استفاده از روش YOLO");
                // var result2 = ocrService2.RecognizePlate(plateImage);
            }

            // مقایسه نتایج
            // Console.WriteLine($"Simple: {result1.Text} ({result1.ProcessingTimeMs}ms)");
            // Console.WriteLine($"YOLO: {result2.Text} ({result2.ProcessingTimeMs}ms)");
        }

        /// <summary>
        /// مثال 5: استفاده در کنار PlateCorrectionService
        /// </summary>
        public void Example_WithPlateCorrection()
        {
            Console.WriteLine("=== مثال 5: ترکیب با اصلاح پلاک ===\n");

            var config = new NodeConfiguration
            {
                OcrMethod = OcrMethod.Yolo,
                YoloOcrModelPath = "models/plate-ocr.onnx"
            };

            var correctionService = new PlateCorrectionService();
            using var ocrService = new PlateDetectionOCRService(config);

            // فرض: تصویر کامل و مختصات پلاک
            // string imagePath = "full-image.jpg";
            // var boundingBox = new BoundingBox { X = 100, Y = 100, Width = 200, Height = 50 };

            // 1. اصلاح پلاک (چرخش و برش)
            // var correctedPlate = correctionService.CorrectPlateImage(imagePath, boundingBox);

            // 2. تشخیص متن از پلاک اصلاح شده
            // if (correctedPlate != null)
            // {
            //     var ocrResult = ocrService.RecognizePlate(correctedPlate);
            //     
            //     if (ocrResult.IsSuccessful)
            //     {
            //         Console.WriteLine($"✅ پلاک شناسایی شد: {ocrResult.Text}");
            //     }
            // }
        }

        /// <summary>
        /// مثال 6: پردازش batch تصاویر
        /// </summary>
        public void Example_BatchProcessing()
        {
            Console.WriteLine("=== مثال 6: پردازش دسته‌ای ===\n");

            var config = new NodeConfiguration
            {
                OcrMethod = OcrMethod.Yolo
            };

            using var ocrService = new PlateDetectionOCRService(config);

            // فرض: لیست تصاویر پلاک
            // var plateImages = new List<Bitmap>
            // {
            //     new Bitmap("plate1.jpg"),
            //     new Bitmap("plate2.jpg"),
            //     new Bitmap("plate3.jpg"),
            // };

            // پردازش هر تصویر
            // foreach (var plateImage in plateImages)
            // {
            //     var result = ocrService.RecognizePlate(plateImage);
            //     
            //     if (result.IsSuccessful)
            //     {
            //         Console.WriteLine($"✅ {result.Text} (اعتماد: {result.Confidence:P0})");
            //     }
            //     else
            //     {
            //         Console.WriteLine($"❌ شکست: {result.ErrorMessage}");
            //     }
            // }

            // آمار کلی
            // var successCount = results.Count(r => r.IsSuccessful);
            // var avgConfidence = results.Where(r => r.IsSuccessful).Average(r => r.Confidence);
            // var avgTime = results.Average(r => r.ProcessingTimeMs);
            // 
            // Console.WriteLine($"\n📊 آمار:");
            // Console.WriteLine($"  موفق: {successCount}/{results.Count}");
            // Console.WriteLine($"  میانگین اعتماد: {avgConfidence:P0}");
            // Console.WriteLine($"  میانگین زمان: {avgTime:F2}ms");
        }

        /// <summary>
        /// مثال 7: مدیریت خطاها
        /// </summary>
        public void Example_ErrorHandling()
        {
            Console.WriteLine("=== مثال 7: مدیریت خطاها ===\n");

            try
            {
                var config = new NodeConfiguration
                {
                    OcrMethod = OcrMethod.Yolo,
                    YoloOcrModelPath = "models/non-existent.onnx" // فایل وجود ندارد
                };

                using var ocrService = new PlateDetectionOCRService(config);

                // سرویس به صورت خودکار به روش ساده Fallback می‌کند
                Console.WriteLine("✅ سرویس با روش جایگزین ایجاد شد");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطا: {ex.Message}");
            }
        }

        /// <summary>
        /// مثال 8: کار با OpenCV Mat
        /// </summary>
        public void Example_WithOpenCvMat()
        {
            Console.WriteLine("=== مثال 8: استفاده با OpenCV Mat ===\n");

            var config = new NodeConfiguration
            {
                OcrMethod = OcrMethod.Yolo
            };

            using var ocrService = new PlateDetectionOCRService(config);

            // کار مستقیم با Mat
            // using var image = Cv2.ImRead("plate.jpg");
            // var result = ocrService.RecognizePlate(image);

            // یا با پیش‌پردازش
            // using var gray = new Mat();
            // Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            // var result = ocrService.RecognizePlate(gray);
        }

        /// <summary>
        /// اجرای همه مثال‌ها
        /// </summary>
        public static void RunAllExamples()
        {
            var examples = new OcrServiceExample();

            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║   مثال‌های استفاده از OCR Service   ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");

            try
            {
                examples.Example_SimpleOcr();
                Console.WriteLine();

                examples.Example_YoloOcr();
                Console.WriteLine();

                examples.Example_IronOcr();
                Console.WriteLine();

                examples.Example_SwitchOcrMethod();
                Console.WriteLine();

                examples.Example_WithPlateCorrection();
                Console.WriteLine();

                examples.Example_BatchProcessing();
                Console.WriteLine();

                examples.Example_ErrorHandling();
                Console.WriteLine();

                examples.Example_WithOpenCvMat();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ خطا در اجرای مثال‌ها: {ex.Message}");
            }

            Console.WriteLine("\n✅ همه مثال‌ها اجرا شد");
        }
    }
}

