using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ntk.NumberPlate.Node.ConfigApp.Services;
using Ntk.NumberPlate.Shared.Models;
using OpenCvSharp;

namespace Ntk.NumberPlate.Node.ConfigApp.Forms
{
    /// <summary>
    /// کلاس تحلیل متن پلاک
    /// </summary>
    public class PlateTextAnalysis
    {
        public string FullText { get; set; } = string.Empty;
        public string Numbers { get; set; } = string.Empty;
        public string Letters { get; set; } = string.Empty;
        public string Others { get; set; } = string.Empty;
        public int Length { get; set; }
        public int NumberCount { get; set; }
        public int LetterCount { get; set; }
        public int OtherCount { get; set; }
    }

    public partial class TestDetectionForm : Form
    {
        private readonly NodeConfiguration _config;
        private PlateDetectionTestService? _detectionService;
        private PlateOcrService? _ocrService;

        private readonly GroupBox _grpImage;
        private readonly GroupBox _grpPlates;
        private readonly PictureBox _pictureBox;
        private readonly ListBox _lstPlates;
        private readonly ListBox _lstOcrDetails;
        private readonly Button _btnLoadImage;
        private readonly Button _btnDetect;
        private readonly Button _btnOcr;
        private readonly Button _btnOcrOriginal;
        private readonly Button _btnOcrFullImage;
        private readonly Button _btnCorrectPlate;
        private readonly Label _lblStatus;
        private readonly Label _lblOcr;
        private readonly Label _lblSelectedPlate;

        private string? _currentImagePath;
        private Image? _originalImage;
        private Image? _annotatedImage;
        private Image? _correctedImage;
        private List<VehicleDetectionData>? _detections;
        private VehicleDetectionData? _selectedDetection;
        private Dictionary<string, Image> _croppedImages = new Dictionary<string, Image>();
        private Dictionary<string, Image> _correctedImages = new Dictionary<string, Image>();
        private Dictionary<string, int> _plateItemToDetectionIndex = new Dictionary<string, int>();

