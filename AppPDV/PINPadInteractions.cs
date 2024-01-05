using PGW.Dll;
using static PGW.Enums;

namespace AppPDV
{
    public class PINPadInteractions
    {
        private readonly Func<E_PWRET> loopPP;
        private readonly Dictionary<E_PWDAT, Func<ushort, E_PWRET>> actions_map;

        public PINPadInteractions(Func<E_PWRET> loopPP)
        {
            this.loopPP = loopPP;

            actions_map = new Dictionary<E_PWDAT, Func<ushort, E_PWRET>>
            {
                { E_PWDAT.PWDAT_CARDOFF, CARDOFF },
                { E_PWDAT.PWDAT_CARDONL, CARDONL },
                { E_PWDAT.PWDAT_PPCONF, PPCONF },
                { E_PWDAT.PWDAT_PPDATAPOSCNF, PPDATAPOSCNF },
                { E_PWDAT.PWDAT_PPGENCMD, PPGENCMD },
                { E_PWDAT.PWDAT_PPENCPIN, PPENCPIN },
                { E_PWDAT.PWDAT_PPENTRY, PPENTRY },
                { E_PWDAT.PWDAT_PPREMCRD, PPREMCRD },
            };
        }

        public E_PWRET? Interact(E_PWDAT option, ushort index)
        {
            if (actions_map.TryGetValue(option, out var action))
            {
                return action(index);
            }

            return null;
        }

        // Captura de dados do cartão
        private E_PWRET CARDINF(ushort index)
        {
            E_PWRET result = (E_PWRET)Interop.PW_iPPGetCard(index);
            Logger.Debug(string.Format("PW_iPPGetCard={0}", result.ToString()));
            if (result == (int)E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }

        // Processamento offline do cartão
        private E_PWRET CARDOFF(ushort index)
        {
            Logger.Info("CARDOFF");
            E_PWRET result = (E_PWRET)Interop.PW_iPPGoOnChip(index);
            Logger.Debug(string.Format("PW_iPPGoOnChip={0}", result.ToString()));
            if (result == E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }

        // Processamento online do cartão
        private E_PWRET CARDONL(ushort index)
        {
            Logger.Info("CARDONL");
            E_PWRET result = (E_PWRET)Interop.PW_iPPFinishChip(index);
            Logger.Debug(string.Format("PW_iPPFinishChip={0}", result.ToString()));
            if (result == (int)E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }

        // Confirmação de dado no PIN-pad
        private E_PWRET PPCONF(ushort index)
        {
            Logger.Info("PPCONF");
            E_PWRET result = (E_PWRET)Interop.PW_iPPConfirmData(index);
            Logger.Debug(string.Format("PW_iPPConfirmData={0}", result.ToString()));
            if (result == (int)E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }

        // Confirmação positiva PIN-pad
        private E_PWRET PPDATAPOSCNF(ushort index)
        {
            Logger.Info("PPDATAPOSCNF");
            E_PWRET result = (E_PWRET)Interop.PW_iPPPositiveConfirmation(index);
            Logger.Debug(string.Format("PW_iPPPositiveConfirmation={0}", result.ToString()));
            if (result == (int)E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }

        // Comando genérico no PIN-pad
        private E_PWRET PPGENCMD(ushort index)
        {
            E_PWRET result = (E_PWRET)Interop.PW_iPPGenericCMD(index);
            Logger.Debug(string.Format("PW_iPPGenericCMD={0}", result.ToString()));
            if (result == (int)E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }

        // Senha do portador
        private E_PWRET PPENCPIN(ushort index)
        {
            E_PWRET result = (E_PWRET)Interop.PW_iPPGetPIN(index);
            Logger.Debug(string.Format("PW_iPPGetPIN={0}", result.ToString()));
            if (result == (int)E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }

        // Entrada digitada no PIN-pad
        private E_PWRET PPENTRY(ushort index)
        {
            E_PWRET result = (E_PWRET)Interop.PW_iPPGetData(index);
            Logger.Debug(string.Format("PW_iPPGetData={0}", result.ToString()));
            if (result == (int)E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }

        // Remoção de cartão do PIN-pad
        private E_PWRET PPREMCRD(ushort index)
        {
            E_PWRET result = (E_PWRET)Interop.PW_iPPRemoveCard();
            Logger.Debug(string.Format("PW_iPPRemoveCard={0}", result.ToString()));
            if (result == (int)E_PWRET.PWRET_OK)
                result = loopPP();
            return result;
        }
    }
}