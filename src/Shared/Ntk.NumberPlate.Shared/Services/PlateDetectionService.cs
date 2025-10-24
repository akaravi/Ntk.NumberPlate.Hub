//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using Microsoft.ML.OnnxRuntime;
//using Microsoft.ML.OnnxRuntime.Tensors;
//using Ntk.NumberPlate.Shared.Models;
//using OpenCvSharp;
//using OpenCvSharp.Extensions;

//namespace Ntk.NumberPlate.Shared.Services;

///// <summary>
///// سرویس تشخیص پلاک با استفاده از مدل YOLO
///// این سرویس مسئول تشخیص موقعیت پلاک‌ها در تصاویر است
///// </summary>
//public class PlateDetectionService : IDisposable
//{
//    private InferenceSession? _session;
//    private string _inputName = "images";
//    private int _inputWidth = 640;
//    private int _inputHeight = 640;
//    private readonly string _modelPath;
//    private readonly float _confidenceThreshold;
//    private bool _disposed;

//    public PlateDetectionService(string modelPath, float confidenceThreshold = 0.5f)
//    {
//        _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
//        _confidenceThreshold = confidenceThreshold;
        
//        Debug.WriteLine($"🔧 سرویس تشخیص پلاک با مدل: {_modelPath}");
//    }

//    /// <summary>
//    /// مقداردهی اولیه مدل YOLO
//    /// این متد مدل را بارگذاری می‌کند و پارامترهای ورودی را کشف می‌کند
//    /// </summary>
//    /// <param name="errorMessage">پیام خطا در صورت عدم موفقیت</param>
//    /// <returns>true اگر مدل با موفقیت بارگذاری شد</returns>
//    public bool Initialize(out string errorMessage)
//    {
//        errorMessage = string.Empty;

//        try
//        {
//            Debug.WriteLine("🔄 شروع مقداردهی مدل تشخیص پلاک...");

//            // بررسی وجود فایل مدل
//            if (string.IsNullOrWhiteSpace(_modelPath))
//            {
//                errorMessage = "مسیر مدل خالی است";
//                Debug.WriteLine("❌ مسیر مدل مشخص نشده");
//                return false;
//            }

//            // بررسی وجود فایل در سیستم
//            if (!File.Exists(_modelPath))
//            {
//                errorMessage = $"فایل مدل در مسیر زیر یافت نشد:\n{_modelPath}";
//                Debug.WriteLine($"❌ فایل مدل یافت نشد: {_modelPath}");
//                return false;
//            }

//            // بررسی فرمت فایل (باید ONNX باشد)
//            var extension = Path.GetExtension(_modelPath).ToLower();
//            if (extension != ".onnx")
//            {
//                errorMessage = $"فرمت فایل باید ONNX باشد (.onnx)\nفرمت فعلی: {extension}";
//                Debug.WriteLine($"❌ فرمت فایل نامعتبر: {extension}");
//                return false;
//            }

//            // بررسی حجم فایل (فایل‌های کوچک احتمالاً خراب هستند)
//            var fileInfo = new FileInfo(_modelPath);
//            if (fileInfo.Length < 1024) // کمتر از 1KB
//            {
//                errorMessage = $"فایل مدل خراب یا ناقص است (حجم: {fileInfo.Length} بایت)";
//                Debug.WriteLine($"❌ فایل مدل کوچک است: {fileInfo.Length} بایت");
//                return false;
//            }

//            Debug.WriteLine($"📊 اطلاعات فایل مدل: حجم {fileInfo.Length / 1024 / 1024:F2} MB");

//            // ایجاد تنظیمات جلسه ONNX
//            var sessionOptions = new SessionOptions();
            
//            try
//            {
//                // فعال‌سازی Custom Ops برای پشتیبانی از عملیات اضافی YOLO
//                sessionOptions.RegisterOrtExtensions();
//                Debug.WriteLine("✅ Custom Ops فعال شد");
//            }
//            catch (Exception ex)
//            {
//                // اگر Extensions در دسترس نیست، ادامه می‌دهیم
//                Debug.WriteLine($"⚠️ Custom Ops در دسترس نیست: {ex.Message}");
//            }

//            // بارگذاری مدل ONNX
//            _session = new InferenceSession(_modelPath, sessionOptions);
//            Debug.WriteLine("✅ مدل ONNX بارگذاری شد");

