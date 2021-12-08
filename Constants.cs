using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI
{
    public static class Constants
    {
        public const int MAX_CHARACTER_PER_ACCOUNT = 1;

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
