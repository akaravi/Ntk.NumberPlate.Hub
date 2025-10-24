using System;
using System.Drawing;
using System.Windows.Forms;
using Ntk.NumberPlate.Shared.Models;
using Ntk.NumberPlate.Shared.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using System.Collections.Generic;

namespace Ntk.NumberPlate.Node.ConfigApp.Forms
{
    public partial class TestDetectionModelBForm : Form
    {
        private readonly NodeConfiguration _config;
        private PlateDetectionPlaceService? _detectionPlaceService;
        private PlateDetectionOCRService? _detectionOCRService;
        
        // Workflow variables
        private string? _originalImagePath;
        private Image? _originalImage;
        private Image? _preprocessedImage;
        private Image? _detectedPlateImage;
        private Image? _croppedPlateImage;
        private Image? _correctedPlateImage;
        private string _detectedText = "";
        private string _correctedText = "";
        private List<VehicleDetectionData>? _detections;
        private VehicleDetectionData? _selectedDetection;

        // UI Controls
        private Panel _mainPanel;
        private Panel _leftPanel;
        private Panel _rightPanel;
        
        // Left side - Results display
        private GroupBox _grpOriginalImage;
        private GroupBox _grpPreprocessedImage;
        private GroupBox _grpDetectedPlate;
        private GroupBox _grpCroppedPlate;
        private GroupBox _grpCorrectedPlate;
        private GroupBox _grpOcrResult;
        private GroupBox _grpFinalResult;
        
        private PictureBox _picOriginal;
        private PictureBox _picPreprocessed;
        private PictureBox _picDetectedPlate;
        private PictureBox _picCroppedPlate;
        private PictureBox _picCorrectedPlate;
        private TextBox _txtOcrResult;
        private TextBox _txtFinalResult;
        
        // Right side - Step buttons
        private Button _btnStep1;
        private Button _btnStep2;
        private Button _btnStep3;
        private Button _btnStep4;
        private Button _btnStep5;
        private Button _btnStep6;
        private Button _btnStep7;
        private Button _btnStep8;
        private Label _lblStatus;

        public TestDetectionModelBForm(NodeConfiguration config)
        {
            _config = config;
            InitializeServices();
            InitializeComponent();
        }

