using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain;
using MyJetWallet.Sdk.Service.Tools;
using MyNoSqlServer.Abstractions;
using Service.AssetsDictionary.Client;
using Service.PriceHistory.Domain.Models;
using Service.PriceHistory.Domain.Models.NoSql;
using SimpleTrading.Abstraction.Candles;
using SimpleTrading.CandlesHistory.Grpc;
using SimpleTrading.CandlesHistory.Grpc.Contracts;
using SimpleTrading.CandlesHistory.Grpc.Models;

namespace Service.PriceHistory.Jobs
{
    public class PriceUpdatingJob : IStartable, IDisposable
    {
        private readonly ISimpleTradingCandlesHistoryGrpc _candlesHistory;
        private readonly ILogger<PriceUpdatingJob> _logger;
        private readonly IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> _dataWriter;
        private readonly IAssetsDictionaryClient _assetsDictionaryClient;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;
        private readonly MyTaskTimer _timer;
        private readonly PriceUpdatingJobV2 _priceUpdatingJobV2;
        private Dictionary<string, AssetPriceRecord> _prices = new ();
        private Dictionary<string, string> _instruments = new ();
        private Dictionary<string, List<CandleGrpcModel>> _candles = new();

        public PriceUpdatingJob(ISimpleTradingCandlesHistoryGrpc candlesHistory, 
            ILogger<PriceUpdatingJob> logger, 
            IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> dataWriter, 
            IAssetsDictionaryClient assetsDictionaryClient, 
            PriceUpdatingJobV2 priceUpdatingJobV2,
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient)
        {
            _candlesHistory = candlesHistory;
            _logger = logger;
            _dataWriter = dataWriter;
            _assetsDictionaryClient = assetsDictionaryClient;
            _priceUpdatingJobV2 = priceUpdatingJobV2;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            _timer = new MyTaskTimer(typeof(PriceUpdatingJob), TimeSpan.FromSeconds(Program.Settings.TimerPeriodInSec), _logger, DoTime);
        }

        private async Task DoTime()
        {
            await UpdateInstruments();
            await UpdateCandles();
            await UpdateCurrentPrice();
            await UpdateHistoricPrices();
            
            
            await _priceUpdatingJobV2.RefreshAssetPrices();
        }

        private async Task UpdateInstruments()
        {
            var count = _instruments.Count;

            _instruments.Clear();

            var instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrokerIdentity()
            {
                BrokerId = DomainConstants.DefaultBroker
            });

            if (!instruments.Any())
            {
                await Task.Delay(5000);
                instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrokerIdentity()
                {
                    BrokerId = DomainConstants.DefaultBroker
                });
            }

            foreach (var spotInstrument in instruments)
            {
                _instruments[spotInstrument.Symbol] = null;
            }

