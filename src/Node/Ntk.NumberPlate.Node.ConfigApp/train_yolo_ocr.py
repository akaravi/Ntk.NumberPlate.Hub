"""
اسکریپت آموزش مدل YOLO برای تشخیص حروف و اعداد پلاک (OCR)
"""

from ultralytics import YOLO
import sys
import os

def train_plate_ocr_model(data_yaml='plate-ocr.yaml', epochs=100, batch_size=16, img_size=640):
    """
    آموزش مدل YOLO برای OCR پلاک
    
    Args:
        data_yaml: مسیر فایل تنظیمات دیتاست
        epochs: تعداد epoch های آموزش
        batch_size: اندازه batch
        img_size: اندازه تصویر ورودی
    """
    
    print(f"🚀 شروع آموزش مدل YOLO OCR")
    print(f"📊 پارامترها:")
    print(f"  - Dataset: {data_yaml}")
    print(f"  - Epochs: {epochs}")
    print(f"  - Batch Size: {batch_size}")
    print(f"  - Image Size: {img_size}")
    
    try:
        # بررسی وجود فایل دیتاست
        if not os.path.exists(data_yaml):
            print(f"❌ خطا: فایل {data_yaml} یافت نشد!")
            return False
        
        # بارگذاری مدل پایه
        print("\n🔄 بارگذاری مدل پایه YOLOv11n...")
        model = YOLO('yolo11n.pt')
        
        print("✅ مدل پایه بارگذاری شد")
        
        # آموزش مدل
        print(f"\n🔄 شروع آموزش...")
        results = model.train(
            data=data_yaml,           # فایل تنظیمات دیتاست
            epochs=epochs,            # تعداد epoch
            imgsz=img_size,           # اندازه تصویر
            batch=batch_size,         # اندازه batch
            name='plate-ocr',         # نام پروژه
            patience=50,              # صبر برای Early Stopping
            save=True,                # ذخیره checkpoint ها
            device=0,                 # استفاده از GPU (0 = اولین GPU, cpu = CPU)
            workers=8,                # تعداد worker ها
            pretrained=True,          # استفاده از وزن‌های از پیش آموزش دیده
            optimizer='AdamW',        # نوع optimizer
            lr0=0.01,                 # نرخ یادگیری اولیه
            lrf=0.01,                 # نرخ یادگیری نهایی
            momentum=0.937,           # momentum
            weight_decay=0.0005,      # weight decay
            warmup_epochs=3.0,        # تعداد epoch های warm-up
            warmup_momentum=0.8,      # momentum در warm-up
            cos_lr=True,              # استفاده از cosine learning rate
            amp=True,                 # Automatic Mixed Precision
            hsv_h=0.015,              # افزایش داده: Hue
            hsv_s=0.7,                # افزایش داده: Saturation
            hsv_v=0.4,                # افزایش داده: Value
            degrees=10,               # افزایش داده: چرخش
            translate=0.1,            # افزایش داده: انتقال
            scale=0.5,                # افزایش داده: مقیاس
            shear=0.0,                # افزایش داده: shear
            perspective=0.0,          # افزایش داده: perspective
            flipud=0.0,               # افزایش داده: flip up-down
            fliplr=0.5,               # افزایش داده: flip left-right
            mosaic=1.0,               # افزایش داده: mosaic
            mixup=0.0,                # افزایش داده: mixup
            copy_paste=0.0,           # افزایش داده: copy-paste
        )
        
        print("\n✅ آموزش با موفقیت به پایان رسید!")
        
        # Export به ONNX
        print("\n🔄 Export مدل به فرمت ONNX...")
        export_path = model.export(
            format='onnx',            # فرمت خروجی
            imgsz=img_size,           # اندازه ورودی
            simplify=True,            # ساده‌سازی گراف
            dynamic=False,            # بدون dynamic shape
            opset=12,                 # نسخه ONNX opset
            half=False,               # بدون half precision
        )
        
        print(f"✅ Export موفقیت‌آمیز: {export_path}")
        print(f"\n📁 فایل‌های خروجی در: runs/detect/plate-ocr/")
        print(f"\n💡 مدل ONNX را به پوشه models کپی کنید:")
        print(f"   cp {export_path} models/plate-ocr.onnx")
        
        return True
        
    except Exception as e:
        print(f"\n❌ خطا در آموزش: {e}")
        import traceback
        traceback.print_exc()
        return False


