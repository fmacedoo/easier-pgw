using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;

namespace AppPDV
{
    public class WebViewInteractionController
    {
        private static class Scripts
        {
            public static Func<string, string> GetShowScript(string message) => requestId => $"gateway.on_message('{requestId}', '{cleanupMessage(message)}');";
            public static Func<string, string> GetShowConfirmationScript(string message) => requestId => $"gateway.on_message_confirmation('{requestId}', '{cleanupMessage(message)}');";
            public static Func<string, string> GetShowMenu(IEnumerable<string> options, string defaultOption) => requestId => $"gateway.on_menu('{requestId}', {JsonSerializer.Serialize(options)}, '{defaultOption}');";
            public static Func<string, string> GetShowPrompt(PromptConfig config) => requestId => $"gateway.on_prompt('{requestId}', {JsonSerializer.Serialize(config)});";
            public static string GetCloseScript() => $"gateway.close()";
            public static string GetSuccessScript() => $"gateway.success()";
            public static string GetOnInitScript() => $"gateway.on_init()";

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
            Logger.Info("WebViewInteractionController constructor");
            this.webView = webView;
        }

        private readonly HashSet<string> interactions = new HashSet<string>();
        private readonly HashSet<string> aborted = new HashSet<string>();
        private readonly Dictionary<string, string> confirmed = new Dictionary<string, string>();

        public async Task<string?> NotifyMessage(string message, int? timeoutToClose = null)
        {
            return await SetScript(Scripts.GetShowScript(message), timeoutToClose);
        }

        public async Task<string?> NotifyMessageWithConfirmation(string message, int? timeoutToClose = null)
        {
            return await SetScript(Scripts.GetShowConfirmationScript(message), timeoutToClose);
        }

        public async Task<string?> NotifyMenu(IEnumerable<string> options, string defaultOption)
        {
            return await SetScript(Scripts.GetShowMenu(options, defaultOption));
        }

        public async Task<string?> NotifyPrompt(PromptConfig config)
        {
            return await SetScript(Scripts.GetShowPrompt(config));
        }

        public void Abort(string? requestId = null)
        {
            if (requestId != null)
            {
                interactions.Remove(requestId);
                aborted.Add(requestId);
            }

            _ = ExecuteScript(Scripts.GetCloseScript());
        }

        public void Confirm(string requestId, string value)
        {
            interactions.Remove(requestId);
            confirmed.Add(requestId, value);

            _ = ExecuteScript(Scripts.GetCloseScript());
        }

        public void NotifyInit()
        {
            _ = ExecuteScript(Scripts.GetOnInitScript());
        }

        public void NotifySuccess()
        {
            _ = ExecuteScript(Scripts.GetSuccessScript());
        }

        private async Task<string?> SetScript(Func<string, string> scriptBuilder, int? timeoutToClose = null)
        {
            var requestId = Guid.NewGuid().ToString();
            interactions.Add(requestId);

            Logger.Debug($"Bulding script: timeout={timeoutToClose}");
            var script = scriptBuilder(requestId);
            await ExecuteScript(script);
            Logger.Debug("Script called!");

            if (timeoutToClose.HasValue)
            {
                _ = Task.Run(() =>
                {
                    Thread.Sleep(timeoutToClose.Value);
                    if (interactions.Contains(requestId))
                    {
                        interactions.Remove(requestId);
                        _ = ExecuteScript(Scripts.GetCloseScript());
                    }
                });
            }

            while (interactions.Contains(requestId))
            {
                Thread.Sleep(200);
            }

            bool isAborted = aborted.Remove(requestId);
            string? value = confirmed.ContainsKey(requestId) ? confirmed[requestId] : null;

            if (isAborted) return null;
            if (value != null) return value;

            return "confirmed";
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