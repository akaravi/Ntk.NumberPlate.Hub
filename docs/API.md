# 📡 مستندات API

## Base URL

```
http://localhost:5000/api
```

## Authentication

برای احراز هویت نودها از Bearer Token استفاده می‌شود:

```http
Authorization: Bearer YOUR_API_TOKEN
```

---

## 🚗 Vehicle Detection Endpoints

### 1. ثبت تشخیص جدید

ثبت یک تشخیص پلاک جدید از نود

**Endpoint:** `POST /vehicledetection/submit`

**Request Type:** `multipart/form-data`

**Parameters:**

| Parameter | Type | Required | Description   |
| --------- | ---- | -------- | ------------- |
| detection | JSON | Yes      | اطلاعات تشخیص |
| image     | File | No       | تصویر خودرو   |

**Detection JSON Structure:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "nodeId": "node-1",
  "nodeName": "Node Entrance 1",
  "plateNumber": "12ایران345-67",
  "speed": 65.5,
  "detectionTime": "2024-10-03T10:30:00",
  "imageFileName": "detection_001.jpg",
  "confidence": 0.95,
  "vehicleType": 1,
  "vehicleColor": "white",
  "isSpeedViolation": true,
  "speedLimit": 50.0
}
```

**Response:**

```json
{
  "success": true,
  "message": "تشخیص با موفقیت ثبت شد",
  "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2024-10-03T10:30:05"
}
```

**cURL Example:**

```bash
curl -X POST "http://localhost:5000/api/vehicledetection/submit" \
  -H "Content-Type: multipart/form-data" \
  -F "detection={\"nodeId\":\"node-1\",\"plateNumber\":\"12ایران345-67\",\"speed\":65.5}" \
  -F "image=@/path/to/image.jpg"
