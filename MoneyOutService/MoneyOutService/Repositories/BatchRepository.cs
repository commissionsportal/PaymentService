using Dapper;
using MoneyOutService.Inerfaces;
using MoneyOutService.Models;

namespace MoneyOutService.Repositories
{
    public class BatchRepository : IBatchRepository
    {
        private IConnectionService _connectionService;

        public BatchRepository(IConnectionService connectionService)
        {
            _connectionService = connectionService;
        }
        
        public async Task<long> CreateBatchId(int clientId)
        {
            using (var connection = _connectionService.GetConnection())
            {
                var sql = @"
INSERT INTO [dbo].[ACHBatches]
    ([ClientId] ,[Created])
OUTPUT INSERTED.ID
VALUES 
    (@clientId ,@created)";

                connection.Open();
                return await connection.ExecuteScalarAsync<long>(sql, new
                {
                    clientId,
                    created = DateTime.UtcNow
                });
            }
        }

        public async Task<Batch> UpdateBatch(int clientId, long batchId, ReleaseResult[] releases)
        {
            foreach (var release in releases)
            {
                await InsertBatchRows(clientId, batchId, release);
            }

            return await GetBatch(clientId, batchId);
        }

        private async Task<int> InsertBatchRows(int clientId, long batchId, ReleaseResult release)
        {
            using (var connection = _connectionService.GetConnection())
            {
                var sql = @"
INSERT INTO [dbo].[BatchPayments]
           ([ClientId]
           ,[BatchId]
           ,[NodeId]
           ,[BonusId]
           ,[PeriodId]
           ,[DetailId]
           ,[Amount])
OUTPUT INSERTED.ID
     VALUES
           (@clientId
           ,@batchId
           ,@nodeId
           ,@bonusId
           ,@periodId
           ,@detailId
           ,@amount)";

                connection.Open();
                return await connection.ExecuteScalarAsync<int>(sql, new
                {
                    clientId,
                    batchId,
                    release.Amount,
                    release.BonusId,
                    release.NodeId,
                    release.PeriodId,
                    release.DetailId
                });
            }
        }

        public async Task<Batch> GetBatch(int clientId, long batchId)
        {
            using (var connection = _connectionService.GetConnection())
            {
                var sql = @"
SELECT Id, ClientId, Created from ACHBatches 
WHERE ClientId = @clientId and Id = @batchId

select * from BatchPayments 
WHERE ClientId = @clientId and BatchId = @batchId";

                connection.Open();
                using (var multi = await connection.QueryMultipleAsync(sql, new { clientId, batchId }))
                {
                    var batch = multi.ReadFirstOrDefault<Batch>();
                    if (batch == null) return new Batch { Id = batchId, Created = null, Bonuses = null};

                    batch.Bonuses = multi.Read<BonusRelease>().ToArray();

                    return batch;
                }
            }
        }

        public async Task<BatchSummaryCount> GetBatches(int clientId, DateTime? begin, DateTime? end, int offset, int count)
        {
            using (var connection = _connectionService.GetConnection())
            {
                var filter = "";
                if (begin.HasValue)
                {
                    filter += " AND Created >= @begin";
                }

                if (end.HasValue)
                {
                    filter += " AND Created <= @end";
                }

                var sql = @"
SELECT b.Id, b.ClientId, b.Created, p.Count, P.Amount
FROM [ACHBatches] b
LEFT JOIN (
	SELECT sum(amount) Amount, count(Id) Count, BatchId, ClientId from BatchPayments
	WHERE ClientId = @clientId
	group by BatchId, ClientId
) p on p.BatchId = b.Id and p.ClientId = b.ClientId
WHERE b.ClientId = @clientId" + filter + @"
ORDER BY Created DESC
OFFSET @offset ROWS
FETCH NEXT @count ROWS ONLY;

SELECT Count(1) 
FROM [ACHBatches] 
WHERE ClientId = @clientId" + filter + ";";

                connection.Open();
                using (var multi = await connection.QueryMultipleAsync(sql, new
                {
                    clientId,
                    begin,
                    end,
                    offset,
                    count
                }))
                {
                    var batches = multi.Read<BatchSummary>();
                    var batchCount = multi.Read<int>().FirstOrDefault();

                    return new BatchSummaryCount
                    {
                        Batches = batches.ToArray(),
                        Count = batchCount
                    };
                }
            }
        }
    }
}
