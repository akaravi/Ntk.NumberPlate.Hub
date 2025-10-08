# سرویس OCR پلاک خودرو

این سرویس برای تشخیص نوشته‌های داخل پلاک خودرو با پشتیبانی از سه روش مختلف طراحی شده است.

## 📐 معماری

سیستم OCR بر اساس معماری **Interface-Based** طراحی شده که امکان توسعه و جایگزینی آسان موتورهای مختلف را فراهم می‌کند.

```
Services/
├── IOcrEngine.cs              # ← Interface مشترک
├── PlateOcrService.cs         # ← سرویس اصلی (مدیریت موتورها)
└── Ocr/
    ├── SimpleOcrEngine.cs     # ← روش ساده
    ├── YoloOcrEngine.cs       # ← روش YOLO
    ├── IronOcrEngine.cs       # ← روش IronOCR
    └── README.md              # ← مستندات معماری
```

**جریان کار:**

```
PlateOcrService → انتخاب موتور بر اساس تنظیمات
                  ↓
              IOcrEngine (Interface)
                  ↓
    ┌─────────────┼─────────────┐
    ↓             ↓             ↓
SimpleOcr      YoloOcr      IronOcr
```

## 📋 روش‌های پشتیبانی شده

### 1. روش ساده (Simple OCR)

- استفاده از الگوریتم‌های پایه‌ای پردازش تصویر
- بدون نیاز به مدل یادگیری عمیق
- سریع و سبک
- مناسب برای محیط‌های با منابع محدود

### 2. روش YOLO

- استفاده از مدل یادگیری عمیق YOLO
- دقت بالاتر در تشخیص حروف و اعداد
- نیاز به فایل مدل ONNX
- پیشنهاد می‌شود برای محیط‌های تولیدی

### 3. روش IronOCR

- استفاده از کتابخانه IronOCR
- پشتیبانی از زبان‌های مختلف
- دقت بالا برای متون فارسی/انگلیسی

## 🔧 پیکربندی

### تنظیمات در `node-config.json`

```json
{
  "OcrMethod": 0, // 0: Simple, 1: Yolo, 2: IronOcr
  "YoloOcrModelPath": "models/plate-ocr.onnx",
  "OcrConfidenceThreshold": 0.5
}
```

## 💻 نحوه استفاده

```csharp
// ایجاد سرویس
var config = new NodeConfiguration
{
    OcrMethod = OcrMethod.Yolo,
    YoloOcrModelPath = "models/plate-ocr.onnx",
    OcrConfidenceThreshold = 0.5f
};

using var ocrService = new PlateOcrService(config);

// تشخیص از Bitmap
var result = ocrService.RecognizePlate(plateImage);

if (result.IsSuccessful)
{
    Console.WriteLine($"متن پلاک: {result.Text}");
    Console.WriteLine($"اعتماد: {result.Confidence:P0}");
    Console.WriteLine($"زمان: {result.ProcessingTimeMs}ms");
}
else
{
    Console.WriteLine($"خطا: {result.ErrorMessage}");
}
```

## 🎯 آموزش مدل YOLO برای OCR

برای استفاده از روش YOLO، باید یک مدل YOLO برای تشخیص حروف و اعداد آموزش دهید:

### 1. آماده‌سازی دیتاست

```
dataset/
├── train/
│   ├── images/
│   │   ├── img1.jpg
│   │   ├── img2.jpg
│   │   └── ...
│   └── labels/
│       ├── img1.txt  # فرمت YOLO: class x y w h
│       ├── img2.txt
│       └── ...
└── val/
    ├── images/
    └── labels/
```

### 2. فایل تنظیمات `plate-ocr.yaml`

```yaml
# مسیرها
path: ./dataset
train: train/images
val: val/images

# کلاس‌ها (حروف و اعداد پلاک ایرانی)
names:
  0: "0"
  1: "1"
  2: "2"
  3: "3"
  4: "4"
  5: "5"
  6: "6"
  7: "7"
  8: "8"
  9: "9"
  10: "الف"
  11: "ب"
  12: "پ"
  13: "ت"
  14: "ث"
  15: "ج"
  16: "د"
  17: "ز"
  18: "س"
  19: "ش"
  20: "ص"
  21: "ط"
  22: "ع"
  23: "ف"
  24: "ق"
  25: "ک"
  26: "گ"
  27: "ل"
  28: "م"
  29: "ن"
  30: "و"
  31: "ه"
  32: "ی"
  33: "ایران"
```

