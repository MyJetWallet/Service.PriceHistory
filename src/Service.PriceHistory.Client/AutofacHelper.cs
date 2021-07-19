using Autofac;
using MyJetWallet.Sdk.NoSql;
using MyNoSqlServer.DataReader;
using Service.PriceHistory.Domain.NoSql;
using Service.PriceHistory.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.PriceHistory.Client
{
    public static class AutofacHelper
    {
        public static void RegisterPriceHistoryClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new PriceHistoryClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetBasePriceService()).As<IBasePriceSerivce>().SingleInstance();
        }

        public static void RegisterPriceHistoryNoSqlClient(this ContainerBuilder builder, MyNoSqlTcpClient client)
        {
            builder.RegisterMyNoSqlReader<InstrumentPriceRecordNoSqlEntity>(client,
                InstrumentPriceRecordNoSqlEntity.TableName);
        }
    }
}
