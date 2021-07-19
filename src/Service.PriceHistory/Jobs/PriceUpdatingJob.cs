using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using MyNoSqlServer.Abstractions;
using Service.AssetsDictionary.Domain.Models;
using Service.AssetsDictionary.Grpc;
using Service.PriceHistory.Domain.Models;
using Service.PriceHistory.Domain.NoSql;
using SimpleTrading.CandlesHistory.Grpc;
using SimpleTrading.CandlesHistory.Grpc.Contracts;
using SimpleTrading.CandlesHistory.Grpc.Models;

namespace Service.PriceHistory.Jobs
{
    public class PriceUpdatingJob : IStartable, IDisposable
    {
        private readonly ISimpleTradingCandlesHistoryGrpc _candlesHistory;
        private readonly ISpotInstrumentsDictionaryService _instrumentsDictionaryService;
        private readonly ILogger<PriceUpdatingJob> _logger;
        private readonly IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> _dataWriter;
        private readonly MyTaskTimer _timer;
        private Dictionary<string, AssetPriceRecord> _prices = new ();
        private List<SpotInstrument> _instruments = new ();

        public PriceUpdatingJob(ISimpleTradingCandlesHistoryGrpc candlesHistory, ILogger<PriceUpdatingJob> logger, ISpotInstrumentsDictionaryService instrumentsDictionaryService, IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> dataWriter)
        {
            _candlesHistory = candlesHistory;
            _logger = logger;
            _instrumentsDictionaryService = instrumentsDictionaryService;
            _dataWriter = dataWriter;
            _timer = new MyTaskTimer(typeof(PriceUpdatingJob), TimeSpan.FromSeconds(Program.Settings.TimerPeriodInSec), _logger, DoTime);
        }

        private async Task DoTime()
        {
            await UpdateCurrentPrice();
            await UpdateHourlyPrices();
            await UpdateDailyPrices();
        }

        private async Task UpdateCurrentPrice()
        {
            _logger.LogInformation("Updating current prices");
            foreach (var instrument in _instruments)
            {
                var candles = await _candlesHistory.GetLastCandlesAsync(new GetLastCandlesGrpcRequestContract()
                {
                    Instrument = instrument.Symbol,
                    Bid = false,
                    CandleType = CandleTypeGrpcModel.Minute,
                    Amount = 10
                });
                
                if(TryGetCandlePrice(candles.ToList(), out var price))
                    _prices[instrument.Symbol].CurrentPrice = price;
            }
        }

        private async Task UpdateHourlyPrices()
        {
            
            foreach (var instrument in _instruments)
            {
                if (_prices[instrument.Symbol].H24.RecordTime < DateTime.UtcNow - TimeSpan.FromHours(1))
                {
                    _logger.LogInformation("Updating 24H prices for {Instrument}", instrument.Symbol);
                    var candles = await _candlesHistory.GetCandlesHistoryAsync(
                        new GetCandlesHistoryGrpcRequestContract()
                        {
                            Instrument = instrument.Symbol,
                            Bid = false,
                            CandleType = CandleTypeGrpcModel.Hour,
                            From = DateTime.UtcNow - TimeSpan.FromDays(2),
                            To = DateTime.UtcNow - TimeSpan.FromDays(1)
                        });

                    if (TryGetCandlePrice(candles.ToList(), out var price)){
                        _prices[instrument.Symbol].H24 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };                        
                        
                        await _dataWriter.InsertOrReplaceAsync(
                            AssetPriceRecordNoSqlEntity.Create(_prices[instrument.Symbol]));
                    }
                }
            }
        }
    
        
        private async Task UpdateDailyPrices()
        {
            foreach (var instrument in _instruments)
            {
                if (_prices[instrument.Symbol].D7.RecordTime < DateTime.UtcNow - TimeSpan.FromDays(1))
                {
                    _logger.LogInformation("Updating D7 prices for {Instrument}", instrument.Symbol);
                    var candles = await _candlesHistory.GetCandlesHistoryAsync(
                        new GetCandlesHistoryGrpcRequestContract()
                        {
                            Instrument = instrument.Symbol,
                            Bid = false,
                            CandleType = CandleTypeGrpcModel.Day,
                            From = DateTime.UtcNow - TimeSpan.FromDays(14),
                            To = DateTime.UtcNow - TimeSpan.FromDays(7)
                        });

                    if (TryGetCandlePrice(candles.ToList(), out var price))
                    {
                        _prices[instrument.Symbol].D7 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            AssetPriceRecordNoSqlEntity.Create(_prices[instrument.Symbol]));
                    }
                }
                
                if (_prices[instrument.Symbol].M1.RecordTime < DateTime.UtcNow - TimeSpan.FromDays(1))
                {
                    _logger.LogInformation("Updating M1 prices for {Instrument}", instrument.Symbol);
                    var candles = await _candlesHistory.GetCandlesHistoryAsync(
                        new GetCandlesHistoryGrpcRequestContract()
                        {
                            Instrument = instrument.Symbol,
                            Bid = false,
                            CandleType = CandleTypeGrpcModel.Day,
                            From = DateTime.UtcNow - TimeSpan.FromDays(45),
                            To = DateTime.UtcNow - TimeSpan.FromDays(28)
                        });

                    if (TryGetCandlePrice(candles.ToList(), out var price))
                    {
                        _prices[instrument.Symbol].M1 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            AssetPriceRecordNoSqlEntity.Create(_prices[instrument.Symbol]));
                    }
                }
                
