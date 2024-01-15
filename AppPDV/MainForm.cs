using Microsoft.Web.WebView2.WinForms;
using PGW;

namespace AppPDV
{
    public class FormFunctions
    {
        public void go()
        {
            var result = PromptBox.ShowConfirmation("101", "vai mesmo?", 3000);
            Logger.Debug($"result is: {result}");
        }
    }

    public partial class MainForm : Form
    {
        private WebView2? webView;
        AppSettings? settings;

        public MainForm()
        {
            settings = ConfigurationManager.LoadAppSettings();
            ConfigureForm();
            InitializeWebView();
        }

        private void ConfigureForm()
        {
            // WindowState = FormWindowState.Maximized;
            FormClosing += MainForm_FormClosing;
            this.Size = new Size(width: 800, height: 600);
        }

        private async void InitializeWebView()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(webView);

            string folderPath = Directory.GetCurrentDirectory();
            #if DEBUG
            string indexPath = Path.Combine(folderPath, "..", "public", "index.html");
            #else
            string indexPath = Path.Combine(folderPath, "public", "index.html");
            #endif
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
                webView.CoreWebView2.AddHostObjectToScript("formfunctions", new FormFunctions());
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

        private void DefaultMessageRaisingHandler(string message, int? timeoutToClose = null)
        {
            PromptBox.Show(message, timeoutToClose);
        }

        private PromptConfirmationResult DefaultPromptConfirmationRaisingHandler(string message, int? timeoutToClose = null)
        {
            var result = PromptBox.ShowConfirmation("101", message, timeoutToClose);
            return result ? PromptConfirmationResult.OK : PromptConfirmationResult.Cancel;
        }

        private string? DefaultPromptInputRaisingHandler(string message)
        {
            if (message == "INSIRA A SENHA TÉCNICA") return settings?.PGWSettings?.SENHA_TECNICA;
            if (message == "ID PONTO DE CAPTURA:") return settings?.PGWSettings?.PONTO_CAPTURA;
            if (message == "CNPJ/CPF:") return settings?.PGWSettings?.CPNJ;
            if (message == "NOME/IP SERVIDOR:") return settings?.PGWSettings?.SERVIDOR;

            return PromptBox.Prompt("101", message);
        }

        private string? DefaultPromptMenuRaisingHandler(IEnumerable<string> options)
        {
            return PromptBox.PromptList("Escolha uma opção:", options);
        }
    }
}
