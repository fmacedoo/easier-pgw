using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;
using static PGW.Enums;

namespace AppPDV
{
    static class Scripts
    {
        public static string GetShowScript(string requestId, string message) => $"gateway.show_message('{requestId}', '{message}')";
        public static string GetShowConfirmationScript(string message) => $"gateway.show_message_confirmation('{message}')";
        public static string GetCloseScript() => $"gateway.close()";
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class ProcessGateway
    {
        private readonly PGW pgw;
        private readonly WebView2 webView;
        private readonly HashSet<string> timeouts = new HashSet<string>();
        private readonly HashSet<string> aborted = new HashSet<string>();

        public ProcessGateway(WebView2 webView)
        {
            this.webView = webView;
            pgw = new PGW(
                DefaultMessageRaisingHandler,
                DefaultPromptConfirmationRaisingHandler,
                DefaultPromptInputRaisingHandler,
                DefaultPromptMenuRaisingHandler
            );
        }

        public async Task<bool> test_message()
        {
            var requestId = Guid.NewGuid().ToString();
            timeouts.Add(requestId);

            Logger.Debug("Calling test_message()");
            await CallJavaScriptFunction(Scripts.GetShowScript(requestId, "test_message from C#"));
            Logger.Debug("Called!");

            int? timeoutToClose = 3000;
            if (timeoutToClose.HasValue)
            {
                _ = Task.Run(() => {
                    Thread.Sleep(timeoutToClose.Value);
                    if (timeouts.Contains(requestId)) {
                        timeouts.Remove(requestId);
                        _ = CallJavaScriptFunction(Scripts.GetCloseScript());
                    }
                });
            }

            while (timeouts.Contains(requestId))
            {
                Thread.Sleep(200);
            }

            return !aborted.Remove(requestId);
        }

        public void test_message_confirmation_async()
        {
            Task.Run(async () => {
                Thread.Sleep(1000);
                var confirmed = await test_message_confirmation();
                Logger.Debug($"After test_message_confirmation (aborted={!confirmed})");
            });
        }

        public async Task<bool> test_message_confirmation()
        {
            var requestId = Guid.NewGuid().ToString();
            timeouts.Add(requestId);

            Logger.Debug("Calling test_message()");
            await CallJavaScriptFunction(Scripts.GetShowScript(requestId, "test_message_confirmation from C#"));
            Logger.Debug("Called!");

            int? timeoutToClose = 3000;
            if (timeoutToClose.HasValue)
            {
                _ = Task.Run(() => {
                    Thread.Sleep(timeoutToClose.Value);
                    if (timeouts.Contains(requestId)) {
                        timeouts.Remove(requestId);
                        _ = CallJavaScriptFunction(Scripts.GetCloseScript());
                    }
                });
            }

            while (timeouts.Contains(requestId))
            {
                Thread.Sleep(200);
            }

            return !aborted.Remove(requestId);
        }

        public void abort(string? requestId = null)
        {
            if (requestId != null)
            {
                timeouts.Remove(requestId);
                aborted.Add(requestId);
            }


            Logger.Debug("Calling close()");
            _ = CallJavaScriptFunction(Scripts.GetCloseScript());
        }

        private async Task CallJavaScriptFunction(string script)
        {
            Logger.Info("CallJavaScriptFunction:");
            if (webView.InvokeRequired)
            {
                Logger.Info("Invoking:");
                await webView.Invoke(Invoke);
                return;
            }

            await Invoke();

            async Task Invoke()
            {
                if (webView != null && webView.CoreWebView2 != null)
                {
                    Logger.Debug($"script: {script}");
                    await webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
        }

        private void DefaultMessageRaisingHandler(string message, int? timeoutToClose = null)
        {
            PromptBox.Show(message, timeoutToClose);
            // await CallJavaScriptFunction(Scripts.GetShowScript(message));
            
            // if (timeoutToClose.HasValue)
            // {
            //     Thread.Sleep(timeoutToClose.Value);
            //     _ = CallJavaScriptFunction(Scripts.GetCloseScript());
            // }
        }

        private PromptConfirmationResult DefaultPromptConfirmationRaisingHandler(string message, int? timeoutToClose = null)
        {
            var result = PromptBox.ShowConfirmation("101", message, timeoutToClose);
            return result ? PromptConfirmationResult.OK : PromptConfirmationResult.Cancel;

            // await CallJavaScriptFunction(Scripts.GetShowConfirmationScript(message));

            // CallJavaScriptFunction("listen_abort")
            
            // if (timeoutToClose.HasValue)
            // {
            //     Thread.Sleep(timeoutToClose.Value);
            //     _ = CallJavaScriptFunction("gateway_escape", "");
            // }
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

        public E_PWRET installation()
        {
            return pgw.Installation();
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

        public E_PWRET payment()
        {
            Logger.Info("Payment");
            E_PWRET result = pgw.Operation(E_PWOPER.PWOPER_SALE);

            Logger.Debug($"Payment result: {result}");
            return result;
        }
    }
}