```

---

### 2. دریافت آخرین تشخیص‌ها

**Endpoint:** `GET /vehicledetection/recent?count={count}`

**Parameters:**

| Parameter | Type | Default | Description            |
| --------- | ---- | ------- | ---------------------- |
| count     | int  | 100     | تعداد رکوردهای بازگشتی |

**Response:**

```json
{
  "success": true,
  "message": "عملیات با موفقیت انجام شد",
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "nodeId": "node-1",
      "nodeName": "Node Entrance 1",
      "plateNumber": "12ایران345-67",
      "speed": 65.5,
      "detectionTime": "2024-10-03T10:30:00",
      "imageFileName": "detection_001.jpg",
      "confidence": 0.95,
      "vehicleType": 1,
      "isSpeedViolation": true,
      "speedLimit": 50.0
    }
  ],
  "timestamp": "2024-10-03T10:35:00"
}
```

---

### 3. جستجوی پلاک

جستجوی تشخیص‌ها بر اساس شماره پلاک

**Endpoint:** `GET /vehicledetection/by-plate/{plateNumber}`

**Parameters:**

| Parameter   | Type   | Required | Description |
| ----------- | ------ | -------- | ----------- |
| plateNumber | string | Yes      | شماره پلاک  |

**Example:** `GET /vehicledetection/by-plate/12ایران345-67`

**Response:**

```json
{
  "success": true,
  "message": "عملیات با موفقیت انجام شد",
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "plateNumber": "12ایران345-67",
      "speed": 65.5,
      "detectionTime": "2024-10-03T10:30:00",
      "nodeName": "Node Entrance 1"
    }
  ]
}
```

---

### 4. دریافت تشخیص‌های یک نود

**Endpoint:** `GET /vehicledetection/by-node/{nodeId}?from={from}&to={to}`

**Parameters:**

| Parameter | Type     | Required | Description |
| --------- | -------- | -------- | ----------- |
| nodeId    | string   | Yes      | شناسه نود   |
| from      | DateTime | No       | از تاریخ    |
| to        | DateTime | No       | تا تاریخ    |

**Example:**

```
GET /vehicledetection/by-node/node-1?from=2024-10-01&to=2024-10-03
```

---

### 5. دریافت آمار

دریافت آمار کلی سیستم

**Endpoint:** `GET /vehicledetection/statistics`

**Response:**

```json
{
  "success": true,
  "data": {
    "totalDetections": 15234,
    "todayDetections": 456,
    "speedViolations": 89,
    "activeNodes": 12
  }
}
```

---

## 🖥️ Node Management Endpoints

### 1. ثبت نود جدید

**Endpoint:** `POST /node/register`

**Request Body:**

```json
{
  "nodeId": "node-1",
  "nodeName": "Node Entrance 1"
}
```

**Response:**

```json
{
  "success": true,
  "message": "نود با موفقیت ثبت شد",
  "data": "node-1"
}
```

---

### 2. ارسال Heartbeat

**Endpoint:** `POST /node/heartbeat/{nodeId}`

**Parameters:**

| Parameter | Type   | Required | Description |
| --------- | ------ | -------- | ----------- |
| nodeId    | string | Yes      | شناسه نود   |

**Response:**

```json
{
  "success": true,
  "message": "OK",
  "data": "OK"
}
```

**Note:** نودها باید هر 30 ثانیه heartbeat ارسال کنند.

---

### 3. دریافت لیست نودها

**Endpoint:** `GET /node/list`

**Response:**

```json
{
  "success": true,
  "data": [
    {
      "nodeId": "node-1",
      "nodeName": "Node Entrance 1",
      "lastHeartbeat": "2024-10-03T10:35:00",
      "isOnline": true,
      "ipAddress": "192.168.1.100",
      "totalDetections": 1234,
      "registeredAt": "2024-10-01T08:00:00",
      "lastDetectionTime": "2024-10-03T10:30:00"
    }
  ]
}
```

---

### 4. دریافت اطلاعات یک نود

**Endpoint:** `GET /node/{nodeId}`

**Response:**

```json
{
  "success": true,
  "data": {
    "nodeId": "node-1",
    "nodeName": "Node Entrance 1",
    "lastHeartbeat": "2024-10-03T10:35:00",
    "isOnline": true,
    "ipAddress": "192.168.1.100",
    "totalDetections": 1234,
    "registeredAt": "2024-10-01T08:00:00"
  }
}
```

---

## 📊 Data Models

### VehicleDetectionData

```typescript
interface VehicleDetectionData {
  id: string; // GUID
  nodeId: string; // شناسه نود
  nodeName: string; // نام نود
  plateNumber: string; // شماره پلاک
  speed: number; // سرعت (km/h)
  detectionTime: DateTime; // زمان تشخیص
  imageFileName: string; // نام فایل تصویر
  confidence: number; // اعتماد (0-1)
  vehicleType: VehicleType; // نوع خودرو
  vehicleColor?: string; // رنگ خودرو
  isSpeedViolation: boolean; // آیا تخلف سرعت است
  speedLimit: number; // حد مجاز سرعت
  plateBoundingBox?: BoundingBox; // مختصات پلاک
}
```

### VehicleType Enum

```typescript
enum VehicleType {
  Unknown = 0,
  Car = 1,
  Motorcycle = 2,
  Truck = 3,
  Bus = 4,
  Van = 5,
}
```

### NodeInfo

```typescript
interface NodeInfo {
  nodeId: string;
  nodeName: string;
  lastHeartbeat: DateTime;
  isOnline: boolean;
  ipAddress?: string;
  version?: string;
  totalDetections: number;
  registeredAt: DateTime;
  lastDetectionTime?: DateTime;
}
```

---

## ❌ Error Responses

تمام خطاها با فرمت استاندارد زیر برگردانده می‌شوند:

```json
{
  "success": false,
  "message": "پیام خطا",
  "errors": ["جزئیات خطا 1", "جزئیات خطا 2"],
  "timestamp": "2024-10-03T10:35:00"
}
```

### HTTP Status Codes

- `200 OK` - درخواست موفق
- `400 Bad Request` - خطای اعتبارسنجی
- `401 Unauthorized` - نیاز به احراز هویت
- `404 Not Found` - منبع یافت نشد
- `500 Internal Server Error` - خطای سرور

---

## 🔐 Rate Limiting

- حداکثر 100 درخواست در دقیقه برای هر نود
- حداکثر 10 درخواست Submit در دقیقه

---

## 📝 Examples

### C# Example (Node Service)

```csharp
using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:5000");

var detection = new VehicleDetectionData
{
    NodeId = "node-1",
    PlateNumber = "12ایران345-67",
    Speed = 65.5,
    DetectionTime = DateTime.Now
};

using var content = new MultipartFormDataContent();
content.Add(new StringContent(JsonConvert.SerializeObject(detection)), "detection");

var response = await httpClient.PostAsync("/api/vehicledetection/submit", content);
var result = await response.Content.ReadAsStringAsync();
```

### JavaScript/TypeScript Example (Angular)

```typescript
const detection = {
  nodeId: "node-1",
  plateNumber: "12ایران345-67",
  speed: 65.5,
  detectionTime: new Date(),
};

this.http
  .get("http://localhost:5000/api/vehicledetection/recent")
  .subscribe((response) => {
    console.log(response.data);
  });
```

### Python Example

```python
import requests

url = "http://localhost:5000/api/vehicledetection/recent"
response = requests.get(url, params={"count": 50})
data = response.json()

for detection in data['data']:
    print(f"Plate: {detection['plateNumber']}, Speed: {detection['speed']}")
```

---

برای اطلاعات بیشتر، Swagger UI را در آدرس زیر مشاهده کنید:

```
http://localhost:5000/swagger
```

