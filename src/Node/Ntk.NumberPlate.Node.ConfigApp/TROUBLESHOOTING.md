# 🔧 راهنمای عیب‌یابی - Troubleshooting

## ❌ خطا: "مدل بارگذاری نشده است"

اگر هنگام باز کردن فرم تست این خطا را دریافت می‌کنید، مراحل زیر را دنبال کنید:

### ✅ چک‌لیست بررسی

#### 1️⃣ بررسی وجود فایل

**مشکل:** فایل مدل در مسیر مشخص شده وجود ندارد

**راه حل:**

```
- مطمئن شوید فایل .onnx واقعاً در مسیر مشخص شده موجود است
- مسیر کامل و صحیح را کپی کنید (کلیک راست روی فایل > Copy as path)
- از مسیر مطلق استفاده کنید: C:\Models\plate_model.onnx
```

**مثال صحیح:**

```
✅ C:\AI\Models\license_plate_yolo.onnx
✅ D:\Projects\Models\best.onnx
❌ Models\best.onnx (مسیر نسبی ممکن است مشکل داشته باشد)
```

#### 2️⃣ بررسی فرمت فایل

**مشکل:** فایل به فرمت ONNX نیست

**راه حل:**

```
فقط فایل‌های با پسوند .onnx پشتیبانی می‌شوند

✅ model.onnx
❌ model.pt (فرمت PyTorch)
❌ model.h5 (فرمت Keras)
❌ model.pb (فرمت TensorFlow)
```

**تبدیل فرمت‌ها:**

**از PyTorch (.pt) به ONNX:**

```bash
pip install ultralytics
yolo export model=your_model.pt format=onnx
```

**از TensorFlow به ONNX:**

```bash
pip install tf2onnx
python -m tf2onnx.convert --saved-model model_dir --output model.onnx
```

#### 3️⃣ بررسی سلامت فایل

**مشکل:** فایل خراب یا ناقص دانلود شده

**راه حل:**

```powershell
# بررسی حجم فایل در Windows PowerShell
Get-Item "C:\path\to\model.onnx" | Select-Object Name, Length

# حجم نرمال: معمولاً 5MB تا 500MB
# اگر حجم کمتر از 1KB است، فایل خراب است
```

**اقدامات:**

- فایل را دوباره دانلود کنید
- از دانلود کامل شدن فایل اطمینان حاصل کنید
- فایل ZIP/RAR را کامل Extract کنید

#### 4️⃣ بررسی دسترسی‌ها (Permissions)

**مشکل:** برنامه اجازه خواندن فایل را ندارد

**راه حل:**

```
1. کلیک راست روی فایل model.onnx
2. Properties > Security
3. مطمئن شوید کاربر فعلی یا Everyone دسترسی Read دارد
4. اگر ندارد، Edit > Add > Everyone > Read ✓
```

**یا:**

- فایل را به مسیر دیگری (مثلاً Desktop) کپی کنید
- برنامه را "Run as Administrator" اجرا کنید

#### 5️⃣ بررسی سازگاری مدل

**مشکل:** مدل با نسخه ONNX Runtime سازگار نیست

**علائم:**

```
- خطا: "Model format not supported"
- خطا: "Invalid model file"
- خطا: "Unsupported opset version"
```

**راه حل:**

**چک کردن نسخه ONNX Runtime:**

```csharp
// در کد C#
Console.WriteLine(Microsoft.ML.OnnxRuntime.OrtEnv.Instance.GetVersionString());
```

**تبدیل مدل با Opset سازگار:**

```python
import onnx
from onnx import version_converter

model = onnx.load("model.onnx")
converted_model = version_converter.convert_version(model, 12)  # Opset 12
onnx.save(converted_model, "model_v12.onnx")
```

#### 6️⃣ بررسی وابستگی‌ها

**مشکل:** کتابخانه‌های لازم نصب نشده‌اند

**راه حل:**

```bash
# بازسازی پروژه
cd src/Node/Ntk.NumberPlate.Node.ConfigApp
dotnet restore
dotnet build

# بررسی نصب بودن پکیج‌ها
dotnet list package
```

**پکیج‌های مورد نیاز:**

- Microsoft.ML.OnnxRuntime (v1.17.0)
- OpenCvSharp4 (v4.9.0)
- OpenCvSharp4.runtime.win
- OpenCvSharp4.Extensions

---

## 🐛 خطاهای رایج دیگر

### خطا: "Input name 'images' not found"

**دلیل:** نام Input لایه مدل متفاوت است

**راه حل:**

