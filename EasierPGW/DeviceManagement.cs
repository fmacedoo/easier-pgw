using System.Management;

namespace EasierPGW
{
    public class DeviceManagement
    {
        public static string?[] List()
        {
            string query = "SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                var result = searcher.Get();
                var devices = new string?[result.Count];
                int i = 0;
                foreach (var obj in result)
                {
                    var caption = obj["Caption"];
                    if (caption == null) continue;
                    devices[i++] = caption.ToString();
                }

                return devices;
            }
        }
    }
}
