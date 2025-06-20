using System.Text;
using static PGW.CustomObjects;

namespace EasierPGW.Tests
{
    public interface IPGWInterop
    {
        short PW_iInit(string pszWorkingDir);
        short PW_iNewTransac(byte bOper);
        short PW_iAddParam(ushort wParam, string pszValue);
        short PW_iExecTransac(PW_GetData[] vstParam, ref short piNumParam);
        short PW_iGetResult(short iInfo, StringBuilder pszData, uint ulDataSize);
        short PW_iConfirmation(uint ulResult, string pszReqNum, string pszLocRef, string pszExtRef, string pszVirtMerch, string pszAuthSyst);
        short PW_iGetOperations(byte bOperType, PW_Operations[] vstOperations, ref short piNumOperations);
        short PW_iPPEventLoop(StringBuilder pszDisplay, uint ulDisplaySize);
        short PW_iPPGetCard(ushort uiIndex);
        short PW_iPPGetPIN(ushort uiIndex);
        short PW_iPPAbort();
        short PW_iPPGetData(ushort uiIndex);
        short PW_iPPGoOnChip(ushort uiIndex);
        short PW_iPPFinishChip(ushort uiIndex);
        short PW_iPPConfirmData(ushort uiIndex);
        short PW_iPPPositiveConfirmation(ushort uiIndex);
        short PW_iPPGenericCMD(ushort uiIndex);
        short PW_iPPRemoveCard();
    }

    public class MockInteropWrapper : IPGWInterop
    {
        public short PW_iInit(string pszWorkingDir) => MockInterop.PW_iInit(pszWorkingDir);
        public short PW_iNewTransac(byte bOper) => MockInterop.PW_iNewTransac(bOper);
        public short PW_iAddParam(ushort wParam, string pszValue) => MockInterop.PW_iAddParam(wParam, pszValue);
        public short PW_iExecTransac(PW_GetData[] vstParam, ref short piNumParam) => MockInterop.PW_iExecTransac(vstParam, ref piNumParam);
        public short PW_iGetResult(short iInfo, StringBuilder pszData, uint ulDataSize) => MockInterop.PW_iGetResult(iInfo, pszData, ulDataSize);
        public short PW_iConfirmation(uint ulResult, string pszReqNum, string pszLocRef, string pszExtRef, string pszVirtMerch, string pszAuthSyst) => MockInterop.PW_iConfirmation(ulResult, pszReqNum, pszLocRef, pszExtRef, pszVirtMerch, pszAuthSyst);
        public short PW_iGetOperations(byte bOperType, PW_Operations[] vstOperations, ref short piNumOperations) => MockInterop.PW_iGetOperations(bOperType, vstOperations, ref piNumOperations);
        public short PW_iPPEventLoop(StringBuilder pszDisplay, uint ulDisplaySize) => MockInterop.PW_iPPEventLoop(pszDisplay, ulDisplaySize);
        public short PW_iPPGetCard(ushort uiIndex) => MockInterop.PW_iPPGetCard(uiIndex);
        public short PW_iPPGetPIN(ushort uiIndex) => MockInterop.PW_iPPGetPIN(uiIndex);
        public short PW_iPPAbort() => MockInterop.PW_iPPAbort();
        public short PW_iPPGetData(ushort uiIndex) => MockInterop.PW_iPPGetData(uiIndex);
        public short PW_iPPGoOnChip(ushort uiIndex) => MockInterop.PW_iPPGoOnChip(uiIndex);
        public short PW_iPPFinishChip(ushort uiIndex) => MockInterop.PW_iPPFinishChip(uiIndex);
        public short PW_iPPConfirmData(ushort uiIndex) => MockInterop.PW_iPPConfirmData(uiIndex);
        public short PW_iPPPositiveConfirmation(ushort uiIndex) => MockInterop.PW_iPPPositiveConfirmation(uiIndex);
        public short PW_iPPGenericCMD(ushort uiIndex) => MockInterop.PW_iPPGenericCMD(uiIndex);
        public short PW_iPPRemoveCard() => MockInterop.PW_iPPRemoveCard();
    }
}