using System.Runtime.InteropServices;
using System.Text.Json;
using static PGW.CustomObjects;
using static PGW.Enums;

namespace AppPDV
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class PGWGateway
    {
        PGW pgw;

        public PGWGateway(
            OnMessageRaisingEventHandler onMessageRaising,
            OnPromptConfirmationRaisingEventHandler onPromptConfirmationRaising,
            OnPromptInputRaisingEventHandler onPromptInputRaising,
            OnPromptMenuRaisingEventHandler onPromptMenuRaising
        )
        {
            pgw = new PGW(
                onMessageRaising,
                onPromptConfirmationRaising,
                onPromptInputRaising,
                onPromptMenuRaising
            );
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