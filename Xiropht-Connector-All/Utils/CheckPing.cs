using System.Net.NetworkInformation;
using Xiropht_Connector_All.Setting;

namespace Xiropht_Connector_All.Utils
{
    public class CheckPing
    {
        /// <summary>
        ///     Check the ping from host and return the ping time.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static int CheckPingHost(string host)
        {
            try
            {
                using (var pingTestNode = new Ping())
                {
                    var replyNode = pingTestNode.Send(host);
                    if (replyNode.Status == IPStatus.Success) return (int)replyNode.RoundtripTime;
                    else
                        return ClassConnectorSetting.MaxTimeoutConnectRemoteNode;
                }
            }
            catch
            {
                return ClassConnectorSetting.MaxTimeoutConnectRemoteNode;
            }
        }
    }
}