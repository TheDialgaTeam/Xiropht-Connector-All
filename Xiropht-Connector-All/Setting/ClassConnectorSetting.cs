using System.Collections.Generic;

namespace Xiropht_Connector_All.Setting
{
    public class ClassConnectorSettingEnumeration
    {
        /// <summary>
        /// List of login types.
        /// </summary>
        public const string WalletLoginType = "WALLET";
        public const string WalletCreateType = "WALLET-CREATE";
        public const string WalletRestoreType = "WALLET-ASK";
        public const string RemoteLoginType = "REMOTE";
        public const string MinerLoginType = "MINER";
    }

    public class ClassConnectorSetting
    {
        public const int SeedNodePort = 18000;
        public const int RemoteNodeHttpPort = 18001;
        public const int RemoteNodePort = 18002;

        /// <summary>
        ///     UPDATES - First Major Update done at 10/08/2018
        /// </summary>
        public const bool MAJOR_UPDATE_1 = true; // Implementation of: Timestamp recv transaction (for wallet), link blockchain height for transaction, last block found.

        public const bool MAJOR_UPDATE_1_SECURITY = true; // Implementation of: Certificate for include a new layer of protection.

        public const int MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE = 256; // Size of Certificate encryption [Between tools and seed nodes]

        public const int MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE_ITEM = 256;

        public const string NETWORK_GENESIS_DEFAULT_KEY = "XIROPHTKEY"; // DEFAULT GENESIS KEY [Small static part of dynamic key encryption between tools and blockchain.]

        /// <summary>
        ///     UPDATES - Update done at 17/10/2018
        /// </summary>
        public static string NETWORK_GENESIS_KEY = "XIROPHTKEY"; // GENESIS KEY [Small static part included on dynamic key encryption between tools and blockchain, updated by the blockchain in real time.]

        public const string NETWORK_GENESIS_SECONDARY_KEY = "XIROPHTSEED"; // GENESIS SECONDARY KEY [Layer encryption key included on dynamic certificate key between tools and seed nodes]

        public const decimal NETWORK_MINING_ACCURACY_EXPECTED = 80; // 80% average of accuracy from miners expected. Use for calculate network hashrate from network difficulty.

        public static List<string> SeedNodeIp = new List<string> {"87.98.156.228"};

        public const decimal MinimumWalletTransactionFee = 0.000010000m;
        public const decimal MinimumWalletTransactionAnonymousFee = 0.000010000m;
        public const int MaxTimeoutConnect = 5000;
        public const int MaxTimeoutConnectRemoteNode = 500;
        public const int MaxTimeoutSendPacket = 2000;
        public const int MaxTimeoutConnectLocalhostRemoteNode = 100;
        public const int MaxNetworkPacketSize = 8192;
        public const int MaxRemoteNodeInvalidPacket = 10;
        public const int MaxRemoteNodeBanTime = 60; // 60 seconds.
        public const int MaxDelayRemoteNodeTrust = 30; // 30 seconds.
        public const int MaxDelayRemoteNodeSyncResponse = 10; // 10 seconds.
        public const int MaxDelayRemoteNodeWaitResponse = 5; // 5 seconds.
        public const string CoinName = "Xiropht";
        public const string CoinNameMin = "XIR";
        public const string NetworkPhase = "Public Test";
    }
}