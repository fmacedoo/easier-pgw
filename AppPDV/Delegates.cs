namespace AppPDV
{
    public delegate void OnMessageRaisingEventHandler(string message, int? timeoutToClose = null);
    public delegate PromptConfirmationResult OnPromptConfirmationRaisingEventHandler(string message, int? timeoutToClose = null);
    public delegate string? OnPromptInputRaisingEventHandler(string message);
    public delegate string? OnPromptMenuRaisingEventHandler(IEnumerable<string> options);
}