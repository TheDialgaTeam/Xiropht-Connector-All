using System;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xiropht_Connector_All.Remote;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.Utils;

namespace Xiropht_Connector_All.Seed
{
    public class ClassSeedNodeConnectorObjectSendPacket : IDisposable
    {
        public byte[] packetByte;
        private bool disposed;

        public ClassSeedNodeConnectorObjectSendPacket(string packet)
        {
            packetByte = Encoding.UTF8.GetBytes(packet);
        }

        ~ClassSeedNodeConnectorObjectSendPacket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                packetByte = null;
            }

            disposed = true;
        }
    }

    public class ClassSeedNodeConnectorObjectPacket : IDisposable
    {
        public byte[] buffer;
        public string packet;
        private bool disposed;

        public ClassSeedNodeConnectorObjectPacket()
        {
            buffer = new byte[ClassConnectorSetting.MaxNetworkPacketSize];
            packet = string.Empty;
        }

        ~ClassSeedNodeConnectorObjectPacket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                buffer = null;
                packet = null;
            }

            disposed = true;
        }
    }
    public class ClassSeedNodeConnector : IDisposable
    {
        private TcpClient _connector;
        private bool _isConnected;
        private bool disposed;


        ~ClassSeedNodeConnector()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _connector = null;
            }
            disposed = true;
        }

        /// <summary>
        ///     Start to connect on the seed node.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StartConnectToSeedAsync(string host, int port = ClassConnectorSetting.SeedNodePort, bool isLinux = false)
        {
            if (!string.IsNullOrEmpty(host))
            {
#if DEBUG
                Console.WriteLine("Host target: " + host);
#endif
                try
                {
                    _connector = new TcpClient();
                    await _connector.ConnectAsync(host, port);
                }

                catch (Exception error)
                {
#if DEBUG
                    Console.WriteLine("Error to connect on manual host node: " + error.Message);
#endif
                    _isConnected = false;
                    return false;
                }

                _isConnected = true;
                _connector.SetSocketKeepAliveValues(20 * 60 * 1000, 30 * 1000);
                return true;
            }

            var success = false;
            for (var i = 0; i < ClassConnectorSetting.SeedNodeIp.Count; i++)
            {
                if (i < ClassConnectorSetting.SeedNodeIp.Count)
                {
#if DEBUG
                    Console.WriteLine("Seed Node Host target: " + ClassConnectorSetting.SeedNodeIp[i]);
#endif
                    try
                    {
                        _connector = new TcpClient();
                        await _connector.ConnectAsync(ClassConnectorSetting.SeedNodeIp[i], port);
                        success = true;
                        _isConnected = true;
                        break;
                    }
                    catch (Exception error)
                    {
#if DEBUG
                        Console.WriteLine("Error to connect on seed node: " + error.Message);
#endif
                    }
                }
            }
            if (success)
            {
                _isConnected = true;

                _connector.SetSocketKeepAliveValues(20 * 60 * 1000, 30 * 1000);

                new Thread(delegate () { EnableCheckConnection(); }).Start();

                return true;
            }
            else
            {
                _isConnected = false;
            }
            return false;
        }

        /// <summary>
        /// Check the connection opened to the network.
        /// </summary>
        private void EnableCheckConnection()
        {
            while(_isConnected)
            {
                try
                {
                    if (!ClassUtils.SocketIsConnected(_connector))
                    {
                        _isConnected = false;
                        break;
                    }
                }
                catch
                {
                    _isConnected = false;
                    break;
                }
               Thread.Sleep(1000);
            }
        }



        /// <summary>
        ///     Send packet to seed node.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="certificate"></param>
        /// <param name="isSeedNode"></param>
        /// <param name="isEncrypted"></param>
        /// <returns></returns>
        public async Task<bool> SendPacketToSeedNodeAsync(string packet, string certificate, bool isSeedNode = false,
            bool isEncrypted = false)
        {
            if (!ReturnStatus())
            {
                return false;
            }
            try
            {

                using(var _connectorStream = new NetworkStream(_connector.Client))
                {

                    // 10/08/2018 - MAJOR_UPDATE_1_SECURITY
                    if (ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY) // SSL Layer for Send packet.
                    {
                        if (isEncrypted)
                        {

                            using (ClassSeedNodeConnectorObjectSendPacket packetObject = new ClassSeedNodeConnectorObjectSendPacket(ClassAlgo.GetEncryptedResult(ClassAlgoEnumeration.Rijndael, packet, certificate,
                                ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE) + "*"))
                            {
                                await _connectorStream.WriteAsync(packetObject.packetByte, 0, packetObject.packetByte.Length);
                                await _connectorStream.FlushAsync();
                            }

                        }
                        else
                        {
                            if (isSeedNode)
                            {
                                using (ClassSeedNodeConnectorObjectSendPacket packetObject = new ClassSeedNodeConnectorObjectSendPacket(packet + "*"))
                                {
                                    await _connectorStream.WriteAsync(packetObject.packetByte, 0, packetObject.packetByte.Length);
                                    await _connectorStream.FlushAsync();
                                }
                            }
                            else
                            {
                                using (ClassSeedNodeConnectorObjectSendPacket packetObject = new ClassSeedNodeConnectorObjectSendPacket(packet))
                                {
                                    await _connectorStream.WriteAsync(packetObject.packetByte, 0, packetObject.packetByte.Length);
                                    await _connectorStream.FlushAsync();
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception error)
            {
#if DEBUG
                Console.WriteLine("Error to send packet on seed node: " + error.Message);
#endif
                _isConnected = false;
                return false;
            }

            return true;
        }


        /// <summary>
        ///     Listen and return packet from Seed Node.
        /// </summary>
        /// <returns></returns>
        [HostProtection(ExternalThreading = true)]
        public async Task<string> ReceivePacketFromSeedNodeAsync(string certificate, bool isSeedNode = false,
            bool isEncrypted = false)
        {
            try
            {

                if (ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY) // New Layer for receive packet.
                {
                    using (var bufferPacket = new ClassSeedNodeConnectorObjectPacket())
                    {
                        using (var _connectorStream = new NetworkStream(_connector.Client))
                        {
                            int received = await _connectorStream.ReadAsync(bufferPacket.buffer, 0, bufferPacket.buffer.Length);

                            if (received > 0)
                            {
                                bufferPacket.packet = Encoding.UTF8.GetString(bufferPacket.buffer, 0, received);


                                if (bufferPacket.packet != ClassSeedNodeStatus.SeedError && bufferPacket.packet != ClassSeedNodeStatus.SeedNone)
                                {
                                    if (isEncrypted)
                                    {

                                        if (bufferPacket.packet.Contains("*"))
                                        {
                                            var splitPacket = bufferPacket.packet.Split(new[] { "*" }, StringSplitOptions.None);
                                            bufferPacket.packet = string.Empty;
                                            foreach (var packetEach in splitPacket)
                                            {
                                                if (packetEach != null)
                                                {
                                                    if (!string.IsNullOrEmpty(packetEach))
                                                    {
                                                        if (packetEach.Length > 1)
                                                        {
                                                            if (packetEach.Contains(ClassRemoteNodeCommand.ClassRemoteNodeRecvFromSeedEnumeration.RemoteSendTransactionPerId))
                                                            {
                                                                bufferPacket.packet += packetEach.Replace("*", "") + "*";
                                                            }
                                                            else
                                                            {

                                                                bufferPacket.packet += ClassAlgo.GetDecryptedResult(ClassAlgoEnumeration.Rijndael, packetEach.Replace("*", ""), certificate,
                                                                    ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE) + "*";
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!bufferPacket.packet.Contains(ClassRemoteNodeCommand.ClassRemoteNodeRecvFromSeedEnumeration.RemoteSendTransactionPerId))
                                            {
                                                try
                                                {
                                                    bufferPacket.packet = ClassAlgo.GetDecryptedResult(ClassAlgoEnumeration.Rijndael, bufferPacket.packet, certificate,
                                                    ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE) + "*";
                                                }
                                                catch
                                                {

                                                }
                                            }
                                        }
                                    }
                                }
                                if (bufferPacket.packet == ClassSeedNodeCommand.ClassReceiveSeedEnumeration.DisconnectPacket)
                                {
                                    _isConnected = false;
                                    return ClassSeedNodeStatus.SeedError;
                                }

                                return bufferPacket.packet;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                _isConnected = false;
                return ClassSeedNodeStatus.SeedError;
            }

            return ClassSeedNodeStatus.SeedNone;
        }

        /// <summary>
        ///     Return the status of connection.
        /// </summary>
        /// <returns></returns>
        public bool GetStatusConnectToSeed(bool isLinux = false)
        {

            if (!ClassUtils.SocketIsConnected(_connector))
            {
                _isConnected = false;
            }

            return _isConnected;
        }
        
        /// <summary>
        /// Return directly status without to proceed check.
        /// </summary>
        /// <returns></returns>
        public bool ReturnStatus()
        {
            return _isConnected;
        }

        /// <summary>
        ///     Disconnect to Seed Node.
        /// </summary>
        public void DisconnectToSeed()
        {
            ClassConnectorSetting.NETWORK_GENESIS_KEY = ClassConnectorSetting.NETWORK_GENESIS_DEFAULT_KEY;
            _isConnected = false;
            _connector?.Close();
            _connector?.Dispose();
            Dispose();
        }
    }
}