def validate_model(model_path, data_yaml='plate-ocr.yaml'):
    """
    اعتبارسنجی مدل آموزش دیده
    
    Args:
        model_path: مسیر فایل مدل .pt
        data_yaml: مسیر فایل تنظیمات دیتاست
    """
    
    print(f"\n🔍 اعتبارسنجی مدل: {model_path}")
    
    try:
        model = YOLO(model_path)
        
        # اجرای validation
        results = model.val(
            data=data_yaml,
            imgsz=640,
            batch=16,
            save_json=True,
            save_hybrid=False,
        )
        
        print("\n📊 نتایج اعتبارسنجی:")
        print(f"  - mAP@50: {results.box.map50:.4f}")
        print(f"  - mAP@50-95: {results.box.map:.4f}")
        print(f"  - Precision: {results.box.p:.4f}")
        print(f"  - Recall: {results.box.r:.4f}")
        
        return True
        
    except Exception as e:
        print(f"❌ خطا در اعتبارسنجی: {e}")
        return False


def create_sample_yaml():
    """
    ایجاد فایل نمونه plate-ocr.yaml
    """
    
    yaml_content = """# تنظیمات دیتاست YOLO OCR برای پلاک ایرانی

# مسیرها
path: ./dataset  # مسیر root دیتاست
train: train/images  # مسیر تصاویر train
val: val/images  # مسیر تصاویر validation

# تعداد کلاس‌ها
nc: 34

# نام کلاس‌ها (اعداد + حروف پلاک ایرانی)
names:
  0: '0'
  1: '1'
  2: '2'
  3: '3'
  4: '4'
  5: '5'
  6: '6'
  7: '7'
  8: '8'
  9: '9'
  10: 'الف'
  11: 'ب'
  12: 'پ'
  13: 'ت'
  14: 'ث'
  15: 'ج'
  16: 'د'
  17: 'ز'
  18: 'س'
  19: 'ش'
  20: 'ص'
  21: 'ط'
  22: 'ع'
  23: 'ف'
  24: 'ق'
  25: 'ک'
  26: 'گ'
  27: 'ل'
  28: 'م'
  29: 'ن'
  30: 'و'
  31: 'ه'
  32: 'ی'
  33: 'ایران'

# نکات:
# - برای هر تصویر باید یک فایل label به فرمت YOLO وجود داشته باشد
# - فرمت label: class x_center y_center width height (نرمال شده 0-1)
# - ساختار دیتاست:
#   dataset/
#   ├── train/
#   │   ├── images/
#   │   │   ├── img1.jpg
#   │   │   └── ...
#   │   └── labels/
#   │       ├── img1.txt
#   │       └── ...
#   └── val/
#       ├── images/
#       └── labels/
"""
    
    with open('plate-ocr.yaml', 'w', encoding='utf-8') as f:
        f.write(yaml_content)
    
    print("✅ فایل نمونه plate-ocr.yaml ایجاد شد")


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='آموزش مدل YOLO OCR برای پلاک')
    parser.add_argument('--data', type=str, default='plate-ocr.yaml',
                       help='مسیر فایل تنظیمات دیتاست')
    parser.add_argument('--epochs', type=int, default=100,
                       help='تعداد epoch های آموزش')
    parser.add_argument('--batch', type=int, default=16,
                       help='اندازه batch')
    parser.add_argument('--img-size', type=int, default=640,
                       help='اندازه تصویر ورودی')
    parser.add_argument('--create-yaml', action='store_true',
                       help='ایجاد فایل نمونه plate-ocr.yaml')
    parser.add_argument('--validate', type=str,
                       help='مسیر مدل برای اعتبارسنجی')
    
    args = parser.parse_args()
    
    # ایجاد فایل نمونه
    if args.create_yaml:
        create_sample_yaml()
        return
    
    # اعتبارسنجی مدل
    if args.validate:
        validate_model(args.validate, args.data)
        return
    
    # آموزش مدل
    success = train_plate_ocr_model(
        data_yaml=args.data,
        epochs=args.epochs,
        batch_size=args.batch,
        img_size=args.img_size
    )
    
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    # نمونه‌های استفاده:
    # python train_yolo_ocr.py --create-yaml  # ایجاد فایل نمونه
    # python train_yolo_ocr.py --data plate-ocr.yaml --epochs 100  # آموزش
    # python train_yolo_ocr.py --validate runs/detect/plate-ocr/weights/best.pt  # اعتبارسنجی
    
    main()

