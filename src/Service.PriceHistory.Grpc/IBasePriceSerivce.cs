using System.ServiceModel;
using System.Threading.Tasks;
using Service.PriceHistory.Grpc.Models;

namespace Service.PriceHistory.Grpc
{
    [ServiceContract]
    public interface IBasePriceSerivce
    {
        [OperationContract]
        Task<BasePriceListResponse> GetAllPrices(BasePriceRequest request);
        
        [OperationContract]
        Task<BasePriceResponse> GetPricesByAsset(BasePriceRequest request);
        
        [OperationContract]
        Task<BasePriceResponse> EditBasePriceRecord(BasePriceEditRequest request);
        
        [OperationContract]
        Task<GetAssetPricesV2Response> GetAssetPricesV2();
    }
}