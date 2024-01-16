using Microsoft.Web.WebView2.WinForms;
using PGW;

namespace AppPDV
{
    public partial class MainForm : Form
    {
        private WebView2? webView;

        public MainForm()
        {
            ConfigureForm();
            InitializeWebView();
        }

        private void ConfigureForm()
        {
            var LastWindowState = Enum.Parse<FormWindowState>(AppSettings.Instance.WebView.FormSize != null ? AppSettings.Instance.WebView.FormSize : "Normal");
            WindowState = LastWindowState;
            FormClosing += MainForm_FormClosing;
            Resize += MainForm_Resize;
            this.Size = new Size(width: 800, height: 600);
        }

        private async void InitializeWebView()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(webView);

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
            Console.WriteLine($"Using web client at {indexPath}");

            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Navigate(new Uri(indexPath).AbsoluteUri);

            ProcessGateway pgw = new ProcessGateway();
            webView.CoreWebView2.AddHostObjectToScript("gateway", pgw);
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
