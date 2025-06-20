using PGW.Dll;
using static PGW.CustomObjects;
using static PGW.Enums;

namespace EasierPGW
{
    public class Interactions
    {
        public event OnMessageRaisingEventHandler? MessageRaising;
        public event OnPromptConfirmationRaisingEventHandler? PromptConfirmationRaising;
        public event OnPromptInputRaisingEventHandler? PromptInputRaising;
        public event OnPromptMenuRaisingEventHandler? PromptMenuRaising;

        private readonly Dictionary<E_PWDAT, Func<PW_GetData, E_PWRET>> actions_map;
        private readonly PINPadInteractions pinPadInteractions;

        public Interactions(Func<E_PWRET> loopPP)
        {
            Logger.Info("Interactions constructor");
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
            };
        }

        public E_PWRET? Interact(PW_GetData data, ushort index)
        {
            Logger.Info("Interact");
            E_PWDAT option = (E_PWDAT)data.bTipoDeDado;
            Logger.Debug($"Interact: {Enum.GetName(typeof(E_PWDAT), option)}");
            if (actions_map.TryGetValue(option, out var action))
            {
                // Executa qualquer ação que não seja captura de dados do cartão (PWDAT_CARDINF) digitado (PW_GetData.ulTipoEntradaCartao = 1)
                // Do contrário, não achando nenhuma ação ou tendo achando PWDAT_CARDINF porém lido pelo PIN-pad
                // Deixa a execução para PINPadInteractions.
                if (option != E_PWDAT.PWDAT_CARDINF || (option == E_PWDAT.PWDAT_CARDINF && data.ulTipoEntradaCartao == 1))
                {
                    return action(data);
                }
            }

            return pinPadInteractions.Interact(option, index);
        }

        public void RaiseMessage(string message, int? timeoutToClose = null)
        {
            MessageRaising?.Invoke(message, timeoutToClose);
        }

        public PromptConfirmationResult? RaisePromptConfirmation(string message, int? timeoutToClose = null)
        {
            var promptTask = PromptConfirmationRaising?.Invoke(message, timeoutToClose);
            promptTask?.Wait();
            return promptTask != null ? promptTask.Result : null;
        }

