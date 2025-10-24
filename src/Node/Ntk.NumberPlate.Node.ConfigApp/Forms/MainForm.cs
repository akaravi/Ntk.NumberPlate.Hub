using System.Diagnostics;
using Newtonsoft.Json;
using Ntk.NumberPlate.Shared.Models;

namespace Ntk.NumberPlate.Node.ConfigApp.Forms;

public partial class MainForm : Form
{
    private const string ConfigFileName = "node-config.json";
    private NodeConfiguration _config;

    // Controls
    private TextBox txtNodeId;
    private TextBox txtNodeName;
    private TextBox txtHubUrl;
    private TextBox txtApiToken;
    private TextBox txtYoloPlateModelPath;
    private NumericUpDown numConfidence;
    private TextBox txtVideoSource;
    private NumericUpDown numProcessingFps;
    private NumericUpDown numSpeedLimit;
    private CheckBox chkEnableSpeed;
    private NumericUpDown numDetectionDistance;
    private CheckBox chkSaveImages;
    private TextBox txtImagePath;
    private CheckBox chkAutoSend;
    private ComboBox cmbOcrMethod;
    private TextBox txtYoloOcrModelPath;
    private Button btnBrowseOcrModel;
    private NumericUpDown numOcrConfidence;
    private Button btnSave;
    private Button btnTest;
    private Button btnStartService;
    private Button btnTestDetection;
    private Button btnTestDetection2;
    private Button btnBrowseModel;
    private Button btnBrowseImagePath;
    private TextBox txtLog;

    public MainForm()
    {
        _config = new NodeConfiguration();
        InitializeComponents();
        LoadConfiguration();
    }

