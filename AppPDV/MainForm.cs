using Microsoft.Web.WebView2.WinForms;
using PGW;

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

            // WindowState = FormWindowState.Maximized;
            FormClosing += MainForm_FormClosing;
            this.Size = new Size(width: 800, height: 600);

            string folderPath = Directory.GetCurrentDirectory();
            string indexPath = Path.Combine(folderPath, "public", "index.html");
            Console.WriteLine($"Using path {indexPath}");

            if (Directory.Exists(folderPath) && File.Exists(indexPath))
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Navigate(new Uri(indexPath).AbsoluteUri);
                
                PGWGateway pgw = new PGWGateway(
                    DefaultMessageRaisingHandler,
                    DefaultPromptConfirmationRaisingHandler,
                    DefaultPromptInputRaisingHandler,
                    DefaultPromptMenuRaisingHandler
                );
                webView.CoreWebView2.AddHostObjectToScript("pgw", pgw);
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

        private void DefaultMessageRaisingHandler(string message)
        {
            MessageBox.Show(message, "101");
        }

        private PromptConfirmationResult DefaultPromptConfirmationRaisingHandler(string message)
        {
            var result = PromptBox.ShowConfirmation("101", message);
            return result ? PromptConfirmationResult.OK : PromptConfirmationResult.Cancel;
        }

        private string? DefaultPromptInputRaisingHandler(string message)
        {
            if (message == "INSIRA A SENHA TÉCNICA") return "314159";
            if (message == "ID PONTO DE CAPTURA:") return "86629";
            if (message == "CNPJ/CPF:") return "33.838.198/0001-36";
            if (message == "NOME/IP SERVIDOR:") return "esba-hom01.tpgweb.io:17500";
            return PromptBox.Show("101", message);
        }

        private string? DefaultPromptMenuRaisingHandler(IEnumerable<string> options)
        {
            return PromptBox.ShowList("Escolha uma opção:", options);
        }
    }
}
