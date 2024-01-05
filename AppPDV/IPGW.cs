using System.Runtime.InteropServices;
using static PGW.Enums;

namespace AppPDV
{
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IPGW
    {
        E_PWRET installation();
    }
}