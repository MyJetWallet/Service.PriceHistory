using MyNoSqlServer.Abstractions;

namespace Service.PriceHistory.Domain.Models.NoSql
{
    public class AssetPricesNoSqlEntity: MyNoSqlDbEntity
    {
        public const string TableName = "jetwallet-asset-prices";
        public static string GeneratePartitionKey(string brokerId) => brokerId;
        public static string GenerateRowKey(string baseAsset) => baseAsset;

        public AssetPrices AssetPrices;
        
        public static AssetPricesNoSqlEntity Create(AssetPrices prices)
        {
            return new()
            {
                PartitionKey = GeneratePartitionKey(prices.BrokerId),
                RowKey = GenerateRowKey(prices.BaseAsset),
                AssetPrices = prices
            };
        }
        
    }
}