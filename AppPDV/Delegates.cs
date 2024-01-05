namespace AppPDV
{
    public delegate void OnMessageRaisingEventHandler(string message);
    public delegate PromptConfirmationResult OnPromptConfirmationRaisingEventHandler(string message);
    public delegate string? OnPromptInputRaisingEventHandler(string message);
    public delegate string? OnPromptMenuRaisingEventHandler(IEnumerable<string> options);
}