//            // کشف نام و ابعاد ورودی از متادیتای مدل
//            var inputMeta = _session.InputMetadata.FirstOrDefault();
//            if (!string.IsNullOrWhiteSpace(inputMeta.Key))
//            {
//                _inputName = inputMeta.Key;
//                Debug.WriteLine($"📝 نام ورودی مدل: {_inputName}");
                
//                // استخراج ابعاد ورودی
//                var dims = inputMeta.Value.Dimensions;
//                if (dims != null && dims.Length == 4)
//                {
//                    // فرمت معمول: [batch, channels, height, width]
//                    if (dims[2] > 0) _inputHeight = dims[2];
//                    if (dims[3] > 0) _inputWidth = dims[3];
//                    Debug.WriteLine($"📝 ابعاد ورودی مدل: {_inputWidth}x{_inputHeight}");
//                }
//            }

//            Debug.WriteLine("✅ سرویس تشخیص پلاک با موفقیت آماده شد");
//            return true;
//        }
//        catch (Exception ex)
//        {
//            errorMessage = $"خطا در بارگذاری مدل:\n{ex.Message}\n\nجزئیات:\n{ex.InnerException?.Message}";
//            Debug.WriteLine($"❌ خطا در مقداردهی: {ex.Message}");
//            return false;
//        }
//    }

//    /// <summary>
//    /// تشخیص پلاک‌ها در تصویر
//    /// این متد اصلی برای تشخیص پلاک‌ها در یک فریم تصویر است
//    /// </summary>
//    /// <param name="frame">فریم تصویر برای تشخیص</param>
//    /// <returns>لیست تشخیص‌های پلاک</returns>
//    public async Task<List<VehicleDetectionData>> DetectPlatesAsync(Mat frame)
//    {
//        var detections = new List<VehicleDetectionData>();

//        try
//        {
//            Debug.WriteLine($"🔄 شروع تشخیص پلاک در فریم {frame.Width}x{frame.Height}...");

//            // بررسی آماده بودن مدل
//            if (_session == null)
//            {
//                Debug.WriteLine("❌ مدل آماده نیست");
//                return detections;
//            }

//            // مرحله 1: پیش‌پردازش تصویر برای ورودی YOLO
//            var (inputTensor, ratio, padW, padH) = PreprocessImageForYolo(frame);
//            Debug.WriteLine($"✅ پیش‌پردازش انجام شد (ratio: {ratio:F3}, pad: {padW}x{padH})");

//            // مرحله 2: اجرای مدل YOLO
//            var output = await RunYoloInference(inputTensor);
//            Debug.WriteLine($"✅ استنتاج YOLO انجام شد");

//            // مرحله 3: پس‌پردازش نتایج
//            detections = PostProcessDetections(output, frame.Width, frame.Height, ratio, padW, padH);
//            Debug.WriteLine($"📊 {detections.Count} تشخیص اولیه یافت شد");

//            // مرحله 4: فیلتر بر اساس آستانه اعتماد
//            detections = detections.Where(d => d.Confidence >= _confidenceThreshold).ToList();
//            Debug.WriteLine($"📊 {detections.Count} تشخیص پس از فیلتر اعتماد");

//            // مرحله 5: حذف تشخیص‌های تکراری با NMS
//            detections = ApplyNonMaximumSuppression(detections);
//            Debug.WriteLine($"📊 {detections.Count} تشخیص نهایی پس از NMS");

//            return detections;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"❌ خطا در تشخیص پلاک: {ex.Message}");
//            return detections;
//        }
//    }

//    /// <summary>
//    /// پیش‌پردازش تصویر برای ورودی YOLO
//    /// این مرحله تصویر را به فرمت مورد نیاز YOLO تبدیل می‌کند
//    /// </summary>
//    private (DenseTensor<float> Tensor, float ResizeRatio, int PadW, int PadH) PreprocessImageForYolo(Mat frame)
//    {
//        Debug.WriteLine("🔄 شروع پیش‌پردازش تصویر...");

//        // تعیین ابعاد هدف مدل
//        int targetW = _inputWidth;
//        int targetH = _inputHeight;
//        Debug.WriteLine($"📝 ابعاد هدف: {targetW}x{targetH}");

