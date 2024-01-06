using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PGW
{
    public class CustomObjects
    {
        public class PW_Parameter
        {
            public string? parameterName;
            public ushort parameterCode;
            public string? parameterValue;

            public override string ToString()
            {
                return string.Format("{0}({1}): {2}", parameterName, parameterCode, parameterValue);
            }

            public PW_Parameter() {}

            public PW_Parameter(string Name, ushort Code, string Value)
            {
                parameterName = Name;
                parameterCode = Code;
                parameterValue = Value;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct PW_Operations
        {
            public byte bOperType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
            public string szText;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)]
            public string szValue;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct PW_GetData
        {
            public ushort wIdentificador;
            public byte bTipoDeDado;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 84)]
            public string szPrompt;
            public byte bNumOpcoesMenu;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public TextoMenu[] vszTextoMenu;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public ValorMenu[] vszValorMenu;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
            public string szMascaraDeCaptura;
            public byte bTiposEntradaPermitidos;
            public byte bTamanhoMinimo;
            public byte bTamanhoMaximo;
            public int ulValorMinimo;
            public int ulValorMaximo;
            public byte bOcultarDadosDigitados;
            public byte bValidacaoDado;
            public byte bAceitaNulo;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
            public string szValorInicial;
            public byte bTeclasDeAtalho;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 84)]
            public string szMsgValidacao;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 84)]
            public string szMsgConfirmacao;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 84)]
            public string szMsgDadoMaior;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 84)]
            public string szMsgDadoMenor;
            public byte bCapturarDataVencCartao;
            public int ulTipoEntradaCartao;
            public byte bItemInicial;
            public byte bNumeroCapturas;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 84)]
            public string szMsgPrevia;
            public byte bTipoEntradaCodigoBarras;
            public byte bOmiteMsgAlerta;
            public byte bIniciaPelaEsquerda;
            public byte bNotificarCancelamento;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct TextoMenu
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
            public string szTextoMenu;
        }
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ValorMenu
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szValorMenu;
        }
    }
}