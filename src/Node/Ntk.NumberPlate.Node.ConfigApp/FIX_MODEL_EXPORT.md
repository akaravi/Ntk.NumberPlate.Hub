# 🔧 راهنمای رفع مشکل Export مدل

## ❌ مشکل فعلی:

```
[ShapeInferenceError] Mismatch between the sum of 'split' (84) and the split dimension of the input (5)
```

این خطا به این معنی است که مدل ONNX شما شامل **لایه‌های پس‌پردازش ناسازگار** است.

---

## ✅ راه‌حل: Export مجدد مدل

### روش 1: استفاده از اسکریپت آماده (توصیه می‌شود)

```bash
# نصب Ultralytics (اگر نصب نیست)
pip install ultralytics

# Export مدل
python export_model_simple.py D:\model.pt -o D:\model_fixed.onnx
```

### روش 2: Export دستی با Python

```python
from ultralytics import YOLO

# بارگذاری مدل
model = YOLO('D:\\model.pt')

# Export با تنظیمات صحیح
model.export(
    format='onnx',
    imgsz=640,
    simplify=True,      # مهم: ساده‌سازی گراف
    dynamic=False,      # مهم: بدون dynamic shapes
    opset=12,           # نسخه سازگار
)
```

این کد یک فایل `model.onnx` در همان پوشه ایجاد می‌کند.

### روش 3: استفاده از ONNX Simplifier

اگر مدل Export شده است اما همچنان مشکل دارد:

```bash
# نصب ابزار
pip install onnx onnx-simplifier

# ساده‌سازی مدل
python -m onnxsim D:\model.onnx D:\model_simplified.onnx
```

---

## 🎯 تنظیمات Export صحیح

### برای مدل با یک کلاس (فقط پلاک):

```python
from ultralytics import YOLO

model = YOLO('license_plate_model.pt')

# بررسی مدل
print(f"تعداد کلاس‌ها: {len(model.names)}")
print(f"نام کلاس‌ها: {model.names}")

# Export
success = model.export(
    format='onnx',
    imgsz=640,           # اندازه ورودی
    simplify=True,       # ساده‌سازی گراف ONNX
    dynamic=False,       # Shape ثابت
    opset=12,            # ONNX Opset version
    half=False,          # Float32 (نه Float16)
)

print(f"Export موفق: {success}")
```

### چک کردن مدل Export شده:

```python
import onnx

# بارگذاری مدل
model = onnx.load('model.onnx')

# بررسی ورودی و خروجی
print("--- Inputs ---")
for inp in model.graph.input:
    print(f"Name: {inp.name}")
    print(f"Shape: {[d.dim_value for d in inp.type.tensor_type.shape.dim]}")

print("\n--- Outputs ---")
for out in model.graph.output:
    print(f"Name: {out.name}")
    print(f"Shape: {[d.dim_value for d in out.type.tensor_type.shape.dim]}")
```

خروجی مورد انتظار:

```
--- Inputs ---
Name: images
Shape: [1, 3, 640, 640]

--- Outputs ---
Name: output0
Shape: [1, 25200, 85]  # برای 80 کلاس
یا
Shape: [1, 25200, 6]   # برای 1 کلاس
```

---

## 🔍 تشخیص مشکلات مدل

### اسکریپت تست سریع:

```python
import onnxruntime as ort
import numpy as np

# مسیر مدل
model_path = "D:\\model.onnx"

try:
    # بارگذاری مدل
    session = ort.InferenceSession(model_path)

    print("✅ مدل بارگذاری شد!")

    # نمایش اطلاعات
    print(f"\n📊 Input:")
    for inp in session.get_inputs():
        print(f"  - Name: {inp.name}, Shape: {inp.shape}, Type: {inp.type}")

    print(f"\n📊 Output:")
    for out in session.get_outputs():
        print(f"  - Name: {out.name}, Shape: {out.shape}, Type: {out.type}")

    # تست اجرا
    print(f"\n🧪 تست اجرای مدل...")
    dummy_input = np.random.randn(1, 3, 640, 640).astype(np.float32)
    output = session.run(None, {session.get_inputs()[0].name: dummy_input})

    print(f"✅ مدل اجرا شد!")
    print(f"📊 Output shape: {output[0].shape}")

except Exception as e:
    print(f"❌ خطا: {e}")
```

---

## 🚀 مراحل عملی رفع مشکل

### مرحله 1: تهیه محیط Python

```bash
# ایجاد محیط مجازی
python -m venv venv

# فعال‌سازی
# Windows:
venv\Scripts\activate
# Linux/Mac:
source venv/bin/activate

# نصب پکیج‌ها
pip install ultralytics onnx onnxruntime
```

### مرحله 2: Export مدل

```bash
# اجرای اسکریپت
python export_model_simple.py D:\model.pt -o D:\model_fixed.onnx
```

### مرحله 3: تست مدل جدید

```bash
# تست با اسکریپت بالا
python test_model.py D:\model_fixed.onnx
```

### مرحله 4: استفاده در برنامه

1. در ConfigApp مسیر مدل جدید را وارد کنید: `D:\model_fixed.onnx`
2. تنظیمات را ذخیره کنید
3. فرم تست را باز کنید
4. باید پیام "✓ مدل بارگذاری شد" را ببینید

---

## 🆘 اگر مشکل ادامه داشت

### گزینه 1: استفاده از مدل YOLOv8 پیش‌آموزش‌دیده (برای تست)

```python
from ultralytics import YOLO

# دانلود و export مدل YOLOv8n
model = YOLO('yolov8n.pt')
model.export(format='onnx', simplify=True)
```

این یک مدل عمومی است که حتماً کار می‌کند (اما برای پلاک ایرانی بهینه نیست).

### گزینه 2: استفاده از فرمت دیگر

اگر ONNX کار نکرد، می‌توانید از فرمت‌های دیگر استفاده کنید:

```python
# Export به TorchScript
model.export(format='torchscript')

# یا Export به TensorFlow
model.export(format='tensorflow')
```

(نیاز به تغییرات در کد C# دارد)

### گزینه 3: درخواست مدل از منبع اصلی

اگر مدل را از جایی دانلود کرده‌اید، از صاحب مدل بخواهید که:

- مدل را با تنظیمات صحیح Export کند
- یا فایل `.pt` را به شما بدهد تا خودتان Export کنید

---

## 📞 پشتیبانی

اگر با این روش‌ها مشکل حل نشد:

1. فایل `.pt` (نه `.onnx`) را تهیه کنید
2. با استفاده از اسکریپت بالا خودتان Export کنید
3. لاگ خطای دقیق را برای من بفرستید

---

## ✅ چک‌لیست نهایی

پس از Export مدل جدید:

- [ ] فایل `.onnx` حجم معقولی دارد (5MB - 500MB)
- [ ] اسکریپت تست Python خطا نمی‌دهد
- [ ] مدل در برنامه C# بارگذاری می‌شود
- [ ] آستانه اعتماد مناسب تنظیم شده (0.25 - 0.5)
- [ ] تصاویر تست با کیفیت خوب هستند

موفق باشید! 🎉
