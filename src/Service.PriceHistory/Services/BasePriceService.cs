using System;
using System.Linq;
using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using Service.PriceHistory.Domain.Models;
using Service.PriceHistory.Domain.Models.NoSql;
using Service.PriceHistory.Grpc;
using Service.PriceHistory.Grpc.Models;

namespace Service.PriceHistory.Services
{
    public class BasePriceService : IBasePriceSerivce
    {
        private readonly IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> _dataWriter;

        public BasePriceService(IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> dataWriter)
        {
            _dataWriter = dataWriter;
        }

        public async Task<BasePriceListResponse> GetAllPrices(BasePriceRequest request)
        {
            var entities = await _dataWriter.GetAsync(request.BrokerId);
            return new BasePriceListResponse()
            {
                BasePrices = entities.Select(rec => new BasePriceResponse()
                {
                    AssetId = rec.AssetPriceRecord.AssetSymbol,
                    BrokerId = rec.AssetPriceRecord.BrokerId,
                    CurrentPrice = rec.AssetPriceRecord.CurrentPrice,
                    H24A = rec.AssetPriceRecord.H24.Price,
                    H24P = rec.AssetPriceRecord.H24P,
                    D7 = rec.AssetPriceRecord.D7.Price,
                    M1 = rec.AssetPriceRecord.M1.Price,
                    M3 = rec.AssetPriceRecord.M3.Price,
                }).ToList()
            };
        }

        public async Task<BasePriceResponse> GetPricesByAsset(BasePriceRequest request)
        {
            var entity = await _dataWriter.GetAsync(request.BrokerId, request.AssetId);
            return new BasePriceResponse()
            {
                AssetId = entity.AssetPriceRecord.AssetSymbol,
                BrokerId = entity.AssetPriceRecord.BrokerId,
                CurrentPrice = entity.AssetPriceRecord.CurrentPrice,
                H24A = entity.AssetPriceRecord.H24.Price,
                H24P = entity.AssetPriceRecord.H24P,
                D7 = entity.AssetPriceRecord.D7.Price,
                M1 = entity.AssetPriceRecord.M1.Price,
                M3 = entity.AssetPriceRecord.M3.Price,
            };
        }

        public async Task<BasePriceResponse> EditBasePriceRecord(BasePriceEditRequest request)
        {
            var record = new AssetPriceRecord()
            {
                AssetSymbol = request.AssetId,
                BrokerId = request.BrokerId,
                CurrentPrice = request.CurrentPrice,
                H24 = new BasePrice()
                {
                    Price = request.H24,
                    RecordTime = DateTime.UtcNow
                },
                D7 = new BasePrice()
                {
                    Price = request.D7,
                    RecordTime = DateTime.UtcNow
                },
                M1 = new BasePrice()
                {
                    Price = request.M1,
                    RecordTime = DateTime.UtcNow
                },
                M3 = new BasePrice()
                {
                    Price = request.M3,
                    RecordTime = DateTime.UtcNow
                },
            };
            await _dataWriter.InsertOrReplaceAsync(AssetPriceRecordNoSqlEntity.Create(record));
            
            var entity = await _dataWriter.GetAsync(request.BrokerId, request.AssetId);
            return new BasePriceResponse()
            {
                AssetId = entity.AssetPriceRecord.AssetSymbol,
                BrokerId = entity.AssetPriceRecord.BrokerId,
                CurrentPrice = entity.AssetPriceRecord.CurrentPrice,
                H24A = entity.AssetPriceRecord.H24.Price,
                H24P = entity.AssetPriceRecord.H24P,
                D7 = entity.AssetPriceRecord.D7.Price,
                M1 = entity.AssetPriceRecord.M1.Price,
                M3 = entity.AssetPriceRecord.M3.Price,
            };
        }
    }
}