```csharp
// بررسی نام Input مدل
var inputNames = _session.InputMetadata.Keys;
// از نام صحیح استفاده کنید

// در PlateDetectionTestService.cs خط 124:
// تغییر "images" به نام صحیح مثلاً "input" یا "data"
NamedOnnxValue.CreateFromTensor("images", inputTensor)  // ❌
NamedOnnxValue.CreateFromTensor("input", inputTensor)   // ✅
```

### خطا: "Tensor shape mismatch"

**دلیل:** اندازه Input با مدل سازگار نیست

**راه حل:**

```csharp
// بررسی اندازه مورد انتظار مدل
var inputMetadata = _session.InputMetadata.First().Value;
// تطبیق اندازه در PreprocessImage
```

### خطا: "Out of Memory"

**دلیل:** مدل خیلی بزرگ است یا حافظه کافی نیست

**راه حل:**

```
- مدل کوچک‌تر استفاده کنید (مثلاً YOLOv8n به جای YOLOv8x)
- تصاویر کوچک‌تر آپلود کنید
- برنامه‌های دیگر را ببندید
- از GPU استفاده کنید (نیاز به تنظیمات اضافی)
```

---

## 📋 تست سریع

برای مطمئن شدن از صحت مدل، این اسکریپت Python را اجرا کنید:

```python
import onnxruntime as ort
import numpy as np

# مسیر مدل خود را وارد کنید
model_path = "C:/path/to/your/model.onnx"

try:
    # بارگذاری مدل
    session = ort.InferenceSession(model_path)

    print("✅ مدل با موفقیت بارگذاری شد!")
    print(f"نام Input: {session.get_inputs()[0].name}")
    print(f"شکل Input: {session.get_inputs()[0].shape}")
    print(f"نام Output: {session.get_outputs()[0].name}")
    print(f"شکل Output: {session.get_outputs()[0].shape}")

    # تست اجرای مدل
    input_shape = session.get_inputs()[0].shape
    dummy_input = np.random.randn(1, 3, 640, 640).astype(np.float32)
    output = session.run(None, {session.get_inputs()[0].name: dummy_input})

    print("✅ مدل با موفقیت اجرا شد!")
    print(f"شکل Output: {output[0].shape}")

except Exception as e:
    print(f"❌ خطا: {e}")
```

---

## 💡 نکات مهم

### ذخیره مسیر در تنظیمات

1. مسیر کامل فایل را کپی کنید
2. در ConfigApp در فیلد "مسیر مدل YOLO" paste کنید
3. دکمه "ذخیره تنظیمات" را بزنید
4. سپس "تست تشخیص پلاک" را باز کنید

### استفاده از مدل نمونه برای تست

اگر فقط می‌خواهید برنامه را تست کنید:

```bash
# دانلود مدل نمونه YOLOv8
pip install ultralytics
python -c "from ultralytics import YOLO; YOLO('yolov8n.pt').export(format='onnx')"

# فایل yolov8n.onnx ایجاد می‌شود
```

⚠️ **توجه:** این مدل برای پلاک ایرانی آموزش ندیده و فقط برای تست برنامه مناسب است.

---

## 🆘 کمک بیشتر

اگر مشکل حل نشد:

1. **لاگ خطا را بررسی کنید:**

   - خطای دقیق را یادداشت کنید
   - اسکرین‌شات از پیام خطا بگیرید

2. **اطلاعات سیستم:**

   - نسخه Windows
   - نسخه .NET SDK
   - حجم RAM
   - مدل GPU (در صورت وجود)

3. **اطلاعات مدل:**

   - حجم فایل
   - منبع دریافت مدل
   - نسخه YOLO (v5, v8, etc.)

4. **ایجاد Issue در GitHub:**
   - با اطلاعات بالا یک Issue ایجاد کنید
   - لاگ خطای کامل را attach کنید

---

## ✅ چک‌لیست نهایی

قبل از باز کردن Issue، این موارد را بررسی کنید:

- [ ] فایل .onnx در مسیر مشخص شده موجود است
- [ ] پسوند فایل دقیقاً .onnx است (نه .pt یا چیز دیگر)
- [ ] حجم فایل بیشتر از 1MB است
- [ ] مسیر به صورت کامل (Absolute path) وارد شده: `C:\...`
- [ ] دسترسی Read روی فایل وجود دارد
- [ ] پروژه با `dotnet build` بدون خطا build می‌شود
- [ ] تمام پکیج‌های NuGet نصب شده‌اند (`dotnet restore`)
- [ ] تنظیمات ذخیره شده است (دکمه "ذخیره تنظیمات")

اگر همه موارد بالا ✅ است اما مشکل ادامه دارد، لطفاً با ما تماس بگیرید.
