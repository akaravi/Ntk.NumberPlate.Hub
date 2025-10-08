"""
اسکریپت Export مدل YOLO به ONNX بدون لایه‌های Post-Processing
"""

from ultralytics import YOLO
import sys

def export_model_for_csharp(model_path, output_name="model_simple.onnx"):
    """
    Export مدل YOLO به فرمت ONNX ساده
    
    Args:
        model_path: مسیر فایل مدل .pt
        output_name: نام فایل خروجی
    """
    
    print(f"🔄 در حال بارگذاری مدل از: {model_path}")
    
    try:
        # بارگذاری مدل
        model = YOLO(model_path)
        
        print("✅ مدل بارگذاری شد")
        print(f"📊 تعداد کلاس‌ها: {len(model.names)}")
        print(f"📝 نام کلاس‌ها: {model.names}")
        
        # Export به ONNX با تنظیمات بهینه
        print("\n🔄 در حال Export به ONNX...")
        
        model.export(
            format='onnx',           # فرمت خروجی
            imgsz=640,               # اندازه ورودی
            simplify=True,           # ساده‌سازی گراف
            dynamic=False,           # بدون dynamic shape
            opset=12,                # نسخه ONNX opset
            half=False,              # بدون half precision
            int8=False,              # بدون quantization
        )
        
        print("✅ Export موفقیت‌آمیز بود!")
        print(f"📁 فایل خروجی: {output_name}")
        print("\n💡 نکات:")
        print("  - از این مدل در برنامه C# استفاده کنید")
        print("  - پیش‌پردازش و پس‌پردازش در کد C# انجام می‌شود")
        
    except Exception as e:
        print(f"❌ خطا: {e}")
        sys.exit(1)


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='Export YOLO model to ONNX')
    parser.add_argument('model', help='مسیر فایل مدل .pt')
    parser.add_argument('-o', '--output', default='model_simple.onnx', 
                       help='نام فایل خروجی (پیش‌فرض: model_simple.onnx)')
    
    args = parser.parse_args()
    
    export_model_for_csharp(args.model, args.output)


if __name__ == "__main__":
    # مثال استفاده:
    # python export_model_simple.py your_model.pt -o output.onnx
    
    if len(sys.argv) < 2:
        print("❌ خطا: مسیر مدل را وارد کنید")
        print("\nنحوه استفاده:")
        print("  python export_model_simple.py model.pt")
        print("  python export_model_simple.py model.pt -o custom_name.onnx")
        sys.exit(1)
    
    main()