//        // محاسبه نسبت تغییر اندازه با حفظ نسبت ابعاد
//        float r = Math.Min((float)targetW / frame.Width, (float)targetH / frame.Height);
//        int newW = (int)Math.Round(frame.Width * r);
//        int newH = (int)Math.Round(frame.Height * r);
//        Debug.WriteLine($"📝 ابعاد جدید: {newW}x{newH} (ratio: {r:F3})");

//        // تغییر اندازه تصویر
//        var resized = new Mat();
//        Cv2.Resize(frame, resized, new OpenCvSharp.Size(newW, newH));
//        Debug.WriteLine("✅ تغییر اندازه انجام شد");

//        // محاسبه padding برای رسیدن به ابعاد هدف
//        int padW = (targetW - newW) / 2;
//        int padH = (targetH - newH) / 2;
//        Debug.WriteLine($"📝 padding: {padW}x{padH}");

//        // ایجاد تصویر با padding (رنگ پس‌زمینه: خاکستری)
//        var padded = new Mat(new OpenCvSharp.Size(targetW, targetH), MatType.CV_8UC3, new Scalar(114, 114, 114));
//        resized.CopyTo(new Mat(padded, new Rect(padW, padH, newW, newH)));
//        Debug.WriteLine("✅ padding اعمال شد");

//        // تبدیل BGR به RGB (YOLO معمولاً RGB می‌خواهد)
//        Cv2.CvtColor(padded, padded, ColorConversionCodes.BGR2RGB);
//        Debug.WriteLine("✅ تبدیل رنگ BGR->RGB انجام شد");

//        // تبدیل به Tensor برای ورودی مدل
//        var tensor = new DenseTensor<float>(new[] { 1, 3, targetH, targetW });
        
//        // کپی پیکسل‌ها به Tensor با نرمال‌سازی (0-1)
//        for (int y = 0; y < targetH; y++)
//        {
//            for (int x = 0; x < targetW; x++)
//            {
//                var pixel = padded.At<Vec3b>(y, x);
//                tensor[0, 0, y, x] = pixel.Item2 / 255f; // R
//                tensor[0, 1, y, x] = pixel.Item1 / 255f; // G
//                tensor[0, 2, y, x] = pixel.Item0 / 255f; // B
//            }
//        }
//        Debug.WriteLine("✅ تبدیل به Tensor انجام شد");

//        resized.Dispose();
//        padded.Dispose();

//        return (tensor, r, padW, padH);
//    }

//    /// <summary>
//    /// اجرای استنتاج YOLO
//    /// این متد مدل را روی تصویر پیش‌پردازش شده اجرا می‌کند
//    /// </summary>
//    private async Task<float[]> RunYoloInference(DenseTensor<float> inputTensor)
//    {
//        Debug.WriteLine("🔄 شروع استنتاج YOLO...");

//        try
//        {
//            // ایجاد ورودی‌های مدل
//            var inputs = new List<NamedOnnxValue>
//            {
//                NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
//            };

//            // اجرای مدل
//            using var results = _session!.Run(inputs);
//            var outputTensor = results.First().AsTensor<float>();
//            var output = outputTensor.ToArray();

//            Debug.WriteLine($"✅ استنتاج YOLO تکمیل شد (خروجی: {output.Length} عنصر)");

//            await Task.CompletedTask; // برای async بودن
//            return output;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"❌ خطا در استنتاج YOLO: {ex.Message}");
//            throw;
//        }
//    }

//    /// <summary>
//    /// پس‌پردازش نتایج YOLO
//    /// این متد خروجی خام YOLO را به تشخیص‌های قابل استفاده تبدیل می‌کند
//    /// </summary>
//    private List<VehicleDetectionData> PostProcessDetections(float[] output, int originalWidth, int originalHeight, float resizeRatio, int padW, int padH)
//    {
//        var detections = new List<VehicleDetectionData>();

//        try
//        {
//            Debug.WriteLine("🔄 شروع پس‌پردازش نتایج...");

//            // تحلیل ساختار خروجی YOLO
//            // YOLO معمولاً خروجی‌ای با فرمت [batch, anchors, attributes] دارد
//            // attributes شامل: [x, y, w, h, confidence, class_scores...]

