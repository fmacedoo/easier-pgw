namespace AppPDV
{
    public delegate Task OnMessageRaisingEventHandler(string message, int? timeoutToClose = null);
    public delegate Task<PromptConfirmationResult> OnPromptConfirmationRaisingEventHandler(string message, int? timeoutToClose = null);
    public delegate string? OnPromptInputRaisingEventHandler(string message);
    public delegate string? OnPromptMenuRaisingEventHandler(IEnumerable<string> options);
}