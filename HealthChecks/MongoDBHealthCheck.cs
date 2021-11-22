using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.HealthChecks
{
    public class MongoDBHealthCheck : IHealthCheck
    {
        private const string MONGODB_PING_CMD = "{ping:1}";

        private IMongoClient _client;

        public MongoDBHealthCheck(IMongoClient client)
        {
            _client = client;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var kryxiviaDb = _client.GetDatabase(Domain.Constants.MONGO_KRYXIVIA_DB_NAME);
            var blockchainStorageTransferLogsDb = _client.GetDatabase(Constants.MongoBlockchainStorageTransferLogsDBName);

            var description = string.Empty;

            var isKryxiviaDbLive = (double) (await kryxiviaDb.RunCommandAsync((Command<BsonDocument>)MONGODB_PING_CMD))?["ok"];
            if (isKryxiviaDbLive != 1) description = "Kryxivia database is not accessible; ";

            var isBlockchainStorageTransferLogsDbLive = (double) (await blockchainStorageTransferLogsDb.RunCommandAsync((Command<BsonDocument>)MONGODB_PING_CMD))?["ok"];
            if (isBlockchainStorageTransferLogsDbLive != 1) description += "BlockchainStorageTransferLogs database is not accessible; ";

            if (isKryxiviaDbLive != 1) return HealthCheckResult.Unhealthy(description);
            if (isBlockchainStorageTransferLogsDbLive != 1) return HealthCheckResult.Degraded(description);

            return HealthCheckResult.Healthy();
        }
    }
}
