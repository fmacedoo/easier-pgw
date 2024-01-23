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

        private async Task DefaultMessageRaisingHandler(string message, int? timeoutToClose = null)
        {
            await webViewInteractionController.NotifyMessage(message, timeoutToClose);
        }

        private async Task<PromptConfirmationResult> DefaultPromptConfirmationRaisingHandler(string message, int? timeoutToClose = null)
        {
            var confirmed = await webViewInteractionController.NotifyMessage(message, timeoutToClose);
            return confirmed != null ? PromptConfirmationResult.OK : PromptConfirmationResult.Cancel;
        }

        private async Task<string?> DefaultPromptInputRaisingHandler(PromptConfig config)
        {
            if (config.Identifier == E_PWINFO.PWINFO_AUTHTECHUSER) return AppSettings.Instance.PGW?.SENHA_TECNICA;
            if (config.Identifier == E_PWINFO.PWINFO_POSID) return AppSettings.Instance.PGW?.PONTO_CAPTURA;
            if (config.Message == "CNPJ/CPF:") return AppSettings.Instance.PGW?.CPNJ;
            if (config.Message == "NOME/IP SERVIDOR:") return AppSettings.Instance.PGW?.SERVIDOR;

            return await webViewInteractionController.NotifyPrompt(config);
        }

        private async Task<string?> DefaultPromptMenuRaisingHandler(IEnumerable<string> options, string defaultOption)
        {
            return await webViewInteractionController.NotifyMenu(options, defaultOption);
        }

        #region Interaction Methods

        public void NotifyInit()
        {
            webViewInteractionController.NotifyInit();
        }

        public void abort(string? requestId = null)
        {
            webViewInteractionController.Abort(requestId);
        }

        public void confirm(string requestId, string value)
        {
            webViewInteractionController.Confirm(requestId, value);
        }

        #endregion

        #region Data Methods

        public string?[] devices()
        {
            Logger.Info("devices:");
            var devices = DeviceManagement.List();
            Logger.Debug(JsonSerializer.Serialize(devices));
            return devices;
        }

        #endregion

        #region PayGo Methods

        public void installation()
        {
            Task.Run(() => {
                E_PWRET result = pgw.Installation();
                Logger.Debug($"Installation result: {result}");
                if (result == E_PWRET.PWRET_OK) webViewInteractionController.NotifySuccess();
            });
        }

        public Operation[] operations()
        {
            var operations = pgw.GetOperations();
            var result = new Operation[operations.Count];
            var i = 0;
            foreach (var op in operations)
            {
                result[i++] = new Operation
                {
                    bOperType = op.bOperType,
                    szText = op.szText,
                    szValue = op.szValue,
                };
            }

            return result;
        }

        public void payment()
        {
            Task.Run(() => {
                Logger.Info("Payment");
                E_PWRET result = pgw.Operation(E_PWOPER.PWOPER_SALE);
                if (result == E_PWRET.PWRET_OK) webViewInteractionController.NotifySuccess();
            });
        }

        #endregion
    }

    public class Operation
    {
        public byte bOperType;
        public string? szText;
        public string? szValue;
    }
}