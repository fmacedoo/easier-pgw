using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;
using static PGW.Enums;

namespace AppPDV
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class ProcessGateway
    {
        private readonly PGW pgw;
        private readonly WebView2 webView;
        private readonly WebViewInteractionController webViewInteractionController;

        public ProcessGateway(WebView2 webView)
        {
            this.webView = webView;
            pgw = new PGW(
                DefaultMessageRaisingHandler,
                DefaultPromptConfirmationRaisingHandler,
                DefaultPromptInputRaisingHandler,
                DefaultPromptMenuRaisingHandler
            );
            webViewInteractionController = new WebViewInteractionController(webView);
        }

        public void test_message()
        {
            _ = Task.Run(async () => {
                var confirmed = await webViewInteractionController.Show("test_message from c#");
                Logger.Debug($"After test_message (aborted={!confirmed})");
            });
        }

        public void test_message_confirmation()
        {
            Task.Run(async () => {
                var confirmed = await webViewInteractionController.ShowWithConfirmation("test_message_confirmation from c#");
                Logger.Debug($"After test_message_confirmation (aborted={!confirmed})");
            });
        }

        public void abort(string? requestId = null)
        {
            webViewInteractionController.Abort(requestId);
        }

        private async Task DefaultMessageRaisingHandler(string message, int? timeoutToClose = null)
        {
            await webViewInteractionController.Show(message, timeoutToClose);
        }

        private async Task<PromptConfirmationResult> DefaultPromptConfirmationRaisingHandler(string message, int? timeoutToClose = null)
        {
            var confirmed = await webViewInteractionController.Show(message, timeoutToClose);
            return confirmed ? PromptConfirmationResult.OK : PromptConfirmationResult.Cancel;
        }

        private string? DefaultPromptInputRaisingHandler(string message)
        {
            if (message == "INSIRA A SENHA TÉCNICA") return AppSettings.Instance.PGW?.SENHA_TECNICA;
            if (message == "ID PONTO DE CAPTURA:") return AppSettings.Instance.PGW?.PONTO_CAPTURA;
            if (message == "CNPJ/CPF:") return AppSettings.Instance.PGW?.CPNJ;
            if (message == "NOME/IP SERVIDOR:") return AppSettings.Instance.PGW?.SERVIDOR;

            return PromptBox.Prompt("101", message);
        }

        private string? DefaultPromptMenuRaisingHandler(IEnumerable<string> options)
        {
            return PromptBox.PromptList("Escolha uma opção:", options);
        }

        public void installation()
        {
            Task.Run(() => {
                E_PWRET result = pgw.Installation();
                Logger.Debug($"PaymInstallationent result: {result}");
            });
        }

        public string operations()
        {
            var result = new List<object>();
            var operations = pgw.GetOperations();
            foreach (var op in operations)
            {
                result.Add(new
                {
                    bOperType = op.bOperType,
                    szText = op.szText,
                    szValue = op.szValue,
                });
            }

            var serialized = JsonSerializer.Serialize(result);
            Console.WriteLine(serialized);

            return serialized;
        }

        public void payment()
        {
            Task.Run(() => {
                Logger.Info("Payment");
                E_PWRET result = pgw.Operation(E_PWOPER.PWOPER_SALE);

                Logger.Debug($"Payment result: {result}");
            });
        }
    }
}