//            // محاسبه تعداد anchor ها و attributes
//            int numAnchors = 0;
//            int numAttrs = 0;
//            bool anchorsLast = false;

//            // تلاش برای تشخیص ساختار خروجی
//            if (output.Length > 0)
//            {
//                // فرض: اگر خروجی کوچک است، احتمالاً ساختار ساده دارد
//                if (output.Length < 10000)
//                {
//                    numAttrs = 85; // فرمت استاندارد YOLO
//                    numAnchors = output.Length / numAttrs;
//                    Debug.WriteLine($"📝 ساختار ساده تشخیص داده شد: {numAnchors} anchor, {numAttrs} attribute");
//                }
//                else
//                {
//                    // برای خروجی‌های بزرگ، از ساختار پیچیده‌تر استفاده می‌کنیم
//                    numAttrs = 85;
//                    numAnchors = output.Length / numAttrs;
//                    Debug.WriteLine($"📝 ساختار پیچیده تشخیص داده شد: {numAnchors} anchor, {numAttrs} attribute");
//                }
//            }

//            // پردازش هر anchor
//            for (int a = 0; a < numAnchors; a++)
//            {
//                // استخراج مختصات و اعتماد
//                int baseIdx = a * numAttrs;
//                float cx = output[baseIdx + 0];     // مرکز X
//                float cy = output[baseIdx + 1];     // مرکز Y
//                float w = output[baseIdx + 2];       // عرض
//                float h = output[baseIdx + 3];      // ارتفاع
//                float obj = output[baseIdx + 4];    // اعتماد objectness

//                // محاسبه اعتماد نهایی
//                float confidence = obj;

//                // اگر کلاس‌های اضافی وجود دارد، اعتماد کلاس را نیز در نظر بگیر
//                if (numAttrs > 5)
//                {
//                    float maxClassConf = 0;
//                    for (int c = 5; c < numAttrs; c++)
//                    {
//                        float classConf = output[baseIdx + c];
//                        if (classConf > maxClassConf)
//                            maxClassConf = classConf;
//                    }
//                    confidence *= maxClassConf;
//                }

//                // فیلتر بر اساس آستانه اعتماد
//                if (confidence < _confidenceThreshold)
//                    continue;

//                // تشخیص اینکه آیا مختصات نرمال هستند (0-1) یا پیکسلی
//                bool normalized = (w <= 1.5f && h <= 1.5f && cx <= 1.5f && cy <= 1.5f);
                
//                if (normalized)
//                {
//                    // تبدیل مختصات نرمال به پیکسل
//                    cx *= _inputWidth;
//                    cy *= _inputHeight;
//                    w *= _inputWidth;
//                    h *= _inputHeight;
//                    Debug.WriteLine($"📝 مختصات نرمال تشخیص داده شد و تبدیل شد");
//                }

//                // تبدیل مرکز به گوشه‌ها
//                float x1 = cx - w / 2f;
//                float y1 = cy - h / 2f;
//                float x2 = cx + w / 2f;
//                float y2 = cy + h / 2f;

//                // حذف padding
//                x1 -= padW; x2 -= padW;
//                y1 -= padH; y2 -= padH;

//                // بازنگاشت به ابعاد تصویر اصلی
//                x1 /= resizeRatio; x2 /= resizeRatio;
//                y1 /= resizeRatio; y2 /= resizeRatio;

//                // محدود کردن به ابعاد تصویر
//                int ix1 = (int)Math.Round(Math.Clamp(x1, 0, originalWidth - 1));
//                int iy1 = (int)Math.Round(Math.Clamp(y1, 0, originalHeight - 1));
//                int ix2 = (int)Math.Round(Math.Clamp(x2, 0, originalWidth - 1));
//                int iy2 = (int)Math.Round(Math.Clamp(y2, 0, originalHeight - 1));

//                int bw = Math.Max(1, ix2 - ix1);
//                int bh = Math.Max(1, iy2 - iy1);

//                // ایجاد تشخیص جدید
//                var detection = new VehicleDetectionData
//                {
//                    PlateBoundingBox = new BoundingBox { X = ix1, Y = iy1, Width = bw, Height = bh },
//                    Confidence = confidence,
//                    PlateNumber = "در انتظار OCR", // بعداً با OCR پر می‌شود
//                    DetectionTime = DateTime.Now,
//                    VehicleType = VehicleType.Car
//                };