    private void InitializeComponents()
    {
        this.Text = "پیکربندی نود تشخیص پلاک";
        this.Size = new Size(700, 1000);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.RightToLeft = RightToLeft.Yes;
        this.RightToLeftLayout = true;

        var y = 20;
        var labelWidth = 150;
        var controlWidth = 400;
        var controlX = 220;

        // Node ID
        AddLabel("شناسه نود:", 20, y, labelWidth);
        txtNodeId = AddTextBox(controlX, y, controlWidth);
        y += 35;

        // Node Name
        AddLabel("نام نود:", 20, y, labelWidth);
        txtNodeName = AddTextBox(controlX, y, controlWidth);
        y += 35;

        // Hub URL
        AddLabel("آدرس Hub Server:", 20, y, labelWidth);
        txtHubUrl = AddTextBox(controlX, y, controlWidth);
        y += 35;

        // API Token
        AddLabel("توکن API:", 20, y, labelWidth);
        txtApiToken = AddTextBox(controlX, y, controlWidth);
        y += 35;

        // YOLO Plate Model Path
        AddLabel("مسیر مدل YOLO تشخیص پلاک:", 20, y, labelWidth);
        txtYoloPlateModelPath = AddTextBox(controlX, y, 300);
        btnBrowseModel = AddButton("...", controlX + 310, y - 2, 50, 25);
        btnBrowseModel.Click += BtnBrowseModel_Click;
        y += 35;

        // Confidence Threshold
        AddLabel("آستانه اعتماد (0-1):", 20, y, labelWidth);
        numConfidence = AddNumericUpDown(controlX, y, controlWidth);
        numConfidence.DecimalPlaces = 2;
        numConfidence.Increment = 0.05M;
        numConfidence.Minimum = 0;
        numConfidence.Maximum = 1;
        y += 35;

        // Video Source
        AddLabel("منبع ویدیو:", 20, y, labelWidth);
        txtVideoSource = AddTextBox(controlX, y, controlWidth);
        y += 35;

        // Processing FPS
        AddLabel("فریم ریت پردازش:", 20, y, labelWidth);
        numProcessingFps = AddNumericUpDown(controlX, y, controlWidth);
        numProcessingFps.Minimum = 1;
        numProcessingFps.Maximum = 60;
        y += 35;

        // Enable Speed Detection
        chkEnableSpeed = AddCheckBox("فعال‌سازی تشخیص سرعت", controlX, y);
        y += 35;

        // Speed Limit
        AddLabel("حداکثر سرعت مجاز:", 20, y, labelWidth);
        numSpeedLimit = AddNumericUpDown(controlX, y, controlWidth);
        numSpeedLimit.Minimum = 10;
        numSpeedLimit.Maximum = 200;
        y += 35;

        // Detection Distance
        AddLabel("فاصله تشخیص (متر):", 20, y, labelWidth);
        numDetectionDistance = AddNumericUpDown(controlX, y, controlWidth);
        numDetectionDistance.Minimum = 1;
        numDetectionDistance.Maximum = 1000;
        numDetectionDistance.DecimalPlaces = 1;
        y += 35;

        // Save Images
        chkSaveImages = AddCheckBox("ذخیره تصاویر به صورت محلی", controlX, y);
        y += 35;

        // Image Path
        AddLabel("مسیر ذخیره تصاویر:", 20, y, labelWidth);
        txtImagePath = AddTextBox(controlX, y, 300);
        btnBrowseImagePath = AddButton("...", controlX + 310, y - 2, 50, 25);
        btnBrowseImagePath.Click += BtnBrowseImagePath_Click;
        y += 35;

        // Auto Send
        chkAutoSend = AddCheckBox("ارسال خودکار به Hub", controlX, y);
        y += 45;

        // ========== بخش تنظیمات OCR ==========
        var groupOcr = new GroupBox
        {
            Text = "تنظیمات OCR (تشخیص نوشته پلاک)",
            Location = new Point(20, y),
            Size = new Size(620, 170),
            Font = new Font("Tahoma", 9, FontStyle.Bold)
        };
        this.Controls.Add(groupOcr);
        // مختصات داخلی کنترل‌های گروه OCR
        var gy = 25;

        // OCR Method
        var lblOcrMethod = new Label
        {
            Text = "روش تشخیص OCR:",
            Location = new Point(420, gy),
            Size = new Size(180, 20),
            Font = new Font("Tahoma", 9)
        };
        groupOcr.Controls.Add(lblOcrMethod);

        cmbOcrMethod = new ComboBox
        {
            Location = new Point(200, gy),
            Size = new Size(200, 25),
            Font = new Font("Tahoma", 9),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbOcrMethod.Items.Add("Simple OCR - روش ساده");
        cmbOcrMethod.Items.Add("YOLO OCR - روش YOLO");
        cmbOcrMethod.Items.Add("IronOCR - کتابخانه IronOCR");
        cmbOcrMethod.SelectedIndex = 0; // مقدار پیش‌فرض: Simple OCR
        cmbOcrMethod.SelectedIndexChanged += CmbOcrMethod_SelectedIndexChanged;
        groupOcr.Controls.Add(cmbOcrMethod);
        gy += 35;

        // YOLO OCR Model Path
        var lblOcrModel = new Label
        {
            Text = "مسیر مدل YOLO OCR:",
            Location = new Point(420, gy),
            Size = new Size(180, 20),
            Font = new Font("Tahoma", 9)
        };
        groupOcr.Controls.Add(lblOcrModel);

        txtYoloOcrModelPath = new TextBox
        {
            Location = new Point(50, gy),
            Size = new Size(150, 25),
            Font = new Font("Tahoma", 9),
            Text = "models/plate-ocr.onnx" // مقدار پیش‌فرض
        };
        groupOcr.Controls.Add(txtYoloOcrModelPath);

        btnBrowseOcrModel = new Button
        {
            Text = "...",
            Location = new Point(210, gy - 2),
            Size = new Size(50, 25),
            Font = new Font("Tahoma", 9)
        };
        btnBrowseOcrModel.Click += BtnBrowseOcrModel_Click;
        groupOcr.Controls.Add(btnBrowseOcrModel);
        gy += 35;

        // OCR Confidence Threshold
        var lblOcrConf = new Label
        {
            Text = "آستانه اعتماد OCR:",
            Location = new Point(420, gy),
            Size = new Size(180, 20),
            Font = new Font("Tahoma", 9)
        };
        groupOcr.Controls.Add(lblOcrConf);

        numOcrConfidence = new NumericUpDown
        {
            Location = new Point(200, gy),
            Size = new Size(200, 25),
            Font = new Font("Tahoma", 9),
            DecimalPlaces = 2,
            Increment = 0.05M,
            Minimum = 0,
            Maximum = 1,
            Value = 0.5M
        };
        groupOcr.Controls.Add(numOcrConfidence);
        // ارتفاع گروه OCR را به انتهای بخش بعدی اضافه کن
        y += groupOcr.Height + 15;
        // ========================================

        // Buttons
        btnSave = AddButton("ذخیره تنظیمات", 520, y, 120, 35);
        btnSave.BackColor = Color.FromArgb(0, 120, 215);
        btnSave.ForeColor = Color.White;
        btnSave.Click += BtnSave_Click;

        btnTest = AddButton("تست اتصال", 390, y, 120, 35);
        btnTest.Click += BtnTest_Click;

        btnStartService = AddButton("راه‌اندازی سرویس", 260, y, 120, 35);
        btnStartService.BackColor = Color.FromArgb(16, 124, 16);
        btnStartService.ForeColor = Color.White;
        btnStartService.Click += BtnStartService_Click;

        btnTestDetection = AddButton("تست تشخیص پلاک", 130, y, 120, 35);
        btnTestDetection.BackColor = Color.FromArgb(156, 39, 176);
        btnTestDetection.ForeColor = Color.White;
        btnTestDetection.Click += BtnTestDetection_Click;

        btnTestDetection2 = AddButton("تست تشخیص پلاک ۲", 10, y, 120, 35);
        btnTestDetection2.BackColor = Color.FromArgb(255, 87, 34);
        btnTestDetection2.ForeColor = Color.White;
        btnTestDetection2.Click += BtnTestDetection2_Click;
        y += 50;

        // Log TextBox
        AddLabel("لاگ:", 20, y, labelWidth);
        txtLog = new TextBox
        {
            Location = new Point(20, y + 25),
            Size = new Size(620, 150),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Tahoma", 9)
        };
        this.Controls.Add(txtLog);

        // مقداردهی اولیه کنترل‌های OCR
        UpdateOcrControls();
    }

    private Label AddLabel(string text, int x, int y, int width)
    {
        var label = new Label
        {
            Text = text,
            Location = new Point(x, y + 3),
            Size = new Size(width, 20),
            Font = new Font("Tahoma", 9)
        };
        this.Controls.Add(label);
        return label;
    }

    private TextBox AddTextBox(int x, int y, int width)
    {
        var textBox = new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 25),
            Font = new Font("Tahoma", 9)
        };
        this.Controls.Add(textBox);
        return textBox;
    }

    private NumericUpDown AddNumericUpDown(int x, int y, int width)
    {
        var numeric = new NumericUpDown
        {
            Location = new Point(x, y),
            Size = new Size(width, 25),
            Font = new Font("Tahoma", 9)
        };
        this.Controls.Add(numeric);
        return numeric;
    }

    private CheckBox AddCheckBox(string text, int x, int y)
    {
        var checkBox = new CheckBox
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(400, 25),
            Font = new Font("Tahoma", 9)
        };
        this.Controls.Add(checkBox);
        return checkBox;
    }

    private Button AddButton(string text, int x, int y, int width, int height)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            Font = new Font("Tahoma", 9, FontStyle.Bold)
        };
        this.Controls.Add(button);
        return button;
    }

    private void LoadConfiguration()
    {
        try
        {
            if (File.Exists(ConfigFileName))
            {
                var json = File.ReadAllText(ConfigFileName);
                _config = JsonConvert.DeserializeObject<NodeConfiguration>(json) ?? new NodeConfiguration();
                Log("تنظیمات موجود بارگذاری شد.");
            }
            else
            {
                _config = new NodeConfiguration();
                Log("تنظیمات پیش‌فرض بارگذاری شد.");
            }

            // Load to UI
            txtNodeId.Text = _config.NodeId;
            txtNodeName.Text = _config.NodeName;
            txtHubUrl.Text = _config.HubServerUrl;
            txtApiToken.Text = _config.ApiToken ?? "";
            txtYoloPlateModelPath.Text = _config.YoloPlateModelPath;
            numConfidence.Value = (decimal)_config.ConfidenceThreshold;
            txtVideoSource.Text = _config.VideoSource;
            numProcessingFps.Value = _config.ProcessingFps;
            numSpeedLimit.Value = (decimal)_config.SpeedLimit;
            chkEnableSpeed.Checked = _config.EnableSpeedDetection;
            numDetectionDistance.Value = (decimal)_config.DetectionDistance;
            chkSaveImages.Checked = _config.SaveImagesLocally;
            txtImagePath.Text = _config.LocalImagePath;
            chkAutoSend.Checked = _config.AutoSendEnabled;

            // Load OCR settings
            cmbOcrMethod.SelectedIndex = (int)_config.OcrMethod;
            txtYoloOcrModelPath.Text = _config.YoloOcrModelPath;
            numOcrConfidence.Value = (decimal)_config.OcrConfidenceThreshold;
            UpdateOcrControls();
        }
        catch (Exception ex)
        {
            Log($"خطا در بارگذاری تنظیمات: {ex.Message}");
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // Update config from UI
            _config.NodeId = txtNodeId.Text.Trim();
            _config.NodeName = txtNodeName.Text.Trim();
            _config.HubServerUrl = txtHubUrl.Text.Trim();
            _config.ApiToken = txtApiToken.Text.Trim();
            _config.YoloPlateModelPath = txtYoloPlateModelPath.Text.Trim();
            _config.ConfidenceThreshold = (float)numConfidence.Value;
            _config.VideoSource = txtVideoSource.Text.Trim();
            _config.ProcessingFps = (int)numProcessingFps.Value;
            _config.SpeedLimit = (double)numSpeedLimit.Value;
            _config.EnableSpeedDetection = chkEnableSpeed.Checked;
            _config.DetectionDistance = (double)numDetectionDistance.Value;
            _config.SaveImagesLocally = chkSaveImages.Checked;
            _config.LocalImagePath = txtImagePath.Text.Trim();
            _config.AutoSendEnabled = chkAutoSend.Checked;

            // Save OCR settings
            _config.OcrMethod = (OcrMethod)cmbOcrMethod.SelectedIndex;
            _config.YoloOcrModelPath = txtYoloOcrModelPath.Text.Trim();
            _config.OcrConfidenceThreshold = (float)numOcrConfidence.Value;

            // Validate
            if (string.IsNullOrWhiteSpace(_config.NodeName))
            {
                MessageBox.Show("لطفا نام نود را وارد کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_config.HubServerUrl))
            {
                MessageBox.Show("لطفا آدرس Hub Server را وارد کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Save
            var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
            File.WriteAllText(ConfigFileName, json);

            Log("تنظیمات با موفقیت ذخیره شد.");
            MessageBox.Show("تنظیمات با موفقیت ذخیره شد.", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"خطا در ذخیره تنظیمات: {ex.Message}");
            MessageBox.Show($"خطا در ذخیره تنظیمات:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnTest_Click(object? sender, EventArgs e)
    {
        try
        {
            Log("در حال تست اتصال به Hub Server...");
            btnTest.Enabled = false;

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{txtHubUrl.Text.Trim()}/api/node/heartbeat/{txtNodeId.Text.Trim()}";
            var response = await client.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                Log("✓ اتصال به Hub Server با موفقیت برقرار شد.");
                MessageBox.Show("اتصال با موفقیت برقرار شد!", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                Log($"✗ خطا در اتصال: {response.StatusCode}");
                MessageBox.Show($"خطا در اتصال: {response.StatusCode}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"✗ خطا در اتصال: {ex.Message}");
            MessageBox.Show($"خطا در اتصال:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTest.Enabled = true;
        }
    }

    private void BtnStartService_Click(object? sender, EventArgs e)
    {
        try
        {
            var result = MessageBox.Show(
                "آیا مایل به راه‌اندازی سرویس هستید؟\n\nتوجه: ابتدا تنظیمات را ذخیره کنید.",
                "تایید",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var serviceExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "Ntk.NumberPlate.Node.Service", "bin", "Debug", "net8.0-windows",
                    "Ntk.NumberPlate.Node.Service.exe");

                if (File.Exists(serviceExePath))
                {
                    Process.Start(serviceExePath);
                    Log("سرویس راه‌اندازی شد.");
                    MessageBox.Show("سرویس با موفقیت راه‌اندازی شد.", "موفق", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Log($"فایل سرویس یافت نشد: {serviceExePath}");
                    MessageBox.Show("فایل سرویس یافت نشد. لطفا ابتدا پروژه را Build کنید.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"خطا در راه‌اندازی سرویس: {ex.Message}");
            MessageBox.Show($"خطا در راه‌اندازی سرویس:\n{ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnBrowseModel_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "ONNX Model Files (*.onnx)|*.onnx|All Files (*.*)|*.*",
            Title = "انتخاب فایل مدل YOLO"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtYoloPlateModelPath.Text = dialog.FileName;
        }
    }

    private void BtnBrowseImagePath_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "انتخاب پوشه ذخیره تصاویر"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtImagePath.Text = dialog.SelectedPath;
        }
    }

    private void BtnBrowseOcrModel_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "ONNX Model Files (*.onnx)|*.onnx|All Files (*.*)|*.*",
            Title = "انتخاب فایل مدل YOLO OCR"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            txtYoloOcrModelPath.Text = dialog.FileName;
        }
    }

    private void CmbOcrMethod_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateOcrControls();
    }

    private void UpdateOcrControls()
    {
        // بررسی null برای جلوگیری از خطا در هنگام مقداردهی اولیه
        if (cmbOcrMethod == null || txtYoloOcrModelPath == null || btnBrowseOcrModel == null)
            return;

        // فعال/غیرفعال کردن کنترل‌های مربوط به YOLO OCR
        bool isYoloOcr = cmbOcrMethod.SelectedIndex == 1; // YOLO OCR

        txtYoloOcrModelPath.Enabled = isYoloOcr;
        btnBrowseOcrModel.Enabled = isYoloOcr;

        if (txtLog == null)
            return;

        if (isYoloOcr)
        {
            Log("✅ روش YOLO OCR انتخاب شد - لطفاً مسیر مدل را مشخص کنید.");
            Log("💡 برای انتخاب مدل، روی دکمه '...' کلیک کنید");
        }
        else if (cmbOcrMethod.SelectedIndex == 0)
        {
            Log("روش Simple OCR انتخاب شد - بدون نیاز به مدل خارجی.");
        }
        else if (cmbOcrMethod.SelectedIndex == 2)
        {
            Log("روش IronOCR انتخاب شد - نیاز به نصب پکیج IronOcr دارد.");
            Log("💡 برای استفاده از مدل YOLO، ابتدا روش OCR را روی 'YOLO OCR' تغییر دهید");
        }
    }

    private void BtnTestDetection_Click(object? sender, EventArgs e)
    {
        try
        {
            // بررسی مدل YOLO
            if (string.IsNullOrWhiteSpace(_config.YoloPlateModelPath) || !File.Exists(_config.YoloPlateModelPath))
            {
                var result = MessageBox.Show(
                    "مسیر مدل YOLO معتبر نیست.\n\nآیا می‌خواهید تنظیمات را ویرایش کنید؟",
                    "خطا",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    return;
                }
            }

            // باز کردن فرم تست
            var testForm = new TestDetectionModelBForm(_config);
            testForm.ShowDialog();

            Log("فرم تست تشخیص باز شد.");
        }
        catch (Exception ex)
        {
            Log($"خطا در باز کردن فرم تست: {ex.Message}");
            MessageBox.Show($"خطا در باز کردن فرم تست:\n{ex.Message}", "خطا",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnTestDetection2_Click(object? sender, EventArgs e)
    {
        try
        {
            Log("دکمه 'تست تشخیص پلاک ۲' کلیک شد.");
            
            // باز کردن فرم تست جدید
            var testFormB = new TestDetectionModelBForm(_config);
            testFormB.ShowDialog();

            Log("فرم تست تشخیص مدل B باز شد.");
        }
        catch (Exception ex)
        {
            Log($"خطا در باز کردن فرم تست مدل B: {ex.Message}");
            MessageBox.Show($"خطا در باز کردن فرم تست مدل B:\n{ex.Message}", "خطا",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Log(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => Log(message));
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        txtLog.AppendText($"[{timestamp}] {message}\r\n");
    }
}


