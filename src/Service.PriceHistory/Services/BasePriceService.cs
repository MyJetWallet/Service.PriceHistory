using System;
using System.Linq;
using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using Service.PriceHistory.Domain.Models;
using Service.PriceHistory.Domain.NoSql;
using Service.PriceHistory.Grpc;
using Service.PriceHistory.Grpc.Models;

namespace Service.PriceHistory.Services
{
    public class BasePriceService : IBasePriceSerivce
    {
        private readonly IMyNoSqlServerDataWriter<InstrumentPriceRecordNoSqlEntity> _dataWriter;

        public BasePriceService(IMyNoSqlServerDataWriter<InstrumentPriceRecordNoSqlEntity> dataWriter)
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
                    InstrumentId = rec.InstrumentPriceRecord.InstrumentSymbol,
                    BrokerId = rec.InstrumentPriceRecord.BrokerId,
                    CurrentPrice = rec.InstrumentPriceRecord.CurrentPrice,
                    H24A = rec.InstrumentPriceRecord.H24.Price,
                    H24P = rec.InstrumentPriceRecord.H24P,
                    D7 = rec.InstrumentPriceRecord.D7.Price,
                    M1 = rec.InstrumentPriceRecord.M1.Price,
                    M3 = rec.InstrumentPriceRecord.M3.Price,
                }).ToList()
            };
        }

        public async Task<BasePriceResponse> GetPricesByAsset(BasePriceRequest request)
        {
            var entity = await _dataWriter.GetAsync(request.BrokerId, request.InstrumentId);
            return new BasePriceResponse()
            {
                InstrumentId = entity.InstrumentPriceRecord.InstrumentSymbol,
                BrokerId = entity.InstrumentPriceRecord.BrokerId,
                CurrentPrice = entity.InstrumentPriceRecord.CurrentPrice,
                H24A = entity.InstrumentPriceRecord.H24.Price,
                H24P = entity.InstrumentPriceRecord.H24P,
                D7 = entity.InstrumentPriceRecord.D7.Price,
                M1 = entity.InstrumentPriceRecord.M1.Price,
                M3 = entity.InstrumentPriceRecord.M3.Price,
            };
        }

        public async Task<BasePriceResponse> EditBasePriceRecord(BasePriceEditRequest request)
        {
            var record = new InstrumentPriceRecord()
            {
                InstrumentSymbol = request.InstrumentId,
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
            await _dataWriter.InsertOrReplaceAsync(InstrumentPriceRecordNoSqlEntity.Create(record));
            
            var entity = await _dataWriter.GetAsync(request.BrokerId, request.InstrumentId);
            return new BasePriceResponse()
            {
                InstrumentId = entity.InstrumentPriceRecord.InstrumentSymbol,
                BrokerId = entity.InstrumentPriceRecord.BrokerId,
                CurrentPrice = entity.InstrumentPriceRecord.CurrentPrice,
                H24A = entity.InstrumentPriceRecord.H24.Price,
                H24P = entity.InstrumentPriceRecord.H24P,
                D7 = entity.InstrumentPriceRecord.D7.Price,
                M1 = entity.InstrumentPriceRecord.M1.Price,
                M3 = entity.InstrumentPriceRecord.M3.Price,
            };
        }
    }
}