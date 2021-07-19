using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.PriceHistory.Grpc;

namespace Service.PriceHistory.Client
{
    [UsedImplicitly]
    public class PriceHistoryClientFactory: MyGrpcClientFactory
    {
        public PriceHistoryClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IBasePriceSerivce GetBasePriceService() => CreateGrpcService<IBasePriceSerivce>();
    }
}
