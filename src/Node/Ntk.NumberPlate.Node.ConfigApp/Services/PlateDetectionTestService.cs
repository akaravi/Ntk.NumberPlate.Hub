using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Ntk.NumberPlate.Shared.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace Ntk.NumberPlate.Node.ConfigApp.Services;

/// <summary>
/// سرویس تست تشخیص پلاک برای ConfigApp
/// </summary>
public class PlateDetectionTestService : IDisposable
{
    private InferenceSession? _session;
    private string _inputName = "images";
    private int _inputWidth = 640;
    private int _inputHeight = 640;
    private readonly string _modelPath;
    private readonly float _confidenceThreshold;

    public PlateDetectionTestService(string modelPath, float confidenceThreshold)
    {
        _modelPath = modelPath;
        _confidenceThreshold = confidenceThreshold;
    }

    public bool Initialize(out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(_modelPath))
            {
                errorMessage = "مسیر مدل خالی است";
                return false;
            }

            if (!File.Exists(_modelPath))
            {
                errorMessage = $"فایل مدل در مسیر زیر یافت نشد:\n{_modelPath}";
                return false;
            }

            // بررسی پسوند فایل
            var extension = Path.GetExtension(_modelPath).ToLower();
            if (extension != ".onnx")
            {
                errorMessage = $"فرمت فایل باید ONNX باشد (.onnx)\nفرمت فعلی: {extension}";
                return false;
            }

            // بررسی حجم فایل
            var fileInfo = new FileInfo(_modelPath);
            if (fileInfo.Length < 1024) // کمتر از 1KB
            {
                errorMessage = $"فایل مدل خراب یا ناقص است (حجم: {fileInfo.Length} بایت)";
                return false;
            }

            // بارگذاری مدل با پشتیبانی از Extensions
            var sessionOptions = new SessionOptions();

            try
            {
                // فعال‌سازی Custom Ops برای پشتیبانی از عملیات اضافی
                sessionOptions.RegisterOrtExtensions();
            }
            catch
            {
                // اگر Extensions در دسترس نیست، از حالت عادی استفاده می‌کنیم
            }

            _session = new InferenceSession(_modelPath, sessionOptions);

