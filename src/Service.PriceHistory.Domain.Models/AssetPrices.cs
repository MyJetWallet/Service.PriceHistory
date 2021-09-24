using System;
using System.Collections.Generic;

namespace Service.PriceHistory.Domain.Models
{
    public class AssetPrices
    {
        public string BrokerId { get; set; }
        public string BaseAsset { get; set; }
        public DateTime CalculatingTime { get; set; }
        public List<AssetPrice> PricesByQuoteAsset { get; set; }
    }

    public class AssetPrice
    {
        public string Asset { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal H24 { get; set; }
        public decimal D7 { get; set; }
        public decimal M1 { get; set; }
        public decimal M3 { get; set; }
    }
}