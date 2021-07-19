using MyNoSqlServer.Abstractions;
using Service.PriceHistory.Domain.Models;

namespace Service.PriceHistory.Domain.NoSql
{
    public class InstrumentPriceRecordNoSqlEntity: MyNoSqlDbEntity
    {
        
            public const string TableName = "base-price-history";

            public static string GeneratePartitionKey(string brokerId) => brokerId;
            public static string GenerateRowKey(string instrumentSymbol) => instrumentSymbol;

            public InstrumentPriceRecord InstrumentPriceRecord;


            public static InstrumentPriceRecordNoSqlEntity Create(InstrumentPriceRecord priceRecord)
            {
                return new()
                {
                    PartitionKey = GeneratePartitionKey(priceRecord.BrokerId),
                    RowKey = GenerateRowKey(priceRecord.InstrumentSymbol),
                    InstrumentPriceRecord = priceRecord
                };
            }
        
    }
}