# 📦 راهنمای نصب و راه‌اندازی

## مراحل نصب کامل سیستم

### 1️⃣ نصب پیش‌نیازها

#### Windows

1. **نصب .NET 8.0 SDK**

   - دانلود از: https://dotnet.microsoft.com/download/dotnet/8.0
   - نصب و تایید نصب:

   ```cmd
   dotnet --version
   ```

2. **نصب SQL Server**

   - دانلود SQL Server Express: https://www.microsoft.com/sql-server/sql-server-downloads
   - یا استفاده از LocalDB (به همراه Visual Studio نصب می‌شود)

3. **نصب Node.js و npm**

   - دانلود از: https://nodejs.org/ (LTS version)
   - تایید نصب:

   ```cmd
   node --version
   npm --version
   ```

4. **نصب Visual Studio 2022** (اختیاری)
   - Community Edition: https://visualstudio.microsoft.com/
   - یا JetBrains Rider

### 2️⃣ آماده‌سازی دیتابیس

#### استفاده از LocalDB

```cmd
cd src\Hub\Ntk.NumberPlate.Hub.Api

# نصب ابزار EF Core (اگر نصب نشده)
dotnet tool install --global dotnet-ef

# اجرای Migration
dotnet ef database update
```

#### استفاده از SQL Server

1. Connection String را در `appsettings.json` تغییر دهید:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=NtkNumberPlateHub;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  }
}
```

2. اجرای Migration:

```cmd
dotnet ef database update
```

### 3️⃣ راه‌اندازی Hub Server

```cmd
cd src\Hub\Ntk.NumberPlate.Hub.Api

# نصب Dependencies
dotnet restore

# Build
dotnet build

# اجرا
dotnet run
```

تست API:

- Swagger UI: http://localhost:5000/swagger
- Health Check: http://localhost:5000/api/node/list

### 4️⃣ راه‌اندازی Angular Dashboard

```cmd
cd src\Hub\Ntk.NumberPlate.Hub.Dashboard

# نصب Dependencies
npm install

# اجرای Development Server
npm start
```

دسترسی به Dashboard: http://localhost:4200

برای Build Production:

```cmd
npm run build
```

فایل‌های build شده در پوشه `dist/` قرار می‌گیرند.

### 5️⃣ آماده‌سازی مدل YOLO

#### گزینه 1: استفاده از مدل از پیش آموزش دیده

1. دانلود مدل ONNX از منابع معتبر
2. قرار دادن فایل `.onnx` در پوشه `models/`

#### گزینه 2: آموزش مدل سفارشی

```python
# نصب YOLOv8
pip install ultralytics

# آموزش مدل
from ultralytics import YOLO

model = YOLO('yolov8n.pt')
results = model.train(
    data='license_plate.yaml',
    epochs=100,
    imgsz=640,
    batch=16
)

# Export به ONNX
model.export(format='onnx')
```

### 6️⃣ راه‌اندازی Node

#### Build Node Service

```cmd
cd src\Node\Ntk.NumberPlate.Node.Service
dotnet build -c Release
```

#### Build Configuration App

```cmd
cd src\Node\Ntk.NumberPlate.Node.ConfigApp
dotnet build -c Release
```

#### پیکربندی اولیه

1. اجرای Configuration App:

```cmd
cd bin\Release\net8.0-windows
Ntk.NumberPlate.Node.ConfigApp.exe
```

2. وارد کردن تنظیمات:

   - **شناسه نود**: خودکار تولید می‌شود (یا دستی وارد کنید)
   - **نام نود**: نام قابل شناسایی (مثلا: "Node-Entrance-1")
   - **آدرس Hub**: http://localhost:5000
   - **مسیر مدل YOLO**: مسیر فایل .onnx
   - **منبع ویدیو**:
     - `0` برای دوربین پیش‌فرض
     - `1`, `2`, ... برای دوربین‌های دیگر
     - `rtsp://...` برای دوربین IP
     - `path/to/video.mp4` برای فایل ویدیو

