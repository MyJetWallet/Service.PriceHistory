using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using MyNoSqlServer.Abstractions;
using Service.AssetsDictionary.Client;
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
        private readonly ISpotInstrumentDictionaryClient _instrumentDictionaryClient;
        private readonly ILogger<PriceUpdatingJob> _logger;
        private readonly IMyNoSqlServerDataWriter<InstrumentPriceRecordNoSqlEntity> _dataWriter;
        private readonly MyTaskTimer _timer;
        private Dictionary<string, InstrumentPriceRecord> _prices = new ();
        private List<ISpotInstrument> _instruments = new ();
        private Dictionary<string, List<CandleGrpcModel>> _candles = new();

        public PriceUpdatingJob(ISimpleTradingCandlesHistoryGrpc candlesHistory, ILogger<PriceUpdatingJob> logger, IMyNoSqlServerDataWriter<InstrumentPriceRecordNoSqlEntity> dataWriter, ISpotInstrumentDictionaryClient instrumentDictionaryClient)
        {
            _candlesHistory = candlesHistory;
            _logger = logger;
            _dataWriter = dataWriter;
            _instrumentDictionaryClient = instrumentDictionaryClient;
            _timer = new MyTaskTimer(typeof(PriceUpdatingJob), TimeSpan.FromSeconds(Program.Settings.TimerPeriodInSec), _logger, DoTime);
        }

        private async Task DoTime()
        {
            await UpdateCandles();
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

                if (TryGetCandlePrice(candles.ToList(), out var price))
                {
                    _prices[instrument.Symbol].CurrentPrice = Math.Round(price, instrument.Accuracy, MidpointRounding.ToPositiveInfinity);
                    _prices[instrument.Symbol].H24P = Calculate24HPercent(_prices[instrument.Symbol]);
                }
            }
        }

        private async Task UpdateHourlyPrices()
        {
            
            foreach (var instrument in _instruments)
            {
                if (_prices[instrument.Symbol].H24.RecordTime < DateTime.UtcNow - TimeSpan.FromHours(1))
                {
                    _logger.LogInformation("Updating 24H prices for {Instrument}", instrument.Symbol);
                    if (TryGetCandlePrice(instrument.Symbol, DateTime.UtcNow - TimeSpan.FromHours(24), out var price))
                    {
                        _prices[instrument.Symbol].H24 = new BasePrice
                        {
                            Price = Math.Round(price, instrument.Accuracy, MidpointRounding.ToPositiveInfinity),
                            RecordTime = DateTime.UtcNow
                        };

                        _prices[instrument.Symbol].H24P = Calculate24HPercent(_prices[instrument.Symbol]);
                        
                        await _dataWriter.InsertOrReplaceAsync(
                            InstrumentPriceRecordNoSqlEntity.Create(_prices[instrument.Symbol]));
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
                    if (TryGetCandlePrice(instrument.Symbol, DateTime.UtcNow.AddDays(-7), out var price))
                    {
                        _prices[instrument.Symbol].D7 = new BasePrice
                        {
                            Price = Math.Round(price, instrument.Accuracy, MidpointRounding.ToPositiveInfinity),
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            InstrumentPriceRecordNoSqlEntity.Create(_prices[instrument.Symbol]));
                    }
                }
                
                if (_prices[instrument.Symbol].M1.RecordTime < DateTime.UtcNow - TimeSpan.FromDays(1))
                {
                    _logger.LogInformation("Updating M1 prices for {Instrument}", instrument.Symbol);
                    if (TryGetCandlePrice(instrument.Symbol, DateTime.UtcNow.AddMonths(-1), out var price))
                    {
                        _prices[instrument.Symbol].M1 = new BasePrice
                        {
                            Price = Math.Round(price, instrument.Accuracy, MidpointRounding.ToPositiveInfinity),
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            InstrumentPriceRecordNoSqlEntity.Create(_prices[instrument.Symbol]));
                    }
                }
                
                //if (_prices[instrument.Symbol].M3.RecordTime < DateTime.UtcNow - TimeSpan.FromDays(1))
                {
                    _logger.LogInformation("Updating M3 prices for {Instrument}", instrument.Symbol);
                    if (TryGetCandlePrice(instrument.Symbol, DateTime.UtcNow.AddMonths(-3), out var price))
                    {
                        _prices[instrument.Symbol].M3 = new BasePrice
                        {
                            Price = Math.Round(price, instrument.Accuracy, MidpointRounding.ToPositiveInfinity),
                            RecordTime = DateTime.UtcNow
                        };
                        await _dataWriter.InsertOrReplaceAsync(
                            InstrumentPriceRecordNoSqlEntity.Create(_prices[instrument.Symbol]));
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
            foreach (var instrument in _instruments)
            {
                var candles = (await _candlesHistory.GetCandlesHistoryAsync(
                    new GetCandlesHistoryGrpcRequestContract()
                    {
                        Instrument = instrument.Symbol,
                        Bid = false,
                        CandleType = CandleTypeGrpcModel.Hour,
                        From = DateTime.UtcNow - TimeSpan.FromDays(2),
                        To = DateTime.UtcNow 
                    })).OrderByDescending(e=>e.DateTime).ToList();
                
                candles.AddRange((await _candlesHistory.GetCandlesHistoryAsync(
                    new GetCandlesHistoryGrpcRequestContract()
                    {
                        Instrument = instrument.Symbol,
                        Bid = false,
                        CandleType = CandleTypeGrpcModel.Day,
                        From = DateTime.UtcNow - TimeSpan.FromDays(100),
                        To = DateTime.UtcNow - TimeSpan.FromDays(2)
                    })).OrderByDescending(e=>e.DateTime).ToList());
                _candles.Add(instrument.Symbol, candles);
            }
        }

        private bool TryGetCandlePrice(string instrument, DateTime timePoint, out decimal price)
        {
            var candles = _candles[instrument];
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
        
        private decimal Calculate24HPercent(InstrumentPriceRecord priceRecord)
        {
            if (priceRecord.CurrentPrice == 0 || priceRecord.H24.Price == 0)
                return 0;
            var percentage = ((priceRecord.CurrentPrice - priceRecord.H24.Price) / priceRecord.H24.Price) * 100;
            return Math.Round(percentage, 2, MidpointRounding.ToPositiveInfinity);
        }
        
        public async void Start()
        {
            var prices = await _dataWriter.GetAsync();
            if(prices.Any())
                _prices = prices.Select(t => t.InstrumentPriceRecord).ToDictionary(key => key.InstrumentSymbol, value => value);
            while (!_instruments.Any())
            {
                _instruments = _instrumentDictionaryClient.GetAllSpotInstruments().ToList();
                Thread.Sleep(1000);
            }
            foreach (var instrument in _instruments)
            {
                if (!_prices.TryGetValue(instrument.Symbol, out _))
                {
                    var record = new InstrumentPriceRecord()
                    {
                        InstrumentSymbol = instrument.Symbol,
                        BrokerId = instrument.BrokerId,
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
                    await _dataWriter.InsertAsync(InstrumentPriceRecordNoSqlEntity.Create(record));
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