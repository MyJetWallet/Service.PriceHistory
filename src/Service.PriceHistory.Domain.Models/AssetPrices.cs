using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.PriceHistory.Domain.Models
{
    [DataContract]
    public class AssetPrices
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string BaseAsset { get; set; }
        [DataMember(Order = 3)] public DateTime CalculatingTime { get; set; }
        [DataMember(Order = 4)] public List<AssetPrice> PricesByQuoteAsset { get; set; }
    }

    [DataContract]
    public class AssetPrice
    {
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public decimal CurrentPrice { get; set; }
        [DataMember(Order = 3)] public decimal H24 { get; set; }
        [DataMember(Order = 4)] public decimal D7 { get; set; }
        [DataMember(Order = 5)] public decimal M1 { get; set; }
        [DataMember(Order = 6)] public decimal M3 { get; set; }
    }
}