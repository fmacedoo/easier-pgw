using System.Runtime.InteropServices;
using System.Text.Json;
using static PGW.Enums;

namespace AppPDV
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class ProcessGateway
    {
        private readonly PGW pgw;

        public ProcessGateway()
        {
            pgw = new PGW(
                DefaultMessageRaisingHandler,
                DefaultPromptConfirmationRaisingHandler,
                DefaultPromptInputRaisingHandler,
                DefaultPromptMenuRaisingHandler
            );
        }

        private void DefaultMessageRaisingHandler(string message, int? timeoutToClose = null)
        {
            PromptBox.Show(message, timeoutToClose);
        }

        private PromptConfirmationResult DefaultPromptConfirmationRaisingHandler(string message, int? timeoutToClose = null)
        {
            var result = PromptBox.ShowConfirmation("101", message, timeoutToClose);
            return result ? PromptConfirmationResult.OK : PromptConfirmationResult.Cancel;
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