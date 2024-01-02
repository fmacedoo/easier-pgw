using System.Windows.Forms;
using System.Runtime.InteropServices;
using PGW;
using static PGW.Enums;
using static PGW.CustomObjects;

namespace AppPDV
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
	[ComVisible(true)]
    public class Bridge
    {
        PGWLib pGWLib;

        public Bridge()
        {
            Console.WriteLine("Bridge: Loading PGWLib");
            pGWLib = new PGWLib();
            Console.WriteLine("Bridge: PGWLib Loaded");
        }

        public void ShowMessageBox(string message)
        {
            MessageBox.Show(message, "C# Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public int ExecuteTransaction()
        {
            E_PWOPER operation = E_PWOPER.PWOPER_INSTALL;
            List<PW_Parameter> paramList;
            int ret;
            string resultMessage;
            PW_Parameter param;
            E_PWCNF transactionStatus;

            int autCap = (int)E_PWAutCapabilities.FIXO + (int)E_PWAutCapabilities.CUPOMRED + (int)E_PWAutCapabilities.CUPOMDIF
                + (int)E_PWAutCapabilities.DSPCHECKOT + (int)E_PWAutCapabilities.DSPQRCODE;
            PW_Parameter[] parameters = {
                new PW_Parameter(E_PWINFO.PWINFO_AUTNAME.ToString(), (int)E_PWINFO.PWINFO_AUTNAME, "PDVS"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTVER.ToString(), (int)E_PWINFO.PWINFO_AUTVER, "1.0"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTDEV.ToString(), (int)E_PWINFO.PWINFO_AUTDEV, "PayGo Pagamentos"),
                new PW_Parameter(E_PWINFO.PWINFO_AUTCAP.ToString(), (int)E_PWINFO.PWINFO_AUTCAP, autCap.ToString()),
                new PW_Parameter(E_PWINFO.PWINFO_DSPQRPREF.ToString(), (int)E_PWINFO.PWINFO_DSPQRPREF, ((int)E_PWQrcodePref.CHECKOUT).ToString()),
            };

            // Executa a transação
            ret = pGWLib.StartTransaction(operation, parameters.ToList());

            // Obtem todos os resultados da transação
            paramList = GetTransactionResult();

            // Caso a operação tenha sido cancelada, obtém a mensagem a ser exibida nesse caso
            if(ret==(int)E_PWRET.PWRET_CANCEL)
                param = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_CNCDSPMSG);
            else    
                param = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_RESULTMSG);

            // Caso não seja possível obter uma mensagem de resultado da biblioteca, atribui uma padrão
            if (param != null)
                resultMessage = param.parameterValue;
            else
                resultMessage = "TRANSACAO FINALIZADA";

            // Exibe a mensagem de resultado, substituindo a quebra de linha utilizada
            // pela biblioteca pela quebra de linha utilizada na janela
            MessageBox.Show(resultMessage.Replace("\r", "\n"));

            // Transação com erro
            if (ret != 0)
            {
                // A última transação não foi confirmada corretamente
                if (ret == (int)E_PWRET.PWRET_FROMHOSTPENDTRN)
                {
                    // Captura do usuário o que ele deseja fazer com a transação que ficou com problema de integridade
                    // Esse é um ponto que vai ser atingido somente se ocorrer algum erro de tratamento da automação
                    // ou um erro grave de sistema e uma nova transação não poderá ser realizada até que
                    // a última seja resolvida
                    // A captura do que deve ser feita do usuário é somente um exemplo, é possível a automação
                    // obter essa informação em seu banco de dados e saber se a transação deve ser confirmada ou desfeita
                    
                    // Obtem os identificadores da transação pendente
                    PW_Parameter authSyst, virtMerch, reqNum, autLocRef, autExtRef;
                    authSyst = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTHSYST);
                    virtMerch = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDVIRTMERCH);
                    reqNum = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDREQNUM);
                    autLocRef = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTLOCREF);
                    autExtRef = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_PNDAUTEXTREF);

                    // Exibe uma mensagem identificando a transação que está pendente
                    MessageBox.Show(string.Format("Existe uma transação pendente:\n" +
                        "PNDAUTHSYST={0}\n" +
                        "PNDVIRTMERCH={1}\n" +
                        "PNDREQNUM={2}\n" +
                        "PNDAUTLOCREF={3}\n" +
                        "PNDAUTEXTREF={4}\n" +
                        "Será necessário resolvê-la !!!", 
                        authSyst==null ? "" : authSyst.parameterValue,
                        virtMerch == null ? "" : virtMerch.parameterValue,
                        reqNum == null ? "" : reqNum.parameterValue,
                        autLocRef == null ? "" : autLocRef.parameterValue,
                        autExtRef == null ? "" : autExtRef.parameterValue));                    

                    // Pergunta ao usuário qual status de confirmação atribuir para a transação
                    transactionStatus = E_PWCNF.PWCNF_REV_ABORT;

                    // Executa a resolução de pendencia
                    ConfirmUndoTransaction(paramList, transactionStatus, true);
                }
            }
            // Transação com sucesso
            else
            {
                // Verifica se é necessário confirmar a transação
                PW_Parameter confirmacaoNecessaria;
                confirmacaoNecessaria = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_CNFREQ);
                if (confirmacaoNecessaria != null && int.Parse(confirmacaoNecessaria.parameterValue) == 1)
                {
                    // Pergunta ao usuário qual status de confirmação atribuir para a transação
                    transactionStatus = E_PWCNF.PWCNF_REV_ABORT;

                    // Executa a resolução de pendencia
                    ConfirmUndoTransaction(paramList, transactionStatus);
                }
            }

            // *** IMPORTANTE:
            // Sempre após a finalização de uma transação, é necessário verificar o conteúdo
            // da informação PWINFO_IDLEPROCTIME e agendar uma chamada automática da função
            // PW_iIdleProc no horário apontado por ela
            // Essa implementação é necessária para que a biblioteca seja acionada de tempos
            // em tempos, de acordo com a sua necessidade, para resolver pendências com
            // o sistema, mesmo que nenhuma nova transação seja feita
            // PW_Parameter idleProc = paramList.Find(item => item.parameterCode == (ushort)E_PWINFO.PWINFO_IDLEPROCTIME);

            return ret;
        }

        private List<PW_Parameter> GetTransactionResult()
        {
            List<PW_Parameter> results = new List<PW_Parameter>();
            
            // Obtem os resultado da biblioteca
            results = pGWLib.GetTransactionResult();

            // Escreve na janela
            foreach (PW_Parameter item in results)
            {
                // Se é recibo quebra linha por linha e insere no listbox
                // para resolver bug de scroll dos recibos.
                if ((item.parameterCode == (ushort)E_PWINFO.PWINFO_RCPTCHOLDER) ||
                     (item.parameterCode == (ushort)E_PWINFO.PWINFO_RCPTCHSHORT) ||
                     (item.parameterCode == (ushort)E_PWINFO.PWINFO_RCPTFULL) ||
                     (item.parameterCode == (ushort)E_PWINFO.PWINFO_RCPTMERCH) ||
                     (item.parameterCode == (ushort)E_PWINFO.PWINFO_RCPTPRN)
                   )
                {
                    string _input = item.ToString();

                    using (StringReader reader = new StringReader(_input))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Console.WriteLine(line);
                        }
                    }
                }
                else
                {
                    Console.WriteLine(item.ToString());
                }
            }

            return results;
        }

        private int ConfirmUndoTransaction(List<PW_Parameter> transactionResult, E_PWCNF transactionStatus, bool isPending=false)
        {
            int ret;

            if (isPending)
                ret = pGWLib.ConfirmUndoPendingTransaction(transactionStatus, transactionResult);
            else
                ret = pGWLib.ConfirmUndoNormalTransaction(transactionStatus, transactionResult);

            return ret;

        }
    }
}