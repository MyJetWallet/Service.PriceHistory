using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.PriceHistory.Grpc.Models
{
    [DataContract]
    public class BasePriceListResponse
    {
        [DataMember(Order = 1)] public List<BasePriceResponse> BasePrices { get; set; }
    }
}