            // کشف نام و ابعاد ورودی از متادیتای مدل
            var inputMeta = _session.InputMetadata.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(inputMeta.Key))
            {
                _inputName = inputMeta.Key;
                var dims = inputMeta.Value.Dimensions;
                if (dims != null && dims.Length == 4)
                {
                    if (dims[2] > 0) _inputHeight = dims[2];
                    if (dims[3] > 0) _inputWidth = dims[3];
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"خطا در بارگذاری مدل:\n{ex.Message}\n\nجزئیات:\n{ex.InnerException?.Message}";
            return false;
        }
    }

    public async Task<List<VehicleDetectionData>> DetectPlatesAsync(Mat frame)
    {
        var detections = new List<VehicleDetectionData>();

        try
        {
            if (_session == null)
                return detections;

            // پیش‌پردازش تصویر
            var (inputTensor, ratio, padW, padH) = PreprocessImage(frame);

            // اجرای مدل
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
            };

            using var results = _session.Run(inputs);
            var outputTensor = results.First().AsTensor<float>();
            var dims = outputTensor.Dimensions.ToArray();
            var output = outputTensor.ToArray();

            // پس‌پردازش نتایج
            detections = PostProcessResults(output, dims, frame.Width, frame.Height, ratio, padW, padH);

            // فیلتر بر اساس آستانه اعتماد
            detections = detections.Where(d => d.Confidence >= _confidenceThreshold).ToList();

            // حذف تشخیص‌های تکراری با استفاده از NMS (Non-Maximum Suppression)
            detections = ApplyNonMaximumSuppression(detections);
        }
        catch (Exception)
        {
            // خطا در تشخیص
        }

        await Task.CompletedTask;
        return detections;
    }

    public async Task<(List<VehicleDetectionData> Detections, Bitmap AnnotatedImage)> DetectAndAnnotateAsync(string imagePath)
    {
        var detections = new List<VehicleDetectionData>();
        Bitmap? annotatedImage = null;

        try
        {
            // بارگذاری تصویر
            using var frame = Cv2.ImRead(imagePath);
            if (frame.Empty())
            {
                return (detections, new Bitmap(1, 1));
            }

            // تشخیص پلاک‌ها
            detections = await DetectPlatesAsync(frame);

            // رسم Bounding Box‌ها
            var annotated = frame.Clone();
            foreach (var detection in detections)
            {
                if (detection.PlateBoundingBox != null)
                {
                    var bbox = detection.PlateBoundingBox;

                    // رسم مستطیل
                    Cv2.Rectangle(annotated,
                        new OpenCvSharp.Point(bbox.X, bbox.Y),
                        new OpenCvSharp.Point(bbox.X + bbox.Width, bbox.Y + bbox.Height),
                        new Scalar(0, 255, 0), 2);

                    // نوشتن شماره پلاک و اعتماد
                    var text = $"{detection.PlateNumber} ({detection.Confidence:P0})";
                    Cv2.PutText(annotated, text,
                        new OpenCvSharp.Point(bbox.X, bbox.Y - 10),
                        HersheyFonts.HersheySimplex, 0.6,
                        new Scalar(0, 255, 0), 2);
                }
            }

            // تبدیل به Bitmap
            annotatedImage = BitmapConverter.ToBitmap(annotated);
        }
        catch (Exception)
        {
            annotatedImage = new Bitmap(1, 1);
        }

        return (detections, annotatedImage ?? new Bitmap(1, 1));
    }

    private (DenseTensor<float> Tensor, float ResizeRatio, int PadW, int PadH) PreprocessImage(Mat frame)
    {
        int targetW = _inputWidth;
        int targetH = _inputHeight;
        float r = Math.Min((float)targetW / frame.Width, (float)targetH / frame.Height);
        int newW = (int)Math.Round(frame.Width * r);
        int newH = (int)Math.Round(frame.Height * r);

        var resized = new Mat();
        Cv2.Resize(frame, resized, new OpenCvSharp.Size(newW, newH));

        int padW = (targetW - newW) / 2;
        int padH = (targetH - newH) / 2;

        var padded = new Mat(new OpenCvSharp.Size(targetW, targetH), MatType.CV_8UC3, new Scalar(114, 114, 114));
        resized.CopyTo(new Mat(padded, new Rect(padW, padH, newW, newH)));

        // تبدیل به RGB
        Cv2.CvtColor(padded, padded, ColorConversionCodes.BGR2RGB);

        var tensor = new DenseTensor<float>(new[] { 1, 3, targetH, targetW });
        for (int y = 0; y < targetH; y++)
        {
            for (int x = 0; x < targetW; x++)
            {
                var pixel = padded.At<Vec3b>(y, x);
                tensor[0, 0, y, x] = pixel.Item2 / 255f; // R
                tensor[0, 1, y, x] = pixel.Item1 / 255f; // G
                tensor[0, 2, y, x] = pixel.Item0 / 255f; // B
            }
        }

        return (tensor, r, padW, padH);
    }

    private List<VehicleDetectionData> PostProcessResults(float[] output, int[] outputDims, int originalWidth, int originalHeight, float resizeRatio, int padW, int padH)
    {
        var detections = new List<VehicleDetectionData>();

        try
        {
            var dims = outputDims;
            int numAnchors, numAttrs; // attrs = 4 + 1 + C
            bool anchorsLast;

            if (dims.Length == 3)
            {
                if (dims[1] < dims[2])
                {
                    numAttrs = dims[1];
                    numAnchors = dims[2];
                    anchorsLast = true; // (1, attrs, anchors)
                }
                else
                {
                    numAnchors = dims[1];
                    numAttrs = dims[2];
                    anchorsLast = false; // (1, anchors, attrs)
                }
            }
            else
            {
                int flat = output.Length;
                numAttrs = 85;
                numAnchors = flat / numAttrs;
                anchorsLast = false;
            }

            int numClasses = Math.Max(0, numAttrs - 5);

            for (int a = 0; a < numAnchors; a++)
            {
                float cx, cy, w, h, obj;
                if (anchorsLast)
                {
                    // Layout: (1, attrs, anchors) => index = attrIdx * numAnchors + anchorIdx
                    cx = output[0 * numAnchors + a];
                    cy = output[1 * numAnchors + a];
                    w = output[2 * numAnchors + a];
                    h = output[3 * numAnchors + a];
                    obj = output[4 * numAnchors + a];
                }
                else
                {
                    int baseIdx = a * numAttrs;
                    cx = output[baseIdx + 0];
                    cy = output[baseIdx + 1];
                    w = output[baseIdx + 2];
                    h = output[baseIdx + 3];
                    obj = output[baseIdx + 4];
                }

                float score = obj;
                if (numClasses > 0)
                {
                    float maxCls = 0f;
                    for (int c = 0; c < numClasses; c++)
                    {
                        float clsProb = anchorsLast ? output[(5 + c) * numAnchors + a] : output[a * numAttrs + 5 + c];
                        if (clsProb > maxCls) maxCls = clsProb;
                    }
                    score *= maxCls;
                }

                if (score < _confidenceThreshold) continue;

                // اگر مدل مختصات نرمال داده باشد (0..1)، به پیکسل تبدیل کن
                bool normalized = (w <= 1.5f && h <= 1.5f && cx <= 1.5f && cy <= 1.5f);
                if (normalized)
                {
                    cx *= _inputWidth;
                    cy *= _inputHeight;
                    w *= _inputWidth;
                    h *= _inputHeight;
                }

                // تبدیل به گوشه‌ها در فضای ورودی مدل (letterboxed)
                float x1 = cx - w / 2f;
                float y1 = cy - h / 2f;
                float x2 = cx + w / 2f;
                float y2 = cy + h / 2f;

                // حذف padding letterbox
                x1 -= padW; x2 -= padW;
                y1 -= padH; y2 -= padH;

                // بازنگاشت به اندازه تصویر اصلی با نسبت تغییر اندازه
                x1 /= resizeRatio; x2 /= resizeRatio;
                y1 /= resizeRatio; y2 /= resizeRatio;

                int ix1 = (int)Math.Round(Math.Clamp(x1, 0, originalWidth - 1));
                int iy1 = (int)Math.Round(Math.Clamp(y1, 0, originalHeight - 1));
                int ix2 = (int)Math.Round(Math.Clamp(x2, 0, originalWidth - 1));
                int iy2 = (int)Math.Round(Math.Clamp(y2, 0, originalHeight - 1));

                int bw = Math.Max(1, ix2 - ix1);
                int bh = Math.Max(1, iy2 - iy1);

                detections.Add(new VehicleDetectionData
                {
                    PlateBoundingBox = new BoundingBox { X = ix1, Y = iy1, Width = bw, Height = bh },
                    Confidence = score,
                    PlateNumber = GenerateSamplePlateNumber(),
                    DetectionTime = DateTime.Now,
                    VehicleType = VehicleType.Car
                });
            }
        }
        catch (Exception ex)
        {
            // لاگ خطا برای دیباگ
            System.Diagnostics.Debug.WriteLine($"خطا در پس‌پردازش: {ex.Message}");
        }

        return detections;
    }

    private string GenerateSamplePlateNumber()
    {
        // برای تست، یک شماره پلاک نمونه تولید می‌کنیم
        // در پیاده‌سازی واقعی، باید OCR انجام شود
        var random = new Random();
        return $"{random.Next(10, 99)}ایران{random.Next(100, 999)}-{random.Next(10, 99)}";
    }

    /// <summary>
    /// اعمال Non-Maximum Suppression برای حذف تشخیص‌های تکراری
    /// </summary>
    private List<VehicleDetectionData> ApplyNonMaximumSuppression(List<VehicleDetectionData> detections)
    {
        if (detections.Count <= 1)
            return detections;

        // مرتب‌سازی بر اساس اعتماد (بزرگترین اول)
        var sortedDetections = detections.OrderByDescending(d => d.Confidence).ToList();
        var result = new List<VehicleDetectionData>();
        var suppressed = new bool[sortedDetections.Count];

        for (int i = 0; i < sortedDetections.Count; i++)
        {
            if (suppressed[i])
                continue;

            var current = sortedDetections[i];
            result.Add(current);

            // بررسی همپوشانی با سایر تشخیص‌ها
            for (int j = i + 1; j < sortedDetections.Count; j++)
            {
                if (suppressed[j])
                    continue;

                var other = sortedDetections[j];
                var overlap = CalculateIoU(current.PlateBoundingBox!, other.PlateBoundingBox!);

                // اگر همپوشانی زیاد است، تشخیص دوم را حذف کن
                if (overlap > 0.5) // آستانه همپوشانی
                {
                    suppressed[j] = true;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// محاسبه IoU (Intersection over Union) بین دو مستطیل
    /// </summary>
    private double CalculateIoU(BoundingBox box1, BoundingBox box2)
    {
        // محاسبه ناحیه تقاطع
        var x1 = Math.Max(box1.X, box2.X);
        var y1 = Math.Max(box1.Y, box2.Y);
        var x2 = Math.Min(box1.X + box1.Width, box2.X + box2.Width);
        var y2 = Math.Min(box1.Y + box1.Height, box2.Y + box2.Height);

        if (x2 <= x1 || y2 <= y1)
            return 0; // هیچ تقاطعی وجود ندارد

        var intersectionArea = (x2 - x1) * (y2 - y1);
        var box1Area = box1.Width * box1.Height;
        var box2Area = box2.Width * box2.Height;
        var unionArea = box1Area + box2Area - intersectionArea;

        return unionArea > 0 ? (double)intersectionArea / unionArea : 0;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}

