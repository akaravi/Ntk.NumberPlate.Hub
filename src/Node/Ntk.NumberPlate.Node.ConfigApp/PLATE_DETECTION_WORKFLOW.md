# فرآیند کامل تشخیص پلاک

## 🔄 **مراحل تشخیص پلاک (4 مرحله):**

### **مرحله 1: تشخیص محل پلاک‌های تصویر**

- **هدف:** پیدا کردن موقعیت پلاک‌ها در تصویر
- **روش:** استفاده از مدل YOLO برای تشخیص اشیاء
- **خروجی:** مختصات مستطیلی پلاک‌ها (BoundingBox)
- **کد:** `_detectionService.DetectPlatesAsync(frame)`

### **مرحله 2: جدا سازی پلاک‌های تصویر**

- **هدف:** برش پلاک‌ها از تصویر اصلی
- **روش:** استفاده از مختصات BoundingBox برای برش
- **خروجی:** تصاویر جداگانه هر پلاک
- **کد:** `ExtractPlateFromImage(plateBoundingBox)`

### **مرحله 3: اصلاح پلاک‌های تصویر**

- **هدف:** بهبود کیفیت و صاف کردن پلاک‌ها
- **روش:** استفاده از PlateCorrectionService
- **خروجی:** پلاک‌های اصلاح شده و صاف
- **کد:** `CorrectPlateImage(plateImage)`

### **مرحله 4: OCR پلاک اصلاح شده**

- **هدف:** تشخیص متن روی پلاک
- **روش:** استفاده از OCR engines (Simple/YOLO/IronOCR)
- **خروجی:** شماره پلاک تشخیص داده شده
- **کد:** `PerformOcrOnPlate(correctedPlate)`

## 🔧 **پیاده‌سازی:**

### **متد اصلی:**

```csharp
private async Task<string> ProcessCompletePlateDetection(PlateDetectionResult detection, int plateIndex)
{
    // مرحله 1: تشخیص محل پلاک (قبلاً انجام شده)
    // مرحله 2: جدا سازی پلاک
    var croppedPlate = ExtractPlateFromImage(detection.PlateBoundingBox);

    // مرحله 3: اصلاح پلاک
    var correctedPlate = await CorrectPlateImage(croppedPlate);

    // مرحله 4: OCR پلاک
    var plateNumber = await PerformOcrOnPlate(correctedPlate, plateIndex);

    return plateNumber;
}
```

### **مرحله 2: جدا سازی پلاک:**

```csharp
private Bitmap? ExtractPlateFromImage(Rectangle plateBoundingBox)
{
    // برش پلاک از تصویر اصلی
    using var srcBitmap = new Bitmap(_originalImage!);
    var rect = new Rectangle(plateBoundingBox.X, plateBoundingBox.Y,
                           plateBoundingBox.Width, plateBoundingBox.Height);

    // بررسی محدوده و برش
    rect.Intersect(new Rectangle(0, 0, srcBitmap.Width, srcBitmap.Height));
    var plateBitmap = new Bitmap(rect.Width, rect.Height);

    using (var g = Graphics.FromImage(plateBitmap))
    {
        g.DrawImage(srcBitmap, new Rectangle(0, 0, rect.Width, rect.Height),
                   rect, GraphicsUnit.Pixel);
    }

    return plateBitmap;
}
```

### **مرحله 3: اصلاح پلاک:**

```csharp
private async Task<Bitmap?> CorrectPlateImage(Bitmap plateImage)
{
    // استفاده از PlateCorrectionService
    using var correctionService = new PlateCorrectionService();
    var correctedImage = await Task.Run(() => correctionService.CorrectPlate(plateImage));

    return correctedImage ?? plateImage; // fallback به تصویر اصلی
}
```

### **مرحله 4: OCR پلاک:**

```csharp
private async Task<string> PerformOcrOnPlate(Bitmap correctedPlate, int plateIndex)
{
    if (_ocrService == null) return "نامشخص";

    var ocrResult = await Task.Run(() => _ocrService.RecognizePlate(correctedPlate));

    if (ocrResult.IsSuccessful && !string.IsNullOrEmpty(ocrResult.Text))
    {
        return ocrResult.Text;
    }

    return "نامشخص";
}
```

## 📊 **نحوه استفاده:**

### **1. تشخیص خودکار (BtnDetect_Click):**

```csharp
// برای هر پلاک تشخیص داده شده
for (int i = 0; i < _detections.Count; i++)
{
    var detection = _detections[i];
    string plateNumber = await ProcessCompletePlateDetection(detection, i);
    // نمایش نتیجه در لیست
}
```

### **2. تشخیص دستی (BtnOcr_Click):**

```csharp
// برای پلاک انتخاب شده
var targetDetection = _selectedDetection ?? _detections.OrderByDescending(d => d.Confidence).First();
var plateNumber = await ProcessCompletePlateDetection(targetDetection, 0);
// نمایش نتیجه در UI
```

## 🔍 **لاگ‌های فرآیند:**

### **مرحله 1:**

```
✅ مرحله 1: محل پلاک 1 تشخیص داده شد
```

### **مرحله 2:**

```
📏 پلاک جدا شد: 120x40
✅ مرحله 2: پلاک 1 جدا شد
```

### **مرحله 3:**

```
✅ پلاک اصلاح شد
✅ مرحله 3: پلاک 1 اصلاح شد
```

### **مرحله 4:**

```
✅ OCR موفق: '12ب34567' (اعتماد: 85%)
✅ مرحله 4: OCR پلاک 1 - نتیجه: '12ب34567'
```

## ⚠️ **مدیریت خطا:**

### **خطاهای احتمالی:**

- **محدوده نامعتبر:** `❌ محدوده پلاک نامعتبر است`
- **جدا سازی ناموفق:** `❌ پلاک 1: جدا سازی ناموفق`
- **اصلاح ناموفق:** `⚠️ اصلاح پلاک ناموفق - استفاده از تصویر اصلی`
- **OCR ناموفق:** `❌ OCR ناموفق: خطا در تشخیص`

### **Fallback Strategy:**

- اگر اصلاح ناموفق بود → استفاده از تصویر اصلی
- اگر OCR ناموفق بود → نمایش "نامشخص"
- اگر خطا رخ داد → نمایش "خطا"

## 🎯 **نتیجه:**

### **قبل از تغییرات:**

- ❌ OCR فقط روی تصویر خام انجام می‌شد
- ❌ کیفیت تشخیص پایین بود
- ❌ فرآیند یکپارچه نبود

### **بعد از تغییرات:**

- ✅ فرآیند 4 مرحله‌ای کامل
- ✅ کیفیت تشخیص بالا
- ✅ مدیریت خطا و fallback
- ✅ لاگ‌های تفصیلی
- ✅ UI responsive

## 📋 **نحوه تست:**

1. **انتخاب تصویر:** تصویر حاوی پلاک را انتخاب کنید
2. **تشخیص پلاک:** روی "تشخیص پلاک" کلیک کنید
3. **مشاهده فرآیند:** در Debug Output مراحل را ببینید
4. **تست OCR:** روی "شناسایی متن" کلیک کنید
5. **مشاهده نتایج:** شماره پلاک‌های تشخیص داده شده

حالا فرآیند تشخیص پلاک کاملاً یکپارچه و حرفه‌ای است! 🎉
