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
            Logger.Info("ProcessGateway constructor");
            this.webView = webView;
            pgw = new PGW(
                DefaultMessageRaisingHandler,
                DefaultPromptConfirmationRaisingHandler,
                DefaultPromptInputRaisingHandler,
                DefaultPromptMenuRaisingHandler
            );
            webViewInteractionController = new WebViewInteractionController(webView);
        }

        public void NotifyInit()
        {
            webViewInteractionController.NotifyInit();
        }

        public string?[] devices()
        {
            var devices = DeviceManagement.List();
            Console.WriteLine(JsonSerializer.Serialize(devices));
            return devices;
        }

        public void abort(string? requestId = null)
        {
            webViewInteractionController.Abort(requestId);
        }

        public void confirm(string requestId, string value)
        {
            webViewInteractionController.Confirm(requestId, value);
        }

        private async Task DefaultMessageRaisingHandler(string message, int? timeoutToClose = null)
        {
            await webViewInteractionController.ShowMessage(message, timeoutToClose);
        }

        private async Task<PromptConfirmationResult> DefaultPromptConfirmationRaisingHandler(string message, int? timeoutToClose = null)
        {
            var confirmed = await webViewInteractionController.ShowMessage(message, timeoutToClose);
            return confirmed != null ? PromptConfirmationResult.OK : PromptConfirmationResult.Cancel;
        }

        private async Task<string?> DefaultPromptInputRaisingHandler(PromptConfig config)
        {
            if (config.Identifier == E_PWINFO.PWINFO_AUTHTECHUSER) return AppSettings.Instance.PGW?.SENHA_TECNICA;
            if (config.Identifier == E_PWINFO.PWINFO_POSID) return AppSettings.Instance.PGW?.PONTO_CAPTURA;
            if (config.Message == "CNPJ/CPF:") return AppSettings.Instance.PGW?.CPNJ;
            if (config.Message == "NOME/IP SERVIDOR:") return AppSettings.Instance.PGW?.SERVIDOR;

            return await webViewInteractionController.ShowPrompt(config);
        }

        private async Task<string?> DefaultPromptMenuRaisingHandler(IEnumerable<string> options, string defaultOption)
        {
            return await webViewInteractionController.ShowMenu(options, defaultOption);
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