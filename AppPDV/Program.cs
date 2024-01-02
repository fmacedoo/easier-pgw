using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace WebViewFolderViewer
{
    public partial class MainForm : Form
    {
        private WebView2? webView;

        public MainForm()
        {
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(webView);

            string folderPath = Directory.GetCurrentDirectory();
            string indexPath = Path.Combine(folderPath, "web", "index.html");
            Console.WriteLine($"Path is {indexPath}");

            if (Directory.Exists(folderPath) && File.Exists(indexPath))
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Navigate(new Uri(indexPath).AbsoluteUri);
                webView.CoreWebView2.AddHostObjectToScript("picudo", new Picuda());
                // webView.CoreWebView2.OpenDevToolsWindow();
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

    [ClassInterface(ClassInterfaceType.AutoDual)]
	[ComVisible(true)]
    public class Picuda
    {
        public void ShowMessageBox(string message)
        {
            Console.WriteLine("ShowMessageBox");
            MessageBox.Show(message, "C# Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
