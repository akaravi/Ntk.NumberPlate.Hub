# 🚗 سامانه تشخیص پلاک و سرعت خودرو (Ntk NumberPlate Hub)

سیستم جامع تشخیص پلاک خودرو و اندازه‌گیری سرعت با معماری Hub-Node، مبتنی بر YOLO و C#

## 📋 معرفی

این پروژه یک سیستم کامل برای تشخیص پلاک و سرعت خودرو است که شامل:

- **Hub Server**: سرور مرکزی برای جمع‌آوری و مدیریت داده‌ها (ASP.NET Core Web API)
- **Angular Dashboard**: داشبورد تحت وب برای نمایش و مدیریت اطلاعات
- **Node Service**: سرویس ویندوز برای تشخیص خودکار پلاک (Windows Service)
- **Configuration App**: نرم‌افزار پیکربندی نودها (Windows Forms)

## 🏗️ معماری سیستم

```
┌─────────────────────────────────────────────────────────┐
│                    Hub Server                            │
│  ┌──────────────┐         ┌──────────────┐             │
│  │  Web API     │◄────────┤   Database   │             │
│  │ (ASP.NET Core)│        │  (SQL Server)│             │
│  └──────┬───────┘         └──────────────┘             │
│         │                                               │
│         │                                               │
│  ┌──────▼────────────────────────────────┐            │
│  │      Angular Dashboard                 │            │
│  │  (Real-time Monitoring & Management)   │            │
│  └────────────────────────────────────────┘            │
└────────────────────┬──────────────────────────────────┘
                     │
                     │ HTTP API
                     │
       ┌─────────────┴─────────────┬─────────────┐
       │                           │             │
   ┌───▼────┐                 ┌───▼────┐   ┌───▼────┐
   │ Node 1 │                 │ Node 2 │   │ Node N │
   │        │                 │        │   │        │
   │ ┌────┐ │                 │ ┌────┐ │   │ ┌────┐ │
   │ │YOLO│ │                 │ │YOLO│ │   │ │YOLO│ │
   │ └────┘ │                 │ └────┘ │   │ └────┘ │
   │ Camera │                 │ Camera │   │ Camera │
   └────────┘                 └────────┘   └────────┘
```

## 🎯 ویژگی‌ها

### Hub Server

- ✅ API RESTful برای دریافت اطلاعات از نودها
- ✅ ذخیره‌سازی تصاویر و متادیتا در SQL Server
- ✅ مدیریت نودها و Heartbeat
- ✅ ارائه آمار و گزارش‌ها
- ✅ لاگ‌گذاری با Serilog

### Angular Dashboard

- ✅ داشبورد لحظه‌ای با نمایش آمار
- ✅ مشاهده تشخیص‌های اخیر
- ✅ جستجو و فیلتر براساس پلاک
- ✅ نمایش تخلفات سرعت
- ✅ مدیریت نودها (Online/Offline)
- ✅ به‌روزرسانی خودکار Real-time
- ✅ رابط کاربری زیبا و مدرن با طراحی RTL

### Node Service

- ✅ تشخیص پلاک با YOLO (ONNX Runtime)
- ✅ پردازش ویدیو با OpenCV
- ✅ محاسبه سرعت خودرو
- ✅ تشخیص تخلفات سرعت
- ✅ ارسال خودکار به Hub
- ✅ ذخیره محلی تصاویر
- ✅ اجرا به صورت Windows Service

### Configuration App

- ✅ رابط گرافیکی ساده برای پیکربندی
- ✅ تست اتصال به Hub
- ✅ پیکربندی YOLO Model
- ✅ تنظیم دوربین و پارامترها
- ✅ مدیریت سرویس

## 🚀 نصب و راه‌اندازی

### پیش‌نیازها

- .NET 8.0 SDK
- Node.js 18+ و npm
- SQL Server (LocalDB یا SQL Server Express)
- Visual Studio 2022 یا JetBrains Rider

### مراحل نصب

#### 1. Clone کردن پروژه

