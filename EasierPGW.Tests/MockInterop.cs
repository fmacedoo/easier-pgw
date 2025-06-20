using System.Text;
using static PGW.CustomObjects;
using static PGW.Enums;

namespace EasierPGW.Tests
{
    public static class MockInterop
    {
        private static bool _initialized = false;
        private static bool _transactionStarted = false;
        private static E_PWOPER _currentOperation;
        private static Dictionary<ushort, string> _parameters = new();
        private static int _stepCounter = 0;
        private static Random _random = new();

        // Mock data for operations
        private static readonly List<PW_Operations> _availableOperations = new()
        {
            new PW_Operations { bOperType = (byte)E_PWOperType.ADMIN, szText = "Instalação", szValue = "1" },
            new PW_Operations { bOperType = (byte)E_PWOperType.SALE, szText = "Venda", szValue = "33" },
            new PW_Operations { bOperType = (byte)E_PWOperType.SALE, szText = "Cancelamento", szValue = "34" },
            new PW_Operations { bOperType = (byte)E_PWOperType.ADMIN, szText = "Administrativa", szValue = "32" }
        };

        public static short PW_iInit(string pszWorkingDir)
        {
            Console.WriteLine($"[MOCK] PW_iInit: {pszWorkingDir}");
            _initialized = true;
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iNewTransac(byte bOper)
        {
            Console.WriteLine($"[MOCK] PW_iNewTransac: {(E_PWOPER)bOper}");
            
            if (!_initialized)
                return (short)E_PWRET.PWRET_DLLNOTINIT;

            _transactionStarted = true;
            _currentOperation = (E_PWOPER)bOper;
            _parameters.Clear();
            _stepCounter = 0;
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iAddParam(ushort wParam, string pszValue)
        {
            Console.WriteLine($"[MOCK] PW_iAddParam: {(E_PWINFO)wParam} = {pszValue}");
            
            if (!_transactionStarted)
                return (short)E_PWRET.PWRET_TRNNOTINIT;

            _parameters[wParam] = pszValue;
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iExecTransac(PW_GetData[] vstParam, ref short piNumParam)
        {
            Console.WriteLine($"[MOCK] PW_iExecTransac: Step {_stepCounter}");
            
            if (!_transactionStarted)
                return (short)E_PWRET.PWRET_TRNNOTINIT;

            _stepCounter++;

            // Simulate different transaction flows based on operation and step
            switch (_currentOperation)
            {
                case E_PWOPER.PWOPER_INSTALL:
                    return SimulateInstallationFlow(vstParam, ref piNumParam);

                case E_PWOPER.PWOPER_SALE:
                    return SimulateSaleFlow(vstParam, ref piNumParam);

                default:
                    return (short)E_PWRET.PWRET_OK;
            }
        }

        private static short SimulateInstallationFlow(PW_GetData[] vstParam, ref short piNumParam)
        {
            switch (_stepCounter)
            {
                case 1:
                    // Simulate needing PIN-pad confirmation
                    piNumParam = 1;
                    vstParam[0] = CreateMockGetData(E_PWDAT.PWDAT_DSPCHECKOUT, "Confirme a instalação no PIN-pad");
                    return (short)E_PWRET.PWRET_MOREDATA;

                case 2:
                    // Simulate successful installation
                    return (short)E_PWRET.PWRET_OK;

                default:
                    return (short)E_PWRET.PWRET_OK;
            }
        }

        private static short SimulateSaleFlow(PW_GetData[] vstParam, ref short piNumParam)
        {
            switch (_stepCounter)
            {
                case 1:
                    // Simulate needing transaction amount
                    piNumParam = 1;
                    vstParam[0] = CreateMockGetData(E_PWDAT.PWDAT_TYPED, "Digite o valor da transação", 
                        mask: "R$ @@@.@@@,@@", maxLength: 12);
                    return (short)E_PWRET.PWRET_MOREDATA;

                case 2:
                    // Simulate payment method selection
                    piNumParam = 1;
                    vstParam[0] = CreateMockGetData(E_PWDAT.PWDAT_MENU, "Selecione o tipo de pagamento");
                    SetupPaymentMethodMenu(vstParam[0]);
                    return (short)E_PWRET.PWRET_MOREDATA;

                case 3:
                    // Simulate card reading
                    piNumParam = 1;
                    vstParam[0] = CreateMockGetData(E_PWDAT.PWDAT_CARDINF, "Insira ou aproxime o cartão");
                    return (short)E_PWRET.PWRET_MOREDATA;

                case 4:
                    // Simulate PIN entry
                    piNumParam = 1;
                    vstParam[0] = CreateMockGetData(E_PWDAT.PWDAT_PPENCPIN, "Digite a senha no PIN-pad");
                    return (short)E_PWRET.PWRET_MOREDATA;

                case 5:
                    // Simulate transaction processing
                    piNumParam = 1;
                    vstParam[0] = CreateMockGetData(E_PWDAT.PWDAT_DSPCHECKOUT, "Processando transação...");
                    return (short)E_PWRET.PWRET_MOREDATA;

                case 6:
                    // Simulate successful transaction
                    return (short)E_PWRET.PWRET_OK;

                default:
                    return (short)E_PWRET.PWRET_OK;
            }
        }

        private static PW_GetData CreateMockGetData(E_PWDAT dataType, string prompt, string mask = "", byte maxLength = 50)
        {
            return new PW_GetData
            {
                wIdentificador = (ushort)E_PWINFO.PWINFO_TOTAMNT, // Example identifier
                bTipoDeDado = (byte)dataType,
                szPrompt = prompt,
                szMascaraDeCaptura = mask,
                bTamanhoMaximo = maxLength,
                bTiposEntradaPermitidos = (byte)E_PWTypeInput.TYPED_NUMERIC
            };
        }

        private static void SetupPaymentMethodMenu(PW_GetData data)
        {
            data.bNumOpcoesMenu = 3;
            data.vszTextoMenu = new TextoMenu[40];
            data.vszValorMenu = new ValorMenu[40];
            
            data.vszTextoMenu[0] = new TextoMenu { szTextoMenu = "Cartão de Crédito" };
            data.vszValorMenu[0] = new ValorMenu { szValorMenu = "1" };
            
            data.vszTextoMenu[1] = new TextoMenu { szTextoMenu = "Cartão de Débito" };
            data.vszValorMenu[1] = new ValorMenu { szValorMenu = "2" };
            
            data.vszTextoMenu[2] = new TextoMenu { szTextoMenu = "Voucher" };
            data.vszValorMenu[2] = new ValorMenu { szValorMenu = "4" };
        }

        public static short PW_iGetResult(short iInfo, StringBuilder pszData, uint ulDataSize)
        {
            var info = (E_PWINFO)iInfo;
            Console.WriteLine($"[MOCK] PW_iGetResult: {info}");

            // Simulate mock results based on the requested info
            string result = info switch
            {
                E_PWINFO.PWINFO_RESULTMSG => GetMockResultMessage(),
                E_PWINFO.PWINFO_REQNUM => GenerateReqNum(),
                E_PWINFO.PWINFO_AUTLOCREF => GenerateReference(),
                E_PWINFO.PWINFO_AUTEXTREF => GenerateReference(),
                E_PWINFO.PWINFO_VIRTMERCH => "12345678",
                E_PWINFO.PWINFO_AUTHSYST => "MOCK_PROVIDER",
                E_PWINFO.PWINFO_CNFREQ => "1", // Requires confirmation
                E_PWINFO.PWINFO_CARDNAME => "MOCK CARD",
                E_PWINFO.PWINFO_AUTHCODE => "123456",
                E_PWINFO.PWINFO_RCPTFULL => GenerateMockReceipt(),
                _ => ""
            };

            if (!string.IsNullOrEmpty(result))
            {
                pszData.Clear();
                pszData.Append(result);
                return (short)E_PWRET.PWRET_OK;
            }

            return (short)E_PWRET.PWRET_NODATA;
        }

        private static string GetMockResultMessage()
        {
            return _currentOperation switch
            {
                E_PWOPER.PWOPER_INSTALL => "Instalação realizada com sucesso",
                E_PWOPER.PWOPER_SALE => "Transação aprovada",
                _ => "Operação concluída"
            };
        }

        private static string GenerateReqNum()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss") + _random.Next(1000, 9999);
        }

        private static string GenerateReference()
        {
            return _random.Next(100000, 999999).ToString();
        }

        private static string GenerateMockReceipt()
        {
            return $"""
                    COMPROVANTE DE VENDA
                    
                    Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}
                    Valor: R$ 100,00
                    Cartão: ****1234
                    Autorização: 123456
                    
                    TRANSAÇÃO APROVADA
                    """;
        }

        public static short PW_iConfirmation(uint ulResult, string pszReqNum, string pszLocRef,
                                           string pszExtRef, string pszVirtMerch, string pszAuthSyst)
        {
            Console.WriteLine($"[MOCK] PW_iConfirmation: Status={(E_PWCNF)ulResult}, ReqNum={pszReqNum}");
            // Simulate random confirmation success/failure
            return _random.Next(10) < 8 ? (short)E_PWRET.PWRET_OK : (short)E_PWRET.PWRET_FROMHOST;
        }

        public static short PW_iGetOperations(byte bOperType, PW_Operations[] vstOperations, ref short piNumOperations)
        {
            Console.WriteLine($"[MOCK] PW_iGetOperations: Type={bOperType}");
            
            var filteredOps = _availableOperations.Where(op => 
                (bOperType & op.bOperType) != 0).ToList();
            
            int count = Math.Min(filteredOps.Count, piNumOperations);
            for (int i = 0; i < count; i++)
            {
                vstOperations[i] = filteredOps[i];
            }
            
            piNumOperations = (short)count;
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPEventLoop(StringBuilder pszDisplay, uint ulDisplaySize)
        {
            // Simulate PIN-pad event processing
            Thread.Sleep(100); // Simulate processing time
            
            // Randomly simulate different events
            var eventType = _random.Next(10);
            
            if (eventType < 7)
            {
                return (short)E_PWRET.PWRET_NOTHING; // Continue processing
            }
            else if (eventType < 9)
            {
                pszDisplay.Clear();
                pszDisplay.Append("Processando...\rAguarde");
                return (short)E_PWRET.PWRET_DISPLAY;
            }
            else
            {
                return (short)E_PWRET.PWRET_OK; // Operation completed
            }
        }

        // Additional mock methods for PIN-pad operations
        public static short PW_iPPGetCard(ushort uiIndex)
        {
            Console.WriteLine($"[MOCK] PW_iPPGetCard: Index={uiIndex}");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPGetPIN(ushort uiIndex)
        {
            Console.WriteLine($"[MOCK] PW_iPPGetPIN: Index={uiIndex}");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPAbort()
        {
            Console.WriteLine("[MOCK] PW_iPPAbort");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPGetData(ushort uiIndex)
        {
            Console.WriteLine($"[MOCK] PW_iPPGetData: Index={uiIndex}");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPGoOnChip(ushort uiIndex)
        {
            Console.WriteLine($"[MOCK] PW_iPPGoOnChip: Index={uiIndex}");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPFinishChip(ushort uiIndex)
        {
            Console.WriteLine($"[MOCK] PW_iPPFinishChip: Index={uiIndex}");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPConfirmData(ushort uiIndex)
        {
            Console.WriteLine($"[MOCK] PW_iPPConfirmData: Index={uiIndex}");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPPositiveConfirmation(ushort uiIndex)
        {
            Console.WriteLine($"[MOCK] PW_iPPPositiveConfirmation: Index={uiIndex}");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPGenericCMD(ushort uiIndex)
        {
            Console.WriteLine($"[MOCK] PW_iPPGenericCMD: Index={uiIndex}");
            return (short)E_PWRET.PWRET_OK;
        }

        public static short PW_iPPRemoveCard()
        {
            Console.WriteLine("[MOCK] PW_iPPRemoveCard");
            return (short)E_PWRET.PWRET_OK;
        }

        // Reset method for testing
        public static void Reset()
        {
            _initialized = false;
            _transactionStarted = false;
            _parameters.Clear();
            _stepCounter = 0;
        }
    }
}