                if (_prices[instrument.Symbol].M3.RecordTime < DateTime.UtcNow - TimeSpan.FromDays(1))
                {
                    _logger.LogInformation("Updating M3 prices for {Instrument}", instrument.Symbol);
                    var candles = await _candlesHistory.GetCandlesHistoryAsync(
                        new GetCandlesHistoryGrpcRequestContract()
                        {
                            Instrument = instrument.Symbol,
                            Bid = false,
                            CandleType = CandleTypeGrpcModel.Day,
                            From = DateTime.UtcNow - TimeSpan.FromDays(100),
                            To = DateTime.UtcNow - TimeSpan.FromDays(88)
                        });

                    if (TryGetCandlePrice(candles.ToList(), out var price))
                    {
                        _prices[instrument.Symbol].M3 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            AssetPriceRecordNoSqlEntity.Create(_prices[instrument.Symbol]));
                    }
                }
            }
        }

        private bool TryGetCandlePrice(List<CandleGrpcModel> candles, out double price)
        {
            price = 0;
            candles = candles.OrderByDescending(t => t.DateTime).ToList();
            if (candles.FirstOrDefault() == null)
                return false; 
            if (candles.First().Open == 0)
            {
                foreach (var candle in candles.Where(candle => candle.Close != 0))
                {
                    price = candle.Close;
                    return true;
                }
            }

            price = candles.First().Open;
            return true;
        }
        
        public async void Start()
        {
            var prices = await _dataWriter.GetAsync();
            _prices = prices.Select(t => t.AssetPriceRecord).ToDictionary(key => key.InstrumentSymbol, value => value);
            _instruments = (await _instrumentsDictionaryService.GetAllSpotInstrumentsAsync()).SpotInstruments.ToList();
            foreach (var instrument in _instruments)
            {
                if (!_prices.TryGetValue(instrument.Symbol, out _))
                {
                    var record = new AssetPriceRecord()
                    {
                        InstrumentSymbol = instrument.Symbol,
                        BrokerId = instrument.BrokerId,
                        H24 = new BasePrice()
                        {
                            Price = 0,
                            RecordTime = DateTime.MinValue
                        },
                        D7 = new BasePrice()
                        {
                            Price = 0,
                            RecordTime = DateTime.MinValue
                        },
                        M1 = new BasePrice()
                        {
                            Price = 0,
                            RecordTime = DateTime.MinValue
                        },
                        M3 = new BasePrice()
                        {
                            Price = 0,
                            RecordTime = DateTime.MinValue
                        }
                    };
                    await _dataWriter.InsertAsync(AssetPriceRecordNoSqlEntity.Create(record));
                    _prices.Add(instrument.Symbol, record);
                }
            }

            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}