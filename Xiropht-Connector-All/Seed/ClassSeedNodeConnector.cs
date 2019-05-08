using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private string _currentSeedNodeHost;


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
                _currentSeedNodeHost = host;
                _connector.SetSocketKeepAliveValues(20 * 60 * 1000, 30 * 1000);
                return true;
            }

            Dictionary<string, int> ListOfSeedNodesSpeed = new Dictionary<string, int>();
            foreach (var seedNode in ClassConnectorSetting.SeedNodeIp)
            {

                try
                {
                    int seedNodeResponseTime = -1;
                    Task taskCheckSeedNode = Task.Run(() => seedNodeResponseTime = CheckPing.CheckPingHost(seedNode.Key, true));
                    taskCheckSeedNode.Wait(ClassConnectorSetting.MaxPingDelay);
                    if (seedNodeResponseTime == -1)
                    {
                        seedNodeResponseTime = ClassConnectorSetting.MaxTimeoutConnect;
                    }
#if DEBUG
                    Console.WriteLine(seedNode.Key + " response time: " + seedNodeResponseTime + " ms.");
#endif
                    ListOfSeedNodesSpeed.Add(seedNode.Key, seedNodeResponseTime);

                }
                catch
                {
                    ListOfSeedNodesSpeed.Add(seedNode.Key, ClassConnectorSetting.MaxTimeoutConnect); // Max delay.
                }

            }

            ListOfSeedNodesSpeed = ListOfSeedNodesSpeed.OrderBy(u => u.Value).ToDictionary(z => z.Key, y => y.Value);

            var success = false;
            var seedNodeTested = false;
            foreach (var seedNode in ListOfSeedNodesSpeed)
            {
#if DEBUG
                Console.WriteLine("Seed Node Host target: " + seedNode.Key);
#endif
                try
                {
                    if (ClassConnectorSetting.SeedNodeDisconnectScore.ContainsKey(seedNode.Key))
                    {
                        int totalDisconnection = ClassConnectorSetting.SeedNodeDisconnectScore[seedNode.Key].Item1;
                        long lastDisconnection = ClassConnectorSetting.SeedNodeDisconnectScore[seedNode.Key].Item2;
                        if (lastDisconnection + ClassConnectorSetting.SeedNodeMaxKeepAliveDisconnection < DateTimeOffset.Now.ToUnixTimeSeconds())
                        {
                            totalDisconnection = 0;
                            ClassConnectorSetting.SeedNodeDisconnectScore[seedNode.Key] = new Tuple<int, long>(totalDisconnection, lastDisconnection);
                        }
                        if (totalDisconnection < ClassConnectorSetting.SeedNodeMaxDisconnection)
                        {
                            _connector = new TcpClient();
                            seedNodeTested = true;
                            if (_connector.ConnectAsync(seedNode.Key, port).Wait(ClassConnectorSetting.MaxTimeoutConnect))
                            {
                                success = true;
                                _isConnected = true;
                                _currentSeedNodeHost = seedNode.Key;
                                break;
                            }
                        }
                    }

                }
                catch (Exception error)
                {
#if DEBUG
                        Console.WriteLine("Error to connect on seed node: " + error.Message);
#endif
                }

            }
            if (!seedNodeTested) // Clean up just in case if every seed node return too much disconnection saved in their counter.
            {
                foreach(var seednode in ClassConnectorSetting.SeedNodeIp)
                {
                    if (ClassConnectorSetting.SeedNodeDisconnectScore.ContainsKey(seednode.Key))
                    {
                        ClassConnectorSetting.SeedNodeDisconnectScore[seednode.Key] = new Tuple<int, long>(0, 0);
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
                    using (var bufferedNetworkStream = new BufferedStream(_connectorStream, ClassConnectorSetting.MaxNetworkPacketSize))
                    {
                        // 10/08/2018 - MAJOR_UPDATE_1_SECURITY
                        if (ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY) // SSL Layer for Send packet.
                        {
                            if (isEncrypted)
                            {

                                using (ClassSeedNodeConnectorObjectSendPacket packetObject = new ClassSeedNodeConnectorObjectSendPacket(ClassAlgo.GetEncryptedResult(ClassAlgoEnumeration.Rijndael, packet, certificate,
                                    ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE) + "*"))
                                {
                                    await bufferedNetworkStream.WriteAsync(packetObject.packetByte, 0, packetObject.packetByte.Length);
                                    await bufferedNetworkStream.FlushAsync();
                                }

                            }
                            else
                            {
                                if (isSeedNode)
                                {
                                    using (ClassSeedNodeConnectorObjectSendPacket packetObject = new ClassSeedNodeConnectorObjectSendPacket(packet + "*"))
                                    {
                                        await bufferedNetworkStream.WriteAsync(packetObject.packetByte, 0, packetObject.packetByte.Length);
                                        await bufferedNetworkStream.FlushAsync();
                                    }
                                }
                                else
                                {
                                    using (ClassSeedNodeConnectorObjectSendPacket packetObject = new ClassSeedNodeConnectorObjectSendPacket(packet))
                                    {
                                        await bufferedNetworkStream.WriteAsync(packetObject.packetByte, 0, packetObject.packetByte.Length);
                                        await bufferedNetworkStream.FlushAsync();
                                    }
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
                            using (var bufferedNetworkStream = new BufferedStream(_connectorStream, ClassConnectorSetting.MaxNetworkPacketSize))
                            {
                                int received = await bufferedNetworkStream.ReadAsync(bufferPacket.buffer, 0, bufferPacket.buffer.Length);

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

                                                                    string packetDecrypt = ClassAlgo.GetDecryptedResult(ClassAlgoEnumeration.Rijndael, packetEach.Replace("*", ""), certificate,
                                                                        ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE);

                                                                    if (packetDecrypt.Contains(ClassSeedNodeCommand.ClassReceiveSeedEnumeration.WalletSendSeedNode))
                                                                    {
                                                                        var packetNewSeedNode = packetDecrypt.Replace(ClassSeedNodeCommand.ClassReceiveSeedEnumeration.WalletSendSeedNode, "");
                                                                        var splitPacketNewSeedNode = packetNewSeedNode.Split(new[] { ";" }, StringSplitOptions.None);
                                                                        var newSeedNodeHost = splitPacketNewSeedNode[0];
                                                                        var newSeedNodeCountry = splitPacketNewSeedNode[1];
                                                                        if(!ClassConnectorSetting.SeedNodeIp.ContainsKey(newSeedNodeHost))
                                                                        {
                                                                            ClassConnectorSetting.SeedNodeIp.Add(newSeedNodeHost, newSeedNodeCountry);
                                                                        }
                                                                        if (!ClassConnectorSetting.SeedNodeDisconnectScore.ContainsKey(newSeedNodeHost))
                                                                        {
                                                                            ClassConnectorSetting.SeedNodeDisconnectScore.Add(newSeedNodeHost, new Tuple<int, long>(0, 0));
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        bufferPacket.packet += packetDecrypt + "*";
                                                                    }
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
                                                        string packetDecrypt = ClassAlgo.GetDecryptedResult(ClassAlgoEnumeration.Rijndael, bufferPacket.packet, certificate,
                                                        ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE);

                                                        if (bufferPacket.packet.Contains(ClassSeedNodeCommand.ClassReceiveSeedEnumeration.WalletSendSeedNode))
                                                        {
                                                            var packetNewSeedNode = packetDecrypt.Replace(ClassSeedNodeCommand.ClassReceiveSeedEnumeration.WalletSendSeedNode, "");
                                                            var splitPacketNewSeedNode = packetNewSeedNode.Split(new[] { ";" }, StringSplitOptions.None);
                                                            var newSeedNodeHost = splitPacketNewSeedNode[0];
                                                            var newSeedNodeCountry = splitPacketNewSeedNode[1];
                                                            if (!ClassConnectorSetting.SeedNodeIp.ContainsKey(newSeedNodeHost))
                                                            {
                                                                ClassConnectorSetting.SeedNodeIp.Add(newSeedNodeHost, newSeedNodeCountry);
                                                            }
                                                            if (!ClassConnectorSetting.SeedNodeDisconnectScore.ContainsKey(newSeedNodeHost))
                                                            {
                                                                ClassConnectorSetting.SeedNodeDisconnectScore.Add(newSeedNodeHost, new Tuple<int, long>(0, 0));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            bufferPacket.packet = packetDecrypt + "*";
                                                        }
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
        /// Return the current seed node host used.
        /// </summary>
        /// <returns></returns>
        public string ReturnCurrentSeedNodeHost()
        {
            return _currentSeedNodeHost;
        }

        /// <summary>
        ///     Disconnect to Seed Node.
        /// </summary>
        public void DisconnectToSeed()
        {
            if (!string.IsNullOrEmpty(_currentSeedNodeHost))
            {
                if (ClassConnectorSetting.SeedNodeDisconnectScore.ContainsKey(_currentSeedNodeHost))
                {
                    int totalDisconnection = ClassConnectorSetting.SeedNodeDisconnectScore[_currentSeedNodeHost].Item1 + 1;
                    ClassConnectorSetting.SeedNodeDisconnectScore[_currentSeedNodeHost] = new Tuple<int, long>(totalDisconnection, DateTimeOffset.Now.ToUnixTimeSeconds());
                }
            }
            ClassConnectorSetting.NETWORK_GENESIS_KEY = ClassConnectorSetting.NETWORK_GENESIS_DEFAULT_KEY;
            _isConnected = false;
            _currentSeedNodeHost = string.Empty;
            _connector?.Close();
            _connector?.Dispose();
            Dispose();
        }
    }
}