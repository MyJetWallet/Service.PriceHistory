using System;

namespace Service.PriceHistory.Domain.Models
{
    public class AssetPriceRecord
    {
        public string BrokerId { get; set; }
        public string AssetSymbol { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal H24P { get; set; }
        public BasePrice H24 { get; set; }
        public BasePrice D7 { get; set; }
        public BasePrice M1 { get; set; }
        public BasePrice M3 { get; set; }
    }

    public class BasePrice
    {
        public decimal Price { get; set; }
        public DateTime RecordTime { get; set; }
    }
}