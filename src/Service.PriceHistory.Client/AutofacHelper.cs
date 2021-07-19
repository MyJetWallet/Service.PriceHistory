using Autofac;
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
    }
}
