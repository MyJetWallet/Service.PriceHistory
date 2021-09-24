using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.PriceHistory.Domain.Models;

namespace Service.PriceHistory.Grpc.Models
{
    [DataContract]
    public class GetAssetPricesV2Response
    {
        [DataMember(Order = 1)] public List<AssetPrices> AssetPrices { get; set; }
    }
}