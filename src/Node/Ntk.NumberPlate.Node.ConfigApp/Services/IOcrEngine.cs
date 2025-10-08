using System;
using System.Drawing;
using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Node.ConfigApp.Services
{
    /// <summary>
    /// رابط مشترک برای تمام موتورهای OCR
    /// </summary>
    public interface IOcrEngine : IDisposable
    {
        /// <summary>
        /// تشخیص متن از تصویر پلاک
        /// </summary>
        /// <param name="plateImage">تصویر پلاک</param>
        /// <returns>نتیجه تشخیص OCR</returns>
        OcrResult Recognize(Bitmap plateImage);

        /// <summary>
        /// نام موتور OCR
        /// </summary>
        string EngineName { get; }

        /// <summary>
        /// روش OCR
        /// </summary>
        OcrMethod Method { get; }

        /// <summary>
        /// آیا موتور آماده است
        /// </summary>
        bool IsReady { get; }
    }
}

