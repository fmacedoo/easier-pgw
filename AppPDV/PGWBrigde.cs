using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PGW;
using PGW.Dll;
using static PGW.CustomObjects;
using static PGW.Enums;

namespace Name
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class PGWBrigde
    {
        public PGWBrigde()
        {
            // Cria o diretório que será utilizado pela função PW_iInit
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\PGWebLib\\");
            string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\PGWebLib\\";

            // Inicializa a biblioteca, indicando a pasta de trabalho a ser utilizada para gravação
            // de logs e arquivos
            int ret = Interop.PW_iInit(path);

            // Caso ocorra um erro no processo de inicialização da biblioteca, dispara uma exceção
            if (ret != (int)E_PWRET.PWRET_OK)
                throw new System.Exception(string.Format("Erro {0} ao executar PW_iInit", ret.ToString()));
        }

        private List<PW_Parameter> _GetMandatoryParameters()
        {
            Console.WriteLine("Setting Parameters");

            int autCap = (int)E_PWAutCapabilities.FIXO + (int)E_PWAutCapabilities.CUPOMRED + (int)E_PWAutCapabilities.CUPOMDIF
                + (int)E_PWAutCapabilities.DSPCHECKOT + (int)E_PWAutCapabilities.DSPQRCODE;
            PW_Parameter[] parameters = {
                new PW_Parameter(E_PWINFO.PWINFO_AUTNAME.ToString(), (int)E_PWINFO.PWINFO_AUTNAME, "PDVS"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTVER.ToString(), (int)E_PWINFO.PWINFO_AUTVER, "1.0"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTDEV.ToString(), (int)E_PWINFO.PWINFO_AUTDEV, "PayGo Pagamentos"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTCAP.ToString(), (int)E_PWINFO.PWINFO_AUTCAP, autCap.ToString()),
                new PW_Parameter(E_PWINFO.PWINFO_DSPQRPREF.ToString(), (int)E_PWINFO.PWINFO_DSPQRPREF, ((int)E_PWQrcodePref.CHECKOUT).ToString()),
            };

            return parameters.ToList();
        }

        private List<PW_Parameter> _GetTransactionResponse()
        {
            // Lista com os dados a serem retornados
            List<PW_Parameter> returnList = new List<PW_Parameter>();

            // Percorre todos os dados possíveis
            foreach (E_PWINFO item in Enum.GetValues(typeof(E_PWINFO)).Cast<E_PWINFO>())
            {
                StringBuilder getResultValue = new StringBuilder(10000);

                // Tenta obter o dado
                int getInfoRet = Interop.PW_iGetResult((short)item, getResultValue, 10001);

                // Caso o dado exista, adiciona na lista de retorno
                if (getInfoRet == (int)E_PWRET.PWRET_OK)
                {
                    returnList.Add(new PW_Parameter(item.ToString(), (ushort)item, getResultValue.ToString()));

                    // Registra na janela de debug cada parâmetro obtido com sucesso
                    Debug.Print(string.Format("PW_iGetResult ({0})={1}", item.ToString(), getResultValue));
                }
            }

            return returnList;
        }

        string _ResolveTransactionMessage(int transactionResult, List<PW_Parameter> transactionResponse)
        {
            PW_Parameter? param;
            // Caso a operação tenha sido cancelada, obtém a mensagem a ser exibida nesse caso
            if (transactionResult == (int)E_PWRET.PWRET_CANCEL)
                param = transactionResponse?.Find(item => item?.parameterCode == (ushort)E_PWINFO.PWINFO_CNCDSPMSG);
            else
                param = transactionResponse?.Find(item => item?.parameterCode == (ushort)E_PWINFO.PWINFO_RESULTMSG);

            // Caso não seja possível obter uma mensagem de resultado da biblioteca, atribui uma padrão
            if (param != null)
                return param.parameterValue.Replace("\r", "\n");
            else
                return "TRANSACAO FINALIZADA";
        }

        private void _ShowPendingTransaction(List<PW_Parameter> transactionResponse)
        {
            PW_Parameter? authSyst, virtMerch, reqNum, autLocRef, autExtRef;
            authSyst = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTHSYST);
            virtMerch = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDVIRTMERCH);
            reqNum = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDREQNUM);
            autLocRef = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTLOCREF);
            autExtRef = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTEXTREF);

            MessageBox.Show(string.Format("101INSTALL: Existe uma transação pendente:\n" +
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
                    autExtRef == null ? "" : autExtRef.parameterValue));
        }

        // Resolve a pendência de uma transação que, por algum motibo fora do fluxo previsto, ficou
        // pendente no sistema e impediu o ponto de captura de efetuar novas transações
        private int _ConfirmUndoPendingTransaction(E_PWCNF transactionStatus, List<PW_Parameter> transactionResponse)
        {
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
            int ret = Interop.PW_iConfirmation((uint)transactionStatus, pszReqNum, pszLocRef, pszLocRef, pszVirtMerch, pszAuthSyst);

            // Registra na janela de debug a confirmação executada
            Debug.Print(string.Format("PW_iConfirmationPending(ReqNum={0},LocRef={1},ExtRef={2},VirtMerch={3},AuthSyst={4})={5}",
                pszReqNum, pszLocRef, pszExtRef, pszVirtMerch, pszAuthSyst, ret.ToString()));
            return ret;
        }

        // Resolve a pendência de uma transação finalizada com sucesso
        public int _ConfirmUndoNormalTransaction(E_PWCNF transactionStatus, List<PW_Parameter> transactionResponse)
        {
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
            int ret = Interop.PW_iConfirmation((uint)transactionStatus, pszReqNum, pszLocRef, pszLocRef, pszVirtMerch, pszAuthSyst);

            // Registra na janela de debug a confirmação executada
            Debug.Print(string.Format("PW_iConfirmationNormal(ReqNum={0},LocRef={1},ExtRef={2},VirtMerch={3},AuthSyst={4})={5}",
                pszReqNum, pszLocRef, pszExtRef, pszVirtMerch, pszAuthSyst, ret.ToString()));

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
            if (ret != (int)E_PWRET.PWRET_OK)
            {
                _PendencyDelete();
            }
            else // Caso ocorra alguma falha na confirmação
            {
                // Apaga o desfazimento
                _PendencyDelete();

                // Armazena o status recebido para repetição do processo antes da próxima transação
                _PendencyWrite(transactionStatus);
            }

            return ret;
        }

        // Grava uma pendência para posterior resolução
        private int _PendencyWrite(E_PWCNF transactionStatus)
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

            return (int)E_PWRET.PWRET_OK;
        }

        // Descarta uma pendência que já foi resolvida ou não é mais necessária
        private int _PendencyDelete()
        {
            // Nessa função é necessário implementar na automação, de acordo com o tipo de persistência
            // de dados a exclusão de qualquer resolução de pendência que possa estar armazenada.

            return (int)E_PWRET.PWRET_OK;
        }

        // Resolve uma pendência previamente gravada
        private int _PendencyResolve()
        {
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

            return (int)E_PWRET.PWRET_OK;
        }

        public int _StartTransaction(E_PWOPER operation, List<PW_Parameter> paramList)
        {
            // Antes de iniciar uma nova transação, resolve possíveis pendência existentes
            int pendencyResult = _PendencyResolve();

            // Caso ocorra algum problema na resolução de pendência retorna o erro
            if (pendencyResult != (int)E_PWRET.PWRET_OK)
                return pendencyResult;

            // Inicializa a transação solicitada
            int newTransactionResult = Interop.PW_iNewTransac((byte)operation);

            // Caso ocorra algum problema na inicialização retorna o erro
            if (newTransactionResult != (int)E_PWRET.PWRET_OK)
                return newTransactionResult;

            // Loop adicionando todos os parâmetros já capturados pela automação
            // Parâmetros já capturados devem ser adicionados, caso a biblioteca precise de qualquer
            // parâmetro que não foi recebido para finalizar a transação, ela irá solicitá-lo no fluxo
            // implementado em "executeTransaction"
            foreach (PW_Parameter item in paramList)
            {
                // Adiciona o parâmetro
                short addParamResult = Interop.PW_iAddParam(item.parameterCode, item.parameterValue);

                // Registra na janela de debug o resultado da adição do parâmetro
                Debug.Print(string.Format("PW_iAddParam ({0},{1})={2}", item.parameterName,
                    item.parameterValue, addParamResult.ToString()));
            }

            // Inicia o processo de execução da transação
            int result = _ExecuteTransaction();

            return result;
        }

        private int _ExecuteTransaction()
        {
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
                _PendencyDelete();

                // Chama a função que executa um passo da transação
                int execTransacresult = (int)Interop.PW_iExecTransac(structParam, ref numDados);

                // Marca um desfazimento por segurança, caso a automação seja fechada abruptamente
                // durante qualquer passo abaixo, o desfazimento já estará armazenado em disco para
                // ser executado por PendencyResolve antes da próxima transação
                // Esse desfazimento será desmarcado em duas situações:
                // 1-) O loop foi executado novamente e PW_iExecTransac será chamada
                // 2-) Algum erro ocorreu durante o loop
                // 3-) A transação foi finalizada com sucesso, nesse caso o desfazimento permanecerá
                //     gravado até a execução da resolução de pendência da transação em 
                //     "ConfirmUndoNormalTransaction"
                _PendencyWrite(E_PWCNF.PWCNF_REV_PWR_AUT);

                // Registra na janela de debug o resultado da execução
                Debug.Print(string.Format("PW_iExecTransac={0}", execTransacresult.ToString()));

                // Faz o tratamento correto de acordo com o retorno recebido em PW_iExecTransac
                switch (execTransacresult)
                {
                    // Caso a biblioteca tenha solicitado a captura de mais dados, chama a função que
                    // faz a captura de acordo com as informações contidas em structParam
                    case (int)E_PWRET.PWRET_MOREDATA:
                        int userInteractionResult = _ShowCorrespondingWindow(structParam);
                        if (userInteractionResult != (int)E_PWRET.PWRET_OK)
                        {
                            if (userInteractionResult == (int)E_PWRET.PWRET_CANCEL)
                            {
                                // Apaga o status de desfazimento anterior por desligamento abrupto da
                                // automação
                                _PendencyDelete();

                                // Escreve o novo desfazimento a ser executado por transação abortada
                                // pela automação durante uma captura de dados
                                _PendencyWrite(E_PWCNF.PWCNF_REV_ABORT);

                            }
                            return userInteractionResult;
                        }
                        break;

                    // Caso a biblioteca tenha retornado que existe uma transação pendente.
                    // Esse retorno só irá acontecer em caso de alguma falha de tratamento da resolução
                    // de pendência da transação por parte da automação, ou alguma falha de sistema
                    // do Pay&Go WEB, caso um ponto de captura fique com uma transação pendente ele não
                    // irá poder realizar novas transações até que essa pendência seja resolvida
                    case (int)E_PWRET.PWRET_FROMHOSTPENDTRN:
                        // Desmarca o desfazimento marcado por segurança, pois a transação não foi 
                        // finalizada com sucesso
                        _PendencyDelete();

                        return execTransacresult;

                    // Esse retorno indica que nada deve ser feito e PW_iExecTransac deve ser chamada 
                    // novamente para prosseguir o fluxo
                    case (int)E_PWRET.PWRET_NOTHING:
                        break;

                    // Esse retorno indica que a transação foi executada com sucesso
                    case (int)E_PWRET.PWRET_OK:
                        // TODO: Para de exibir o QRcode, caso exista um sendo exibido
                        return execTransacresult;

                    // Qualquer outro código de retorno representa um erro
                    default:
                        // TODO: Para de exibir o QRcode, caso exista um sendo exibido

                        // Desmarca o desfazimento marcado por segurança, pois a transação não foi 
                        // finalizada com sucesso
                        _PendencyDelete();

                        return execTransacresult;
                }
            }
        }

        private int _ShowCorrespondingWindow(PW_GetData[] expectedData)
        {
            ushort index = 0;
            foreach (PW_GetData item in expectedData)
            {
                // Caso exista uma mensagem a ser exibida ao usuário antes da captura do dado
                if (item.szMsgPrevia.Length > 0)
                {
                    MessageBox.Show(item.szMsgPrevia);
                }

                switch (item.bTipoDeDado)
                {
                    case 0:
                        Debug.Print(string.Format("ERRO!!! Item com valor zerado."));
                        return 0;

                    // Caso a automação trabalhe com captura de código de barras, necessário 
                    // implementar os casos que serão aceitos (digitado, leitor...), bem como
                    //  as validações necessárias por tipo de código
                    case (int)E_PWDAT.PWDAT_BARCODE:
                        return _GetTypedDataFromUser(item);

                    // Menu de opções
                    case (int)E_PWDAT.PWDAT_MENU:
                        return _GetMenuFromUser(item);

                    // Captura de dado digitado
                    case (int)E_PWDAT.PWDAT_TYPED:
                        return _GetTypedDataFromUser(item);

                    // Autenticação de permissão de usuário
                    case (int)E_PWDAT.PWDAT_USERAUTH:
                        return _GetTypedDataFromUser(item);

                    // Captura de dados do cartão
                    case (int)E_PWDAT.PWDAT_CARDINF:
                        // Caso só seja aceito o modo de entrada de cartão digitado
                        if (item.ulTipoEntradaCartao == 1)
                        {
                            PW_GetData temp = item;
                            temp.wIdentificador = (ushort)E_PWINFO.PWINFO_CARDFULLPAN;
                            return _GetTypedDataFromUser(temp);
                        }
                        // Caso seja aceito cartão lido pelo PIN-pad
                        else
                        {
                            int getCardResult = Interop.PW_iPPGetCard(index);
                            Debug.Print(string.Format("PW_iPPGetCard={0}", getCardResult.ToString()));
                            if (getCardResult == (int)E_PWRET.PWRET_OK) 
                                getCardResult = _LoopPP();
                            return getCardResult;
                        }
               
                    // Processamento offline do cartão
                    case (int)E_PWDAT.PWDAT_CARDOFF:
                        int goOnChipResult = Interop.PW_iPPGoOnChip(index);
                        Debug.Print(string.Format("PW_iPPGoOnChip={0}", goOnChipResult.ToString()));
                        if (goOnChipResult == (int)E_PWRET.PWRET_OK)
                            goOnChipResult = _LoopPP();
                        return goOnChipResult;

                    // Processamento online do cartão
                    case (int)E_PWDAT.PWDAT_CARDONL:
                        int finishChipResult = Interop.PW_iPPFinishChip(index);
                        Debug.Print(string.Format("PW_iPPFinishChip={0}", finishChipResult.ToString()));
                        if (finishChipResult == (int)E_PWRET.PWRET_OK) 
                            finishChipResult = _LoopPP();
                        return finishChipResult;

                    // Confirmação de dado no PIN-pad
                    case (int)E_PWDAT.PWDAT_PPCONF:
                        int confirmDataResult = Interop.PW_iPPConfirmData(index);
                        Debug.Print(string.Format("PW_iPPConfirmData={0}", confirmDataResult.ToString()));
                        if (confirmDataResult == (int)E_PWRET.PWRET_OK) 
                            confirmDataResult = _LoopPP();
                        return confirmDataResult;

                    // Confirmação positiva PIN-pad
                    case (int)E_PWDAT.PWDAT_PPDATAPOSCNF:
                        int positiveConfirmationResult = Interop.PW_iPPPositiveConfirmation(index);
                        Debug.Print(string.Format("PW_iPPPositiveConfirmation={0}", positiveConfirmationResult.ToString()));
                        if (positiveConfirmationResult == (int)E_PWRET.PWRET_OK)
                            positiveConfirmationResult = _LoopPP();
                        return positiveConfirmationResult;

                    // Comando genérico no PIN-pad
                    case (int)E_PWDAT.PWDAT_PPGENCMD:
                        int genericCMDResult = Interop.PW_iPPGenericCMD(index);
                        Debug.Print(string.Format("PW_iPPGenericCMD={0}", genericCMDResult.ToString()));
                        if (genericCMDResult == (int)E_PWRET.PWRET_OK)
                            genericCMDResult = _LoopPP();
                        return genericCMDResult;

                    // Senha do portador
                    case (int)E_PWDAT.PWDAT_PPENCPIN:
                        int getPINResult = Interop.PW_iPPGetPIN(index);
                        Debug.Print(string.Format("PW_iPPGetPIN={0}", getPINResult.ToString()));
                        if (getPINResult == (int)E_PWRET.PWRET_OK) 
                            getPINResult = _LoopPP();
                        return getPINResult;

                    // Entrada digitada no PIN-pad
                    case (int)E_PWDAT.PWDAT_PPENTRY:
                        int getDataResult = Interop.PW_iPPGetData(index);
                        Debug.Print(string.Format("PW_iPPGetData={0}", getDataResult.ToString()));
                        if (getDataResult == (int)E_PWRET.PWRET_OK) 
                            getDataResult = _LoopPP();
                        return getDataResult;

                    // Remoção de cartão do PIN-pad
                    case (int)E_PWDAT.PWDAT_PPREMCRD:
                        int removeCardResult = Interop.PW_iPPRemoveCard();
                        Debug.Print(string.Format("PW_iPPRemoveCard={0}", removeCardResult.ToString()));
                        if (removeCardResult == (int)E_PWRET.PWRET_OK) 
                            removeCardResult = _LoopPP();
                        return removeCardResult;

                    // Exibição de mensagem de interface no display da automação
                    case (int)E_PWDAT.PWDAT_DSPCHECKOUT:
                        var promptResult = MessageBox.Show(item.szPrompt, "DIALOG", MessageBoxButtons.OKCancel);

                        if(promptResult == DialogResult.Cancel)
                        {
                            // Aborta a operação em curso no PIN-pad
                            Interop.PW_iPPAbort();

                            // Sinaliza a exibição da mensagem para a biblioteca
                            Interop.PW_iAddParam(item.wIdentificador, "");

                            // Retorna operação cancelada
                            return (int)E_PWRET.PWRET_CANCEL;
                        }
                        
                        return (int)E_PWRET.PWRET_OK;

                    // Exibição de QRcode no display da automação
                    case (int)E_PWDAT.PWDAT_DSPQRCODE:

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
                        return (int)E_PWRET.PWRET_CANCEL;

                    default:
                        break;
                }
                index++;
            }

            return (int)E_PWRET.PWRET_OK;
        }

        // Obtém um dado digitado do usuário
        private int _GetTypedDataFromUser(PW_GetData expectedData)
        {
            if (expectedData.wIdentificador == (ushort)E_PWINFO.PWINFO_AUTHMNGTUSER)
            {
                // Exemplificando a captura de uma senha de lojista de até 4 dígitos
                expectedData.szPrompt = "INSIRA A SENHA DO LOJISTA";
                expectedData.bTamanhoMaximo = 4;
            }
            if (expectedData.wIdentificador == (ushort)E_PWINFO.PWINFO_AUTHTECHUSER)
            {
                // Exemplificando a captura de uma senha de lojista de até 10 dígitos
                expectedData.szPrompt = "INSIRA A SENHA TÉCNICA";
                expectedData.bTamanhoMaximo = 10;
            }

            string? value = PromptBox.Show("DIGITE UM VALOR", expectedData.szPrompt.Replace("\r", "\n"));
            
            // Caso o usuário tenha abortado a transação, retorna E_PWRET.PWRET_CANCEL
            if (value is null)
            {
                return (int)E_PWRET.PWRET_CANCEL;
            }

            // Adiciona o dado capturado
            int addParamResult = Interop.PW_iAddParam(expectedData.wIdentificador, value);

            // Registra na janela de debug o resultado da adição do parâmetro
            Debug.Print(string.Format("PW_iAddParam({0},{1})={2}", ((E_PWINFO)expectedData.wIdentificador).ToString(), value, addParamResult.ToString()));

            return addParamResult;
        }

        // Executa um menu de opções para o usuário
        private int _GetMenuFromUser(PW_GetData expectedData)
        {
            List<string> options = new List<string>();
            for (byte b = 0; b < expectedData.bNumOpcoesMenu; b++)
            {
                if (expectedData.bTeclasDeAtalho == 1 && b < 10)
                {
                    options.Add(string.Format("{0}-{1}", b, expectedData.vszTextoMenu[b].szTextoMenu));
                }
                else
                {
                    options.Add(string.Format("{0}", expectedData.vszTextoMenu[b].szTextoMenu));
                }               
            }

            var option = (expectedData.bNumOpcoesMenu == 1 && expectedData.bItemInicial == 0) ?
                expectedData.vszValorMenu[0].szValorMenu :
                PromptBox.ShowList("", options);
            
            if (option is null) return (int)E_PWRET.PWRET_CANCEL;

            int addParamResult = Interop.PW_iAddParam(expectedData.wIdentificador, option);

            // Registra na janela de debug o resultado da adição do parâmetro
            Debug.Print(string.Format("PW_iAddParam({0},{1})={2}", ((E_PWINFO)expectedData.wIdentificador).ToString(), option, addParamResult.ToString()));

            return addParamResult;
        }

        // Aguarda em loop a finalização da operação executada no PIN-pad, fazendo
        // os tratamento necessários dos retornos
        private int _LoopPP()
        {
            int ret;
            bool isFdmStarted = false;

            // Cria a janela para exibição de mensagens de interface
            FormDisplayMessage fdm = new FormDisplayMessage();
            
            // Loop executando até a finalização do comando de PIN-pad, seja ele com um erro
            // ou com sucesso
            do
            { 
                // Chama o loop de eventos
                StringBuilder displayMessage = new StringBuilder(1000);
                ret = Interop.PW_iPPEventLoop(displayMessage, (uint)displayMessage.Capacity);

                // Caso tenha retornado uma mensagem para exibição, exibe
                if (ret == (int)E_PWRET.PWRET_DISPLAY)
                {
                    if (!isFdmStarted)
                    {
                        fdm.Start();
                        isFdmStarted = true;
                    }
                    fdm.ChangeText(displayMessage.ToString());
                }

                // Verifica se o operador abortou a operação no checkout
                if(isFdmStarted)
                {
                    if(fdm.isAborted())
                    {
                        // Aborta a operação em curso no PIN-pad
                        Interop.PW_iPPAbort();

                        // Atribui o retorno de aoperação cancelada
                        ret = (int)E_PWRET.PWRET_CANCEL;

                        break;
                    }
                }

                // Aguarda 200ms para chamar o loop de eventos novamente
                Thread.Sleep(200);
            } while (ret == (int)E_PWRET.PWRET_NOTHING || ret == (int)E_PWRET.PWRET_DISPLAY);

            // Fecha janela para exibição de mensagem
            if(isFdmStarted)
                fdm.Stop();

            // Registra o resultado final do loop de PIN-pad na janela de Debug
            Debug.Print(string.Format("PW_iPPEventLoop={0}", ret.ToString()));

            return ret;
        }

        public void Installation()
        {
            var parameters = _GetMandatoryParameters();

            Console.WriteLine("Starting Transaction");
            E_PWOPER operation = E_PWOPER.PWOPER_INSTALL;
            int result = _StartTransaction(operation, parameters);

            List<PW_Parameter> transactionResponse = _GetTransactionResponse();

            string message = _ResolveTransactionMessage(result, transactionResponse);
            MessageBox.Show($"101INSTALL: {message}");

            if (result != 0)
            {
                _ShowPendingTransaction(transactionResponse);

                E_PWCNF transactionStatus = E_PWCNF.PWCNF_REV_ABORT;
                _ConfirmUndoPendingTransaction(transactionStatus, transactionResponse);
            }

            PW_Parameter? confirmacaoNecessaria = transactionResponse.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_CNFREQ);
            if (confirmacaoNecessaria != null && int.Parse(confirmacaoNecessaria.parameterValue) == 1)
            {
                E_PWCNF transactionStatus = E_PWCNF.PWCNF_REV_ABORT;
                _ConfirmUndoNormalTransaction(transactionStatus, transactionResponse);
            }
        }
    }
}
