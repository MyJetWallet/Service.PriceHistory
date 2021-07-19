using System.Runtime.Serialization;

namespace Service.PriceHistory.Grpc.Models
{
    [DataContract]

    public class BasePriceResponse
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string InstrumentId { get; set; }
        [DataMember(Order = 3)] public double CurrentPrice { get; set; }
        [DataMember(Order = 4)] public double H24P { get; set; }
        [DataMember(Order = 5)] public double H24A { get; set; }
        [DataMember(Order = 6)] public double D7 { get; set; }
        [DataMember(Order = 7)] public double M1 { get; set; }
        [DataMember(Order = 8)] public double M3 { get; set; }
        
    }
}