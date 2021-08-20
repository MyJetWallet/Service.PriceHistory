using System.Runtime.Serialization;

namespace Service.PriceHistory.Grpc.Models
{
    [DataContract]

    public class BasePriceResponse
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string AssetId { get; set; }
        [DataMember(Order = 3)] public decimal CurrentPrice { get; set; }
        [DataMember(Order = 4)] public decimal H24P { get; set; }
        [DataMember(Order = 5)] public decimal H24A { get; set; }
        [DataMember(Order = 6)] public decimal D7 { get; set; }
        [DataMember(Order = 7)] public decimal M1 { get; set; }
        [DataMember(Order = 8)] public decimal M3 { get; set; }
        
    }
}