3. کلیک روی "تست اتصال" برای بررسی ارتباط با Hub

4. ذخیره تنظیمات

#### اجرای Node Service

**روش 1: اجرای مستقیم**

```cmd
cd src\Node\Ntk.NumberPlate.Node.Service
dotnet run
```

**روش 2: نصب به عنوان Windows Service**

با PowerShell به عنوان Administrator:

```powershell
# ساخت سرویس
sc.exe create "NtkNumberPlateNode" binPath="C:\Path\To\Ntk.NumberPlate.Node.Service.exe"

# شروع سرویس
sc.exe start "NtkNumberPlateNode"

# توقف سرویس
sc.exe stop "NtkNumberPlateNode"

# حذف سرویس
sc.exe delete "NtkNumberPlateNode"
```

### 7️⃣ تست سیستم

1. **بررسی Hub Server**

   - باز کردن Dashboard: http://localhost:4200
   - بررسی صفحه "نودها" - باید نود شما نمایش داده شود

2. **بررسی Node**

   - چک کردن لاگ‌های Node در پوشه `logs/`
   - مشاهده پیام‌های "نود ثبت شد" و "آماده است"

3. **تست تشخیص**
   - قرار دادن خودرو/تصویر جلوی دوربین
   - بررسی تشخیص در Dashboard

## ⚙️ پیکربندی‌های پیشرفته

### استفاده از HTTPS

1. تولید Certificate:

```cmd
dotnet dev-certs https --trust
```

2. تغییر `HubServerUrl` در Node به `https://localhost:5001`

### استفاده از دوربین IP

در Configuration App، منبع ویدیو را به این صورت تنظیم کنید:

```
rtsp://username:password@192.168.1.100:554/stream
```

### تنظیم پارامترهای YOLO

- **Confidence Threshold**: آستانه اعتماد (0.5 = 50%)

  - مقادیر بالاتر = دقت بیشتر، تشخیص کمتر
  - مقادیر پایین‌تر = تشخیص بیشتر، خطای بیشتر

- **Processing FPS**: تعداد فریم پردازش شده در ثانیه
  - برای سیستم‌های ضعیف: 1-3 FPS
  - برای سیستم‌های قوی: 5-10 FPS

### استفاده از GPU

برای استفاده از GPU با ONNX Runtime:

1. نصب CUDA Toolkit
2. نصب پکیج:

```cmd
dotnet add package Microsoft.ML.OnnxRuntime.Gpu
```

3. تغییر کد در `YoloDetectionService.cs`:

```csharp
var options = new SessionOptions();
options.AppendExecutionProvider_CUDA(0);
_session = new InferenceSession(config.YoloModelPath, options);
```

## 🔧 عیب‌یابی

### خطای اتصال به دیتابیس

```
Error: Cannot open database
```

**راه‌حل:**

- بررسی Connection String
- اطمینان از اجرای SQL Server
- اجرای `dotnet ef database update`

### خطای دسترسی به دوربین

```
Error: Cannot open video source
```

**راه‌حل:**

- بررسی شماره دوربین
- بررسی دسترسی به دوربین (آنتی‌ویروس)
- تست با فایل ویدیو به جای دوربین

### خطای بارگذاری مدل YOLO

```
Error: ONNX model not found
```

**راه‌حل:**

- بررسی مسیر فایل مدل
- اطمینان از فرمت ONNX
- تست با مدل ساده‌تر

### نود به Hub متصل نمی‌شود

**راه‌حل:**

- بررسی آدرس Hub Server
- بررسی Firewall
- تست با `curl http://localhost:5000/api/node/list`

## 📊 نظارت و بهینه‌سازی

### بررسی لاگ‌ها

```cmd
# لاگ‌های Hub
tail -f src/Hub/Ntk.NumberPlate.Hub.Api/logs/hub-*.log

# لاگ‌های Node
tail -f logs/node-service-*.log
```

### بهینه‌سازی عملکرد

1. **کاهش حجم تصاویر:**

   - کامپرس تصاویر قبل از ارسال
   - ارسال فقط ناحیه پلاک به جای تصویر کامل

