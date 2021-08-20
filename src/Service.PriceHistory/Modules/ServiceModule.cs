using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using MyJetWallet.Sdk.Grpc;
using MyJetWallet.Sdk.NoSql;
using Service.AssetsDictionary.Client;
using Service.AssetsDictionary.Client.Grpc;
using Service.AssetsDictionary.Grpc;
using Service.PriceHistory.Domain.Models.NoSql;
using Service.PriceHistory.Jobs;
using SimpleTrading.CandlesHistory.Grpc;

namespace Service.PriceHistory.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            builder.RegisterMyNoSqlWriter<AssetPriceRecordNoSqlEntity>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl),
                AssetPriceRecordNoSqlEntity.TableName);
            
            
            var factory = new MyGrpcClientFactory(Program.Settings.CandlesServiceGrpcUrl);
            
            builder
                .RegisterInstance(factory.CreateGrpcService<ISimpleTradingCandlesHistoryGrpc>())
                .As<ISimpleTradingCandlesHistoryGrpc>()
                .SingleInstance();

            var noSqlClient = builder.CreateNoSqlClient(Program.ReloadedSettings(e => e.MyNoSqlReaderHostPort));
            builder.RegisterAssetsDictionaryClients(noSqlClient);
                
            builder.RegisterType<PriceUpdatingJob>()
                .AsSelf()
                .AutoActivate()
                .SingleInstance();
            
        }
    }
}