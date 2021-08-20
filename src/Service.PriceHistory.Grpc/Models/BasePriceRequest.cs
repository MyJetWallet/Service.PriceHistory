using System.Runtime.Serialization;

namespace Service.PriceHistory.Grpc.Models
{
    [DataContract]
    public class BasePriceRequest
    {
        [DataMember(Order = 1)]public string BrokerId { get; set; }
        [DataMember(Order = 2)]public string AssetId { get; set; }
    }
}