2. **Batch Processing:**

   - ذخیره محلی و ارسال دسته‌ای

3. **استفاده از Queue:**
   - RabbitMQ یا Redis برای صف پیام‌ها

## 🔄 به‌روزرسانی

```cmd
# Pull آخرین تغییرات
git pull

# به‌روزرسانی Dependencies
dotnet restore
npm install

# Build مجدد
dotnet build
npm run build
```

---

## 🧪 تست تشخیص پلاک (قابلیت جدید)

برنامه ConfigApp حالا یک بخش تست دارد که به شما امکان می‌دهد قبل از راه‌اندازی سرویس، مدل YOLO و تنظیمات را آزمایش کنید.

### نحوه استفاده از بخش تست

1. **اجرای برنامه ConfigApp:**

```cmd
cd src\Node\Ntk.NumberPlate.Node.ConfigApp
dotnet run
```

2. **تنظیمات اولیه:**

   - مسیر مدل YOLO را وارد کنید (فایل `.onnx`)
   - آستانه اعتماد را تنظیم کنید (توصیه: 0.3 تا 0.7)
   - تنظیمات را ذخیره کنید

3. **باز کردن فرم تست:**

   - روی دکمه **"تست تشخیص پلاک"** (رنگ بنفش) کلیک کنید

4. **تست روی تصویر:**
   - یک تصویر حاوی پلاک خودرو بارگذاری کنید
   - روی دکمه **"شروع تشخیص"** کلیک کنید
   - نتایج را مشاهده کنید

### ویژگی‌های بخش تست

- ✅ بارگذاری تصاویر (JPG, PNG, BMP, GIF)
- ✅ تشخیص خودکار پلاک با YOLO
- ✅ نمایش Bounding Box روی تصویر
- ✅ نمایش اطلاعات دقیق هر تشخیص:
  - شماره پلاک
  - درصد اعتماد
  - مختصات و اندازه
  - زمان پردازش
- ✅ پشتیبانی از تشخیص چند پلاک در یک تصویر

### نکات تست

- مطمئن شوید مدل YOLO برای پلاک ایرانی آموزش دیده باشد
- برای نتایج بهتر، از تصاویر با کیفیت و روشنایی مناسب استفاده کنید
- اگر پلاکی تشخیص داده نمی‌شود، آستانه اعتماد را کاهش دهید

### 📥 دریافت مدل YOLO

**گزینه 1: مدل‌های آماده**

- [Roboflow Universe](https://universe.roboflow.com/) - جستجو: "iranian license plate"
- GitHub - جستجو: `iranian license plate yolo onnx`
- Hugging Face - مدل‌های تشخیص پلاک

**گزینه 2: آموزش مدل شخصی** (توصیه می‌شود)

```bash
# نصب Ultralytics
pip install ultralytics

# آموزش مدل
yolo detect train data=iranian_plates.yaml model=yolov8n.pt epochs=100

# Export به ONNX
yolo export model=runs/detect/train/weights/best.pt format=onnx
```

**نیازمندی‌ها:**

- دیتاست: حداقل 500-1000 عکس پلاک ایرانی
- Annotation: برچسب‌گذاری با LabelImg یا Roboflow
- GPU: برای آموزش سریع‌تر (اختیاری)

📄 **راهنمای کامل دریافت مدل:** `src/Node/Ntk.NumberPlate.Node.ConfigApp/README_TEST_DETECTION.md`

📄 **مستندات کامل:** `src/Node/Ntk.NumberPlate.Node.ConfigApp/README_TEST_DETECTION.md`

---

## 💡 نکات مهم

- همیشه از آخرین نسخه .NET استفاده کنید
- Backup منظم از دیتابیس بگیرید
- لاگ‌ها را به طور مرتب بررسی کنید
- برای Production از HTTPS استفاده کنید
- API Token قوی تنظیم کنید

در صورت بروز مشکل، لطفا Issue در GitHub ایجاد کنید.
