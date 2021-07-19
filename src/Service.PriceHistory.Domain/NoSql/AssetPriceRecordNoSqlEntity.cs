using MyNoSqlServer.Abstractions;
using Service.PriceHistory.Domain.Models;

namespace Service.PriceHistory.Domain.NoSql
{
    public class AssetPriceRecordNoSqlEntity: MyNoSqlDbEntity
    {
        
            public const string TableName = "base-price-history";

            public static string GeneratePartitionKey(string brokerId) => brokerId;
            public static string GenerateRowKey(string assetSymbol) => assetSymbol;

            public AssetPriceRecord AssetPriceRecord;


            public static AssetPriceRecordNoSqlEntity Create(AssetPriceRecord priceRecord)
            {
                return new()
                {
                    PartitionKey = GeneratePartitionKey(priceRecord.BrokerId),
                    RowKey = GenerateRowKey(priceRecord.AssetSymbol),
                    AssetPriceRecord = priceRecord
                };
            }
        
    }
}