```bash
git clone https://github.com/akaravi/Ntk.NumberPlate.Hub.git
cd Ntk.NumberPlate.Hub
```

#### 2. راه‌اندازی Hub Server

```bash
cd src/Hub/Ntk.NumberPlate.Hub.Api

# به‌روزرسانی Connection String در appsettings.json
# اجرای Migration
dotnet ef database update

# اجرای API
dotnet run
```

API در آدرس `http://localhost:5000` در دسترس خواهد بود.

#### 3. راه‌اندازی Dashboard

```bash
cd src/Hub/Ntk.NumberPlate.Hub.Dashboard

# نصب Dependencies
npm install

# اجرای Development Server
npm start
```

Dashboard در آدرس `http://localhost:4200` در دسترس خواهد بود.

#### 4. راه‌اندازی Node

##### 4.1. Build کردن پروژه‌ها

```bash
cd src/Node/Ntk.NumberPlate.Node.ConfigApp
dotnet build

cd ../Ntk.NumberPlate.Node.Service
dotnet build
```

##### 4.2. اجرای Configuration App

1. فایل exe نرم‌افزار Configuration را اجرا کنید
2. تنظیمات زیر را وارد کنید:
   - نام نود
   - آدرس Hub Server
   - مسیر مدل YOLO
   - منبع ویدیو (شماره دوربین یا مسیر فایل)
   - پارامترهای دیگر
3. روی "تست اتصال" کلیک کنید
4. تنظیمات را ذخیره کنید

##### 4.3. اجرای Node Service

روی دکمه "راه‌اندازی سرویس" در Configuration App کلیک کنید یا:

```bash
cd src/Node/Ntk.NumberPlate.Node.Service
dotnet run
```

## 📁 ساختار پروژه

```
Ntk.NumberPlate.Hub/
├── src/
│   ├── Hub/
│   │   ├── Ntk.NumberPlate.Hub.Api/           # ASP.NET Core Web API
│   │   │   ├── Controllers/                   # API Controllers
│   │   │   ├── Services/                      # Business Logic
│   │   │   ├── Data/                          # Database Context
│   │   │   └── Models/                        # Entity Models
│   │   │
│   │   └── Ntk.NumberPlate.Hub.Dashboard/     # Angular Dashboard
│   │       ├── src/app/
│   │       │   ├── components/                # UI Components
│   │       │   ├── pages/                     # Pages
│   │       │   └── services/                  # API Services
│   │       └── package.json
│   │
│   ├── Node/
│   │   ├── Ntk.NumberPlate.Node.Service/      # Windows Service
│   │   │   ├── Services/                      # Core Services
│   │   │   │   ├── YoloDetectionService.cs    # YOLO Integration
│   │   │   │   ├── HubCommunicationService.cs # API Communication
│   │   │   │   └── SpeedCalculationService.cs # Speed Detection
│   │   │   └── Worker.cs                      # Background Worker
│   │   │
│   │   └── Ntk.NumberPlate.Node.ConfigApp/    # Windows Forms Config
│   │       └── MainForm.cs                    # Configuration UI
│   │
│   └── Shared/
│       └── Ntk.NumberPlate.Shared/            # Shared Models & DTOs
│           └── Models/                        # Common Data Models
│
├── Ntk.NumberPlate.Hub.sln                    # Solution File
└── README.md
```

## 🔧 پیکربندی

### Hub Server (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NtkNumberPlateHub;..."
  },
  "ImageStorage": {
    "BasePath": "wwwroot/images",
    "MaxImageSizeMB": 10
  }
}
```

### Node Configuration (node-config.json)

```json
{
  "NodeId": "unique-node-id",
  "NodeName": "Node-1",
  "HubServerUrl": "http://localhost:5000",
  "YoloModelPath": "models/plate-detection.onnx",
  "ConfidenceThreshold": 0.5,
  "VideoSource": "0",
  "ProcessingFps": 5,
  "SpeedLimit": 50,
  "EnableSpeedDetection": true
}
```

## 🎨 مدل YOLO

پروژه از YOLO برای تشخیص پلاک استفاده می‌کند. برای استفاده:

1. مدل YOLO خود را آموزش دهید یا از مدل‌های از پیش آموزش دیده استفاده کنید
2. مدل را به فرمت ONNX تبدیل کنید
3. مسیر مدل را در Configuration App تنظیم کنید

### نمونه آموزش/خروجی مدل با YOLOv11:

```python
from ultralytics import YOLO

