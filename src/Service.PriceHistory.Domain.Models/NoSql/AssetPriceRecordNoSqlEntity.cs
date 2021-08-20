using MyNoSqlServer.Abstractions;

namespace Service.PriceHistory.Domain.Models.NoSql
{
    public class AssetPriceRecordNoSqlEntity: MyNoSqlDbEntity
    {
        
            public const string TableName = "jetwallet-asset-base-price-history";

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