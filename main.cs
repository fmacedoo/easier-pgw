using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace WebViewFolderViewer
{
    public partial class MainForm : Form
    {
        private WebView2 webView;

        public MainForm()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            Controls.Add(webView);

            await webView.EnsureCoreWebView2Async(null);

            string folderPath = @"C:\Path\To\Your\Folder";
            string indexPath = Path.Combine(folderPath, "index.html");

            if (Directory.Exists(folderPath) && File.Exists(indexPath))
            {
                string indexContent = File.ReadAllText(indexPath);
                webView.CoreWebView2.NavigateToString(indexContent);
            }
            else
            {
                MessageBox.Show("Folder or index.html not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
