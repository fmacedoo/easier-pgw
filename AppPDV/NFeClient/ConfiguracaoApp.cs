using System.Net;
using DFe.Classes.Flags;
using DFe.Utils;
using NFe.Classes.Informacoes.Emitente;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Utils;
using NFe.Utils.Email;

namespace AppPDV.NFeClient
{
	public class ConfiguracaoApp
	{
		private ConfiguracaoServico _cfgServico;

		public ConfiguracaoApp()
		{
			CfgServico = ConfiguracaoServico.Instancia;
			CfgServico.tpAmb = TipoAmbiente.Homologacao;
			CfgServico.tpEmis = TipoEmissao.teNormal;
			CfgServico.ProtocoloDeSeguranca = ServicePointManager.SecurityProtocol;
			Emitente = new emit { CPF = "", CRT = CRT.SimplesNacional };
			EnderecoEmitente = new enderEmit();
			ConfiguracaoEmail = new ConfiguracaoEmail("email@dominio.com", "senha", "Envio de NFE", "<html><body><h1>O EMAIL</h1></body></html>", "smtp.dominio.com", 587, true, true);
		}

		public ConfiguracaoServico CfgServico
		{
			get
			{
				ConfiguracaoServico.Instancia.CopiarPropriedades(_cfgServico);
				return _cfgServico;
			}
			set
			{
				_cfgServico = value;
				ConfiguracaoServico.Instancia.CopiarPropriedades(value);
			}
		}

		public emit Emitente { get; set; }
		public enderEmit EnderecoEmitente { get; set; }
		public ConfiguracaoEmail ConfiguracaoEmail { get; set; }
	}
}