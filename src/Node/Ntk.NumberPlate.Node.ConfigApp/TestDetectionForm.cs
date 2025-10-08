using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ntk.NumberPlate.Node.ConfigApp.Services;
using Ntk.NumberPlate.Shared.Models;
using OpenCvSharp;

namespace Ntk.NumberPlate.Node.ConfigApp
{
    public partial class TestDetectionForm : Form
    {
        private readonly NodeConfiguration _config;
        private PlateDetectionTestService? _detectionService;

        private readonly GroupBox _grpImage;
        private readonly GroupBox _grpPlates;
        private readonly PictureBox _pictureBox;
        private readonly ListBox _lstPlates;
        private readonly Button _btnLoadImage;
        private readonly Button _btnDetect;
        private readonly Button _btnOcr;
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
            Size = new System.Drawing.Size(1100, 650);
            StartPosition = FormStartPosition.CenterScreen;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;

            _grpImage = new GroupBox
            {
                Text = "تصویر",
                Location = new System.Drawing.Point(220, 20),
                Size = new System.Drawing.Size(840, 520),
                Font = new Font("Tahoma", 9, FontStyle.Bold)
            };
            Controls.Add(_grpImage);

            _grpPlates = new GroupBox
            {
                Text = "لیست پلاک‌ها",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(190, 520),
                Font = new Font("Tahoma", 9, FontStyle.Bold)
            };
            Controls.Add(_grpPlates);

            _lstPlates = new ListBox
            {
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(170, 440),
                Font = new Font("Tahoma", 9),
                BackColor = Color.WhiteSmoke
            };
            _lstPlates.SelectedIndexChanged += LstPlates_SelectedIndexChanged;
            _grpPlates.Controls.Add(_lstPlates);

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

            _btnLoadImage = new Button
            {
                Text = "📁 انتخاب تصویر",
                Location = new System.Drawing.Point(10, 475),
                Size = new System.Drawing.Size(140, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold)
            };
            _btnLoadImage.Click += BtnLoadImage_Click;
            _grpImage.Controls.Add(_btnLoadImage);

            _btnDetect = new Button
            {
                Text = "🔍 تشخیص پلاک",
                Location = new System.Drawing.Point(160, 475),
                Size = new System.Drawing.Size(140, 30),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnDetect.Click += BtnDetect_Click;
            _grpImage.Controls.Add(_btnDetect);

            _btnOcr = new Button
            {
                Text = "🔤 شناسایی متن",
                Location = new System.Drawing.Point(310, 475),
                Size = new System.Drawing.Size(140, 30),
                BackColor = Color.FromArgb(142, 68, 173),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnOcr.Click += BtnOcr_Click;
            _grpImage.Controls.Add(_btnOcr);

            _btnCorrectPlate = new Button
            {
                Text = "🔧 اصلاح پلاک",
                Location = new System.Drawing.Point(460, 475),
                Size = new System.Drawing.Size(140, 30),
                BackColor = Color.FromArgb(230, 126, 34),
                ForeColor = Color.White,
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnCorrectPlate.Click += BtnCorrectPlate_Click;
            _grpImage.Controls.Add(_btnCorrectPlate);

            _lblStatus = new Label
            {
                Text = "آماده",
                Location = new System.Drawing.Point(220, 555),
                Size = new System.Drawing.Size(600, 25),
                BackColor = Color.LightGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_lblStatus);

            _lblOcr = new Label
            {
                Text = "OCR: -",
                Location = new System.Drawing.Point(840, 555),
                Size = new System.Drawing.Size(220, 25),
                BackColor = Color.Beige,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(_lblOcr);

            InitializeDetectionService();
        }

        private void InitializeDetectionService()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.YoloModelPath))
                {
                    _lblStatus.Text = "خطا: مسیر مدل YOLO خالی است";
                    _lblStatus.BackColor = Color.LightCoral;
                    return;
                }

                _detectionService = new PlateDetectionTestService(_config.YoloModelPath, _config.ConfidenceThreshold);
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
                    var itemText = $"پلاک {i + 1} - {detection.Confidence:P0}";
                    _lstPlates.Items.Add(itemText);
                    _plateItemToDetectionIndex[itemText] = i;
                }

                if (_detections.Any())
                {
                    _lblStatus.Text = $"✓ {_detections.Count} پلاک یافت شد (خودکار ذخیره شد)";
                    _lblStatus.BackColor = Color.LightGreen;
                    _btnOcr.Enabled = true;
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

                // نمایش پلاک انتخاب شده روی تصویر
                if (_originalImage != null && _selectedDetection?.PlateBoundingBox != null)
                {
                    _annotatedImage?.Dispose();
                    _annotatedImage = new Bitmap(_originalImage);

                    using (var g = Graphics.FromImage(_annotatedImage))
                    {
                        // رسم همه پلاک‌ها با رنگ قرمز
                        using (var penNormal = new Pen(Color.Red, 3))
                        {
                            foreach (var d in _detections!)
                            {
                                if (d.PlateBoundingBox == null) continue;
                                var b = d.PlateBoundingBox;
                                g.DrawRectangle(penNormal, b.X, b.Y, b.Width, b.Height);
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

        private void BtnOcr_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_detections == null || !_detections.Any())
                {
                    MessageBox.Show("ابتدا تشخیص پلاک را انجام دهید.", "راهنما", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var best = _detections.OrderByDescending(d => d.Confidence).First();
                var text = best.PlateNumber ?? "-";
                _lblOcr.Text = $"OCR: {text}";
                MessageBox.Show($"متن پلاک: {text}", "OCR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در OCR:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    _lblStatus.Text = "✓ پلاک اصلاح شد (خودکار ذخیره شد)";
                    _lblStatus.BackColor = Color.LightGreen;

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
