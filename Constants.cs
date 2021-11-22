using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI
{
    public static class Constants
    {
        public const string NFT_EXTERNAL_URL_ADDR = "https://kryxivia.io/nft/{ObjectId}";

        public const string MONGO_BLOCKCHAIN_STORAGE_TRANSFER_LOGS_DB_TAG = "TransferLogs";

        public static string MongoBlockchainStorageTransferLogsDBName
        {
            get
            {
                return $"BlockchainStorage{MONGO_BLOCKCHAIN_STORAGE_TRANSFER_LOGS_DB_TAG}";
            }
        }
    }
}
