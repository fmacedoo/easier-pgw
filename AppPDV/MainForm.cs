using Microsoft.Web.WebView2.WinForms;

namespace AppPDV
{
    public partial class MainForm : Form
    {
        private WebView2? webView;

        public MainForm()
        {
            Console.WriteLine("MainForm: Loading Webview");
            InitializeWebView();
            Console.WriteLine("MainForm: Webview Loaded");
        }

        private async void InitializeWebView()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(webView);

            WindowState = FormWindowState.Maximized;
            FormClosing += MainForm_FormClosing;

            string folderPath = Directory.GetCurrentDirectory();
            string indexPath = Path.Combine(folderPath, "public", "index.html");
            Console.WriteLine($"Using path {indexPath}");

            if (Directory.Exists(folderPath) && File.Exists(indexPath))
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Navigate(new Uri(indexPath).AbsoluteUri);
                webView.CoreWebView2.AddHostObjectToScript("bridge", new Bridge());
                // webView.CoreWebView2.OpenDevToolsWindow();
            }
            else
            {
                MessageBox.Show("Folder or index.html not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            webView?.Dispose();
        }
    }
}
