using static PGW.Enums;
using System.Text.Json.Serialization;

namespace AppPDV
{
    public enum PromptFieldType
    {
        Alpha,
        Password,
        Numeric,
        AlphaNumeric,
    }

    public class PromptConfig
    {
        [JsonPropertyName("identifier")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public E_PWINFO Identifier { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("initialValue")]
        public string? InitialValue { get; set; }

        [JsonPropertyName("inputMask")]
        public string? InputMask { get; set; }

        [JsonPropertyName("maxLength")]
        public byte? MaxLength { get; set; }

        [JsonPropertyName("inputType")]
        public PromptFieldType InputType { get; set; }
    }

    public delegate Task OnMessageRaisingEventHandler(string message, int? timeoutToClose = null);
    public delegate Task<PromptConfirmationResult> OnPromptConfirmationRaisingEventHandler(string message, int? timeoutToClose = null);
    public delegate Task<string?> OnPromptInputRaisingEventHandler(PromptConfig config);
    public delegate Task<string?> OnPromptMenuRaisingEventHandler(IEnumerable<string> options, string defaultOption);
}