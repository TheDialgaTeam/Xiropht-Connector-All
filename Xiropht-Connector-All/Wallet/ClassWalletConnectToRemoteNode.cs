using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xiropht_Connector_All.Remote;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.Utils;

namespace Xiropht_Connector_All.Wallet
{

    public class ClassWalletConnectToRemoteNodeObjectSendPacket : IDisposable
    {
        public byte[] packetByte;
        private bool disposed;

        public ClassWalletConnectToRemoteNodeObjectSendPacket(string packet)
        {
            packetByte = Encoding.UTF8.GetBytes(packet);
        }

        ~ClassWalletConnectToRemoteNodeObjectSendPacket()
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

            disposed = true;
        }
    }

    public class ClassWalletConnectToRemoteNodeObjectPacket : IDisposable
    {
        public char[] buffer;
        public string packet;
        private bool disposed;

        public ClassWalletConnectToRemoteNodeObjectPacket()
        {
            buffer = new char[ClassConnectorSetting.MaxNetworkPacketSize];
            packet = string.Empty;
        }

        ~ClassWalletConnectToRemoteNodeObjectPacket()
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

            disposed = true;
        }
    }

    public class ClassWalletConnectToRemoteNodeObjectError
    {
        public const string ObjectError = "ERROR";
        public const string ObjectNone = "NONE";
    }

    public class ClassWalletConnectToRemoteNodeObject
    {
        public const string ObjectAskBlock = "BLOCK";
        public const string ObjectTransaction = "TRANSACTION";
        public const string ObjectSupply = "SUPPLY";
        public const string ObjectCirculating = "CIRCULATING";
        public const string ObjectFee = "FEE";
        public const string ObjectBlockMined = "MINED";
        public const string ObjectDifficulty = "DIFFICULTY";
        public const string ObjectRate = "RATE";
        public const string ObjectPendingTransaction = "PENDING-TRANSACTION";
        public const string ObjectAskWalletTransaction = "ASK-TRANSACTION";
        public const string ObjectAskLastBlockFound = "ASK-LAST-BLOCK-FOUND";
        public const string ObjectAskWalletAnonymityTransaction = "ASK-ANONYMITY-TRANSACTION";
    }

    public class ClassWalletConnectToRemoteNode : IDisposable
    {
        private TcpClient _remoteNodeClient;
        private string _remoteNodeClientType;
        public string RemoteNodeHost;
        private StreamReader _remoteNodeReader;
        public bool RemoteNodeStatus;
        private NetworkStream _remoteNodeStream;
        public int TotalInvalidPacket;
        private bool disposed;
        public long LastTrustDate;

        ~ClassWalletConnectToRemoteNode()
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
                _remoteNodeClient = null;
                _remoteNodeReader = null;
                _remoteNodeStream = null;
            }
            disposed = true;
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="remoteNodeType"></param>
        public ClassWalletConnectToRemoteNode(string remoteNodeType)
        {
            _remoteNodeClientType = remoteNodeType;
            RemoteNodeStatus = true;
        }

        /// <summary>
        /// Return the connection status opened to a remote node.
        /// </summary>
        /// <returns></returns>
        public bool CheckRemoteNode()
        {
            return RemoteNodeStatus;
        }

        /// <summary>
        ///     Connect the wallet to a remote node.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<bool> ConnectToRemoteNodeAsync(string host, int port, bool isLinux = false)
        {
            _remoteNodeClient?.Close();
            TotalInvalidPacket = 0;
            LastTrustDate = 0;
            try
            {
                RemoteNodeStatus = true;
                _remoteNodeClient = new TcpClient();
                await ConnectToTarget(host, port);
            }
            catch (Exception error)
            {
#if DEBUG
                Console.WriteLine("Error to connect wallet on remote nodes: " + error.Message);
#endif
                RemoteNodeStatus = false;
                return false;
            }

            RemoteNodeHost = host;

            new Thread(delegate () { EnableCheckConnection(); }).Start();
            
            return true;
        }

        /// <summary>
        /// Check the connection opened to the remote node.
        /// </summary>
        /// <param name="isLinux"></param>
        private async void EnableCheckConnection()
        {
            while (RemoteNodeStatus)
            {
                try
                {
                    if (!ClassUtils.SocketIsConnected(_remoteNodeClient))
                    {
                        RemoteNodeStatus = false;
                        break;
                    }
                    else
                    {
                        if (!await SendPacketRemoteNodeAsync(ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.KeepAlive + "|0"))
                        {
                            RemoteNodeStatus = false;
                            break;
                        }
                    }

                }
                catch
                {
                    RemoteNodeStatus = false;
                    break;
                }
                Thread.Sleep(5000);
            }
        }


        private async Task<bool> ConnectToTarget(string host, int port)
        {

            var clientTask = _remoteNodeClient.ConnectAsync(host, port);
            var delayTask = Task.Delay(ClassConnectorSetting.MaxTimeoutConnectRemoteNode);

            var completedTask = await Task.WhenAny(new[] { clientTask, delayTask });
            return completedTask == clientTask;

        }

        /// <summary>
        ///     Listen network of remote node.
        /// </summary>
        [HostProtection(ExternalThreading = true)]
        public async Task<string> ListenRemoteNodeNetworkAsync()
        {
            try
            {
                if (_remoteNodeStream == null)
                {
                    _remoteNodeStream = new NetworkStream(_remoteNodeClient.Client);
                    _remoteNodeReader = new StreamReader(_remoteNodeStream);
                }

                using (var bufferPacket = new ClassWalletConnectToRemoteNodeObjectPacket())
                {
                    int received = await _remoteNodeReader.ReadAsync(bufferPacket.buffer, 0, bufferPacket.buffer.Length);
                    if (received > 0)
                    {
                        return new string(bufferPacket.buffer, 0, received);
                    }
                }
            }
            catch (Exception error)
            {
                _remoteNodeStream?.Close();
                _remoteNodeStream?.Dispose();
                _remoteNodeReader?.Close();
                _remoteNodeReader?.Dispose();
                _remoteNodeReader = null;
                _remoteNodeStream = null;
                _remoteNodeClient?.Close();
                _remoteNodeClient?.Dispose();
                RemoteNodeStatus = false;
#if DEBUG
                Console.WriteLine("Error to listen remote node network: " + error.Message);
#endif
                return ClassWalletConnectToRemoteNodeObjectError.ObjectError;
            }

            return ClassWalletConnectToRemoteNodeObjectError.ObjectNone;
        }

        /// <summary>
        ///     Disconnect wallet of remote node.
        /// </summary>
        public void DisconnectRemoteNodeClient()
        {
            _remoteNodeClient?.Close();
            _remoteNodeClientType = string.Empty;
            _remoteNodeReader?.Close();
            _remoteNodeStream?.Close();
            TotalInvalidPacket = 0;
            LastTrustDate = 0;
            Dispose();
        }

        /// <summary>
        ///     Send a selected command to remote node.
        /// </summary>
        /// <param name="command"></param>
        [HostProtection(ExternalThreading = true)]
        public async Task<bool> SendPacketRemoteNodeAsync(string command)
        {

            try
            {
                using (var packetObject = new ClassWalletConnectToRemoteNodeObjectSendPacket(command+"*"))
                {
                    await _remoteNodeClient.GetStream().WriteAsync(packetObject.packetByte, 0, packetObject.packetByte.Length).ConfigureAwait(false);
                    await _remoteNodeClient.GetStream().FlushAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                RemoteNodeStatus = false;
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Send the right packet type to remote node.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SendPacketTypeRemoteNode(string walletId)
        {
           ClassWalletConnectToRemoteNodeObjectSendPacket packet;

            switch (_remoteNodeClientType)
            {
                case ClassWalletConnectToRemoteNodeObject.ObjectTransaction:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.WalletAskHisNumberTransaction +
                        "|" + walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectAskWalletAnonymityTransaction:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.WalletAskHisAnonymityNumberTransaction +
                        "|" + walletId + "*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectSupply:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskCoinMaxSupply + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectCirculating:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskCoinCirculating + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectFee:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskTotalFee + "|" + walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectBlockMined:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskTotalBlockMined + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectDifficulty:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskCurrentDifficulty + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectRate:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskCurrentRate + "|" +
                        walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectPendingTransaction:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskTotalPendingTransaction +
                        "|" + walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectAskLastBlockFound:
                    packet = new ClassWalletConnectToRemoteNodeObjectSendPacket(
                        ClassRemoteNodeCommandForWallet.RemoteNodeSendPacketEnumeration.AskLastBlockFoundTimestamp +
                        "|" + walletId+"*");
                    break;
                case ClassWalletConnectToRemoteNodeObject.ObjectAskWalletTransaction:
                    return true;
                case ClassWalletConnectToRemoteNodeObject.ObjectAskBlock:
                    return true;
                default:
                    return false;
            }


            try
            {

                await _remoteNodeClient.GetStream().WriteAsync(packet.packetByte, 0, packet.packetByte.Length);
                await _remoteNodeClient.GetStream().FlushAsync();
                packet?.Dispose();
               
            }
            catch (Exception error)
            {
                RemoteNodeStatus = false;
#if DEBUG
                Console.WriteLine("Error to send packet on remote node network: " + error.Message);
#endif
                return false;
            }


            return true;
        }
    }
}