### 3. آموزش مدل

```python
from ultralytics import YOLO

# بارگذاری مدل پایه
model = YOLO('yolo11n.pt')

# آموزش
results = model.train(
    data='plate-ocr.yaml',
    epochs=100,
    imgsz=640,
    batch=16,
    name='plate-ocr',
    patience=50,
    save=True,
    device=0  # GPU
)

# Export به ONNX
model.export(
    format='onnx',
    imgsz=640,
    simplify=True,
    dynamic=False,
    opset=12
)
```

### 4. استفاده از مدل

پس از آموزش، فایل `plate-ocr.onnx` را در پوشه `models` قرار دهید و مسیر آن را در تنظیمات مشخص کنید.

## 📦 نصب پکیج‌های مورد نیاز

### برای روش YOLO

```bash
# پکیج ONNX Runtime در csproj وجود دارد
```

### برای روش IronOCR

```bash
dotnet add package IronOcr
```

## 🔍 پیاده‌سازی کامل روش ساده

اگر می‌خواهید روش ساده را بهبود دهید، می‌توانید از تکنیک‌های زیر استفاده کنید:

1. **Template Matching**: تطبیق الگوی حروف با تمپلیت‌های از پیش تعریف شده
2. **Contour Analysis**: تحلیل کانتورها برای شناسایی شکل حروف
3. **Feature Extraction**: استخراج ویژگی‌های حروف (HOG, SIFT, etc.)
4. **Neural Networks**: استفاده از شبکه‌های عصبی ساده برای تشخیص حروف

## 🎨 مثال استفاده در MainForm

```csharp
// در MainForm
private PlateOcrService? _ocrService;

// در InitializeComponents یا LoadConfiguration
_ocrService = new PlateOcrService(_config);

// در هنگام پردازش تصویر پلاک
var ocrResult = _ocrService.RecognizePlate(plateImage);
txtPlateText.Text = ocrResult.Text;
lblConfidence.Text = $"اعتماد: {ocrResult.Confidence:P0}";
```

## 🐛 عیب‌یابی

### مشکل: موتور YOLO مقداردهی نمی‌شود

- مطمئن شوید فایل مدل در مسیر صحیح قرار دارد
- مسیر را به صورت مطلق یا نسبی صحیح مشخص کنید
- اجازه خواندن فایل را بررسی کنید

### مشکل: IronOCR کار نمی‌کند

- پکیج IronOcr را نصب کنید
- لایسنس IronOCR را در صورت نیاز فعال کنید

### مشکل: نتایج OCR دقیق نیست

- کیفیت تصویر ورودی را بهبود دهید
- از PlateCorrectionService برای اصلاح و چرخش پلاک استفاده کنید
- آستانه اعتماد را تنظیم کنید

## 📊 مقایسه روش‌ها

| روش     | دقت   | سرعت  | منابع مورد نیاز | پیچیدگی |
| ------- | ----- | ----- | --------------- | ------- |
| Simple  | متوسط | سریع  | کم              | کم      |
| YOLO    | بالا  | متوسط | متوسط           | متوسط   |
| IronOCR | بالا  | کند   | زیاد            | کم      |

## 🔮 بهبودهای آینده

- [ ] پیاده‌سازی کامل روش ساده با Template Matching
- [ ] پشتیبانی از مدل‌های مختلف YOLO (v8, v11, etc.)
- [ ] پشتیبانی از Tesseract OCR
- [ ] تشخیص خودکار بهترین روش بر اساس کیفیت تصویر
- [ ] پردازش Batch برای چندین پلاک همزمان
- [ ] کش کردن نتایج برای عملکرد بهتر

## 📚 منابع

- [Ultralytics YOLO](https://docs.ultralytics.com/)
- [IronOCR Documentation](https://ironsoftware.com/csharp/ocr/)
- [OpenCV Documentation](https://docs.opencv.org/)
