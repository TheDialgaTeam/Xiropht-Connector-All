using System.Net.Sockets;
using System.Threading.Tasks;
using Xiropht_Connector_All.Setting;

namespace Xiropht_Connector_All.Utils
{
    public class CheckTcp
    {
        /// <summary>
        ///     check tcp port.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static async Task<bool> CheckTcpClientAsync(string host, int port)
        {
            try
            {
                return await ConnectToTarget(host, port);
            }
            catch
            {
                return false;
            }
        }


        private static async Task<bool> ConnectToTarget(string host, int port)
        {
            using (var client = new TcpClient())
            {
                var clientTask = client.ConnectAsync(host, port);
                var delayTask = Task.Delay(ClassConnectorSetting.MaxTimeoutConnectRemoteNode);

                var completedTask = await Task.WhenAny(new[] { clientTask, delayTask });
                return completedTask == clientTask;
            }
        }
    }
}