//                detections.Add(detection);
//                Debug.WriteLine($"📝 تشخیص {detections.Count}: ({ix1},{iy1}) {bw}x{bh} - اعتماد: {confidence:F3}");
//            }

//            Debug.WriteLine($"✅ پس‌پردازش تکمیل شد: {detections.Count} تشخیص");
//            return detections;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"❌ خطا در پس‌پردازش: {ex.Message}");
//            return detections;
//        }
//    }

//    /// <summary>
//    /// اعمال Non-Maximum Suppression برای حذف تشخیص‌های تکراری
//    /// این الگوریتم تشخیص‌های همپوشان را حذف می‌کند
//    /// </summary>
//    private List<VehicleDetectionData> ApplyNonMaximumSuppression(List<VehicleDetectionData> detections)
//    {
//        if (detections.Count <= 1)
//        {
//            Debug.WriteLine("📝 NMS: تعداد تشخیص‌ها کم است، نیازی به NMS نیست");
//            return detections;
//        }

//        Debug.WriteLine($"🔄 شروع NMS روی {detections.Count} تشخیص...");

//        // مرتب‌سازی بر اساس اعتماد (بزرگترین اول)
//        var sortedDetections = detections.OrderByDescending(d => d.Confidence).ToList();
//        var result = new List<VehicleDetectionData>();
//        var suppressed = new bool[sortedDetections.Count];

//        // پردازش هر تشخیص
//        for (int i = 0; i < sortedDetections.Count; i++)
//        {
//            if (suppressed[i])
//                continue;

//            var current = sortedDetections[i];
//            result.Add(current);
//            Debug.WriteLine($"📝 تشخیص {i + 1} انتخاب شد (اعتماد: {current.Confidence:F3})");

//            // بررسی همپوشانی با سایر تشخیص‌ها
//            for (int j = i + 1; j < sortedDetections.Count; j++)
//            {
//                if (suppressed[j])
//                    continue;

//                var other = sortedDetections[j];
//                var overlap = CalculateIoU(current.PlateBoundingBox!, other.PlateBoundingBox!);

//                // اگر همپوشانی زیاد است، تشخیص دوم را حذف کن
//                if (overlap > 0.5) // آستانه همپوشانی 50%
//                {
//                    suppressed[j] = true;
//                    Debug.WriteLine($"📝 تشخیص {j + 1} حذف شد (همپوشانی: {overlap:F3})");
//                }
//            }
//        }

//        Debug.WriteLine($"✅ NMS تکمیل شد: {result.Count} تشخیص باقی ماند");
//        return result;
//    }

//    /// <summary>
//    /// محاسبه IoU (Intersection over Union) بین دو مستطیل
//    /// این معیار میزان همپوشانی دو مستطیل را اندازه‌گیری می‌کند
//    /// </summary>
//    private double CalculateIoU(BoundingBox box1, BoundingBox box2)
//    {
//        // محاسبه ناحیه تقاطع
//        var x1 = Math.Max(box1.X, box2.X);
//        var y1 = Math.Max(box1.Y, box2.Y);
//        var x2 = Math.Min(box1.X + box1.Width, box2.X + box2.Width);
//        var y2 = Math.Min(box1.Y + box1.Height, box2.Y + box2.Height);

//        // اگر تقاطعی وجود ندارد
//        if (x2 <= x1 || y2 <= y1)
//            return 0;

//        // محاسبه مساحت تقاطع
//        var intersectionArea = (x2 - x1) * (y2 - y1);
        
//        // محاسبه مساحت هر مستطیل
//        var box1Area = box1.Width * box1.Height;
//        var box2Area = box2.Width * box2.Height;
        
//        // محاسبه مساحت اجتماع
//        var unionArea = box1Area + box2Area - intersectionArea;

//        // محاسبه IoU
//        return unionArea > 0 ? (double)intersectionArea / unionArea : 0;
//    }

//    public void Dispose()
//    {
//        if (_disposed) return;

//        Debug.WriteLine("🔄 پاکسازی سرویس تشخیص پلاک");
//        _session?.Dispose();
//        _disposed = true;
//        GC.SuppressFinalize(this);
//    }
//}