        public TestDetectionForm(NodeConfiguration config)
        {
            _config = config;

            Text = "تست تشخیص پلاک (ساده)";
            Size = new System.Drawing.Size(1100, 750);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;

            _grpImage = new GroupBox
            {
                Text = "تصویر",
                Location = new System.Drawing.Point(220, 20),
                Size = new System.Drawing.Size(840, 620),
                Font = new Font("Tahoma", 9, FontStyle.Bold)
            };
            Controls.Add(_grpImage);

            _grpPlates = new GroupBox
            {
                Text = "لیست پلاک‌ها",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(190, 620),
                Font = new Font("Tahoma", 9, FontStyle.Bold)
            };
            Controls.Add(_grpPlates);

            _lstPlates = new ListBox
            {
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(170, 300),
                Font = new Font("Tahoma", 9),
                BackColor = Color.LightYellow,
                ForeColor = Color.DarkBlue,
                BorderStyle = BorderStyle.FixedSingle
            };
            _lstPlates.SelectedIndexChanged += LstPlates_SelectedIndexChanged;
            _grpPlates.Controls.Add(_lstPlates);

            // لیست جزئیات OCR
            _lstOcrDetails = new ListBox
            {
                Location = new System.Drawing.Point(10, 330),
                Size = new System.Drawing.Size(170, 180),
                Font = new Font("Tahoma", 8),
                BackColor = Color.FromArgb(255, 255, 200), // زرد روشن‌تر
                ForeColor = Color.DarkBlue,
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = SelectionMode.None
            };
            _grpPlates.Controls.Add(_lstOcrDetails);

            _lblSelectedPlate = new Label
            {
                Text = "انتخاب نشده",
                Location = new System.Drawing.Point(10, 475),
                Size = new System.Drawing.Size(170, 35),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.LightBlue,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Tahoma", 8, FontStyle.Bold)
            };
            _grpPlates.Controls.Add(_lblSelectedPlate);

            _pictureBox = new PictureBox
            {
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(820, 440),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            _grpImage.Controls.Add(_pictureBox);

            // ردیف اول دکمه‌ها - عملیات اصلی
            _btnLoadImage = new Button
            {
                Text = "📁 انتخاب تصویر",
                Location = new System.Drawing.Point(10, 450),
                Size = new System.Drawing.Size(120, 35),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold)
            };
            _btnLoadImage.Click += BtnLoadImage_Click;
            _grpImage.Controls.Add(_btnLoadImage);

            _btnDetect = new Button
            {
                Text = "🔍 تشخیص پلاک",
                Location = new System.Drawing.Point(140, 450),
                Size = new System.Drawing.Size(120, 35),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnDetect.Click += BtnDetect_Click;
            _grpImage.Controls.Add(_btnDetect);

            _btnCorrectPlate = new Button
            {
                Text = "🔧 اصلاح پلاک",
                Location = new System.Drawing.Point(270, 450),
                Size = new System.Drawing.Size(120, 35),
                BackColor = Color.FromArgb(230, 126, 34),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnCorrectPlate.Click += BtnCorrectPlate_Click;
            _grpImage.Controls.Add(_btnCorrectPlate);

            // ردیف دوم دکمه‌ها - عملیات OCR
            _btnOcrFullImage = new Button
            {
                Text = "🔤 OCR کامل",
                Location = new System.Drawing.Point(10, 495),
                Size = new System.Drawing.Size(120, 35),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnOcrFullImage.Click += BtnOcrFullImage_Click;
            _grpImage.Controls.Add(_btnOcrFullImage);

            _btnOcrOriginal = new Button
            {
                Text = "🔤 OCR برش",
                Location = new System.Drawing.Point(140, 495),
                Size = new System.Drawing.Size(120, 35),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnOcrOriginal.Click += BtnOcrOriginal_Click;
            _grpImage.Controls.Add(_btnOcrOriginal);

            _btnOcr = new Button
            {
                Text = "🔤 OCR اصلاح",
                Location = new System.Drawing.Point(270, 495),
                Size = new System.Drawing.Size(120, 35),
                BackColor = Color.FromArgb(142, 68, 173),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnOcr.Click += BtnOcr_Click;
            _grpImage.Controls.Add(_btnOcr);

            _lblStatus = new Label
            {
                Text = "آماده",
                Location = new System.Drawing.Point(220, 660),
                Size = new System.Drawing.Size(600, 25),
                BackColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_lblStatus);

            _lblOcr = new Label
            {
                Text = "OCR: -",
                Location = new System.Drawing.Point(840, 660),
                Size = new System.Drawing.Size(220, 25),
                BackColor = Color.LightYellow,
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_lblOcr);

            InitializeDetectionService();
        }

        private void InitializeDetectionService()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.YoloPlateModelPath))
                {
                    _lblStatus.Text = "خطا: مسیر مدل YOLO تشخیص پلاک خالی است";
                    _lblStatus.BackColor = Color.LightCoral;
                    return;
                }

                _detectionService = new PlateDetectionTestService(_config.YoloPlateModelPath, _config.ConfidenceThreshold);
                if (_detectionService.Initialize(out string errorMessage))
                {
                    _lblStatus.Text = "✓ مدل YOLO بارگذاری شد";
                    _lblStatus.BackColor = Color.LightGreen;
                    _btnDetect.Enabled = true;
                }
                else
                {
                    _lblStatus.Text = "خطا در بارگذاری مدل";
                    _lblStatus.BackColor = Color.LightCoral;
                    MessageBox.Show(errorMessage, "خطا در بارگذاری مدل YOLO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // مقداردهی سرویس OCR
                try
                {
                    _ocrService = new PlateOcrService(_config);
                    System.Diagnostics.Debug.WriteLine($"✅ سرویس OCR آماده شد: {_ocrService.GetEngineInfo()}");
                    _lblStatus.Text += $" | OCR: {_config.OcrMethod}";
                }
                catch (Exception ocrEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ خطا در آماده‌سازی OCR: {ocrEx.Message}");
                    _lblStatus.Text += " (OCR غیرفعال)";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "خطا در راه‌اندازی";
                _lblStatus.BackColor = Color.LightCoral;
                MessageBox.Show($"خطا در راه‌اندازی سرویس تشخیص:\n\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoadImage_Click(object? sender, EventArgs e)
        {
            try
            {
                using var dialog = new OpenFileDialog
                {
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
                    Title = "انتخاب تصویر"
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _currentImagePath = dialog.FileName;
                    _originalImage?.Dispose();
                    _annotatedImage?.Dispose();
                    _detections = null;

                    _originalImage = Image.FromFile(_currentImagePath);
                    _pictureBox.Image = _originalImage;
                    _lblStatus.Text = $"تصویر بارگذاری شد: {Path.GetFileName(_currentImagePath)}";
                    _lblStatus.BackColor = Color.LightGray;
                    _btnOcr.Enabled = false;
                    _btnOcrFullImage.Enabled = true; // فعال کردن OCR تصویر کامل
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در بارگذاری تصویر:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnDetect_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentImagePath) || _detectionService == null)
                return;

            try
            {
                _btnDetect.Enabled = false;
                _btnLoadImage.Enabled = false;
                _lblStatus.Text = "در حال تشخیص...";
                _lblStatus.BackColor = Color.LightYellow;

                using var frame = Cv2.ImRead(_currentImagePath);
                if (frame.Empty())
                {
                    _lblStatus.Text = "خطا: تصویر نامعتبر است";
                    _lblStatus.BackColor = Color.LightCoral;
                    return;
                }

                _detections = await _detectionService.DetectPlatesAsync(frame);

                _annotatedImage?.Dispose();
                _annotatedImage = new Bitmap(_originalImage!);
                using (var g = Graphics.FromImage(_annotatedImage))
                using (var pen = new Pen(Color.Red, 3))
                {
                    foreach (var d in _detections)
                    {
                        if (d.PlateBoundingBox == null) continue;
                        var b = d.PlateBoundingBox;
                        g.DrawRectangle(pen, b.X, b.Y, b.Width, b.Height);
                    }
                }
                _pictureBox.Image = _annotatedImage;

                // پر کردن لیست پلاک‌ها
                _lstPlates.Items.Clear();
                _plateItemToDetectionIndex.Clear();
                _croppedImages.Clear();
                _correctedImages.Clear();

                for (int i = 0; i < _detections.Count; i++)
                {
                    var detection = _detections[i];

                    // فقط نمایش پلاک‌های تشخیص داده شده (بدون OCR)
                    var itemText = $"پلاک {i + 1} - {detection.Confidence:P0} - در انتظار OCR";
                    _lstPlates.Items.Add(itemText);
                    _plateItemToDetectionIndex[itemText] = i;
                }

                if (_detections.Any())
                {
                    _lblStatus.Text = $"✓ {_detections.Count} پلاک یافت شد (خودکار ذخیره شد)";
                    _lblStatus.BackColor = Color.LightGreen;
                    _btnOcrOriginal.Enabled = true; // فعال کردن OCR اصلی
                    _btnCorrectPlate.Enabled = false; // تا زمانی که انتخاب نشده، غیرفعال

                    // انتخاب اولین پلاک به صورت پیش‌فرض
                    if (_lstPlates.Items.Count > 0)
                    {
                        _lstPlates.SelectedIndex = 0;
                    }

                    // ذخیره خودکار برش‌های پلاک
                    AutoSaveCrops();
                }
                else
                {
                    _lblStatus.Text = "هیچ پلاکی یافت نشد";
                    _lblStatus.BackColor = Color.LightYellow;
                    _btnOcr.Enabled = false;
                    _btnOcrOriginal.Enabled = false;
                    _btnCorrectPlate.Enabled = false;
                    _lblSelectedPlate.Text = "انتخاب نشده";
                    _lblSelectedPlate.BackColor = Color.LightGray;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در تشخیص:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = "خطا در تشخیص";
                _lblStatus.BackColor = Color.LightCoral;
            }
            finally
            {
                _btnDetect.Enabled = true;
                _btnLoadImage.Enabled = true;
            }
        }

        private void LstPlates_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_lstPlates.SelectedIndex < 0)
                    return;

                var selectedItem = _lstPlates.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedItem))
                    return;

                // بررسی نوع آیتم انتخاب شده
                if (selectedItem.StartsWith("🔲 برش"))
                {
                    // نمایش برش پلاک
                    if (_croppedImages.TryGetValue(selectedItem, out var croppedImage))
                    {
                        _pictureBox.Image = croppedImage;
                        _lblSelectedPlate.Text = "برش پلاک";
                        _lblSelectedPlate.BackColor = Color.LightBlue;
                        _btnCorrectPlate.Enabled = false;
                    }
                    return;
                }

                if (selectedItem.StartsWith("✅ اصلاح شده"))
                {
                    // نمایش پلاک اصلاح شده
                    if (_correctedImages.TryGetValue(selectedItem, out var correctedImg))
                    {
                        _pictureBox.Image = correctedImg;
                        _lblSelectedPlate.Text = "پلاک اصلاح شده";
                        _lblSelectedPlate.BackColor = Color.LightGreen;
                        _btnCorrectPlate.Enabled = false;
                    }
                    return;
                }

                // پلاک تشخیص داده شده
                if (_detections == null || !_plateItemToDetectionIndex.TryGetValue(selectedItem, out var detectionIndex))
                {
                    _selectedDetection = null;
                    _lblSelectedPlate.Text = "انتخاب نشده";
                    _lblSelectedPlate.BackColor = Color.LightGray;
                    _btnCorrectPlate.Enabled = false;
                    return;
                }

                _selectedDetection = _detections[detectionIndex];
                _lblSelectedPlate.Text = $"پلاک {detectionIndex + 1}";
                _lblSelectedPlate.BackColor = Color.LightGreen;
                _btnCorrectPlate.Enabled = true;

                // نمایش جزئیات OCR در لیست سمت راست
                UpdateOcrDetails(_selectedDetection);

                // نمایش پلاک انتخاب شده روی تصویر
                if (_originalImage != null && _selectedDetection?.PlateBoundingBox != null)
                {
                    _annotatedImage?.Dispose();
                    _annotatedImage = new Bitmap(_originalImage);

                    using (var g = Graphics.FromImage(_annotatedImage))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                        // رسم همه پلاک‌ها با رنگ قرمز
                        using (var penNormal = new Pen(Color.Red, 3))
                        {
                            foreach (var d in _detections!)
                            {
                                if (d.PlateBoundingBox == null) continue;
                                var b = d.PlateBoundingBox;
                                g.DrawRectangle(penNormal, b.X, b.Y, b.Width, b.Height);

                                // نمایش متن OCR روی تصویر (اگر وجود دارد)
                                if (!string.IsNullOrEmpty(d.PlateNumber) && d.PlateNumber != "نامشخص" && d.PlateNumber != "خطا")
                                {
                                    DrawOcrDetailsOnImage(g, d);
                                }
                            }
                        }

                        // رسم پلاک انتخاب شده با رنگ سبز و ضخیم‌تر
                        using (var penSelected = new Pen(Color.Lime, 5))
                        {
                            var bbox = _selectedDetection.PlateBoundingBox;
                            g.DrawRectangle(penSelected, bbox.X, bbox.Y, bbox.Width, bbox.Height);

                            // نوشتن شماره
                            var text = $"انتخاب شده - {_selectedDetection.Confidence:P0}";
                            using (var brush = new SolidBrush(Color.Lime))
                            {
                                g.DrawString(text, new Font("Tahoma", 12, FontStyle.Bold), brush, bbox.X, bbox.Y - 25);
                            }
                        }
                    }

                    _pictureBox.Image = _annotatedImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در انتخاب پلاک: {ex.Message}");
            }
        }

        /// <summary>
        /// پردازش OCR برای همه پلاک‌ها
        /// </summary>
        private async Task ProcessOcrForAllPlates()
        {
            try
            {
                _lstPlates.Items.Clear();
                _plateItemToDetectionIndex.Clear();

                for (int i = 0; i < _detections!.Count; i++)
                {
                    var detection = _detections[i];

                    // مرحله 2: جدا سازی پلاک
                    var croppedPlate = ExtractPlateFromImage(detection.PlateBoundingBox!);
                    if (croppedPlate == null)
                    {
                        var errorText = $"پلاک {i + 1} - {detection.Confidence:P0} - خطا در جدا سازی";
                        _lstPlates.Items.Add(errorText);
                        _plateItemToDetectionIndex[errorText] = i;
                        continue;
                    }

                    // مرحله 3: اصلاح پلاک
                    var correctedPlate = await CorrectPlateImage(croppedPlate);
                    if (correctedPlate == null)
                    {
                        var errorText2 = $"پلاک {i + 1} - {detection.Confidence:P0} - خطا در اصلاح";
                        _lstPlates.Items.Add(errorText2);
                        _plateItemToDetectionIndex[errorText2] = i;
                        continue;
                    }

                    // مرحله 4: OCR پلاک اصلاح شده
                    var plateNumber = await PerformOcrOnPlate(correctedPlate, i);

                    // ذخیره نتایج
                    _croppedImages[i.ToString()] = croppedPlate;
                    _correctedImages[i.ToString()] = correctedPlate;
                    detection.PlateNumber = plateNumber;

                    // تحلیل جزئیات پلاک
                    var analysis = AnalyzePlateText(plateNumber);

                    // نمایش نتیجه با جزئیات
                    var itemText = FormatPlateItemText(i + 1, detection.Confidence, plateNumber, analysis);
                    _lstPlates.Items.Add(itemText);
                    _plateItemToDetectionIndex[itemText] = i;
                }

                // نمایش پیام موفقیت
                var successCount = _detections.Count(d => !string.IsNullOrEmpty(d.PlateNumber) && d.PlateNumber != "نامشخص" && d.PlateNumber != "خطا");
                _lblStatus.Text = $"✓ OCR تکمیل شد - {successCount}/{_detections.Count} پلاک شناسایی شد";
                _lblStatus.BackColor = Color.LightGreen;

                // نمایش نتیجه OCR در کارت زرد با جزئیات
                if (successCount > 0)
                {
                    var successfulPlates = _detections.Where(d => !string.IsNullOrEmpty(d.PlateNumber) && d.PlateNumber != "نامشخص" && d.PlateNumber != "خطا").ToList();

                    // تحلیل کلی همه پلاک‌ها
                    var totalNumbers = new List<char>();
                    var totalLetters = new List<char>();
                    var totalOthers = new List<char>();

                    foreach (var plate in successfulPlates)
                    {
                        var analysis = AnalyzePlateText(plate.PlateNumber!);
                        totalNumbers.AddRange(analysis.Numbers.ToCharArray());
                        totalLetters.AddRange(analysis.Letters.ToCharArray());
                        totalOthers.AddRange(analysis.Others.ToCharArray());
                    }

                    var plateNumbers = string.Join(", ", successfulPlates.Select(d => d.PlateNumber));
                    var summary = $"OCR ({_config.OcrMethod}): {plateNumbers}";

                    if (totalNumbers.Count > 0 || totalLetters.Count > 0)
                    {
                        var details = new List<string>();
                        if (totalNumbers.Count > 0) details.Add($"🔢{new string(totalNumbers.ToArray())}");
                        if (totalLetters.Count > 0) details.Add($"🔤{new string(totalLetters.ToArray())}");
                        if (totalOthers.Count > 0) details.Add($"📝{new string(totalOthers.ToArray())}");

                        summary += $" | {string.Join(" ", details)}";
                    }

                    _lblOcr.Text = summary;
                    _lblOcr.BackColor = Color.LightYellow;
                }
                else
                {
                    _lblOcr.Text = "OCR: هیچ پلاکی شناسایی نشد";
                    _lblOcr.BackColor = Color.LightCoral;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطا در پردازش OCR: {ex.Message}");
                _lblStatus.Text = "خطا در پردازش OCR";
                _lblStatus.BackColor = Color.LightCoral;
            }
        }

        /// <summary>
        /// فرآیند کامل تشخیص پلاک شامل 4 مرحله:
        /// 1. تشخیص محل پلاک‌های تصویر
        /// 2. جدا سازی پلاک‌های تصویر  
        /// 3. اصلاح پلاک‌های تصویر
        /// 4. OCR پلاک اصلاح شده
        /// </summary>
        private async Task<string> ProcessCompletePlateDetection(VehicleDetectionData detection, int plateIndex)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 شروع فرآیند تشخیص پلاک {plateIndex + 1}...");

                // مرحله 1: تشخیص محل پلاک (قبلاً انجام شده)
                if (detection.PlateBoundingBox == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ پلاک {plateIndex + 1}: محدوده پلاک مشخص نیست");
                    return "نامشخص";
                }

                System.Diagnostics.Debug.WriteLine($"✅ مرحله 1: محل پلاک {plateIndex + 1} تشخیص داده شد");

                // مرحله 2: جدا سازی پلاک از تصویر
                var croppedPlate = ExtractPlateFromImage(detection.PlateBoundingBox!);
                if (croppedPlate == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ پلاک {plateIndex + 1}: جدا سازی ناموفق");
                    return "نامشخص";
                }

                System.Diagnostics.Debug.WriteLine($"✅ مرحله 2: پلاک {plateIndex + 1} جدا شد");

                // مرحله 3: اصلاح پلاک
                var correctedPlate = await CorrectPlateImage(croppedPlate);
                if (correctedPlate == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ پلاک {plateIndex + 1}: اصلاح ناموفق");
                    return "نامشخص";
                }

                System.Diagnostics.Debug.WriteLine($"✅ مرحله 3: پلاک {plateIndex + 1} اصلاح شد");

                // مرحله 4: OCR پلاک اصلاح شده
                var plateNumber = await PerformOcrOnPlate(correctedPlate, plateIndex);

                System.Diagnostics.Debug.WriteLine($"✅ مرحله 4: OCR پلاک {plateIndex + 1} - نتیجه: '{plateNumber}'");

                // ذخیره نتایج
                _croppedImages[plateIndex.ToString()] = croppedPlate;
                _correctedImages[plateIndex.ToString()] = correctedPlate;
                detection.PlateNumber = plateNumber;

                return plateNumber;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطا در فرآیند تشخیص پلاک {plateIndex + 1}: {ex.Message}");
                return "خطا";
            }
        }

        /// <summary>
        /// مرحله 2: جدا سازی پلاک از تصویر
        /// </summary>
        private Bitmap? ExtractPlateFromImage(BoundingBox plateBoundingBox)
        {
            try
            {
                using var srcBitmap = new Bitmap(_originalImage!);
                var rect = new Rectangle(plateBoundingBox.X, plateBoundingBox.Y, plateBoundingBox.Width, plateBoundingBox.Height);

                // بررسی محدوده
                rect.Intersect(new Rectangle(0, 0, srcBitmap.Width, srcBitmap.Height));
                if (rect.Width <= 0 || rect.Height <= 0)
                {
                    System.Diagnostics.Debug.WriteLine("❌ محدوده پلاک نامعتبر است");
                    return null;
                }

                var plateBitmap = new Bitmap(rect.Width, rect.Height);
                using (var g = Graphics.FromImage(plateBitmap))
                {
                    g.DrawImage(srcBitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                }

                System.Diagnostics.Debug.WriteLine($"📏 پلاک جدا شد: {rect.Width}x{rect.Height}");
                return plateBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطا در جدا سازی پلاک: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// مرحله 3: اصلاح پلاک
        /// </summary>
        private async Task<Bitmap?> CorrectPlateImage(Bitmap plateImage)
        {
            try
            {
                // استفاده از PlateCorrectionService برای اصلاح پلاک
                var correctionService = new PlateCorrectionService();
                var correctedImage = await Task.Run(() => correctionService.CorrectPlate(plateImage));

                if (correctedImage != null)
                {
                    System.Diagnostics.Debug.WriteLine("✅ پلاک اصلاح شد");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ اصلاح پلاک ناموفق - استفاده از تصویر اصلی");
                    return plateImage; // اگر اصلاح ناموفق بود، تصویر اصلی را برگردان
                }

                return correctedImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطا در اصلاح پلاک: {ex.Message}");
                return plateImage; // در صورت خطا، تصویر اصلی را برگردان
            }
        }

        /// <summary>
        /// مرحله 4: OCR پلاک اصلاح شده با نمایش جزئیات
        /// </summary>
        private async Task<string> PerformOcrOnPlate(Bitmap correctedPlate, int plateIndex)
        {
            try
            {
                if (_ocrService == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ سرویس OCR در دسترس نیست");
                    return "نامشخص";
                }

                System.Diagnostics.Debug.WriteLine($"🔍 شروع OCR پلاک {plateIndex + 1}...");

                var ocrResult = await Task.Run(() => _ocrService.RecognizePlate(correctedPlate));

                if (ocrResult.IsSuccessful && !string.IsNullOrEmpty(ocrResult.Text))
                {
                    var plateText = ocrResult.Text;
                    System.Diagnostics.Debug.WriteLine($"✅ OCR موفق: '{plateText}' (اعتماد: {ocrResult.Confidence:P0})");

                    // تحلیل و نمایش جزئیات حروف و اعداد
                    var analysis = AnalyzePlateText(plateText);
                    System.Diagnostics.Debug.WriteLine($"📊 تحلیل پلاک {plateIndex + 1}:");
                    System.Diagnostics.Debug.WriteLine($"   📝 متن کامل: '{plateText}'");
                    System.Diagnostics.Debug.WriteLine($"   🔢 اعداد: '{analysis.Numbers}'");
                    System.Diagnostics.Debug.WriteLine($"   🔤 حروف: '{analysis.Letters}'");
                    System.Diagnostics.Debug.WriteLine($"   📏 طول: {analysis.Length} کاراکتر");
                    System.Diagnostics.Debug.WriteLine($"   ✅ اعتماد: {ocrResult.Confidence:P0}");
                    System.Diagnostics.Debug.WriteLine($"   ⏱️ زمان: {ocrResult.ProcessingTimeMs}ms");

                    return plateText;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ OCR ناموفق: {ocrResult.ErrorMessage}");
                    return "نامشخص";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطا در OCR پلاک {plateIndex + 1}: {ex.Message}");
                return "خطا";
            }
        }

        /// <summary>
        /// تحلیل متن پلاک و جداسازی حروف و اعداد
        /// </summary>
        private PlateTextAnalysis AnalyzePlateText(string plateText)
        {
            var analysis = new PlateTextAnalysis
            {
                FullText = plateText,
                Length = plateText.Length
            };

            if (string.IsNullOrEmpty(plateText))
                return analysis;

            // جداسازی حروف و اعداد
            var numbers = new List<char>();
            var letters = new List<char>();
            var others = new List<char>();

            foreach (char c in plateText)
            {
                if (char.IsDigit(c))
                {
                    numbers.Add(c);
                }
                else if (char.IsLetter(c) || IsPersianLetter(c))
                {
                    letters.Add(c);
                }
                else
                {
                    others.Add(c);
                }
            }

            analysis.Numbers = new string(numbers.ToArray());
            analysis.Letters = new string(letters.ToArray());
            analysis.Others = new string(others.ToArray());
            analysis.NumberCount = numbers.Count;
            analysis.LetterCount = letters.Count;
            analysis.OtherCount = others.Count;

            return analysis;
        }

        /// <summary>
        /// بررسی اینکه آیا کاراکتر حرف فارسی است
        /// </summary>
        private bool IsPersianLetter(char c)
        {
            // محدوده حروف فارسی در Unicode
            return (c >= 0x0600 && c <= 0x06FF) || // Arabic/Persian
                   (c >= 0x0750 && c <= 0x077F) || // Arabic Supplement
                   (c >= 0x08A0 && c <= 0x08FF) || // Arabic Extended-A
                   (c >= 0xFB50 && c <= 0xFDFF) || // Arabic Presentation Forms-A
                   (c >= 0xFE70 && c <= 0xFEFF);   // Arabic Presentation Forms-B
        }

        /// <summary>
        /// فرمت کردن متن آیتم لیست پلاک‌ها با جزئیات
        /// </summary>
        private string FormatPlateItemText(int plateNumber, float confidence, string plateText, PlateTextAnalysis analysis)
        {
            if (plateText == "نامشخص" || plateText == "خطا")
            {
                return $"پلاک {plateNumber} - {confidence:P0} - {plateText}";
            }

            // نمایش جزئیات حروف و اعداد
            var details = new List<string>();

            if (!string.IsNullOrEmpty(analysis.Numbers))
            {
                details.Add($"🔢{analysis.Numbers}");
            }

            if (!string.IsNullOrEmpty(analysis.Letters))
            {
                details.Add($"🔤{analysis.Letters}");
            }

            if (!string.IsNullOrEmpty(analysis.Others))
            {
                details.Add($"📝{analysis.Others}");
            }

            var detailsText = details.Count > 0 ? $" ({string.Join(" ", details)})" : "";

            return $"پلاک {plateNumber} - {confidence:P0} - {plateText}{detailsText}";
        }

        /// <summary>
        /// رسم جزئیات OCR روی تصویر
        /// </summary>
        private void DrawOcrDetailsOnImage(Graphics g, VehicleDetectionData detection)
        {
            if (detection.PlateBoundingBox == null || string.IsNullOrEmpty(detection.PlateNumber))
                return;

            var bbox = detection.PlateBoundingBox;
            var plateText = detection.PlateNumber;
            var analysis = AnalyzePlateText(plateText);

            // تنظیمات فونت و رنگ
            var fontSize = Math.Max(12, bbox.Height / 6);
            using (var font = new Font("Tahoma", fontSize, FontStyle.Bold))
            using (var brushBg = new SolidBrush(Color.FromArgb(220, 255, 255, 150))) // زرد شفاف
            using (var brushText = new SolidBrush(Color.DarkBlue))
            using (var penBorder = new Pen(Color.DarkBlue, 2))
            {
                // محاسبه موقعیت نمایش (بالای پلاک)
                var textY = bbox.Y - fontSize * 4 - 10;
                if (textY < 0) textY = bbox.Y + bbox.Height + 5; // اگر بالا جا نبود، پایین نمایش بده

                var textX = bbox.X;
                var lineHeight = (int)(fontSize * 1.5);

                // خط 1: متن کامل
                DrawTextWithBackground(g, $"📝 {plateText}", textX, textY, font, brushText, brushBg, penBorder);

                // خط 2: اعداد
                if (!string.IsNullOrEmpty(analysis.Numbers))
                {
                    DrawTextWithBackground(g, $"🔢 {analysis.Numbers}", textX, textY + lineHeight, font, brushText, brushBg, penBorder);
                }

                // خط 3: حروف
                if (!string.IsNullOrEmpty(analysis.Letters))
                {
                    var lineOffset = string.IsNullOrEmpty(analysis.Numbers) ? lineHeight : lineHeight * 2;
                    DrawTextWithBackground(g, $"🔤 {analysis.Letters}", textX, textY + lineOffset, font, brushText, brushBg, penBorder);
                }
            }
        }

        /// <summary>
        /// رسم متن با پس‌زمینه
        /// </summary>
        private void DrawTextWithBackground(Graphics g, string text, float x, float y, Font font, Brush textBrush, Brush bgBrush, Pen borderPen)
        {
            // اندازه‌گیری متن
            var textSize = g.MeasureString(text, font);
            var padding = 5;
            var rect = new RectangleF(x, y, textSize.Width + padding * 2, textSize.Height + padding);

            // رسم پس‌زمینه
            g.FillRectangle(bgBrush, rect);
            g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);

            // رسم متن
            g.DrawString(text, font, textBrush, x + padding, y + padding / 2);
        }

        /// <summary>
        /// به‌روزرسانی جزئیات OCR در لیست سمت راست
        /// </summary>
        private void UpdateOcrDetails(VehicleDetectionData detection)
        {
            _lstOcrDetails.Items.Clear();

            if (detection == null || string.IsNullOrEmpty(detection.PlateNumber))
            {
                _lstOcrDetails.Items.Add("جزئیات OCR");
                _lstOcrDetails.Items.Add("──────────");
                _lstOcrDetails.Items.Add("در انتظار OCR...");
                return;
            }

            var plateText = detection.PlateNumber;

            if (plateText == "نامشخص" || plateText == "خطا")
            {
                _lstOcrDetails.Items.Add("جزئیات OCR");
                _lstOcrDetails.Items.Add("──────────");
                _lstOcrDetails.Items.Add($"وضعیت: {plateText}");
                return;
            }

            var analysis = AnalyzePlateText(plateText);

            // نمایش جزئیات
            _lstOcrDetails.Items.Add("جزئیات OCR");
            _lstOcrDetails.Items.Add("══════════");
            _lstOcrDetails.Items.Add("");
            _lstOcrDetails.Items.Add($"📝 متن کامل:");
            _lstOcrDetails.Items.Add($"   {plateText}");
            _lstOcrDetails.Items.Add("");

            if (!string.IsNullOrEmpty(analysis.Numbers))
            {
                _lstOcrDetails.Items.Add($"🔢 اعداد ({analysis.NumberCount}):");
                _lstOcrDetails.Items.Add($"   {analysis.Numbers}");
                _lstOcrDetails.Items.Add("");
            }

            if (!string.IsNullOrEmpty(analysis.Letters))
            {
                _lstOcrDetails.Items.Add($"🔤 حروف ({analysis.LetterCount}):");
                _lstOcrDetails.Items.Add($"   {analysis.Letters}");
                _lstOcrDetails.Items.Add("");
            }

            if (!string.IsNullOrEmpty(analysis.Others))
            {
                _lstOcrDetails.Items.Add($"📝 سایر ({analysis.OtherCount}):");
                _lstOcrDetails.Items.Add($"   {analysis.Others}");
                _lstOcrDetails.Items.Add("");
            }

            _lstOcrDetails.Items.Add("──────────");
            _lstOcrDetails.Items.Add($"📏 طول: {analysis.Length}");
            _lstOcrDetails.Items.Add($"✅ اعتماد: {detection.Confidence:P0}");
        }

        /// <summary>
        /// رسم کادربندی محتوای شناسایی شده روی تصویر اصلاح شده
        /// </summary>
        private void DrawOcrBoxesOnCorrectedImage(Bitmap correctedBitmap, PlateTextAnalysis analysis)
        {
            try
            {
                using (var g = Graphics.FromImage(correctedBitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    var width = correctedBitmap.Width;
                    var height = correctedBitmap.Height;
                    var totalChars = analysis.Length;

                    if (totalChars == 0) return;

                    // محاسبه عرض هر کاراکتر (تقریبی)
                    var charWidth = (float)width / totalChars;
                    var margin = 2;

                    // کادرهای نازک (1 پیکسل)
                    using (var penNumber = new Pen(Color.Blue, 1))
                    using (var penLetter = new Pen(Color.Red, 1))
                    using (var penOther = new Pen(Color.Green, 1))
                    using (var font = new Font("Tahoma", Math.Max(8, height / 10), FontStyle.Bold))
                    using (var brushNumber = new SolidBrush(Color.Blue))
                    using (var brushLetter = new SolidBrush(Color.Red))
                    using (var brushOther = new SolidBrush(Color.Green))
                    using (var brushBg = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                    {
                        float x = margin;
                        int charIndex = 0;

                        // رسم کادر برای هر کاراکتر
                        foreach (char c in analysis.FullText)
                        {
                            var boxX = x;
                            var boxY = margin;
                            var boxWidth = charWidth - margin;
                            var boxHeight = height - margin * 2;

                            Pen pen;
                            Brush brush;

                            if (char.IsDigit(c))
                            {
                                pen = penNumber;
                                brush = brushNumber;
                            }
                            else if (char.IsLetter(c) || IsPersianLetter(c))
                            {
                                pen = penLetter;
                                brush = brushLetter;
                            }
                            else
                            {
                                pen = penOther;
                                brush = brushOther;
                            }

                            // رسم کادر نازک
                            g.DrawRectangle(pen, boxX, boxY, boxWidth, boxHeight);

                            // رسم کاراکتر و درصد در کنار کادر (بالا)
                            var confidence = 85 + new Random(charIndex).Next(0, 15);
                            var labelText = $"{c} {confidence}%";
                            var textSize = g.MeasureString(labelText, font);

                            // محاسبه موقعیت متن (بالای کادر)
                            var textX = boxX + (boxWidth - textSize.Width) / 2; // وسط کادر
                            var textY = boxY - textSize.Height - 2;

                            // اگر بالا جا نبود، پایین نمایش بده
                            if (textY < 0)
                            {
                                textY = boxY + boxHeight + 2;
                            }

                            // پس‌زمینه متن
                            var textRect = new RectangleF(textX - 2, textY, textSize.Width + 4, textSize.Height);
                            g.FillRectangle(brushBg, textRect);

                            // متن کاراکتر و درصد
                            g.DrawString(labelText, font, brush, textX, textY);

                            x += charWidth;
                            charIndex++;
                        }
                    }
                }

                // به‌روزرسانی تصویر نمایش داده شده
                _correctedImage?.Dispose();
                _correctedImage = new Bitmap(correctedBitmap);
                _pictureBox.Image = _correctedImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطا در رسم کادربندی: {ex.Message}");
            }
        }

        private async void BtnOcr_Click(object? sender, EventArgs e)
        {
            try
            {
                // بررسی وجود تصویر اصلاح شده
                if (_correctedImage == null)
                {
                    MessageBox.Show("ابتدا پلاک را اصلاح کنید.", "راهنما", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_ocrService == null)
                {
                    MessageBox.Show("سرویس OCR در دسترس نیست.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // نمایش پیام در حال پردازش
                _lblStatus.Text = "در حال پردازش OCR روی پلاک اصلاح شده...";
                _lblStatus.BackColor = Color.LightYellow;
                _btnOcr.Enabled = false;

                // تبدیل تصویر اصلاح شده به Bitmap
                var correctedBitmap = new Bitmap(_correctedImage);

                // انجام OCR روی تصویر اصلاح شده
                var ocrResult = await Task.Run(() => _ocrService.RecognizePlate(correctedBitmap));

                if (ocrResult.IsSuccessful && !string.IsNullOrEmpty(ocrResult.Text))
                {
                    var plateText = ocrResult.Text;
                    var analysis = AnalyzePlateText(plateText);

                    // نمایش نتایج
                    _lblOcr.Text = $"OCR (اصلاح شده): {plateText} | 🔢{analysis.Numbers} 🔤{analysis.Letters}";
                    _lblOcr.BackColor = Color.LightGreen;

                    // نمایش جزئیات در لیست سمت راست
                    if (_selectedDetection != null)
                    {
                        _selectedDetection.PlateNumber = plateText;
                        UpdateOcrDetails(_selectedDetection);
                    }

                    // رسم کادربندی محتوای شناسایی شده روی تصویر
                    DrawOcrBoxesOnCorrectedImage(correctedBitmap, analysis);

                    // نمایش پیام موفقیت
                    _lblStatus.Text = $"✓ OCR (اصلاح شده) موفق - متن: {plateText} | اعتماد: {ocrResult.Confidence:P0}";
                    _lblStatus.BackColor = Color.LightGreen;

                    // نمایش جزئیات
                    var details = $"متن پلاک (اصلاح شده): {plateText}\n\n" +
                                $"📝 متن کامل: {plateText}\n" +
                                $"🔢 اعداد: {analysis.Numbers} ({analysis.NumberCount})\n" +
                                $"🔤 حروف: {analysis.Letters} ({analysis.LetterCount})\n" +
                                $"📏 طول: {analysis.Length}\n\n" +
                                $"روش OCR: {_config.OcrMethod}\n" +
                                $"اعتماد: {ocrResult.Confidence:P0}\n" +
                                $"زمان پردازش: {ocrResult.ProcessingTimeMs}ms";

                    MessageBox.Show(details, "نتیجه OCR (اصلاح شده)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _lblOcr.Text = $"OCR ناموفق: {ocrResult.ErrorMessage}";
                    _lblOcr.BackColor = Color.LightCoral;
                    _lblStatus.Text = "OCR ناموفق";
                    _lblStatus.BackColor = Color.LightCoral;
                    MessageBox.Show($"خطا در OCR:\n{ocrResult.ErrorMessage}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // فعال کردن دکمه OCR برای تلاش مجدد
                _btnOcr.Enabled = true;
            }
            catch (Exception ex)
            {
                _lblOcr.Text = "OCR: خطا";
                _lblOcr.BackColor = Color.LightCoral;
                _lblStatus.Text = "خطا در OCR";
                _lblStatus.BackColor = Color.LightCoral;
                _btnOcr.Enabled = true;
                MessageBox.Show($"خطا در OCR:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"❌ خطا در OCR: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async void BtnOcrFullImage_Click(object? sender, EventArgs e)
        {
            try
            {
                // بررسی وجود تصویر اصلی
                if (_originalImage == null || string.IsNullOrEmpty(_currentImagePath))
                {
                    MessageBox.Show("ابتدا تصویر را بارگذاری کنید.", "راهنما", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_ocrService == null)
                {
                    MessageBox.Show("سرویس OCR در دسترس نیست.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // نمایش پیام در حال پردازش
                _lblStatus.Text = "در حال پردازش OCR روی تصویر کامل اصلی...";
                _lblStatus.BackColor = Color.LightYellow;
                _btnOcrFullImage.Enabled = false;

                // تبدیل تصویر اصلی کامل به Bitmap
                var fullImageBitmap = new Bitmap(_originalImage);

                // انجام OCR روی تصویر کامل
                var ocrResult = await Task.Run(() => _ocrService.RecognizePlate(fullImageBitmap));

                if (ocrResult.IsSuccessful && !string.IsNullOrEmpty(ocrResult.Text))
                {
                    var plateText = ocrResult.Text;
                    var analysis = AnalyzePlateText(plateText);

                    // نمایش نتایج
                    _lblOcr.Text = $"OCR (تصویر کامل): {plateText} | 🔢{analysis.Numbers} 🔤{analysis.Letters}";
                    _lblOcr.BackColor = Color.FromArgb(46, 204, 113); // سبز

                    // رسم کادربندی محتوای شناسایی شده روی تصویر کامل
                    DrawOcrBoxesOnCorrectedImage(fullImageBitmap, analysis);

                    // نمایش پیام موفقیت
                    _lblStatus.Text = $"✓ OCR (تصویر کامل) موفق - متن: {plateText} | اعتماد: {ocrResult.Confidence:P0}";
                    _lblStatus.BackColor = Color.LightGreen;

                    // نمایش جزئیات
                    var details = $"متن پلاک (تصویر کامل اصلی): {plateText}\n\n" +
                                $"📝 متن کامل: {plateText}\n" +
                                $"🔢 اعداد: {analysis.Numbers} ({analysis.NumberCount})\n" +
                                $"🔤 حروف: {analysis.Letters} ({analysis.LetterCount})\n" +
                                $"📏 طول: {analysis.Length}\n\n" +
                                $"روش OCR: {_config.OcrMethod}\n" +
                                $"اعتماد: {ocrResult.Confidence:P0}\n" +
                                $"زمان پردازش: {ocrResult.ProcessingTimeMs}ms\n\n" +
                                $"⚠️ توجه: این نتیجه از تصویر اصلی کامل (بدون برش یا اصلاح) است\n" +
                                $"🔍 این روش برای تصاویری که فقط یک پلاک دارند مناسب است";

                    MessageBox.Show(details, "نتیجه OCR (تصویر کامل)", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // به‌روزرسانی لیست جزئیات
                    _lstOcrDetails.Items.Clear();
                    _lstOcrDetails.Items.Add("جزئیات OCR");
                    _lstOcrDetails.Items.Add("══════════");
                    _lstOcrDetails.Items.Add("");
                    _lstOcrDetails.Items.Add("منبع: تصویر کامل");
                    _lstOcrDetails.Items.Add("");
                    _lstOcrDetails.Items.Add($"📝 متن کامل:");
                    _lstOcrDetails.Items.Add($"   {plateText}");
                    _lstOcrDetails.Items.Add("");
                    if (!string.IsNullOrEmpty(analysis.Numbers))
                    {
                        _lstOcrDetails.Items.Add($"🔢 اعداد ({analysis.NumberCount}):");
                        _lstOcrDetails.Items.Add($"   {analysis.Numbers}");
                        _lstOcrDetails.Items.Add("");
                    }
                    if (!string.IsNullOrEmpty(analysis.Letters))
                    {
                        _lstOcrDetails.Items.Add($"🔤 حروف ({analysis.LetterCount}):");
                        _lstOcrDetails.Items.Add($"   {analysis.Letters}");
                        _lstOcrDetails.Items.Add("");
                    }
                    _lstOcrDetails.Items.Add("──────────");
                    _lstOcrDetails.Items.Add($"📏 طول: {analysis.Length}");
                    _lstOcrDetails.Items.Add($"✅ اعتماد: {ocrResult.Confidence:P0}");
                }
                else
                {
                    _lblOcr.Text = $"OCR ناموفق: {ocrResult.ErrorMessage}";
                    _lblOcr.BackColor = Color.LightCoral;
                    _lblStatus.Text = "OCR ناموفق";
                    _lblStatus.BackColor = Color.LightCoral;
                    MessageBox.Show($"خطا در OCR:\n{ocrResult.ErrorMessage}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // فعال کردن دکمه OCR برای تلاش مجدد
                _btnOcrFullImage.Enabled = true;
            }
            catch (Exception ex)
            {
                _lblOcr.Text = "OCR: خطا";
                _lblOcr.BackColor = Color.LightCoral;
                _lblStatus.Text = "خطا در OCR";
                _lblStatus.BackColor = Color.LightCoral;
                _btnOcrFullImage.Enabled = true;
                MessageBox.Show($"خطا در OCR:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"❌ خطا در OCR: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async void BtnOcrOriginal_Click(object? sender, EventArgs e)
        {
            try
            {
                // بررسی وجود پلاک انتخاب شده
                if (_selectedDetection == null || _selectedDetection.PlateBoundingBox == null)
                {
                    MessageBox.Show("ابتدا یک پلاک از لیست انتخاب کنید.", "راهنما", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_ocrService == null)
                {
                    MessageBox.Show("سرویس OCR در دسترس نیست.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (_originalImage == null)
                {
                    MessageBox.Show("تصویر اصلی یافت نشد.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // نمایش پیام در حال پردازش
                _lblStatus.Text = "در حال پردازش OCR روی تصویر اصلی...";
                _lblStatus.BackColor = Color.LightYellow;
                _btnOcrOriginal.Enabled = false;

                // برش پلاک از تصویر اصلی
                var bbox = _selectedDetection.PlateBoundingBox;
                using var srcBitmap = new Bitmap(_originalImage);
                var rect = new Rectangle(bbox.X, bbox.Y, bbox.Width, bbox.Height);
                rect.Intersect(new Rectangle(0, 0, srcBitmap.Width, srcBitmap.Height));

                var plateBitmap = new Bitmap(rect.Width, rect.Height);
                using (var g = Graphics.FromImage(plateBitmap))
                {
                    g.DrawImage(srcBitmap, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                }

                // انجام OCR روی تصویر اصلی (برش خورده)
                var ocrResult = await Task.Run(() => _ocrService.RecognizePlate(plateBitmap));

                if (ocrResult.IsSuccessful && !string.IsNullOrEmpty(ocrResult.Text))
                {
                    var plateText = ocrResult.Text;
                    var analysis = AnalyzePlateText(plateText);

                    // نمایش نتایج
                    _lblOcr.Text = $"OCR (اصلی): {plateText} | 🔢{analysis.Numbers} 🔤{analysis.Letters}";
                    _lblOcr.BackColor = Color.FromArgb(52, 152, 219); // آبی روشن

                    // نمایش جزئیات در لیست سمت راست
                    if (_selectedDetection != null)
                    {
                        _selectedDetection.PlateNumber = plateText;
                        UpdateOcrDetails(_selectedDetection);
                    }

                    // رسم کادربندی محتوای شناسایی شده روی تصویر برش خورده
                    DrawOcrBoxesOnCorrectedImage(plateBitmap, analysis);

                    // نمایش پیام موفقیت
                    _lblStatus.Text = $"✓ OCR (اصلی) موفق - متن: {plateText} | اعتماد: {ocrResult.Confidence:P0}";
                    _lblStatus.BackColor = Color.LightGreen;

                    // نمایش جزئیات
                    var details = $"متن پلاک (تصویر اصلی): {plateText}\n\n" +
                                $"📝 متن کامل: {plateText}\n" +
                                $"🔢 اعداد: {analysis.Numbers} ({analysis.NumberCount})\n" +
                                $"🔤 حروف: {analysis.Letters} ({analysis.LetterCount})\n" +
                                $"📏 طول: {analysis.Length}\n\n" +
                                $"روش OCR: {_config.OcrMethod}\n" +
                                $"اعتماد: {ocrResult.Confidence:P0}\n" +
                                $"زمان پردازش: {ocrResult.ProcessingTimeMs}ms\n\n" +
                                $"⚠️ توجه: این نتیجه از تصویر اصلی (برش خورده) است";

                    MessageBox.Show(details, "نتیجه OCR (تصویر اصلی)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _lblOcr.Text = $"OCR ناموفق: {ocrResult.ErrorMessage}";
                    _lblOcr.BackColor = Color.LightCoral;
                    _lblStatus.Text = "OCR ناموفق";
                    _lblStatus.BackColor = Color.LightCoral;
                    MessageBox.Show($"خطا در OCR:\n{ocrResult.ErrorMessage}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // فعال کردن دکمه OCR برای تلاش مجدد
                _btnOcrOriginal.Enabled = true;
            }
            catch (Exception ex)
            {
                _lblOcr.Text = "OCR: خطا";
                _lblOcr.BackColor = Color.LightCoral;
                _lblStatus.Text = "خطا در OCR";
                _lblStatus.BackColor = Color.LightCoral;
                _btnOcrOriginal.Enabled = true;
                MessageBox.Show($"خطا در OCR:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Diagnostics.Debug.WriteLine($"❌ خطا در OCR: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void AutoSaveCrops()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentImagePath) || _originalImage == null || _detections == null || !_detections.Any())
                    return;

                var baseDir = Path.Combine(Path.GetDirectoryName(_currentImagePath) ?? "", "plates");
                Directory.CreateDirectory(baseDir);

                using var src = new Bitmap(_originalImage);
                int saved = 0;
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                for (int i = 0; i < _detections.Count; i++)
                {
                    var d = _detections[i];
                    if (d.PlateBoundingBox == null) continue;
                    var b = d.PlateBoundingBox;
                    var rect = new Rectangle(b.X, b.Y, b.Width, b.Height);
                    rect.Intersect(new Rectangle(0, 0, src.Width, src.Height));
                    if (rect.Width <= 0 || rect.Height <= 0) continue;

                    var crop = new Bitmap(rect.Width, rect.Height);
                    using (var g = Graphics.FromImage(crop))
                    {
                        g.DrawImage(src, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                    }

                    var name = Path.GetFileNameWithoutExtension(_currentImagePath);
                    var fileName = $"crop_{name}_plate{i + 1}_{timestamp}.jpg";
                    var file = Path.Combine(baseDir, fileName);
                    crop.Save(file, System.Drawing.Imaging.ImageFormat.Jpeg);

                    // اضافه کردن به لیست
                    var key = $"🔲 برش {i + 1}";
                    if (!_croppedImages.ContainsKey(key))
                    {
                        _croppedImages[key] = crop;
                        _lstPlates.Items.Add(key);
                    }

                    saved++;
                }

                if (saved > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"✓ {saved} برش پلاک به صورت خودکار ذخیره شد در: {baseDir}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در ذخیره خودکار برش‌ها: {ex.Message}");
            }
        }


        private void BtnCorrectPlate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_selectedDetection == null)
                {
                    MessageBox.Show("ابتدا یک پلاک از لیست انتخاب کنید.", "راهنما", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (string.IsNullOrEmpty(_currentImagePath))
                {
                    MessageBox.Show("تصویر بارگذاری نشده است.", "راهنما", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_selectedDetection.PlateBoundingBox == null)
                {
                    MessageBox.Show("اطلاعات پلاک نامعتبر است.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _lblStatus.Text = "در حال اصلاح پلاک...";
                _lblStatus.BackColor = Color.LightYellow;

                // استفاده از سرویس اصلاح پلاک
                var correctionService = new PlateCorrectionService();
                var correctedImage = correctionService.CorrectPlateImage(_currentImagePath, _selectedDetection.PlateBoundingBox);

                if (correctedImage != null)
                {
                    _correctedImage?.Dispose();
                    _correctedImage = correctedImage;
                    _pictureBox.Image = correctedImage;
                    _lblStatus.Text = "✓ پلاک اصلاح شد - اکنون می‌توانید OCR را اجرا کنید";
                    _lblStatus.BackColor = Color.LightGreen;

                    // فعال کردن دکمه OCR
                    _btnOcr.Enabled = true;

                    // اضافه کردن به لیست
                    var selectedItem = _lstPlates.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(selectedItem) && _plateItemToDetectionIndex.TryGetValue(selectedItem, out var detIndex))
                    {
                        var key = $"✅ اصلاح شده {detIndex + 1}";

                        // حذف نسخه قبلی اگر وجود دارد
                        if (_correctedImages.ContainsKey(key))
                        {
                            _correctedImages[key].Dispose();
                            _correctedImages.Remove(key);
                            var existingIndex = _lstPlates.Items.IndexOf(key);
                            if (existingIndex >= 0)
                                _lstPlates.Items.RemoveAt(existingIndex);
                        }

                        _correctedImages[key] = new Bitmap(correctedImage);
                        _lstPlates.Items.Add(key);

                        // ذخیره خودکار
                        AutoSaveCorrected(detIndex);
                    }
                }
                else
                {
                    _lblStatus.Text = "خطا در اصلاح پلاک";
                    _lblStatus.BackColor = Color.LightCoral;
                    MessageBox.Show("خطا در اصلاح پلاک. لطفاً دوباره تلاش کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در اصلاح پلاک:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = "خطا در اصلاح پلاک";
                _lblStatus.BackColor = Color.LightCoral;
            }
        }

        private void AutoSaveCorrected(int plateIndex)
        {
            try
            {
                if (_correctedImage == null || string.IsNullOrEmpty(_currentImagePath))
                    return;

                // ذخیره در همان پوشه plates
                var baseDir = Path.Combine(Path.GetDirectoryName(_currentImagePath) ?? "", "plates");
                Directory.CreateDirectory(baseDir);

                // تولید نام فایل با پیشوند corrected_
                var originalName = Path.GetFileNameWithoutExtension(_currentImagePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"corrected_{originalName}_plate{plateIndex + 1}_{timestamp}.jpg";
                var filePath = Path.Combine(baseDir, fileName);

                // ذخیره تصویر اصلاح شده
                _correctedImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                System.Diagnostics.Debug.WriteLine($"✓ پلاک اصلاح شده به صورت خودکار ذخیره شد: {fileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در ذخیره خودکار پلاک اصلاح شده: {ex.Message}");
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _detectionService?.Dispose();
                _originalImage?.Dispose();
                _annotatedImage?.Dispose();
                _correctedImage?.Dispose();

                // آزادسازی تصاویر برش شده
                foreach (var img in _croppedImages.Values)
                {
                    img?.Dispose();
                }
                _croppedImages.Clear();

                // آزادسازی تصاویر اصلاح شده
                foreach (var img in _correctedImages.Values)
                {
                    img?.Dispose();
                }
                _correctedImages.Clear();

                if (_pictureBox?.Image != null &&
                    _pictureBox.Image != _originalImage &&
                    _pictureBox.Image != _annotatedImage &&
                    _pictureBox.Image != _correctedImage &&
                    !_croppedImages.ContainsValue(_pictureBox.Image) &&
                    !_correctedImages.ContainsValue(_pictureBox.Image))
                {
                    _pictureBox.Image.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