            if (_instruments.Count != count)
            {
                await InitPrices();
            }
        }

        private async Task UpdateCurrentPrice()
        {
            _logger.LogInformation("Updating current prices");
            foreach (var instrument in _instruments.Keys)
            {
                var candles = await _candlesHistory.GetLastCandlesAsync(new GetLastCandlesGrpcRequestContract()
                {
                    Instrument = instrument,
                    Bid = false,
                    CandleType = CandleType.Minute,
                    Amount = 10
                });

                if (TryGetCandlePrice(candles.ToList(), out var price))
                {
                    _prices[instrument].CurrentPrice = price;
                    _prices[instrument].H24P = Calculate24HPercent(_prices[instrument]);
                }
            }
        }

        private async Task UpdateHistoricPrices()
        {
            foreach (var instrument in _instruments.Keys)
            {
                if (_prices[instrument].H24.RecordTime < DateTime.UtcNow - TimeSpan.FromHours(1))
                {
                    if (TryGetCandlePrice(instrument, DateTime.UtcNow - TimeSpan.FromHours(24), out var price))
                    {
                        _prices[instrument].H24 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };

                        _prices[instrument].H24P = Calculate24HPercent(_prices[instrument]);

                        await _dataWriter.InsertOrReplaceAsync(AssetPriceRecordNoSqlEntity.Create(_prices[instrument]));

                        _logger.LogInformation("Updated 24H prices for {Instrument}. Price: {price}", instrument, price);
                    }
                }

                if (_prices[instrument].D7.RecordTime < DateTime.UtcNow - TimeSpan.FromHours(1))
                {
                    if (TryGetCandlePrice(instrument, DateTime.UtcNow.AddDays(-7), out var price))
                    {
                        _prices[instrument].D7 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(AssetPriceRecordNoSqlEntity.Create(_prices[instrument]));

                        _logger.LogInformation("Updated D7 prices for {Instrument}. Price: {price}", instrument, price);
                    }
                }
                
                if (_prices[instrument].M1.RecordTime < DateTime.UtcNow - TimeSpan.FromHours(1))
                {
                    if (TryGetCandlePrice(instrument, DateTime.UtcNow.AddMonths(-1), out var price))
                    {
                        _prices[instrument].M1 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(AssetPriceRecordNoSqlEntity.Create(_prices[instrument]));

                        _logger.LogInformation("Updated M1 prices for {Instrument}. Price: {price}", instrument, price);
                    }
                }
                
                if (_prices[instrument].M3.RecordTime < DateTime.UtcNow - TimeSpan.FromHours(1))
                {
                    if (TryGetCandlePrice(instrument, DateTime.UtcNow.AddMonths(-3), out var price))
                    {
                        _prices[instrument].M3 = new BasePrice
                        {
                            Price = price,
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(AssetPriceRecordNoSqlEntity.Create(_prices[instrument]));

                        _logger.LogInformation("Updated M3 prices for {Instrument}. Price: {price}", instrument, price);
                    }
                }
            }
        }

        private bool TryGetCandlePrice(List<CandleGrpcModel> candles, out decimal price)
        {
            price = 0;
            candles = candles.OrderByDescending(t => t.DateTime).ToList();
            if (candles.FirstOrDefault() == null)
                return false; 
            if (candles.First().Open == 0)
            {
                foreach (var candle in candles.Where(candle => candle.Close != 0))
                {
                    price = (decimal)candle.Close;
                    return true;
                }
            }

            price = (decimal)candles.First().Open;
            return true;
        }

        private async Task UpdateCandles()
        {
            _candles = new Dictionary<string, List<CandleGrpcModel>>();

            foreach (var instrument in _instruments.Keys)
            {
                

                var candles = (await _candlesHistory.GetCandlesHistoryAsync(
                    new GetCandlesHistoryGrpcRequestContract()
                    {
                        Instrument = instrument,
                        Bid = true,
                        CandleType = CandleType.Hour,
                        From = DateTime.UtcNow - TimeSpan.FromDays(2),
                        To = DateTime.UtcNow 
                    })).OrderByDescending(e=>e.DateTime).ToList();
                
                candles.AddRange((await _candlesHistory.GetCandlesHistoryAsync(
                    new GetCandlesHistoryGrpcRequestContract()
                    {
                        Instrument = instrument,
                        Bid = true,
                        CandleType = CandleType.Day,
                        From = DateTime.UtcNow - TimeSpan.FromDays(100),
                        To = DateTime.UtcNow - TimeSpan.FromDays(2)
                    })).OrderByDescending(e=>e.DateTime).ToList());
                _candles.Add(instrument, candles);
            }
        }

        private bool TryGetCandlePrice(string instrument, DateTime timePoint, out decimal price)
        {
            var candles = _candles[instrument].Where(t=>t.Open != 0 && t.Close != 0).ToList();


            var candle = candles.LastOrDefault(t => t.DateTime >= timePoint);
            if (candle != null)
            {
                price = (decimal)candle.Close;
                return true;
            }
            
            candle = candles.FirstOrDefault();
            if (candle != null)
            {
                price = (decimal)candle.Close;
                return true;
            }
            
            price = 0;
            return false;
        }
        
        private decimal Calculate24HPercent(AssetPriceRecord priceRecord)
        {
            if (priceRecord.CurrentPrice == 0 || priceRecord.H24.Price == 0)
                return 0;
            var percentage = ((priceRecord.CurrentPrice - priceRecord.H24.Price) / priceRecord.H24.Price) * 100;
            return Math.Round(percentage, 2);
        }
        
        private async Task InitPrices()
        {
            var prices = (await _dataWriter.GetAsync()).ToList();
            
            if (prices.Any())
            {
                _prices = prices.Select(t => t.AssetPriceRecord)
                    .ToDictionary(key => key.AssetSymbol, value => value);
            }

            foreach (var instrument in _instruments)
            {
                if (!_prices.TryGetValue(instrument.Key, out var price))
                {
                    price = new AssetPriceRecord()
                    {
                        AssetSymbol = instrument.Key,
                        BrokerId = DomainConstants.DefaultBroker,
                        H24P = 0,
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
                    await _dataWriter.InsertAsync(AssetPriceRecordNoSqlEntity.Create(price));

                    _prices.Add(instrument.Key, price);
                }

                price.H24.RecordTime = DateTime.MinValue;
                price.D7.RecordTime = DateTime.MinValue;
                price.M1.RecordTime = DateTime.MinValue;
                price.M3.RecordTime = DateTime.MinValue;
            }
        }
        
        public async void Start()
        {
            await UpdateInstruments();
            await InitPrices();
            _timer.Start();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}