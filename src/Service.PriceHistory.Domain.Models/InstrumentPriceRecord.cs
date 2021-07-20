using System;

namespace Service.PriceHistory.Domain.Models
{
    public class InstrumentPriceRecord
    {
        public string BrokerId { get; set; }
        public string InstrumentSymbol { get; set; }
        public double CurrentPrice { get; set; }
        public double H24P { get; set; }
        public BasePrice H24 { get; set; }
        public BasePrice D7 { get; set; }
        public BasePrice M1 { get; set; }
        public BasePrice M3 { get; set; }
    }

    public class BasePrice
    {
        public double Price { get; set; }
        public DateTime RecordTime { get; set; }
    }
}