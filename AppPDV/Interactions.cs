using PGW.Dll;
using static PGW.CustomObjects;
using static PGW.Enums;

namespace AppPDV
{
    public enum PromptResult
    {
        OK,
        Cancel,
    }
    
    public delegate void OnMessageRaisingEventHandler(string message);
    public delegate PromptResult OnPromptRaisingEventHandler(string message);

    public class Interactions
    {
        public event OnMessageRaisingEventHandler? MessageRaising;
        public event OnPromptRaisingEventHandler? PromptRaising;

        private readonly Dictionary<E_PWDAT, Func<PW_GetData, E_PWRET>> actions_map;
        private readonly PINPadInteractions pinPadInteractions;

        public Interactions(Func<E_PWRET> loopPP)
        {
            pinPadInteractions = new PINPadInteractions(loopPP);
            actions_map = new Dictionary<E_PWDAT, Func<PW_GetData, E_PWRET>>
            {
                { E_PWDAT.PWDAT_BARCODE, Input },
                { E_PWDAT.PWDAT_MENU, InputFromMenu },
                { E_PWDAT.PWDAT_TYPED, Input },
                { E_PWDAT.PWDAT_USERAUTH, Input },
                { E_PWDAT.PWDAT_CARDINF, Input },
                { E_PWDAT.PWDAT_DSPCHECKOUT, Prompt },
                { E_PWDAT.PWDAT_DSPQRCODE, QrCode },
                { E_PWDAT.PWDAT_PPENCPIN, (aaa) => E_PWRET.PWRET_BLOCKED }
            };
        }

        public E_PWRET? Interact(PW_GetData data, ushort index)
        {
            E_PWDAT option = (E_PWDAT)data.bTipoDeDado;
            if (actions_map.TryGetValue(option, out var action))
            {
                // Executa qualquer ação que não seja captura de dados do cartão (PWDAT_CARDINF) digitado (PW_GetData.ulTipoEntradaCartao = 1)
                // Do contrário, não achando nenhuma ação ou tendo achando PWDAT_CARDINF porém lido pelo PIN-pad
                // Deixa a execução para PINPadInteractions.
                if (option != E_PWDAT.PWDAT_CARDINF || (option == E_PWDAT.PWDAT_CARDINF && data.ulTipoEntradaCartao == 1)) {
                    return action(data);
                }
            }

            return pinPadInteractions.Interact(option, index);
        }

        public void RaiseMessage(string message)
        {
            MessageRaising?.Invoke(message);
        }

        public PromptResult? RaisePrompt(string message)
        {
            return PromptRaising?.Invoke(message);
        }

        private E_PWRET Input(PW_GetData data)
        {
            return E_PWRET.PWRET_OK;
        }

        private E_PWRET InputFromMenu(PW_GetData data)
        {
            return E_PWRET.PWRET_OK;
        }

        private E_PWRET Prompt(PW_GetData data)
        {
            var message = data.szPrompt;
            var promptResult = PromptRaising?.Invoke(message);

            if (promptResult == PromptResult.Cancel)
            {
                // Aborta a operação em curso no PIN-pad
                Interop.PW_iPPAbort();

                // Sinaliza a exibição da mensagem para a biblioteca
                Interop.PW_iAddParam(data.wIdentificador, string.Empty);

                // Retorna operação cancelada
                return E_PWRET.PWRET_CANCEL;
            }
            
            return E_PWRET.PWRET_OK;
        }

        private E_PWRET QrCode(PW_GetData data)
        {
            // // Exemplo 2: A string com o QR Code é recebida e um QRcode é gerado utilizando uma biblioteca 
            // // de terceiros, para compilar essa opção é necessário descomentar a função AtualizaQRCode na classe FormDisplayQRcode
            // // e instalar a biblioteca  MessagingToolkit.QRCode em seu Visual studio através do gerenciador de pacotes
            // // com o comando "Install-Package MessagingToolkit.QRCode -ProjectName PGWLib"
            // StringBuilder stringQRcode = new StringBuilder(5001);

            // // Tenta obter o valor do QRcode a ser exibido, caso não ache retorna operação cancelada
            // if (Interop.PW_iGetResult((short)E_PWINFO.PWINFO_AUTHPOSQRCODE, stringQRcode, 5001) != (int)E_PWRET.PWRET_OK)
            //     return (int)E_PWRET.PWRET_CANCEL;

            // // Exibe o QRcode e o prompt
            // fdqr.Start();

            // // Para esse caso em específico o QR code é muito grande para exibir no display "padrão"
            // // Somente para esse exemplo, liga o autoSize da janela
            // fdqr.ChangeText(item.szPrompt, stringQRcode.ToString());

            // // Caso o operador tenha apertado a tecla ESC, cancela a operação e aborta o comando do PINpad
            // if (fdqr.isAborted())
            // {
            //     // Aborta a operação em curso no PIN-pad
            //     Interop.PW_iPPAbort();

            //     fdqr.Stop();

            //     // Atribui o retorno de aoperação cancelada
            //     return (int)E_PWRET.PWRET_CANCEL;
            // }
            
            // // Sinaliza a exibição do QRcode para a biblioteca
            // ret = Interop.PW_iAddParam(item.wIdentificador, "");
            // return (int)E_PWRET.PWRET_OK;

            // VOU RETORNAR SEMPRE CANCELADA
            // TODO: Implementar esta parte depois
            return E_PWRET.PWRET_CANCEL;
        }
    }
}