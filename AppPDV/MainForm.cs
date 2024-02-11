using AppPDV.NFeClient;
using Microsoft.Web.WebView2.WinForms;
using PGW;

namespace AppPDV
{
    public partial class MainForm : Form
    {
        private WebView2 webView;

        public MainForm()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Logger.Info("ConfigureForm");
            SuspendLayout();

            Controls.Add(webView);

            var LastWindowState = Enum.Parse<FormWindowState>(AppSettings.Instance.WebView.FormSize != null ? AppSettings.Instance.WebView.FormSize : "Normal");
            WindowState = LastWindowState;
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;
            Load += MainForm_Load;
            Size = new Size(width: 800, height: 600);
            
            ResumeLayout(true);
            Logger.Debug("ConfigureForm finished");
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            Logger.Info("MainForm_Load");
            #if DEBUG
            string indexPath = AppSettings.Instance.WebView.WebClientAddress;
#else
            string indexPath = Path.Combine(folderPath, "public", "index.html");
            string folderPath = Directory.GetCurrentDirectory();
            if (!Directory.Exists(folderPath) || !File.Exists(indexPath))
            {
                MessageBox.Show("Folder or index.html not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#endif
            Logger.Debug($"Using web client at {indexPath}");

            webView.Invoke(async () => {
                Logger.Debug("Webview+Gateway init");
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Navigate(new Uri(indexPath).AbsoluteUri);
                
                _ = Task.Run(() => {
                    try
                    {
                        Logger.Debug("Gateway/NFE init");
                        ProcessGateway pgw = new ProcessGateway(webView);
                        NFeGateway nfe = new NFeGateway();
                        webView.Invoke(() => {
                            webView.CoreWebView2.AddHostObjectToScript("gateway", pgw);
                            webView.CoreWebView2.AddHostObjectToScript("nfe", nfe);
                            Logger.Debug("Gateway/NFE finished");
                            pgw.NotifyInit();
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.Debug($"Error on Gateway/NFE init:" + e.Message);
                    }
                });

                Logger.Debug("Webview finished");
            });
            Logger.Debug("MainForm_Load finished");
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            AppSettings.Instance.WebView.FormSize = WindowState.ToString();
            AppSettings.Persist();
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            webView?.Dispose();
        }
    }
}
