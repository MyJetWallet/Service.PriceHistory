using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.PriceHistory.Settings
{
    public class SettingsModel
    {
        [YamlProperty("PriceHistory.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("PriceHistory.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("PriceHistory.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
        
        [YamlProperty("PriceHistory.CandlesServiceGrpcUrl")]
        public string CandlesServiceGrpcUrl { get; set; }
        
        [YamlProperty("PriceHistory.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }
        
        [YamlProperty("PriceHistory.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }
        
        [YamlProperty("PriceHistory.TimerPeriodInSec")]
        public int TimerPeriodInSec { get; set; }

        [YamlProperty("PriceHistory.BasePriceAssetId")]
        public string BasePriceAssetId { get; set; }

    }
}