# آموزش مدل (YOLOv11)
model = YOLO('yolo11n.pt')
model.train(data='license_plate.yaml', epochs=100)

# Export به ONNX با ورودی 640 و opset مناسب
model.export(format='onnx', opset=12, imgsz=640)
```

## 📊 API Endpoints

### Vehicle Detection

- `POST /api/vehicledetection/submit` - ثبت تشخیص جدید
- `GET /api/vehicledetection/recent?count=100` - دریافت آخرین تشخیص‌ها
- `GET /api/vehicledetection/by-plate/{plateNumber}` - جستجوی پلاک
- `GET /api/vehicledetection/statistics` - دریافت آمار

### Node Management

- `POST /api/node/register` - ثبت نود جدید
- `POST /api/node/heartbeat/{nodeId}` - ارسال Heartbeat
- `GET /api/node/list` - لیست تمام نودها
- `GET /api/node/{nodeId}` - اطلاعات یک نود

## 🔐 امنیت

- استفاده از API Token برای احراز هویت نودها
- اعتبارسنجی ورودی‌ها
- محدودیت حجم فایل‌های آپلود
- لاگ‌گذاری تمام عملیات

## 📈 مانیتورینگ و لاگ‌ها

- لاگ‌های Hub در: `logs/hub-*.log`
- لاگ‌های Node در: `logs/node-service-*.log`
- نمایش وضعیت نودها در Dashboard
- آمار Real-time در Dashboard

## 🤝 مشارکت

برای مشارکت در پروژه:

1. Fork کنید
2. Branch جدید بسازید (`git checkout -b feature/AmazingFeature`)
3. Commit کنید (`git commit -m 'Add some AmazingFeature'`)
4. Push کنید (`git push origin feature/AmazingFeature`)
5. Pull Request ایجاد کنید

## 📝 لایسنس

این پروژه تحت لایسنس MIT منتشر شده است.

## 👨‍💻 توسعه‌دهنده

**Alireza Karavi**

- Senior Software Developer
- Expertise: .NET, C#, Python, Computer Vision, AI/ML
- GitHub: [@akaravi](https://github.com/akaravi)

## 🙏 تشکر

- [YOLO](https://github.com/ultralytics/ultralytics) برای مدل تشخیص
- [OpenCV](https://opencv.org/) برای پردازش تصویر
- [Angular](https://angular.io/) برای Dashboard

## 📞 پشتیبانی

برای سوالات و پشتیبانی:

- GitHub Issues: [Issues](https://github.com/akaravi/Ntk.NumberPlate.Hub/issues)
- Email: support@ntk.ir

---

## 🚦 نکات مهم

### محاسبه سرعت

سیستم سرعت را بر اساس زمان عبور خودرو از دو نقطه محاسبه می‌کند. برای دقت بیشتر:

- فاصله بین دو نقطه را دقیق اندازه‌گیری کنید
- دوربین را در محل مناسب نصب کنید
- کالیبراسیون دوربین را انجام دهید

### بهینه‌سازی عملکرد

- `ProcessingFps` را بر اساس قدرت سخت‌افزار تنظیم کنید
- از GPU برای YOLO استفاده کنید (در صورت وجود)
- تصاویر را در محل ذخیره کنید و به صورت Batch ارسال کنید

### نکات امنیتی

- HTTPS را برای ارتباط نودها با Hub فعال کنید
- API Token قوی استفاده کنید
- دسترسی به Dashboard را محدود کنید
- Backup منظم از دیتابیس بگیرید
