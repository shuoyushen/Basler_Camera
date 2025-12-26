using Basler_Camera.App;
using Basler_Camera.Cli;
using Basler_Camera.Models;
using Basler_Camera.VisionSolution.Vision.Debugger.Controls;
using OpenCvSharp;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static OpenCvSharp.Stitcher;
using Basler.Pylon;

namespace Basler_Camera.VisionSolution
{
    // 如果你工程里已经存在 DebugMainForm.Designer.cs，则这里必须是 partial
    public sealed partial class DebugMainForm : Form
    {
        private readonly TextBox txtImage = new TextBox();
        private readonly Button btnBrowse = new Button();
        private readonly Button btnRun = new Button();
        private readonly Label lblRoi = new Label();
        private readonly TextBox txtOut = new TextBox();
        private readonly ImageViewer imgViewer = new ImageViewer();
        private readonly ComboBox comboMode = new ComboBox();
        private readonly ComboBox comboCamera = new ComboBox();


        private Mat _curMat;

        public DebugMainForm()
        {
            Text = "Vision Debugger";
            Width = 1200;
            Height = 850;
            StartPosition = FormStartPosition.CenterScreen;

            BuildUi();
            WireEvents();

            LogLine("Select an image, draw ROI (optional), then click Run.");

            LoadCameras();  // 加载可用的相机列表
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_curMat != null)
            {
                _curMat.Dispose();
                _curMat = null;
            }
            base.OnFormClosed(e);
        }

        private void BuildUi()
        {
            // ===== Top bar =====
            var top = new Panel { Dock = DockStyle.Top, Height = 48 };
            Controls.Add(top);

            txtImage.Left = 10;
            txtImage.Top = 12;
            txtImage.Width = 700;
            txtImage.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            top.Controls.Add(txtImage);

            comboMode.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMode.Width = 120;
            comboMode.Left = txtImage.Right + 10;
            comboMode.Top = 10;
            comboMode.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // 先放你确定存在的 mode；后面我们再做“自动从帮助/常量读取”
            comboMode.Items.AddRange(new object[] { "list", "grab", "ocr", "grab_ocr", "ai", "grab_ai", "grab_ai_ocr" });
            comboMode.SelectedItem = "ocr";

            top.Controls.Add(comboMode);

            // ===== Add ComboBox for camera selection =====
            comboCamera.Width = 120;
            comboCamera.Left = comboMode.Right + 10;
            comboCamera.Top = 10;
            comboCamera.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            top.Controls.Add(comboCamera);


            btnBrowse.Text = "Browse";
            btnBrowse.Width = 90;
            btnBrowse.Left = comboCamera.Right + 10;
            btnBrowse.Top = 10;
            btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            top.Controls.Add(btnBrowse);

            btnRun.Text = "Run";
            btnRun.Width = 90;
            btnRun.Left = btnBrowse.Right + 10;
            btnRun.Top = 10;
            btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            top.Controls.Add(btnRun);

            lblRoi.AutoSize = true;
            lblRoi.Left = btnRun.Right + 15;
            lblRoi.Top = 14;
            lblRoi.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblRoi.Text = "ROI: (none)";
            top.Controls.Add(lblRoi);



            // ===== Split: image (top) + output (bottom) =====
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 520
            };
            Controls.Add(split);

            // image viewer
            imgViewer.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(imgViewer);

            // output
            txtOut.Dock = DockStyle.Fill;
            txtOut.Multiline = true;
            txtOut.ScrollBars = ScrollBars.Both;
            txtOut.ReadOnly = true;
            txtOut.Font = new System.Drawing.Font("Consolas", 10f);
            split.Panel2.Controls.Add(txtOut);
        }

        private void LoadCameras()
        {
            try
            {
                // 初始化 Pylon 设备
                CameraFinder finder = new CameraFinder();
                CameraList cameraList = finder.GetConnectedCameras();

                comboCamera.Items.Clear();

                // 将所有可用的相机添加到 comboBox 中
                foreach (var camera in cameraList)
                {
                    comboCamera.Items.Add(camera.GetFriendlyName());
                }

                if (comboCamera.Items.Count > 0)
                {
                    comboCamera.SelectedIndex = 0;  // 默认选择第一个相机
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error loading cameras: " + ex.Message);
            }
        }

        private void WireEvents()
        {
            btnBrowse.Click += (s, e) => BrowseImage();
            btnRun.Click += (s, e) => RunOnce();

            imgViewer.RoiChanged += r =>
            {
                if (r.Width > 0 && r.Height > 0)
                    lblRoi.Text = string.Format("ROI: {0},{1},{2},{3}", r.X, r.Y, r.Width, r.Height);
                else
                    lblRoi.Text = "ROI: (none)";
            };
        }

        private void BrowseImage()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All|*.*";
                ofd.Title = "Select image";

                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                txtImage.Text = ofd.FileName;
                LogLine("Image = " + ofd.FileName);

                // load & show
                try
                {
                    if (_curMat != null) { _curMat.Dispose(); _curMat = null; }

                    _curMat = Cv2.ImRead(ofd.FileName, ImreadModes.Color);
                    imgViewer.SetMat(_curMat);
                    imgViewer.Roi = new Rect(); // clear ROI
                    lblRoi.Text = "ROI: (none)";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        private void RunOnce()
        {
            var path = (txtImage.Text ?? "").Trim();
            var mode = (comboMode.SelectedItem as string) ?? "ocr";
            mode = mode.Trim().ToLowerInvariant();

            // 获取选择的相机
            var selectedCamera = comboCamera.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "Please select a valid image file first.");
                return;
            }

            try
            {
                string[] args;

                if (mode == "list")
                {
                    if (string.IsNullOrWhiteSpace(selectedCamera))
                    {
                        MessageBox.Show(this, "Please select a camera for the list operation.");
                        return;
                    }

                    args = new[] { "list", selectedCamera };
                }
                else
                {
                    // 需要图片的 mode
                    var roi = imgViewer.Roi;

                    if (roi.Width > 0 && roi.Height > 0)
                    {
                        args = new[] {
                    mode,
                    path,
                    roi.X.ToString(),
                    roi.Y.ToString(),
                    roi.Width.ToString(),
                    roi.Height.ToString()
                };
                    }
                    else
                    {
                        args = new[] { mode, path };
                    }
                }

                Request req = ArgsParser.Parse(args);
                Response resp = new AppRunner().Run(req);

                txtOut.Clear();
                txtOut.AppendText(DumpResponse(resp));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.GetType().Name + ": " + ex.Message);
            }
        }


        // 不猜字段名：自动枚举 Response 的 public 属性
        private static string DumpResponse(Response resp)
        {
            if (resp == null) return "resp=null";

            var sb = new StringBuilder();

            var props = resp.GetType().GetProperties(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                object v;
                try { v = p.GetValue(resp, null); }
                catch { v = "<get_failed>"; }

                string s;
                if (v == null) s = "";
                else if (v is double) s = ((double)v).ToString("0.###");
                else if (v is float) s = ((float)v).ToString("0.###");
                else s = v.ToString();

                sb.Append(p.Name.ToUpperInvariant());
                sb.Append('=');
                sb.Append(s);
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        private void LogLine(string msg)
        {
            txtOut.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\r\n");
        }
    }
}
