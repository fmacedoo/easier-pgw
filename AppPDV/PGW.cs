using System.Runtime.InteropServices;
using System.Text;
using PGW.Dll;
using static PGW.CustomObjects;
using static PGW.Enums;

namespace AppPDV
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class PGW : IPGW
    {
        private readonly Interactions interactions;

        public PGW(
            OnMessageRaisingEventHandler onMessageRaising,
            OnPromptConfirmationRaisingEventHandler onPromptConfirmationRaising,
            OnPromptInputRaisingEventHandler onPromptInputRaising,
            OnPromptMenuRaisingEventHandler onPromptMenuRaising
        )
        {
            interactions = new Interactions(LoopPP);

            interactions.MessageRaising += onMessageRaising;
            interactions.PromptConfirmationRaising += onPromptConfirmationRaising;
            interactions.PromptInputRaising += onPromptInputRaising;
            interactions.PromptMenuRaising += onPromptMenuRaising;

            Init();
        }

        public List<PW_Operations> GetOperations()
        {
            Logger.Info("GetOperations");
            
            // Lista com as operações a serem retornadas
            List<PW_Operations> returnList = new List<PW_Operations>();

            // Estruturas auxiliares para acionamento da biblioteca
            PW_Operations[] operations = new PW_Operations[50];
            short operationsNum=50;
            byte operType = (byte)E_PWOperType.ADMIN + (byte)E_PWOperType.SALE;

            // Executa o comando que obtem as operações
            E_PWRET result = (E_PWRET)Interop.PW_iGetOperations(operType, operations, ref operationsNum);

            // Registra na janela de debug o comando
            Logger.Debug(string.Format("PW_iGetOperations={0}", result.ToString()));

            // Caso tenham sido obtidas perações, monta a lista de retorno
            if (result == E_PWRET.PWRET_OK)
            {
                // Percorre todas as operações possíveis
                for (short n = 0; n < operationsNum; n++)
                {
                    // Adiciona a operação
                    returnList.Add(operations[n]);

                    // Registra na janela de debug cada operação permitida
                    Logger.Debug(string.Format("GetOperations: PW_Operations ({0},{1},{2}", operations[n].bOperType.ToString(),
                        operations[n].szText, operations[n].szValue));
                }
            }

            return returnList;
        }

        public E_PWRET Installation()
        {
            Logger.Info("Installation");
            E_PWRET result = Operation(E_PWOPER.PWOPER_INSTALL);

            Logger.Debug($"Installation result: {result}");
            return result;
        }

        public E_PWRET Operation(E_PWOPER operation)
        {
            E_PWRET pendencyResult = PendencyResolve();
            if (pendencyResult != E_PWRET.PWRET_OK)
                return pendencyResult;
            
            E_PWRET prepareResult = PrepareTransaction(operation);
            if (prepareResult != E_PWRET.PWRET_OK)
                return prepareResult;

            E_PWRET executeResult = ExecuteTransaction();
            List<PW_Parameter> transactionResponse = TransactionResponse();

            string message = ResolveTransactionResponse(executeResult, transactionResponse);
            interactions.RaiseMessage(message);

            if (executeResult != E_PWRET.PWRET_OK)
            {
                if (executeResult == E_PWRET.PWRET_FROMHOSTPENDTRN)
                {
                    ShowAutomationPendingTransaction(transactionResponse);
                    ConfirmUndoPendingTransaction(E_PWCNF.PWCNF_REV_ABORT, transactionResponse);
                }
            }

            if (IsConfirmationRequired(transactionResponse))
            {
                // TODO: they must be option to let the user pick one
                // foreach (string item in Enum.GetNames(typeof(E_PWCNF)))
                // {
                //     cmbtransactionStatuses.Items.Add(item);
                // }

                E_PWCNF transactionStatus = E_PWCNF.PWCNF_REV_ABORT;
                ConfirmUndoNormalTransaction(transactionStatus, transactionResponse);
            }

            return executeResult;
        }

        private bool IsConfirmationRequired(List<PW_Parameter> transactionResponse)
        {
            PW_Parameter? confirmacaoNecessaria = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_CNFREQ);
            return confirmacaoNecessaria != null && int.Parse(confirmacaoNecessaria.parameterValue) == 1;
        }

        private E_PWRET PendencyResolve()
        {
            // Antes de iniciar uma nova transação, resolve possíveis pendência existentes
            // Caso ocorra algum problema na resolução de pendência retorna o erro

            // Nessa função é necessário implementar na automação, de acordo com o tipo de persistência
            // de dados, a checagem se existe alguma transação necessitando de resolução de pendência
            // e, caso positivo, obter os identificadores dela que foram persistidos anteriormente:
            // PWINFO_REQNUM
            // PWINFO_AUTLOCREF
            // PWINFO_AUTEXTREF
            // PWINFO_VIRTMERCH
            // PWINFO_AUTHSYST
            // Após isso, chamar a função PW_iConfirmation para resolver e:
            //      Caso ocorra algum erro nessa chamada, não desmarcar a resolução de pendência 
            //      em disco e retornar erro, abortando a transação em curso.
            //      Caso a chamada retorne PWRET_OK, desmarcar a resolução de pendência em disco e
            //      prosseguir normalmente com a transação em curso.

            return E_PWRET.PWRET_OK;
        }

        private E_PWRET PendencyWrite(E_PWCNF transactionStatus)
        {
            // Sempre é necessário, antes de marcar este desfazimento, verificar se a transação necessita
            // de resolução de pendência através da obtenção do dado PWINFO_CNFREQ, caso esse valor 
            // seja "0", o tratamento abaixo nã é necessário para a transação corrente.

            // Nessa função é necessário implementar na automação, de acordo com o tipo de persistência
            // de dados, a obtenção da biblioteca dos identificadores da transação através de 
            // PW_iGetResult e armazená-los em disco:
            // PWINFO_REQNUM
            // PWINFO_AUTLOCREF
            // PWINFO_AUTEXTREF
            // PWINFO_VIRTMERCH
            // PWINFO_AUTHSYST
            // Bem como o status a ser utilizado para a resolução de sua pendencia "transactionStatus"

            return E_PWRET.PWRET_OK;
        }

        private E_PWRET PendencyDelete()
        {
            // Nessa função é necessário implementar na automação, de acordo com o tipo de persistência
            // de dados a exclusão de qualquer resolução de pendência que possa estar armazenada.

            return E_PWRET.PWRET_OK;
        }

        private E_PWRET PrepareTransaction(E_PWOPER operation)
        {
            Logger.Info("PreparingTransaction");
            
            // Inicializa a transação solicitada
            // Caso ocorra algum problema na inicialização retorna o erro
            Logger.Debug("PreparingTransaction: PW_iNewTransac");
            E_PWRET pw_iNewTransacResult = (E_PWRET)Interop.PW_iNewTransac((byte)operation);
            if (pw_iNewTransacResult != E_PWRET.PWRET_OK)
                return pw_iNewTransacResult;
            
            Logger.Debug("PreparingTransaction: setting default/mandatory parameters");
            int autCap = (int)E_PWAutCapabilities.FIXO + (int)E_PWAutCapabilities.CUPOMRED + (int)E_PWAutCapabilities.CUPOMDIF
                + (int)E_PWAutCapabilities.DSPCHECKOT + (int)E_PWAutCapabilities.DSPQRCODE;
            PW_Parameter[] parameters = {
                new PW_Parameter(E_PWINFO.PWINFO_AUTNAME.ToString(), (int)E_PWINFO.PWINFO_AUTNAME, "PDVS"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTVER.ToString(), (int)E_PWINFO.PWINFO_AUTVER, "1.0"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTDEV.ToString(), (int)E_PWINFO.PWINFO_AUTDEV, "PayGo Pagamentos"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTCAP.ToString(), (int)E_PWINFO.PWINFO_AUTCAP, autCap.ToString()),
                new PW_Parameter(E_PWINFO.PWINFO_DSPQRPREF.ToString(), (int)E_PWINFO.PWINFO_DSPQRPREF, ((int)E_PWQrcodePref.CHECKOUT).ToString()),
            };

            // Loop adicionando todos os parâmetros já capturados pela automação
            // Parâmetros já capturados devem ser adicionados, caso a biblioteca precise de qualquer
            // parâmetro que não foi recebido para finalizar a transação, ela irá solicitá-lo no fluxo
            // implementado em "executeTransaction"
            foreach (PW_Parameter parameter in parameters)
            {
                // Adiciona o parâmetro
                short addParamResult = Interop.PW_iAddParam(parameter.parameterCode, parameter.parameterValue);

                // Registra na janela de debug o resultado da adição do parâmetro
                Logger.Debug(string.Format("PreparingTransaction: PW_iAddParam ({0},{1})={2}", parameter.parameterName,
                    parameter.parameterValue, addParamResult.ToString()));
            }

            return pw_iNewTransacResult;
        }

        private E_PWRET ExecuteTransaction()
        {
            Logger.Info("ExecuteTransaction");

            // Loop que só será interrompido em caso da finalização da transação, seja ela por algum
            // tipo de erro ou com o sucesso
            while (true)
            {
                // Cria a estrutura necessária para executar a função PW_iExecTransac, caso seja 
                // necessário capturar algum dado, essa estrutura terá detalhes de como deverá ser feita
                // essa captura
                PW_GetData[] structParam = new PW_GetData[10];

                // Parâmetro que, na entrada, indica quantos registros possui a estrutura PW_GetData e
                // na saída indica quantos dados precisam ser capturados
                short numDados = 10;

                // Desmarca o desfazimento marcado por segurança, pois a transação irá para 
                // controle pela biblioteca
                E_PWRET pendencyDeleteResult = PendencyDelete();
                if (pendencyDeleteResult != E_PWRET.PWRET_OK)
                    return pendencyDeleteResult;

                // Chama a função que executa um passo da transação
                E_PWRET pw_iExecTransacResult = (E_PWRET)Interop.PW_iExecTransac(structParam, ref numDados);

                // Marca um desfazimento por segurança, caso a automação seja fechada abruptamente
                // durante qualquer passo abaixo, o desfazimento já estará armazenado em disco para
                // ser executado por PendencyResolve antes da próxima transação
                // Esse desfazimento será desmarcado em duas situações:
                // 1-) O loop foi executado novamente e PW_iExecTransac será chamada
                // 2-) Algum erro ocorreu durante o loop
                // 3-) A transação foi finalizada com sucesso, nesse caso o desfazimento permanecerá
                //     gravado até a execução da resolução de pendência da transação em 
                //     "ConfirmUndoNormalTransaction"
                E_PWRET pendencyWriteResult = PendencyWrite(E_PWCNF.PWCNF_REV_PWR_AUT);
                if (pendencyWriteResult != E_PWRET.PWRET_OK)
                    return pendencyWriteResult;

                // Registra na janela de debug o resultado da execução
                Logger.Debug(string.Format("ExecuteTransaction: PW_iExecTransac={0}", pw_iExecTransacResult.ToString()));

                // Faz o tratamento correto de acordo com o retorno recebido em PW_iExecTransac
                switch (pw_iExecTransacResult)
                {
                    // Caso a biblioteca tenha solicitado a captura de mais dados, chama a função que
                    // faz a captura de acordo com as informações contidas em structParam
                    case E_PWRET.PWRET_MOREDATA:
                        E_PWRET automationUserInteractionResult = ShowAutomationUserInteraction(structParam);
                        if (automationUserInteractionResult != E_PWRET.PWRET_OK)
                        {
                            if (automationUserInteractionResult == E_PWRET.PWRET_CANCEL)
                            {
                                // Apaga o status de desfazimento anterior por desligamento abrupto da
                                // automação
                                PendencyDelete();

                                // Escreve o novo desfazimento a ser executado por transação abortada
                                // pela automação durante uma captura de dados
                                PendencyWrite(E_PWCNF.PWCNF_REV_ABORT);
                            }

                            return automationUserInteractionResult;
                        }

                        break;

                    // Caso a biblioteca tenha retornado que existe uma transação pendente.
                    // Esse retorno só irá acontecer em caso de alguma falha de tratamento da resolução
                    // de pendência da transação por parte da automação, ou alguma falha de sistema
                    // do Pay&Go WEB, caso um ponto de captura fique com uma transação pendente ele não
                    // irá poder realizar novas transações até que essa pendência seja resolvida
                    case E_PWRET.PWRET_FROMHOSTPENDTRN:
                        // Desmarca o desfazimento marcado por segurança, pois a transação não foi 
                        // finalizada com sucesso
                        PendencyDelete();

                        return pw_iExecTransacResult;

                    // Esse retorno indica que nada deve ser feito e PW_iExecTransac deve ser chamada 
                    // novamente para prosseguir o fluxo
                    case E_PWRET.PWRET_NOTHING:
                        break;

                    // Esse retorno indica que a transação foi executada com sucesso
                    case E_PWRET.PWRET_OK:
                        // TODO: Para de exibir o QRcode, caso exista um sendo exibido
                        return pw_iExecTransacResult;

                    // Qualquer outro código de retorno representa um erro
                    default:
                        // TODO: Para de exibir o QRcode, caso exista um sendo exibido
                        // Desmarca o desfazimento marcado por segurança, pois a transação não foi 
                        // finalizada com sucesso
                        PendencyDelete();

                        return pw_iExecTransacResult;
                }
            }
        }

        private List<PW_Parameter> TransactionResponse()
        {
            Logger.Info("TransactionResponse");

            // Lista com os dados a serem retornados
            List<PW_Parameter> response = new List<PW_Parameter>();

            // Percorre todos os dados possíveis
            foreach (E_PWINFO item in Enum.GetValues(typeof(E_PWINFO)).Cast<E_PWINFO>())
            {
                StringBuilder getResultValue = new StringBuilder(10000);

                // Tenta obter o dado
                E_PWRET getInfoRet = (E_PWRET)Interop.PW_iGetResult((short)item, getResultValue, 10001);

                // Caso o dado exista, adiciona na lista de retorno
                if (getInfoRet == E_PWRET.PWRET_OK)
                {
                    response.Add(new PW_Parameter(item.ToString(), (ushort)item, getResultValue.ToString()));

                    // Registra na janela de debug cada parâmetro obtido com sucesso
                    Logger.Debug(string.Format("TransactionResponse: PW_iGetResult ({0})={1}", item.ToString(), getResultValue));
                }
            }

            return response;
        }

        private string ResolveTransactionResponse(E_PWRET executeResult, List<PW_Parameter> transactionResponse)
        {
            Logger.Info("ResolveTransactionResponse");

            PW_Parameter? param;
            // Caso a operação tenha sido cancelada, obtém a mensagem a ser exibida nesse caso
            if (executeResult == E_PWRET.PWRET_CANCEL)
                param = transactionResponse?.Find(item => item?.parameterCode == (ushort)E_PWINFO.PWINFO_CNCDSPMSG);
            else
                param = transactionResponse?.Find(item => item?.parameterCode == (ushort)E_PWINFO.PWINFO_RESULTMSG);

            // Caso não seja possível obter uma mensagem de resultado da biblioteca, atribui uma padrão
            string message = (param != null) ? param.parameterValue : "TRANSAÇÃO FINALIZADA";

            Logger.Debug($"ResolveTransactionResponse: {message}");
            return message.Replace("\r", "\n");
        }

        private E_PWRET LoopPP()
        {
            Logger.Info("LoopPP");
            E_PWRET result;

            // Loop executando até a finalização do comando de PIN-pad, seja ele com um erro
            // ou com sucesso
            do
            { 
                // Chama o loop de eventos
                StringBuilder displayMessage = new StringBuilder(1000);
                result = (E_PWRET)Interop.PW_iPPEventLoop(displayMessage, (uint)displayMessage.Capacity);

                // Caso tenha retornado uma mensagem para exibição, exibe
                if (result == E_PWRET.PWRET_DISPLAY)
                {
                    var promptResult = interactions.RaisePromptConfirmation(displayMessage.ToString().TrimStart('\r').Replace("\r", "\n"));

                    // Verifica se o operador abortou a operação no checkout
                    if (promptResult == PromptConfirmationResult.Cancel)
                    {
                        // Aborta a operação em curso no PIN-pad
                        Interop.PW_iPPAbort();

                        // Atribui o retorno de operação cancelada
                        result = E_PWRET.PWRET_CANCEL;

                        break;
                    }
                }

                // Aguarda 200ms para chamar o loop de eventos novamente
                Thread.Sleep(200);
            } while (result == E_PWRET.PWRET_NOTHING || result == E_PWRET.PWRET_DISPLAY);

            Logger.Debug(string.Format("LoopPP: PW_iPPEventLoop={0}", result.ToString()));

            return result;
        }

        // Resolve a pendência de uma transação que, por algum motibo fora do fluxo previsto, ficou
        // pendente no sistema e impediu o ponto de captura de efetuar novas transações
        private E_PWRET ConfirmUndoPendingTransaction(E_PWCNF status, List<PW_Parameter> transactionResponse)
        {
            Logger.Info("ConfirmUndoPendingTransaction");

            string pszReqNum = string.Empty;
            string pszLocRef = string.Empty;
            string pszExtRef = string.Empty;
            string pszVirtMerch = string.Empty;
            string pszAuthSyst = string.Empty;

            // Obtém os dados necéssários para a resolução de pendência
            foreach (PW_Parameter item in transactionResponse)
            {
                switch (item.parameterCode)
                {
                    case (ushort)E_PWINFO.PWINFO_PNDREQNUM:
                        pszReqNum = item.parameterValue;
                        break;

                    case (ushort)E_PWINFO.PWINFO_PNDAUTLOCREF:
                        pszLocRef = item.parameterValue;
                        break;

                    case (ushort)E_PWINFO.PWINFO_PNDAUTEXTREF:
                        pszExtRef = item.parameterValue;
                        break;

                    case (ushort)E_PWINFO.PWINFO_PNDVIRTMERCH:
                        pszVirtMerch = item.parameterValue;
                        break;

                    case (ushort)E_PWINFO.PWINFO_PNDAUTHSYST:
                        pszAuthSyst = item.parameterValue;
                        break;

                    default:
                        break;
                }
            }

            // Executa a confirmação
            E_PWRET result = (E_PWRET)Interop.PW_iConfirmation((uint)status, pszReqNum, pszLocRef, pszLocRef, pszVirtMerch, pszAuthSyst);

            // Registra na janela de debug a confirmação executada
            Logger.Debug(string.Format("ConfirmUndoPendingTransaction: PW_iConfirmationPending(ReqNum={0},LocRef={1},ExtRef={2},VirtMerch={3},AuthSyst={4})={5}",
                pszReqNum, pszLocRef, pszExtRef, pszVirtMerch, pszAuthSyst, result.ToString()));
            return result;
        }

        // Resolve a pendência de uma transação finalizada com sucesso
        private E_PWRET ConfirmUndoNormalTransaction(E_PWCNF status, List<PW_Parameter> transactionResponse)
        {
            Logger.Info("ConfirmUndoNormalTransaction");

            string pszReqNum = string.Empty;
            string pszLocRef = string.Empty;
            string pszExtRef = string.Empty;
            string pszVirtMerch = string.Empty;
            string pszAuthSyst = string.Empty;

            // Obtém os dados necéssários para a resolução de pendência
            foreach (PW_Parameter item in transactionResponse)
            {
                switch (item.parameterCode)
                {
                    case (ushort)E_PWINFO.PWINFO_REQNUM:
                        pszReqNum = item.parameterValue;
                        break;

                    case (ushort)E_PWINFO.PWINFO_AUTLOCREF:
                        pszLocRef = item.parameterValue;
                        break;

                    case (ushort)E_PWINFO.PWINFO_AUTEXTREF:
                        pszExtRef = item.parameterValue;
                        break;

                    case (ushort)E_PWINFO.PWINFO_VIRTMERCH:
                        pszVirtMerch = item.parameterValue;
                        break;

                    case (ushort)E_PWINFO.PWINFO_AUTHSYST:
                        pszAuthSyst = item.parameterValue;
                        break;

                    default:
                        break;
                }
            }

            // Executa a confirmação
            E_PWRET result = (E_PWRET)Interop.PW_iConfirmation((uint)status, pszReqNum, pszLocRef, pszLocRef, pszVirtMerch, pszAuthSyst);

            // Registra na janela de debug a confirmação executada
            Logger.Debug(string.Format("ConfirmUndoNormalTransaction: PW_iConfirmationNormal(ReqNum={0},LocRef={1},ExtRef={2},VirtMerch={3},AuthSyst={4})={5}",
                pszReqNum, pszLocRef, pszExtRef, pszVirtMerch, pszAuthSyst, result.ToString()));

            // Conforme a arquitetura utilizada pela automação, esse ponto poderá estar rodando em 
            // uma thread. Portanto o tratamento abaixo é feito para que a thread não seja 
            // interrompida até que a confirmação seja enviada ao servidor.
            // Para versões de biblioteca iguais ou superiores a 4.0.96.0, poderá ser utilizada a 
            // função: PW_iWaitConfirmation
            int loopRet;
            StringBuilder displayMessage = new StringBuilder(1000);

            while (true)
            {
                loopRet = Interop.PW_iPPEventLoop(displayMessage, (uint)1000);
                if (loopRet != (int)E_PWRET.PWRET_NOTHING)
                    break;
                Thread.Sleep(500);
            }

            // Caso a confirmação tenha sido executada com sucesso, remove o desfazimento pendente
            if (result != (int)E_PWRET.PWRET_OK)
            {
                PendencyDelete();
            }
            else // Caso ocorra alguma falha na confirmação
            {
                // Apaga o desfazimento
                PendencyDelete();

                // Armazena o status recebido para repetição do processo antes da próxima transação
                PendencyWrite(status);
            }

            return result;
        }
        
        private E_PWRET ShowAutomationUserInteraction(PW_GetData[] expectedData)
        {
            Logger.Info("ShowAutomationUserInteraction");

            ushort index = 0;
            foreach (PW_GetData item in expectedData)
            {
                // Caso exista uma mensagem a ser exibida ao usuário antes da captura do dado
                if (item.szMsgPrevia.Length > 0)
                {
                    Logger.Debug($"ShowAutomationUserInteraction: szMsgPrevia {item.szMsgPrevia}");
                    interactions.RaiseMessage(item.szMsgPrevia);
                }

                if (item.bTipoDeDado == 0)
                {
                    Logger.Debug("ShowAutomationUserInteraction: Item com valor zerado.");
                    return E_PWRET.PWRET_OK;
                }

                E_PWRET? interactionResult = interactions.Interact(item, index);
                if (interactionResult != null)
                {
                    return interactionResult.Value;
                }
                
                index++;
            }

            return E_PWRET.PWRET_OK;
        }

        private void ShowAutomationPendingTransaction(List<PW_Parameter> transactionResponse)
        {
            Logger.Info("ShowAutomationPendingTransaction");

            PW_Parameter? authSyst, virtMerch, reqNum, autLocRef, autExtRef;
            authSyst = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTHSYST);
            virtMerch = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDVIRTMERCH);
            reqNum = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDREQNUM);
            autLocRef = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTLOCREF);
            autExtRef = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTEXTREF);

            string message = string.Format("Existe uma transação pendente:\n" +
                "PNDAUTHSYST={0}\n" +
                "PNDVIRTMERCH={1}\n" +
                "PNDREQNUM={2}\n" +
                "PNDAUTLOCREF={3}\n" +
                "PNDAUTEXTREF={4}\n" +
                "Será necessário resolvê-la !!!",
                authSyst == null ? "" : authSyst.parameterValue,
                virtMerch == null ? "" : virtMerch.parameterValue,
                reqNum == null ? "" : reqNum.parameterValue,
                autLocRef == null ? "" : autLocRef.parameterValue,
                autExtRef == null ? "" : autExtRef.parameterValue);
            
            interactions.RaiseMessage(message);
        }

        private void Init()
        {
            // Define o diretório da lib
            string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\PGWebLib\\";
            Console.WriteLine($"Using lib path: {path}");

            // Cria o diretório que será utilizado pela função PW_iInit
            Directory.CreateDirectory(path);

            // Inicializa a biblioteca, indicando a pasta de trabalho a ser utilizada para gravação
            // de logs e arquivos
            E_PWRET ret = (E_PWRET)Interop.PW_iInit(path);

            // Caso ocorra um erro no processo de inicialização da biblioteca, dispara uma exceção
            if (ret != E_PWRET.PWRET_OK)
                throw new Exception(string.Format("Erro {0} ao executar PW_iInit", ret.ToString()));
        }

        public E_PWRET installation()
        {
            return Installation();
        }
    }
}
