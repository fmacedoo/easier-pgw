using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;

namespace AppPDV
{
    

    public class WebViewInteractionController
    {
        private static class Scripts
        {
            public static Func<string, string> GetShowScript(string message) => requestId => $"gateway.show_message('{requestId}', '{cleanupMessage(message)}')";
            public static Func<string, string> GetShowConfirmationScript(string message) => requestId => $"gateway.show_message_confirmation('{requestId}', '{cleanupMessage(message)}')";
            public static string GetCloseScript() => $"gateway.close()";

            private static string cleanupMessage(string message)
            {
                var pieces = message
                    .TrimStart('\r')
                    .Trim()
                    .Replace("\r", "\n")
                    .Split("\n")
                    .Select(line =>
                        line.Trim()
                    )
                    .Where(line => !string.IsNullOrEmpty(line));
                return string.Join("\\n", pieces);
            }
        }
        
        private readonly WebView2 webView;

        public WebViewInteractionController(WebView2 webView)
        {
            this.webView = webView;
        }

        private readonly HashSet<string> timeouts = new HashSet<string>();
        private readonly HashSet<string> aborted = new HashSet<string>();

        public async Task<bool> Show(string message, int? timeoutToClose = null)
        {
            return await SetScript(Scripts.GetShowScript(message), timeoutToClose);
        }

        public async Task<bool> ShowWithConfirmation(string message, int? timeoutToClose = null)
        {
            return await SetScript(Scripts.GetShowConfirmationScript(message), timeoutToClose);
        }

        public void Abort(string? requestId = null)
        {
            if (requestId != null)
            {
                timeouts.Remove(requestId);
                aborted.Add(requestId);
            }

            _ = ExecuteScript(Scripts.GetCloseScript());
        }

        private async Task<bool> SetScript(Func<string, string> scriptBuilder, int? timeoutToClose = null)
        {
            var requestId = Guid.NewGuid().ToString();
            timeouts.Add(requestId);

            Logger.Debug($"Bulding script: timeout={timeoutToClose}");
            var script = scriptBuilder(requestId);
            await ExecuteScript(script);
            Logger.Debug("Script called!");

            if (timeoutToClose.HasValue)
            {
                _ = Task.Run(() => {
                    Thread.Sleep(timeoutToClose.Value);
                    if (timeouts.Contains(requestId)) {
                        timeouts.Remove(requestId);
                        _ = ExecuteScript(Scripts.GetCloseScript());
                    }
                });
            }

            while (timeouts.Contains(requestId))
            {
                Thread.Sleep(200);
            }

            return !aborted.Remove(requestId);
        }

        private async Task ExecuteScript(string script)
        {
            Logger.Info("ExecuteScript");
            if (webView.InvokeRequired)
            {
                Logger.Info("Invoking");
                await webView.Invoke(Run);
                return;
            }

            await Run();

            async Task Run()
            {
                if (webView != null && webView.CoreWebView2 != null)
                {
                    Logger.Debug($"Script: {script}");
                    await webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
        }
    }
}