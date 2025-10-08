using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Ntk.NumberPlate.Shared.Models;
using OpenCvSharp;

namespace Ntk.NumberPlate.Node.Service.Services;

/// <summary>
/// سرویس تشخیص پلاک با استفاده از YOLO
/// </summary>
public class YoloDetectionService : IDisposable
{
    private readonly ILogger<YoloDetectionService> _logger;
    private InferenceSession? _session;
    private string _inputName = "images";
    private int _inputWidth = 640;
    private int _inputHeight = 640;
    private VideoCapture? _videoCapture;
    private NodeConfiguration? _config;
    private Mat? _currentFrame;

    public YoloDetectionService(ILogger<YoloDetectionService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(NodeConfiguration config)
    {
        _config = config;

        try
        {
            // بارگذاری مدل YOLO
            if (File.Exists(config.YoloModelPath))
            {
                var sessionOptions = new SessionOptions();
                try { sessionOptions.RegisterOrtExtensions(); } catch { }
                _session = new InferenceSession(config.YoloModelPath, sessionOptions);

                // کشف نام ورودی و اندازه ورودی از مدل
                var inputMeta = _session.InputMetadata.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(inputMeta.Key))
                {
                    _inputName = inputMeta.Key;
                }
                var dims = inputMeta.Value.Dimensions;
                if (dims != null && dims.Length == 4)
                {
                    // NCHW
                    if (dims[2] > 0) _inputHeight = dims[2];
                    if (dims[3] > 0) _inputWidth = dims[3];
                }
                _logger.LogInformation($"مدل YOLO بارگذاری شد: {config.YoloModelPath}");
            }
            else
            {
                _logger.LogWarning($"فایل مدل YOLO یافت نشد: {config.YoloModelPath}");
            }

            // راه‌اندازی دوربین
            if (int.TryParse(config.VideoSource, out int cameraIndex))
            {
                _videoCapture = new VideoCapture(cameraIndex);
            }
            else
            {
                _videoCapture = new VideoCapture(config.VideoSource);
            }

            if (_videoCapture.IsOpened())
            {
                _logger.LogInformation($"منبع ویدیو راه‌اندازی شد: {config.VideoSource}");
            }
            else
            {
                _logger.LogError("خطا در راه‌اندازی منبع ویدیو");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در راه‌اندازی سرویس تشخیص");
            throw;
        }
    }

    public async Task<VehicleDetectionData?> ProcessNextFrameAsync()
    {
        if (_videoCapture == null || !_videoCapture.IsOpened() || _config == null)
            return null;

        try
        {
            _currentFrame = new Mat();
            if (!_videoCapture.Read(_currentFrame) || _currentFrame.Empty())
            {
                return null;
            }

            // تشخیص پلاک با YOLO
            var detections = await DetectPlatesAsync(_currentFrame);

            if (detections.Any())
            {
                var bestDetection = detections.OrderByDescending(d => d.Confidence).First();

                // ذخیره تصویر
                string imagePath = SaveImage(_currentFrame, bestDetection.Id);

                bestDetection.ImageFileName = Path.GetFileName(imagePath);
                bestDetection.NodeId = _config.NodeId;
                bestDetection.NodeName = _config.NodeName;

                return bestDetection;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در پردازش فریم");
            return null;
        }
    }

    private async Task<List<VehicleDetectionData>> DetectPlatesAsync(Mat frame)
    {
        var detections = new List<VehicleDetectionData>();

        try
        {
            if (_session == null || _config == null)
                return detections;

            // پیش‌پردازش تصویر برای YOLO
            var (inputTensor, ratio, padW, padH) = PreprocessImage(frame);

            // اجرای مدل
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
            };

            using var results = _session.Run(inputs);
            var first = results.First();
            var outputTensor = first.AsTensor<float>();

            // پس‌پردازش خروجی YOLO
            detections = PostProcessResults(outputTensor, frame.Width, frame.Height, ratio, padW, padH);

            // فیلتر بر اساس آستانه اعتماد
            detections = detections.Where(d => d.Confidence >= _config.ConfidenceThreshold).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تشخیص پلاک");
        }

        await Task.CompletedTask;
        return detections;
    }

    private (DenseTensor<float> Tensor, float ResizeRatio, int PadW, int PadH) PreprocessImage(Mat frame)
    {
        // Letterbox به اندازه ورودی مدل با حفظ نسبت تصویر
        int targetW = _inputWidth;
        int targetH = _inputHeight;

        float r = Math.Min((float)targetW / frame.Width, (float)targetH / frame.Height);
        int newW = (int)Math.Round(frame.Width * r);
        int newH = (int)Math.Round(frame.Height * r);

        var resized = new Mat();
        Cv2.Resize(frame, resized, new Size(newW, newH));

        int padW = (targetW - newW) / 2;
        int padH = (targetH - newH) / 2;

        var padded = new Mat(new Size(targetW, targetH), MatType.CV_8UC3, new Scalar(114, 114, 114));
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

    private List<VehicleDetectionData> PostProcessResults(System.Numerics.Tensors.DenseTensor<float> output, int originalWidth, int originalHeight, float resizeRatio, int padW, int padH)
    {
        var detections = new List<VehicleDetectionData>();

        // خروجی‌های رایج YOLOv8/v9/v10/v11 در ONNX:
        // (1, num_attrs, num_anchors) یا (1, num_anchors, num_attrs)
        // که num_attrs = 4 (cx,cy,w,h) + 1 (obj) + num_classes
        try
        {
            var dims = output.Dimensions.ToArray();
            int numAnchors, numAttrs; // attrs = 4 + 1 + C
            bool anchorsLast;

            if (dims.Length == 3)
            {
                // (1, A, K) یا (1, K, A)
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
                // fallback: بردار تخت
                int flat = output.Length;
                // فرض attrs 84 یا 85
                numAttrs = 85;
                numAnchors = flat / numAttrs;
                anchorsLast = false;
            }

            int numClasses = Math.Max(0, numAttrs - 5);

            var candidates = new List<(BoundingBox Box, float Score)>();

            for (int a = 0; a < numAnchors; a++)
            {
                float cx, cy, w, h, obj;
                if (anchorsLast)
                {
                    cx = output[0, 0, a];
                    cy = output[0, 1, a];
                    w = output[0, 2, a];
                    h = output[0, 3, a];
                    obj = output[0, 4, a];
                }
                else
                {
                    cx = output[0, a, 0];
                    cy = output[0, a, 1];
                    w = output[0, a, 2];
                    h = output[0, a, 3];
                    obj = output[0, a, 4];
                }

                float score = obj;
                if (numClasses > 0)
                {
                    float maxCls = 0f;
                    for (int c = 0; c < numClasses; c++)
                    {
                        float clsProb = anchorsLast ? output[0, 5 + c, a] : output[0, a, 5 + c];
                        if (clsProb > maxCls) maxCls = clsProb;
                    }
                    score *= maxCls;
                }

                if (_config != null && score < _config.ConfidenceThreshold) continue;

                // تبدیل از cx,cy,w,h نرمال‌شده در فضای letterbox به مختصات تصویر اصلی
                float x1 = cx - w / 2f;
                float y1 = cy - h / 2f;
                float x2 = cx + w / 2f;
                float y2 = cy + h / 2f;

                // به مختصات پیکسل در فضای letterboxed
                x1 = x1 * _inputWidth - padW;
                y1 = y1 * _inputHeight - padH;
                x2 = x2 * _inputWidth - padW;
                y2 = y2 * _inputHeight - padH;

                // مقیاس معکوس نسبت تغییر اندازه
                x1 = x1 / resizeRatio;
                y1 = y1 / resizeRatio;
                x2 = x2 / resizeRatio;
                y2 = y2 / resizeRatio;

                int ix1 = (int)Math.Round(Math.Clamp(x1, 0, originalWidth - 1));
                int iy1 = (int)Math.Round(Math.Clamp(y1, 0, originalHeight - 1));
                int ix2 = (int)Math.Round(Math.Clamp(x2, 0, originalWidth - 1));
                int iy2 = (int)Math.Round(Math.Clamp(y2, 0, originalHeight - 1));

                int bw = Math.Max(1, ix2 - ix1);
                int bh = Math.Max(1, iy2 - iy1);

                candidates.Add((new BoundingBox { X = ix1, Y = iy1, Width = bw, Height = bh }, score));
            }

            // NMS
            var kept = NonMaxSuppression(candidates, iouThreshold: 0.45f);
            foreach (var k in kept)
            {
                detections.Add(new VehicleDetectionData
                {
                    PlateBoundingBox = k.Box,
                    Confidence = k.Score,
                    PlateNumber = GenerateSamplePlateNumber(),
                    DetectionTime = DateTime.Now,
                    VehicleType = VehicleType.Car
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در پس‌پردازش نتایج");
        }

        return detections;
    }

    private static List<(BoundingBox Box, float Score)> NonMaxSuppression(List<(BoundingBox Box, float Score)> boxes, float iouThreshold)
    {
        var sorted = boxes.OrderByDescending(b => b.Score).ToList();
        var kept = new List<(BoundingBox, float)>();
        var removed = new bool[sorted.Count];

        for (int i = 0; i < sorted.Count; i++)
        {
            if (removed[i]) continue;
            var a = sorted[i];
            kept.Add(a);
            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (removed[j]) continue;
                if (ComputeIoU(a.Box, sorted[j].Box) > iouThreshold)
                {
                    removed[j] = true;
                }
            }
        }
        return kept;
    }

    private static float ComputeIoU(BoundingBox a, BoundingBox b)
    {
        int x1 = Math.Max(a.X, b.X);
        int y1 = Math.Max(a.Y, b.Y);
        int x2 = Math.Min(a.X + a.Width, b.X + b.Width);
        int y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
        int interW = Math.Max(0, x2 - x1);
        int interH = Math.Max(0, y2 - y1);
        int inter = interW * interH;
        int union = a.Width * a.Height + b.Width * b.Height - inter;
        if (union <= 0) return 0f;
        return (float)inter / union;
    }

    private string GenerateSamplePlateNumber()
    {
        // TODO: اتصال به OCR واقعی در آینده
        var random = new Random();
        return $"{random.Next(10, 99)}ایران{random.Next(100, 999)}-{random.Next(10, 99)}";
    }

    private string SaveImage(Mat frame, Guid id)
    {
        try
        {
            if (_config == null || !_config.SaveImagesLocally)
                return string.Empty;

            var directory = Path.Combine(_config.LocalImagePath, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileName = $"{id}_{DateTime.Now:HHmmss}.jpg";
            var filePath = Path.Combine(directory, fileName);

            Cv2.ImWrite(filePath, frame);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ذخیره تصویر");
            return string.Empty;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
        _videoCapture?.Dispose();
        _currentFrame?.Dispose();
    }
}


