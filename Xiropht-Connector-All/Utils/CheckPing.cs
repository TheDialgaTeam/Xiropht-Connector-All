using System.Net.NetworkInformation;

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
                var pingTestNode = new Ping();
                var replyNode = pingTestNode.Send(host);
                if (replyNode.Status == IPStatus.Success) return (int) replyNode.RoundtripTime;
            }
            catch
            {
                return -1; // Not work.
            }

            return -1;
        }
    }
}