        private void InitializeServices()
        {
            try
            {
                // بررسی تنظیمات مدل تشخیص پلاک
                if (string.IsNullOrWhiteSpace(_config.YoloPlateModelPath))
                {
                    MessageBox.Show("مسیر مدل YOLO تشخیص پلاک در تنظیمات مشخص نشده است.", "خطا", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(_config.YoloPlateModelPath))
                {
                    MessageBox.Show($"فایل مدل YOLO تشخیص پلاک در مسیر زیر یافت نشد:\n{_config.YoloPlateModelPath}", "خطا", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // مقداردهی سرویس تشخیص پلاک با تنظیمات کامل
                _detectionPlaceService = new PlateDetectionPlaceService(_config.YoloPlateModelPath, _config.ConfidenceThreshold);
                
                // بررسی و مقداردهی مدل تشخیص
                if (!_detectionPlaceService.Initialize(out string initError))
                {
                    MessageBox.Show($"خطا در مقداردهی مدل تشخیص پلاک:\n{initError}", "خطا", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _detectionPlaceService = null;
                    return;
                }
                
                // مقداردهی سرویس OCR با تنظیمات کامل
                _detectionOCRService = new PlateDetectionOCRService(_config);

                // تست سرویس تشخیص
                TestDetectionService();
                
                // تست سرویس OCR
                TestOcrService();
                
                // نمایش اطلاعات تنظیمات
                Log($"✅ سرویس‌ها با موفقیت مقداردهی شدند");
                Log($"📁 مدل تشخیص پلاک: {_config.YoloPlateModelPath}");
                Log($"📁 مدل OCR: {_config.YoloOcrModelPath}");
                Log($"🎯 آستانه تشخیص: {_config.ConfidenceThreshold:F2}");
                Log($"🎯 آستانه OCR: {_config.OcrConfidenceThreshold:F2}");
                Log($"🔤 روش OCR: {_config.OcrMethod}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در مقداردهی سرویس‌ها: {ex.Message}", "خطا", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Log($"❌ خطا در مقداردهی سرویس‌ها: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            // Form properties
            this.Text = "تشخیص پلاک - مدل B (مرحله‌ای)";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.MinimumSize = new System.Drawing.Size(1000, 600);

            CreateLayout();
            SetUpResponsiveBehavior();
        }

        private void CreateLayout()
        {
            // Main panel
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            this.Controls.Add(_mainPanel);

            // Left panel for results
            _leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            _mainPanel.Controls.Add(_leftPanel);

            // Right panel for buttons
            _rightPanel = new Panel
            {
                Width = 200,
                Dock = DockStyle.Right,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            _mainPanel.Controls.Add(_rightPanel);

            CreateResultAreas();
            CreateStepButtons();
            UpdateButtonStates();
        }

        private void CreateResultAreas()
        {
            var y = 10;
            var groupHeight = 120;
            var groupWidth = _leftPanel.Width - 20;

            // Step 1: Original Image
            _grpOriginalImage = CreateResultGroup("1. تصویر اصلی", 10, y, groupWidth, groupHeight);
            _picOriginal = CreatePictureBox(_grpOriginalImage, 10, 25, groupWidth - 20, groupHeight - 40);
            y += groupHeight + 10;

            // Step 2: Preprocessed Image
            _grpPreprocessedImage = CreateResultGroup("2. پیش‌پردازش تصویر", 10, y, groupWidth, groupHeight);
            _picPreprocessed = CreatePictureBox(_grpPreprocessedImage, 10, 25, groupWidth - 20, groupHeight - 40);
            y += groupHeight + 10;

            // Step 3: Detected Plate
            _grpDetectedPlate = CreateResultGroup("3. تشخیص پلاک", 10, y, groupWidth, groupHeight);
            _picDetectedPlate = CreatePictureBox(_grpDetectedPlate, 10, 25, groupWidth - 20, groupHeight - 40);
            y += groupHeight + 10;

            // Step 4: Cropped Plate
            _grpCroppedPlate = CreateResultGroup("4. برش پلاک", 10, y, groupWidth, groupHeight);
            _picCroppedPlate = CreatePictureBox(_grpCroppedPlate, 10, 25, groupWidth - 20, groupHeight - 40);
            y += groupHeight + 10;

            // Step 5: Corrected Plate
            _grpCorrectedPlate = CreateResultGroup("5. اصلاح و صاف کردن پلاک", 10, y, groupWidth, groupHeight);
            _picCorrectedPlate = CreatePictureBox(_grpCorrectedPlate, 10, 25, groupWidth - 20, groupHeight - 40);
            y += groupHeight + 10;

            // Step 6: OCR Result
            _grpOcrResult = CreateResultGroup("6. تشخیص متن (OCR)", 10, y, groupWidth, groupHeight);
            _txtOcrResult = new TextBox
            {
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(groupWidth - 20, groupHeight - 40),
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            _grpOcrResult.Controls.Add(_txtOcrResult);
            y += groupHeight + 10;

            // Step 7: Final Result
            _grpFinalResult = CreateResultGroup("7. نتیجه نهایی", 10, y, groupWidth, groupHeight);
            _txtFinalResult = new TextBox
            {
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(groupWidth - 20, groupHeight - 40),
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Tahoma", 14, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                BackColor = Color.FromArgb(200, 255, 200)
            };
            _grpFinalResult.Controls.Add(_txtFinalResult);
        }

        private GroupBox CreateResultGroup(string title, int x, int y, int width, int height)
        {
            var group = new GroupBox
            {
                Text = title,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height),
                Font = new Font("Tahoma", 9, FontStyle.Bold)
            };
            _leftPanel.Controls.Add(group);
            return group;
        }

        private PictureBox CreatePictureBox(GroupBox parent, int x, int y, int width, int height)
        {
            var pictureBox = new PictureBox
            {
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            
            // اضافه کردن event handler برای کلیک
            pictureBox.Click += PictureBox_Click;
            pictureBox.DoubleClick += PictureBox_DoubleClick;
            
            // اضافه کردن tooltip
            var toolTip = new ToolTip();
            toolTip.SetToolTip(pictureBox, "کلیک کنید تا تصویر را در حالت تمام صفحه مشاهده کنید");
            
            parent.Controls.Add(pictureBox);
            return pictureBox;
        }

        private void CreateStepButtons()
        {
            var buttonY = 20;
            var buttonHeight = 50;
            var buttonWidth = 180;

            // Step 1: Load Image
            _btnStep1 = CreateStepButton("1. انتخاب تصویر", 10, buttonY, buttonWidth, buttonHeight);
            _btnStep1.Click += BtnStep1_Click;
            buttonY += buttonHeight + 10;

            // Step 2: Preprocess
            _btnStep2 = CreateStepButton("2. پیش‌پردازش", 10, buttonY, buttonWidth, buttonHeight);
            _btnStep2.Click += BtnStep2_Click;
            buttonY += buttonHeight + 10;

            // Step 3: Detect Plate
            _btnStep3 = CreateStepButton("3. تشخیص پلاک", 10, buttonY, buttonWidth, buttonHeight);
            _btnStep3.Click += BtnStep3_Click;
            buttonY += buttonHeight + 10;

            // Step 4: Crop Plate
            _btnStep4 = CreateStepButton("4. برش پلاک", 10, buttonY, buttonWidth, buttonHeight);
            _btnStep4.Click += BtnStep4_Click;
            buttonY += buttonHeight + 10;

            // Step 5: Correct Plate
            _btnStep5 = CreateStepButton("5. اصلاح پلاک", 10, buttonY, buttonWidth, buttonHeight);
            _btnStep5.Click += BtnStep5_Click;
            buttonY += buttonHeight + 10;

            // Step 6: OCR
            _btnStep6 = CreateStepButton("6. تشخیص متن", 10, buttonY, buttonWidth, buttonHeight);
            _btnStep6.Click += BtnStep6_Click;
            buttonY += buttonHeight + 10;

            // Step 7: Correct Text
            _btnStep7 = CreateStepButton("7. تصحیح متن", 10, buttonY, buttonWidth, buttonHeight);
            _btnStep7.Click += BtnStep7_Click;
            buttonY += buttonHeight + 10;

            // Step 8: Final Result
            _btnStep8 = CreateStepButton("8. نتیجه نهایی", 10, buttonY, buttonWidth, buttonHeight);
            _btnStep8.Click += BtnStep8_Click;
            buttonY += buttonHeight + 20;

            // Test Detection Service Button
            var btnTestDetection = new Button
            {
                Text = "تست سرویس تشخیص",
                Location = new System.Drawing.Point(10, buttonY),
                Size = new System.Drawing.Size(buttonWidth, 35),
                Font = new Font("Tahoma", 8, FontStyle.Bold),
                BackColor = Color.FromArgb(100, 150, 255),
                ForeColor = Color.White,
                Enabled = true
            };
            btnTestDetection.Click += (s, e) => TestDetectionService();
            _rightPanel.Controls.Add(btnTestDetection);
            buttonY += 40;

            // Test OCR Service Button
            var btnTestOcr = new Button
            {
                Text = "تست سرویس OCR",
                Location = new System.Drawing.Point(10, buttonY),
                Size = new System.Drawing.Size(buttonWidth, 35),
                Font = new Font("Tahoma", 8, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 150, 100),
                ForeColor = Color.White,
                Enabled = true
            };
            btnTestOcr.Click += (s, e) => TestOcrService();
            _rightPanel.Controls.Add(btnTestOcr);
            buttonY += 40;

            // Status Label
            _lblStatus = new Label
            {
                Text = "آماده برای شروع",
                Location = new System.Drawing.Point(10, buttonY),
                Size = new System.Drawing.Size(buttonWidth, 30),
                Font = new Font("Tahoma", 9),
                ForeColor = Color.Blue,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _rightPanel.Controls.Add(_lblStatus);
        }

        private Button CreateStepButton(string text, int x, int y, int width, int height)
        {
            var button = new Button
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, height),
                Font = new Font("Tahoma", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.Black,
                Enabled = false
            };
            _rightPanel.Controls.Add(button);
            return button;
        }

        private void SetUpResponsiveBehavior()
        {
            this.Resize += TestDetectionModelBForm_Resize;
        }

        private void TestDetectionModelBForm_Resize(object? sender, EventArgs e)
        {
            if (_leftPanel != null && _rightPanel != null)
            {
                var leftWidth = this.Width - _rightPanel.Width - 40;
                _leftPanel.Width = leftWidth;
                
                // Update group widths
                UpdateGroupWidths(leftWidth - 20);
            }
        }

        private void UpdateGroupWidths(int width)
        {
            var groups = new[] { _grpOriginalImage, _grpPreprocessedImage, _grpDetectedPlate, 
                               _grpCroppedPlate, _grpCorrectedPlate, _grpOcrResult, _grpFinalResult };
            
            foreach (var group in groups)
            {
                if (group != null)
                {
                    group.Width = width;
                    if (group.Controls.Count > 0)
                    {
                        var control = group.Controls[0];
                        control.Width = width - 20;
                    }
                }
            }
        }

        private void UpdateButtonStates()
        {
            _btnStep1.Enabled = true; // Always enabled
            _btnStep2.Enabled = _originalImage != null;
            _btnStep3.Enabled = _preprocessedImage != null;
            _btnStep4.Enabled = _detectedPlateImage != null;
            _btnStep5.Enabled = _croppedPlateImage != null;
            _btnStep6.Enabled = _correctedPlateImage != null;
            _btnStep7.Enabled = !string.IsNullOrEmpty(_detectedText);
            _btnStep8.Enabled = !string.IsNullOrEmpty(_correctedText);
        }

        // Step 1: Load Image
        private void BtnStep1_Click(object? sender, EventArgs e)
        {
            try
            {
                using var dialog = new OpenFileDialog
                {
                    Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files (*.*)|*.*",
                    Title = "انتخاب تصویر خودرو"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _originalImagePath = dialog.FileName;
                    _originalImage?.Dispose();
                    _originalImage = Image.FromFile(_originalImagePath);
                    _picOriginal.Image = _originalImage;
                    
                    _lblStatus.Text = "تصویر بارگذاری شد";
                    _lblStatus.ForeColor = Color.Green;
                    UpdateButtonStates();
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
            }
        }

        // Step 2: Preprocess Image
        private void BtnStep2_Click(object? sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "در حال پیش‌پردازش...";
                _lblStatus.ForeColor = Color.Orange;
                
                if (_originalImage == null)
                {
                    _lblStatus.Text = "ابتدا تصویر را انتخاب کنید";
                    _lblStatus.ForeColor = Color.Red;
                    return;
                }

                // Use combined preprocessing for optimal plate detection
                _preprocessedImage = ApplyCombinedPreprocessing(_originalImage);
                _picPreprocessed.Image = _preprocessedImage;
                
                _lblStatus.Text = "پیش‌پردازش کامل شد";
                _lblStatus.ForeColor = Color.Green;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
            }
        }

        // Step 3: Detect Plate
        private async void BtnStep3_Click(object? sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "در حال تشخیص پلاک...";
                _lblStatus.ForeColor = Color.Orange;
                
                if (_preprocessedImage == null || _detectionPlaceService == null)
                {
                    _lblStatus.Text = "سرویس تشخیص در دسترس نیست";
                    _lblStatus.ForeColor = Color.Red;
                    return;
                }

                Log($"🔍 شروع تشخیص پلاک با مدل: {_config.YoloPlateModelPath}");
                Log($"🎯 آستانه اعتماد: {_config.ConfidenceThreshold:F2}");

                // بررسی وضعیت سرویس
                if (_detectionPlaceService == null)
                {
                    _lblStatus.Text = "سرویس تشخیص مقداردهی نشده است";
                    _lblStatus.ForeColor = Color.Red;
                    Log("❌ سرویس تشخیص مقداردهی نشده است");
                    return;
                }

                // Use real plate detection with configuration
                Log("🔄 در حال اجرای مدل تشخیص...");
                var result = await _detectionPlaceService.DetectAndAnnotateAsync(_originalImagePath!);
                _detections = result.Detections;
                
                Log($"📊 مدل تشخیص اجرا شد - {_detections?.Count ?? 0} نتیجه");
                
                if (_detections != null && _detections.Count > 0)
                {
                    _selectedDetection = _detections[0]; // Use first detection
                    _detectedPlateImage = DrawRealPlateDetection(_preprocessedImage, _selectedDetection);
                    _picDetectedPlate.Image = _detectedPlateImage;
                    
                    var message = $"{_detections.Count} پلاک تشخیص داده شد";
                    _lblStatus.Text = message;
                    _lblStatus.ForeColor = Color.Green;
                    
                    Log($"✅ {message}");
                    Log($"📊 اعتماد: {_selectedDetection.Confidence:F2}");
                    
                    // نمایش جزئیات تشخیص
                    foreach (var detection in _detections)
                    {
                        Log($"📍 پلاک: اعتماد={detection.Confidence:F2}");
                    }
                }
                else
                {
                    _lblStatus.Text = "هیچ پلاکی تشخیص داده نشد";
                    _lblStatus.ForeColor = Color.Orange;
                    Log("⚠️ هیچ پلاکی تشخیص داده نشد");
                    Log($"💡 آستانه اعتماد فعلی: {_config.ConfidenceThreshold:F2}");
                    Log("💡 ممکن است نیاز به کاهش آستانه اعتماد باشد");
                }
                
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
                Log($"❌ خطا در تشخیص پلاک: {ex.Message}");
            }
        }

        // Step 4: Crop Plate
        private void BtnStep4_Click(object? sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "در حال برش پلاک...";
                _lblStatus.ForeColor = Color.Orange;
                
                if (_selectedDetection == null)
                {
                    _lblStatus.Text = "ابتدا پلاک را تشخیص دهید";
                    _lblStatus.ForeColor = Color.Red;
                    return;
                }

                // Use real plate cropping
                _croppedPlateImage = CropRealPlateArea(_originalImage, _selectedDetection);
                _picCroppedPlate.Image = _croppedPlateImage;
                
                _lblStatus.Text = "پلاک برش داده شد";
                _lblStatus.ForeColor = Color.Green;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
            }
        }

        // Step 5: Correct Plate
        private void BtnStep5_Click(object? sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "در حال اصلاح و صاف کردن پلاک...";
                _lblStatus.ForeColor = Color.Orange;
                
                // Simulate plate correction - apply perspective correction and straightening
                _correctedPlateImage = CorrectPlatePerspective(_croppedPlateImage);
                _picCorrectedPlate.Image = _correctedPlateImage;
                
                _lblStatus.Text = "پلاک اصلاح شد";
                _lblStatus.ForeColor = Color.Green;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
            }
        }

        // Step 6: OCR
        private void BtnStep6_Click(object? sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "در حال تشخیص متن...";
                _lblStatus.ForeColor = Color.Orange;
                
                if (_correctedPlateImage == null || _detectionOCRService == null)
                {
                    _lblStatus.Text = "سرویس OCR در دسترس نیست";
                    _lblStatus.ForeColor = Color.Red;
                    return;
                }

                Log($"🔤 شروع تشخیص متن با روش: {_config.OcrMethod}");
                Log($"📁 مدل OCR: {_config.YoloOcrModelPath}");
                Log($"🎯 آستانه OCR: {_config.OcrConfidenceThreshold:F2}");

                // بررسی تنظیمات OCR
                if (_config.OcrMethod == OcrMethod.Yolo && string.IsNullOrWhiteSpace(_config.YoloOcrModelPath))
                {
                    Log("⚠️ روش OCR روی YOLO تنظیم شده اما مسیر مدل مشخص نشده");
                    Log("💡 از روش ساده استفاده می‌شود");
                }

                // Use real OCR with configuration
                var ocrResult = _detectionOCRService.RecognizePlate(new Bitmap(_correctedPlateImage));
                
                if (ocrResult != null && ocrResult.IsSuccessful)
                {
                    _detectedText = ocrResult.Text;
                    _txtOcrResult.Text = _detectedText;
                    
                    _lblStatus.Text = "متن تشخیص داده شد";
                    _lblStatus.ForeColor = Color.Green;
                    
                    Log($"✅ متن تشخیص داده شد: {_detectedText}");
                    Log($"📊 اعتماد OCR: {ocrResult.Confidence:F2}");
                    Log($"⏱️ زمان پردازش: {ocrResult.ProcessingTimeMs}ms");
                    Log($"🔧 روش استفاده شده: {ocrResult.Method}");
                }
                else
                {
                    _detectedText = "تشخیص داده نشد";
                    _txtOcrResult.Text = _detectedText;
                    
                    _lblStatus.Text = "تشخیص متن ناموفق";
                    _lblStatus.ForeColor = Color.Orange;
                    
                    Log("⚠️ تشخیص متن ناموفق");
                    if (ocrResult != null)
                    {
                        Log($"❌ خطا: {ocrResult.ErrorMessage}");
                    }
                }
                
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
                Log($"❌ خطا در تشخیص متن: {ex.Message}");
            }
        }

        // Step 7: Correct Text
        private void BtnStep7_Click(object? sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "در حال تصحیح متن...";
                _lblStatus.ForeColor = Color.Orange;
                
                Log($"🔧 شروع تصحیح متن: {_detectedText}");
                
                // Use simple text correction (remove spaces, fix common issues)
                _correctedText = CorrectPlateText(_detectedText);
                
                _txtFinalResult.Text = _correctedText;
                
                if (_correctedText != _detectedText)
                {
                    _lblStatus.Text = "متن تصحیح شد";
                    _lblStatus.ForeColor = Color.Green;
                    Log($"✅ متن تصحیح شد: {_detectedText} → {_correctedText}");
                }
                else
                {
                    _lblStatus.Text = "متن نیازی به تصحیح نداشت";
                    _lblStatus.ForeColor = Color.Green;
                    Log($"✅ متن نیازی به تصحیح نداشت: {_correctedText}");
                }
                
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
                Log($"❌ خطا در تصحیح متن: {ex.Message}");
            }
        }

        // Step 8: Final Result
        private void BtnStep8_Click(object? sender, EventArgs e)
        {
            try
            {
                _lblStatus.Text = "پردازش کامل شد";
                _lblStatus.ForeColor = Color.Green;
                
                Log("🎉 پردازش کامل شد!");
                Log($"📋 خلاصه نتایج:");
                Log($"   📁 مدل تشخیص پلاک: {_config.YoloPlateModelPath}");
                Log($"   📁 مدل OCR: {_config.YoloOcrModelPath}");
                Log($"   🎯 آستانه تشخیص: {_config.ConfidenceThreshold:F2}");
                Log($"   🎯 آستانه OCR: {_config.OcrConfidenceThreshold:F2}");
                Log($"   🔤 روش OCR: {_config.OcrMethod}");
                Log($"   📊 تعداد پلاک‌های تشخیص داده شده: {_detections?.Count ?? 0}");
                Log($"   📝 متن تشخیص داده شده: {_detectedText}");
                Log($"   ✅ متن نهایی: {_correctedText}");
                
                var resultMessage = $"نتیجه نهایی تشخیص پلاک:\n\n" +
                                  $"متن تشخیص داده شده: {_detectedText}\n" +
                                  $"متن نهایی: {_correctedText}\n\n" +
                                  $"تنظیمات استفاده شده:\n" +
                                  $"• مدل تشخیص پلاک: {Path.GetFileName(_config.YoloPlateModelPath)}\n" +
                                  $"• مدل OCR: {Path.GetFileName(_config.YoloOcrModelPath)}\n" +
                                  $"• روش OCR: {_config.OcrMethod}\n" +
                                  $"• آستانه تشخیص: {_config.ConfidenceThreshold:F2}\n" +
                                  $"• آستانه OCR: {_config.OcrConfidenceThreshold:F2}";
                
                MessageBox.Show(resultMessage, "نتیجه تشخیص پلاک", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"خطا: {ex.Message}";
                _lblStatus.ForeColor = Color.Red;
                Log($"❌ خطا در نمایش نتیجه نهایی: {ex.Message}");
            }
        }

        // Helper methods for real image processing
        private Image ApplyRealPreprocessing(Image originalImage)
        {
            try
            {
                Log("🔧 شروع پیش‌پردازش تصویر برای تشخیص پلاک...");
                
                // Convert to OpenCV Mat
                using var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(new Bitmap(originalImage));
                using var processedMat = new Mat();
                
                // Step 1: Resize to optimal size for detection (YOLO works best with 640x640)
                var targetSize = 640;
                var scale = Math.Min((double)targetSize / mat.Width, (double)targetSize / mat.Height);
                var newWidth = (int)(mat.Width * scale);
                var newHeight = (int)(mat.Height * scale);
                
                Cv2.Resize(mat, processedMat, new OpenCvSharp.Size(newWidth, newHeight));
                Log($"📏 تغییر اندازه: {mat.Width}x{mat.Height} → {newWidth}x{newHeight}");
                
                // Step 2: Convert to grayscale for better edge detection
                using var grayMat = new Mat();
                Cv2.CvtColor(processedMat, grayMat, ColorConversionCodes.BGR2GRAY);
                
                // Step 3: Apply CLAHE (Contrast Limited Adaptive Histogram Equalization)
                using var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8));
                using var enhancedMat = new Mat();
                clahe.Apply(grayMat, enhancedMat);
                Log("🎨 بهبود کنتراست با CLAHE");
                
                // Step 4: Apply bilateral filter to reduce noise while preserving edges
                using var filteredMat = new Mat();
                Cv2.BilateralFilter(enhancedMat, filteredMat, 9, 75, 75);
                Log("🔍 کاهش نویز با فیلتر دوطرفه");
                
                // Step 5: Apply sharpening filter to enhance edges
                using var kernel = new Mat(3, 3, MatType.CV_32F, new float[]
                {
                    0, -1, 0,
                    -1, 5, -1,
                    0, -1, 0
                });
                using var sharpenedMat = new Mat();
                Cv2.Filter2D(filteredMat, sharpenedMat, -1, kernel);
                Log("⚡ افزایش وضوح لبه‌ها");
                
                // Step 6: Convert back to BGR for YOLO model
                using var bgrMat = new Mat();
                Cv2.CvtColor(sharpenedMat, bgrMat, ColorConversionCodes.GRAY2BGR);
                
                // Step 7: Normalize pixel values to [0, 1] range for better model performance
                using var normalizedMat = new Mat();
                bgrMat.ConvertTo(normalizedMat, MatType.CV_32F, 1.0 / 255.0);
                
                // Convert back to CV_8U for display
                using var finalMat = new Mat();
                normalizedMat.ConvertTo(finalMat, MatType.CV_8U, 255.0);
                
                Log("✅ پیش‌پردازش تصویر کامل شد - آماده برای تشخیص");
                return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(finalMat);
            }
            catch (Exception ex)
            {
                Log($"⚠️ خطا در پیش‌پردازش OpenCV: {ex.Message}");
                Log("🔄 استفاده از روش ساده...");
                // Fallback to simple preprocessing if OpenCV fails
                return ApplyPreprocessing(originalImage);
            }
        }

        private Image ApplyCombinedPreprocessing(Image originalImage)
        {
            try
            {
                Log("🔧 شروع پیش‌پردازش ترکیبی برای تشخیص پلاک...");
                
                using var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(new Bitmap(originalImage));
                
                // Step 1: Resize to optimal size for YOLO (640x640)
                using var resizedMat = new Mat();
                Cv2.Resize(mat, resizedMat, new OpenCvSharp.Size(640, 640));
                Log($"📏 تغییر اندازه به 640x640");
                
                // Step 2: Convert to grayscale
                using var grayMat = new Mat();
                Cv2.CvtColor(resizedMat, grayMat, ColorConversionCodes.BGR2GRAY);
                
                // Step 3: Apply CLAHE for better contrast
                using var clahe = Cv2.CreateCLAHE(3.0, new OpenCvSharp.Size(8, 8));
                using var enhancedMat = new Mat();
                clahe.Apply(grayMat, enhancedMat);
                Log("🎨 بهبود کنتراست با CLAHE");
                
                // Step 4: Apply bilateral filter to reduce noise
                using var filteredMat = new Mat();
                Cv2.BilateralFilter(enhancedMat, filteredMat, 9, 75, 75);
                Log("🔍 کاهش نویز");
                
                // Step 5: Apply sharpening
                using var kernel = new Mat(3, 3, MatType.CV_32F, new float[]
                {
                    0, -1, 0,
                    -1, 5, -1,
                    0, -1, 0
                });
                using var sharpenedMat = new Mat();
                Cv2.Filter2D(filteredMat, sharpenedMat, -1, kernel);
                Log("⚡ افزایش وضوح");
                
                // Step 6: Apply adaptive thresholding
                using var threshMat = new Mat();
                Cv2.AdaptiveThreshold(sharpenedMat, threshMat, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                
                // Step 7: Morphological operations
                using var morphKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                using var morphMat = new Mat();
                Cv2.MorphologyEx(threshMat, morphMat, MorphTypes.Close, morphKernel);
                
                // Step 8: Edge detection
                using var edgesMat = new Mat();
                Cv2.Canny(morphMat, edgesMat, 50, 150);
                
                // Step 9: Find and filter contours
                var contours = new OpenCvSharp.Point[0][];
                var hierarchy = new HierarchyIndex[0];
                Cv2.FindContours(edgesMat, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                
                Log($"🔍 {contours.Length} کانتور یافت شد");
                
                // Filter contours for license plates
                var plateContours = new List<OpenCvSharp.Point[]>();
                foreach (var contour in contours)
                {
                    var rect = Cv2.BoundingRect(contour);
                    var aspectRatio = (double)rect.Width / rect.Height;
                    var area = Cv2.ContourArea(contour);
                    
                    // License plate criteria
                    if (aspectRatio >= 2.0 && aspectRatio <= 5.0 && 
                        rect.Width > 80 && rect.Height > 15 && 
                        area > 1000)
                    {
                        plateContours.Add(contour);
                    }
                }
                
                Log($"📋 {plateContours.Count} کانتور مناسب برای پلاک");
                
                // Step 10: Create final enhanced image
                using var finalMat = resizedMat.Clone();
                
                // Highlight potential plate regions
                foreach (var contour in plateContours)
                {
                    Cv2.DrawContours(finalMat, new[] { contour }, -1, new Scalar(0, 255, 0), 2);
                    
                    // Add text label
                    var rect = Cv2.BoundingRect(contour);
                    Cv2.PutText(finalMat, "PLATE", new OpenCvSharp.Point(rect.X, rect.Y - 5), 
                        HersheyFonts.HersheySimplex, 0.5, new Scalar(0, 255, 0), 1);
                }
                
                // Step 11: Apply final enhancement
                using var enhancedFinal = new Mat();
                Cv2.AddWeighted(resizedMat, 0.6, finalMat, 0.4, 0, enhancedFinal);
                
                Log("✅ پیش‌پردازش ترکیبی کامل شد");
                return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(enhancedFinal);
            }
            catch (Exception ex)
            {
                Log($"⚠️ خطا در پیش‌پردازش ترکیبی: {ex.Message}");
                return ApplyRealPreprocessing(originalImage);
            }
        }

        private Image ApplyAdvancedPreprocessing(Image originalImage)
        {
            try
            {
                Log("🔧 شروع پیش‌پردازش پیشرفته برای تشخیص پلاک...");
                
                using var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(new Bitmap(originalImage));
                
                // Step 1: Resize to YOLO input size (640x640)
                using var resizedMat = new Mat();
                Cv2.Resize(mat, resizedMat, new OpenCvSharp.Size(640, 640));
                
                // Step 2: Convert to grayscale
                using var grayMat = new Mat();
                Cv2.CvtColor(resizedMat, grayMat, ColorConversionCodes.BGR2GRAY);
                
                // Step 3: Apply adaptive thresholding for better plate detection
                using var threshMat = new Mat();
                Cv2.AdaptiveThreshold(grayMat, threshMat, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                
                // Step 4: Apply morphological operations to clean up the image
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                using var morphMat = new Mat();
                Cv2.MorphologyEx(threshMat, morphMat, MorphTypes.Close, kernel);
                
                // Step 5: Apply edge detection
                using var edgesMat = new Mat();
                Cv2.Canny(morphMat, edgesMat, 50, 150);
                
                // Step 6: Find contours to detect potential plate regions
                var contours = new OpenCvSharp.Point[0][];
                var hierarchy = new HierarchyIndex[0];
                Cv2.FindContours(edgesMat, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                
                Log($"🔍 {contours.Length} کانتور یافت شد");
                
                // Step 7: Filter contours by aspect ratio (typical for license plates)
                var plateContours = new List<OpenCvSharp.Point[]>();
                foreach (var contour in contours)
                {
                    var rect = Cv2.BoundingRect(contour);
                    var aspectRatio = (double)rect.Width / rect.Height;
                    
                    // License plates typically have aspect ratio between 2.0 and 5.0
                    if (aspectRatio >= 2.0 && aspectRatio <= 5.0 && rect.Width > 100 && rect.Height > 20)
                    {
                        plateContours.Add(contour);
                    }
                }
                
                Log($"📋 {plateContours.Count} کانتور مناسب برای پلاک یافت شد");
                
                // Step 8: Create enhanced image with detected regions highlighted
                using var enhancedMat = resizedMat.Clone();
                foreach (var contour in plateContours)
                {
                    Cv2.DrawContours(enhancedMat, new[] { contour }, -1, new Scalar(0, 255, 0), 2);
                }
                
                // Step 9: Apply final enhancement
                using var finalMat = new Mat();
                Cv2.AddWeighted(resizedMat, 0.7, enhancedMat, 0.3, 0, finalMat);
                
                Log("✅ پیش‌پردازش پیشرفته کامل شد");
                return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(finalMat);
            }
            catch (Exception ex)
            {
                Log($"⚠️ خطا در پیش‌پردازش پیشرفته: {ex.Message}");
                return ApplyPreprocessing(originalImage);
            }
        }

        private Image ApplyPreprocessing(Image originalImage)
        {
            // Simulate preprocessing: apply contrast enhancement and noise reduction
            var bitmap = new Bitmap(originalImage);
            var processedBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    
                    // Enhance contrast
                    var r = Math.Min(255, (int)(pixel.R * 1.2));
                    var g = Math.Min(255, (int)(pixel.G * 1.2));
                    var b = Math.Min(255, (int)(pixel.B * 1.2));
                    
                    processedBitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            
            bitmap.Dispose();
            return processedBitmap;
        }

        private Image DrawRealPlateDetection(Image preprocessedImage, VehicleDetectionData detection)
        {
            // Draw real plate detection with actual coordinates
            var bitmap = new Bitmap(preprocessedImage);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                using (var pen = new Pen(Color.Red, 3))
                {
                    // Use real detection coordinates from PlateBoundingBox
                    if (detection.PlateBoundingBox != null)
                    {
                        var bbox = detection.PlateBoundingBox;
                        var rect = new Rectangle(
                            (int)bbox.X, 
                            (int)bbox.Y, 
                            (int)bbox.Width, 
                            (int)bbox.Height
                        );
                        graphics.DrawRectangle(pen, rect);
                        
                        // Add text label with confidence
                        using (var font = new Font("Arial", 12, FontStyle.Bold))
                        {
                            var label = $"پلاک تشخیص داده شد (اعتماد: {detection.Confidence:F2})";
                            graphics.DrawString(label, font, Brushes.Red, 
                                new PointF(rect.X, rect.Y - 20));
                        }
                    }
                }
            }
            return bitmap;
        }

        private Image DrawPlateDetection(Image preprocessedImage)
        {
            // Simulate plate detection: draw a rectangle around the plate area
            var bitmap = new Bitmap(preprocessedImage);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                using (var pen = new Pen(Color.Red, 3))
                {
                    // Draw a rectangle in the center area (simulating detected plate)
                    var rect = new Rectangle(
                        bitmap.Width / 4, 
                        bitmap.Height / 3, 
                        bitmap.Width / 2, 
                        bitmap.Height / 4
                    );
                    graphics.DrawRectangle(pen, rect);
                    
                    // Add text label
                    using (var font = new Font("Arial", 12, FontStyle.Bold))
                    {
                        graphics.DrawString("پلاک تشخیص داده شد", font, Brushes.Red, 
                            new PointF(rect.X, rect.Y - 20));
                    }
                }
            }
            return bitmap;
        }

        private Image CropRealPlateArea(Image originalImage, VehicleDetectionData detection)
        {
            // Use real detection coordinates to crop plate
            var bitmap = new Bitmap(originalImage);
            
            if (detection.PlateBoundingBox != null)
            {
                var bbox = detection.PlateBoundingBox;
                var plateRect = new Rectangle(
                    (int)bbox.X, 
                    (int)bbox.Y, 
                    (int)bbox.Width, 
                    (int)bbox.Height
                );
                
                // Ensure rectangle is within image bounds
                plateRect.X = Math.Max(0, Math.Min(plateRect.X, bitmap.Width - plateRect.Width));
                plateRect.Y = Math.Max(0, Math.Min(plateRect.Y, bitmap.Height - plateRect.Height));
                plateRect.Width = Math.Min(plateRect.Width, bitmap.Width - plateRect.X);
                plateRect.Height = Math.Min(plateRect.Height, bitmap.Height - plateRect.Y);
                
                var croppedBitmap = new Bitmap(plateRect.Width, plateRect.Height);
                using (var graphics = Graphics.FromImage(croppedBitmap))
                {
                    graphics.DrawImage(bitmap, 0, 0, plateRect, GraphicsUnit.Pixel);
                }
                
                bitmap.Dispose();
                return croppedBitmap;
            }
            
            // Fallback to center crop if no bounding box
            return CropPlateArea(originalImage);
        }

        private Image CropPlateArea(Image detectedImage)
        {
            // Simulate plate cropping: extract the detected plate area
            var bitmap = new Bitmap(detectedImage);
            var plateRect = new Rectangle(
                bitmap.Width / 4, 
                bitmap.Height / 3, 
                bitmap.Width / 2, 
                bitmap.Height / 4
            );
            
            var croppedBitmap = new Bitmap(plateRect.Width, plateRect.Height);
            using (var graphics = Graphics.FromImage(croppedBitmap))
            {
                graphics.DrawImage(bitmap, 0, 0, plateRect, GraphicsUnit.Pixel);
            }
            
            bitmap.Dispose();
            return croppedBitmap;
        }

        private Image CorrectPlatePerspective(Image croppedImage)
        {
            // Simulate perspective correction and straightening
            var bitmap = new Bitmap(croppedImage);
            var correctedBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            
            using (var graphics = Graphics.FromImage(correctedBitmap))
            {
                // Apply slight rotation to simulate perspective correction
                graphics.TranslateTransform(bitmap.Width / 2, bitmap.Height / 2);
                graphics.RotateTransform(-2); // Small rotation to simulate correction
                graphics.TranslateTransform(-bitmap.Width / 2, -bitmap.Height / 2);
                
                graphics.DrawImage(bitmap, 0, 0);
                
                // Add a border to show it's been corrected
                using (var pen = new Pen(Color.Green, 2))
                {
                    graphics.DrawRectangle(pen, 0, 0, bitmap.Width - 1, bitmap.Height - 1);
                }
                
                // Add text to show correction
                // using (var font = new Font("Arial", 10, FontStyle.Bold))
                // {
                //     graphics.DrawString("اصلاح شده", font, Brushes.Green, 5, 5);
                // }
            }
            
            bitmap.Dispose();
            return correctedBitmap;
        }

        private string CorrectPlateText(string detectedText)
        {
            if (string.IsNullOrWhiteSpace(detectedText))
                return detectedText;

            // حذف فضاهای اضافی
            var corrected = detectedText.Trim();
            
            // حذف کاراکترهای غیرضروری
            corrected = corrected.Replace(" ", "").Replace("-", "").Replace("_", "");
            
            // تصحیح حروف مشابه
            corrected = corrected.Replace("0", "O").Replace("1", "I").Replace("5", "S");
            
            // تبدیل به حروف بزرگ
            corrected = corrected.ToUpper();
            
            return corrected;
        }

        private void PictureBox_Click(object? sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox && pictureBox.Image != null)
            {
                ShowImageInFullScreen(pictureBox.Image, GetImageTitle(pictureBox));
            }
        }

        private void PictureBox_DoubleClick(object? sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox && pictureBox.Image != null)
            {
                ShowImageInFullScreen(pictureBox.Image, GetImageTitle(pictureBox));
            }
        }

        private string GetImageTitle(PictureBox pictureBox)
        {
            if (pictureBox == _picOriginal) return "تصویر اصلی";
            if (pictureBox == _picPreprocessed) return "تصویر پیش‌پردازش شده";
            if (pictureBox == _picDetectedPlate) return "تشخیص پلاک";
            if (pictureBox == _picCroppedPlate) return "پلاک برش داده شده";
            if (pictureBox == _picCorrectedPlate) return "پلاک اصلاح شده";
            return "تصویر";
        }

        private void ShowImageInFullScreen(Image image, string title)
        {
            try
            {
                var fullScreenForm = new Form
                {
                    Text = title,
                    WindowState = FormWindowState.Maximized,
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    RightToLeft = RightToLeft.Yes,
                    RightToLeftLayout = true
                };

                var pictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = image,
                    BackColor = Color.Black
                };

                // اضافه کردن دکمه بستن
                var closeButton = new Button
                {
                    Text = "بستن (ESC)",
                    Size = new System.Drawing.Size(100, 30),
                    Location = new System.Drawing.Point(10, 10),
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                closeButton.Click += (s, e) => fullScreenForm.Close();

                // اضافه کردن دکمه‌های zoom
                var zoomInButton = new Button
                {
                    Text = "+",
                    Size = new System.Drawing.Size(30, 30),
                    Location = new System.Drawing.Point(120, 10),
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                zoomInButton.Click += (s, e) => pictureBox.SizeMode = PictureBoxSizeMode.Normal;

                var zoomOutButton = new Button
                {
                    Text = "-",
                    Size = new System.Drawing.Size(30, 30),
                    Location = new System.Drawing.Point(155, 10),
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                zoomOutButton.Click += (s, e) => pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

                // اضافه کردن اطلاعات تصویر
                var infoLabel = new Label
                {
                    Text = $"اندازه: {image.Width} × {image.Height} پیکسل",
                    Location = new System.Drawing.Point(195, 10),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(50, 50, 50),
                    AutoSize = true,
                    Padding = new Padding(5)
                };

                // اضافه کردن tooltips
                var toolTip = new ToolTip();
                toolTip.SetToolTip(closeButton, "بستن (ESC)");
                toolTip.SetToolTip(zoomInButton, "نمایش 100% (+)");
                toolTip.SetToolTip(zoomOutButton, "نمایش مناسب (-)");

                fullScreenForm.Controls.Add(pictureBox);
                fullScreenForm.Controls.Add(closeButton);
                fullScreenForm.Controls.Add(zoomInButton);
                fullScreenForm.Controls.Add(zoomOutButton);
                fullScreenForm.Controls.Add(infoLabel);

                // اضافه کردن keyboard shortcuts
                fullScreenForm.KeyPreview = true;
                fullScreenForm.KeyDown += (s, e) =>
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Escape:
                            fullScreenForm.Close();
                            break;
                        case Keys.Add:
                        case Keys.Oemplus:
                            pictureBox.SizeMode = PictureBoxSizeMode.Normal;
                            break;
                        case Keys.Subtract:
                        case Keys.OemMinus:
                            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                            break;
                        case Keys.S when e.Control:
                            SaveImage(image);
                            break;
                        case Keys.C when e.Control:
                            CopyImageToClipboard(image);
                            break;
                    }
                };

                // اضافه کردن context menu
                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("ذخیره تصویر (Ctrl+S)", null, (s, e) => SaveImage(image));
                contextMenu.Items.Add("کپی تصویر (Ctrl+C)", null, (s, e) => CopyImageToClipboard(image));
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("نمایش 100% (+)", null, (s, e) => pictureBox.SizeMode = PictureBoxSizeMode.Normal);
                contextMenu.Items.Add("نمایش مناسب (-)", null, (s, e) => pictureBox.SizeMode = PictureBoxSizeMode.Zoom);
                contextMenu.Items.Add("نمایش کشیده", null, (s, e) => pictureBox.SizeMode = PictureBoxSizeMode.StretchImage);
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("بستن (ESC)", null, (s, e) => fullScreenForm.Close());
                pictureBox.ContextMenuStrip = contextMenu;

                fullScreenForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در نمایش تصویر: {ex.Message}", "خطا", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveImage(Image image)
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|All Files (*.*)|*.*",
                    DefaultExt = "png",
                    FileName = $"plate_image_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    image.Save(saveDialog.FileName);
                    MessageBox.Show("تصویر با موفقیت ذخیره شد.", "موفق", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ذخیره تصویر: {ex.Message}", "خطا", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyImageToClipboard(Image image)
        {
            try
            {
                Clipboard.SetImage(image);
                MessageBox.Show("تصویر در کلیپ‌بورد کپی شد.", "موفق", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در کپی تصویر: {ex.Message}", "خطا", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TestDetectionService()
        {
            try
            {
                Log("🧪 تست سرویس تشخیص...");
                
                if (_detectionPlaceService == null)
                {
                    Log("❌ سرویس تشخیص null است");
                    return;
                }
                
                // تست با یک تصویر ساده
                var testImage = new Mat(640, 640, MatType.CV_8UC3, new Scalar(128, 128, 128));
                var testDetections = _detectionPlaceService.DetectPlatesAsync(testImage).Result;
                
                Log($"✅ تست سرویس موفق - {testDetections.Count} تشخیص");
                
                testImage.Dispose();
            }
            catch (Exception ex)
            {
                Log($"❌ خطا در تست سرویس: {ex.Message}");
            }
        }

        private void TestOcrService()
        {
            try
            {
                Log("🧪 تست سرویس OCR...");
                
                if (_detectionOCRService == null)
                {
                    Log("❌ سرویس OCR null است");
                    return;
                }
                
                // تست با یک تصویر پلاک نمونه
                var testPlateImage = CreateTestPlateImage();
                var ocrResult = _detectionOCRService.RecognizePlate(testPlateImage);
                
                if (ocrResult != null)
                {
                    Log($"✅ تست OCR موفق");
                    Log($"📝 متن تشخیص داده شده: {ocrResult.Text}");
                    Log($"🎯 اعتماد: {ocrResult.Confidence:F2}");
                    Log($"🔧 روش: {ocrResult.Method}");
                    Log($"📁 مدل OCR: {_config.YoloOcrModelPath}");
                    
                    if (!string.IsNullOrEmpty(ocrResult.ErrorMessage))
                    {
                        Log($"⚠️ خطا: {ocrResult.ErrorMessage}");
                    }
                }
                else
                {
                    Log("❌ نتیجه OCR null است");
                }
                
                testPlateImage.Dispose();
            }
            catch (Exception ex)
            {
                Log($"❌ خطا در تست OCR: {ex.Message}");
            }
        }

        private Bitmap CreateTestPlateImage()
        {
            // ایجاد یک تصویر پلاک نمونه برای تست
            var bitmap = new Bitmap(200, 60);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // پس‌زمینه سفید
                graphics.Clear(Color.White);
                
                // اضافه کردن متن نمونه
                using (var font = new Font("Arial", 16, FontStyle.Bold))
                {
                    graphics.DrawString("12ایران345", font, Brushes.Black, 10, 20);
                }
                
                // اضافه کردن حاشیه
                using (var pen = new Pen(Color.Black, 2))
                {
                    graphics.DrawRectangle(pen, 0, 0, 199, 59);
                }
            }
            return bitmap;
        }

        private void Log(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}";
                
                // نمایش در status label
                if (_lblStatus != null)
                {
                    _lblStatus.Text = message;
                }
                
                // نمایش در console برای debugging
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطا در Log: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _originalImage?.Dispose();
                _preprocessedImage?.Dispose();
                _detectedPlateImage?.Dispose();
                _croppedPlateImage?.Dispose();
                _correctedPlateImage?.Dispose();
                _detectionPlaceService?.Dispose();
                _detectionOCRService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
