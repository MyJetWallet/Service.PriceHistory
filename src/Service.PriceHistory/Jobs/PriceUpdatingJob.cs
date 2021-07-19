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
        private readonly IAssetsDictionaryService _assetsDictionaryService;
        private readonly ILogger<PriceUpdatingJob> _logger;
        private readonly IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> _dataWriter;
        private readonly MyTaskTimer _timer;
        private Dictionary<string, AssetPriceRecord> _prices = new ();
        private List<Asset> _assets = new ();

        public PriceUpdatingJob(ISimpleTradingCandlesHistoryGrpc candlesHistory, ILogger<PriceUpdatingJob> logger, IAssetsDictionaryService assetsDictionaryService, IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> dataWriter)
        {
            _candlesHistory = candlesHistory;
            _logger = logger;
            _assetsDictionaryService = assetsDictionaryService;
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
            foreach (var asset in _assets)
            {
                var candles = await _candlesHistory.GetLastCandlesAsync(new GetLastCandlesGrpcRequestContract()
                {
                    Instrument = $"{asset.Symbol}USD",
                    Bid = false,
                    CandleType = CandleTypeGrpcModel.Minute,
                    Amount = 10
                });
                
                if(TryGetCandlePrice(candles.ToList(), out var price))
                    _prices[asset.Symbol].CurrentPrice = price;
            }
        }

        private async Task UpdateHourlyPrices()
        {
            _logger.LogInformation("Updating hour prices");
            foreach (var asset in _assets)
            {
                if (_prices[asset.Symbol].H24.RecordTime < DateTime.UtcNow - TimeSpan.FromHours(1))
                {
                    var candles = await _candlesHistory.GetCandlesHistoryAsync(
                        new GetCandlesHistoryGrpcRequestContract()
                        {
                            Instrument = $"{asset.Symbol}USD",
                            Bid = false,
                            CandleType = CandleTypeGrpcModel.Hour,
                            From = DateTime.UtcNow - TimeSpan.FromDays(2),
                            To = DateTime.UtcNow - TimeSpan.FromDays(1)
                        });

                    if (TryGetCandlePrice(candles.ToList(), out var price)){
                        _prices[asset.Symbol].H24 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };                        
                        
                        await _dataWriter.InsertOrReplaceAsync(
                            AssetPriceRecordNoSqlEntity.Create(_prices[asset.Symbol]));
                    }
                }
            }
        }
    
        
        private async Task UpdateDailyPrices()
        {
            _logger.LogInformation("Updating daily prices");
            foreach (var asset in _assets)
            {
                if (_prices[asset.Symbol].D7.RecordTime < DateTime.UtcNow - TimeSpan.FromDays(1))
                {
                    var candles = await _candlesHistory.GetCandlesHistoryAsync(
                        new GetCandlesHistoryGrpcRequestContract()
                        {
                            Instrument = $"{asset.Symbol}USD",
                            Bid = false,
                            CandleType = CandleTypeGrpcModel.Day,
                            From = DateTime.UtcNow - TimeSpan.FromDays(14),
                            To = DateTime.UtcNow - TimeSpan.FromDays(7)
                        });

                    if (TryGetCandlePrice(candles.ToList(), out var price))
                    {
                        _prices[asset.Symbol].D7 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            AssetPriceRecordNoSqlEntity.Create(_prices[asset.Symbol]));
                    }
                }
                
                if (_prices[asset.Symbol].M1.RecordTime < DateTime.UtcNow - TimeSpan.FromDays(1))
                {
                    var candles = await _candlesHistory.GetCandlesHistoryAsync(
                        new GetCandlesHistoryGrpcRequestContract()
                        {
                            Instrument = $"{asset.Symbol}USD",
                            Bid = false,
                            CandleType = CandleTypeGrpcModel.Day,
                            From = DateTime.UtcNow - TimeSpan.FromDays(45),
                            To = DateTime.UtcNow - TimeSpan.FromDays(28)
                        });

                    if (TryGetCandlePrice(candles.ToList(), out var price))
                    {
                        _prices[asset.Symbol].M1 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            AssetPriceRecordNoSqlEntity.Create(_prices[asset.Symbol]));
                    }
                }
                
                if (_prices[asset.Symbol].M3.RecordTime < DateTime.UtcNow - TimeSpan.FromDays(1))
                {
                    var candles = await _candlesHistory.GetCandlesHistoryAsync(
                        new GetCandlesHistoryGrpcRequestContract()
                        {
                            Instrument = $"{asset.Symbol}USD",
                            Bid = false,
                            CandleType = CandleTypeGrpcModel.Day,
                            From = DateTime.UtcNow - TimeSpan.FromDays(100),
                            To = DateTime.UtcNow - TimeSpan.FromDays(88)
                        });

                    if (TryGetCandlePrice(candles.ToList(), out var price))
                    {
                        _prices[asset.Symbol].M3 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            AssetPriceRecordNoSqlEntity.Create(_prices[asset.Symbol]));
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
            _prices = prices.Select(t => t.AssetPriceRecord).ToDictionary(key => key.AssetSymbol, value => value);
            _assets = (await _assetsDictionaryService.GetAllAssetsAsync()).Assets.ToList();
            foreach (var asset in _assets)
            {
                if (!_prices.TryGetValue(asset.Symbol, out _))
                {
                    var record = new AssetPriceRecord()
                    {
                        AssetSymbol = asset.Symbol,
                        BrokerId = asset.BrokerId,
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
                    _prices.Add(asset.Symbol, record);
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