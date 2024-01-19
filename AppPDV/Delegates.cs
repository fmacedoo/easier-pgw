namespace AppPDV
{
    public delegate Task OnMessageRaisingEventHandler(string message, int? timeoutToClose = null);
    public delegate Task<PromptConfirmationResult> OnPromptConfirmationRaisingEventHandler(string message, int? timeoutToClose = null);
    public delegate Task<string?> OnPromptInputRaisingEventHandler(string message);
    public delegate Task<string?> OnPromptMenuRaisingEventHandler(IEnumerable<string> options, string defaultOption);
}