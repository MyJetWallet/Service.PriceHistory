using Autofac;
using MyJetWallet.Sdk.Grpc;
using MyJetWallet.Sdk.NoSql;
using Service.AssetsDictionary.Client;
using Service.BaseCurrencyConverter.Client;
using Service.PriceHistory.Domain.Models.NoSql;
using Service.PriceHistory.Jobs;
using SimpleTrading.CandlesHistory.Grpc;

namespace Service.PriceHistory.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            builder.RegisterMyNoSqlWriter<AssetPriceRecordNoSqlEntity>(()=>Program.Settings.MyNoSqlWriterUrl,
                AssetPriceRecordNoSqlEntity.TableName);
            
            builder.RegisterMyNoSqlWriter<AssetPricesNoSqlEntity>(()=>Program.Settings.MyNoSqlWriterUrl,
                AssetPricesNoSqlEntity.TableName);
            
            
            var factory = new MyGrpcClientFactory(Program.Settings.CandlesServiceGrpcUrl);
            
            builder
                .RegisterInstance(factory.CreateGrpcService<ISimpleTradingCandlesHistoryGrpc>())
                .As<ISimpleTradingCandlesHistoryGrpc>()
                .SingleInstance();

            var noSqlClient = builder.CreateNoSqlClient(Program.Settings.MyNoSqlReaderHostPort, Program.LogFactory);
            builder.RegisterAssetsDictionaryClients(noSqlClient);
                
            builder.RegisterType<PriceUpdatingJob>()
                .AsSelf()
                .AutoActivate()
                .SingleInstance();
            
            builder.RegisterType<PriceUpdatingJobV2>()
                .As<IStartable>()
                .AsSelf()
                .AutoActivate()
                .SingleInstance();
            
            builder.RegisterBaseCurrencyConverterClient(Program.Settings.BaseCurrencyConverterGrpcServiceUrl, noSqlClient);
        }
    }
}