        private E_PWRET Input(PW_GetData data)
        {
            Logger.Info("Interactions.Input");

            if (data.wIdentificador == (ushort)E_PWINFO.PWINFO_AUTHMNGTUSER)
            {
                // Exemplificando a captura de uma senha de lojista de até 4 dígitos
                data.szPrompt = "INSIRA A SENHA DO LOJISTA";
                data.bTamanhoMaximo = 4;
            }
            if (data.wIdentificador == (ushort)E_PWINFO.PWINFO_AUTHTECHUSER)
            {
                // Exemplificando a captura de uma senha de lojista de até 10 dígitos
                data.szPrompt = "INSIRA A SENHA TÉCNICA";
                data.bTamanhoMaximo = 10;
            }

            // Se não existir mascara de captura, cria uma padrão baseado no tamanho máximo
            if (data.szMascaraDeCaptura == "")
            {
                data.szMascaraDeCaptura = new string('@', data.bTamanhoMaximo);
            }
            else
            {
                // Ajuste necessário para exibição da moeda
                if (data.szMascaraDeCaptura.Contains("R$"))
                {
                    data.szMascaraDeCaptura = data.szMascaraDeCaptura.Replace("R$", "");
                }
            }

            var config = new PromptConfig
            {
                Identifier = (E_PWINFO)data.wIdentificador,
                Message = data.szPrompt.Replace("\r", "\n"),
                MaxLength = data.bTamanhoMaximo > 0 ? data.bTamanhoMaximo : null,
                InitialValue = (!string.IsNullOrEmpty(data.szValorInicial.Trim())) ? data.szValorInicial : null,
            };

            switch ((E_PWTypeInput)data.bTiposEntradaPermitidos)
            {
                case E_PWTypeInput.TYPED_NUMERIC:
                    config.InputMask = data.szMascaraDeCaptura.Replace("@", "0");
                    config.InputType = PromptFieldType.Numeric;
                    break;
                case E_PWTypeInput.TYPED_ALPHA:
                    config.InputMask = data.szMascaraDeCaptura.Replace("@", "L");
                    config.InputType = PromptFieldType.Alpha;
                    break;
                case E_PWTypeInput.TYPED_ALPHANUMERIC:
                    config.InputMask = data.szMascaraDeCaptura.Replace("@", "a");
                    config.InputType = PromptFieldType.AlphaNumeric;
                    break;
                case E_PWTypeInput.TYPED_ALPHANUMERICSP:
                default:
                    config.InputMask = data.szMascaraDeCaptura.Replace("@", "C");
                    config.InputType = PromptFieldType.AlphaNumeric;
                    break;
            }

            // Não achei este tratamento necessário, ele deve considerar um CPF/CNPJ um valor a ser digitado da esquerda p/ direita.
            // Não acho a melhor opção!
            // // Define se o preenchimento deve ser feito da direita pra esquerda (um valor
            // // em dinheiro, por exemplo)
            // if (data.bIniciaPelaEsquerda != 1)
            //     config.InputType = FieldType.Numeric;

            // Casp seja uma das senhas ou a biblioteca tenha solicitado que o dado
            // seja capturado de forma mascarada
            if (data.wIdentificador == (ushort)E_PWINFO.PWINFO_AUTHMNGTUSER ||
                data.wIdentificador == (ushort)E_PWINFO.PWINFO_AUTHTECHUSER ||
                data.bOcultarDadosDigitados == 1)
                config.InputType = PromptFieldType.Password;
            
            var promptTask = PromptInputRaising?.Invoke(config);
            promptTask?.Wait();

            // Caso o usuário tenha abortado a transação, retorna E_PWRET.PWRET_CANCEL
            if (promptTask?.Result is null)
            {
                return E_PWRET.PWRET_CANCEL;
            }

            // Adiciona o dado capturado
            string value = promptTask.Result;
            E_PWRET result = (E_PWRET)Interop.PW_iAddParam(data.wIdentificador, value);

            // Registra na janela de debug o resultado da adição do parâmetro
            Logger.Debug(string.Format("Interactions.Input: PW_iAddParam({0},{1})={2}", ((E_PWINFO)data.wIdentificador).ToString(), value, result.ToString()));

            return result;
        }

        private E_PWRET InputFromMenu(PW_GetData data)
        {
            Logger.Info("Interactions.InputFromMenu");

            var options = new List<string>();
            for (byte b = 0; b < data.bNumOpcoesMenu; b++)
            {
                if (data.bTeclasDeAtalho == 1 && b < 10)
                {
                    options.Add(string.Format("{0}-{1}", b, data.vszTextoMenu[b].szTextoMenu));
                }
                else
                {
                    options.Add(string.Format("{0}", data.vszTextoMenu[b].szTextoMenu));
                }
            }

            // Caso o menu só tenha uma opção e ela seja a opção default, seleciona automaticamente
            // Caso ela não seja a opção defualt, necessário exibir para confirmação do usuário (PromptMenuRaising)
            var option =
                (data.bNumOpcoesMenu == 1 && data.bItemInicial == 0) ?
                    data.vszValorMenu[0].szValorMenu :
                    null;

            if (option == null) {
                var defaultOption = options[data.bItemInicial];
                var promptTask = PromptMenuRaising?.Invoke(options, defaultOption);
                promptTask?.Wait();
                if (promptTask?.Result != null) {
                    string value = promptTask.Result;
                    option = data.vszValorMenu[options.IndexOf(value)].szValorMenu;
                }
            }

            if (option is null) return E_PWRET.PWRET_CANCEL;

            E_PWRET result = (E_PWRET)Interop.PW_iAddParam(data.wIdentificador, option);

            // Registra na janela de debug o resultado da adição do parâmetro
            Logger.Debug(string.Format("Interactions.InputFromMenu: PW_iAddParam({0},{1})={2}", ((E_PWINFO)data.wIdentificador).ToString(), option, result.ToString()));

            return result;
        }

        private E_PWRET Prompt(PW_GetData data)
        {
            var message = data.szPrompt;
            var promptResult = PromptConfirmationRaising?.Invoke(message).Result;

            if (promptResult == PromptConfirmationResult.Cancel)
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