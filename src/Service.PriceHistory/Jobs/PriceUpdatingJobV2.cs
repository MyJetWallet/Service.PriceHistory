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
using Service.AssetsDictionary.Domain.Models;
using Service.BaseCurrencyConverter.Grpc;
using Service.BaseCurrencyConverter.Grpc.Models;
using Service.PriceHistory.Domain.Models;
using Service.PriceHistory.Domain.Models.NoSql;

namespace Service.PriceHistory.Jobs
{
    public class PriceUpdatingJobV2 : IStartable, IDisposable
    {
        private readonly ILogger<PriceUpdatingJobV2> _logger;
        private readonly MyTaskTimer _timer;
        private readonly IBaseCurrencyConverterService _baseCurrencyConverterService;
        private readonly IAssetsDictionaryClient _assetsDictionaryClient;
        private readonly IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> _assetPriceRecordDataWriter;
        private readonly IMyNoSqlServerDataWriter<AssetPricesNoSqlEntity> _assetPricesDataWriter;

        private List<IAsset> _assets = new();

        public PriceUpdatingJobV2(ILogger<PriceUpdatingJobV2> logger,
            IBaseCurrencyConverterService baseCurrencyConverterService,
            IAssetsDictionaryClient assetsDictionaryClient,
            IMyNoSqlServerDataWriter<AssetPriceRecordNoSqlEntity> assetPriceRecordDataWriter,
            IMyNoSqlServerDataWriter<AssetPricesNoSqlEntity> assetPricesDataWriter)
        {
            _logger = logger;
            _baseCurrencyConverterService = baseCurrencyConverterService;
            _assetsDictionaryClient = assetsDictionaryClient;
            _assetPriceRecordDataWriter = assetPriceRecordDataWriter;
            _assetPricesDataWriter = assetPricesDataWriter;
            _timer = new MyTaskTimer(typeof(PriceUpdatingJobV2),
                TimeSpan.FromSeconds(Program.Settings.TimerPeriodInSec), _logger, DoTime);
        }

        private async Task DoTime()
        {
            await RefreshAssetPrices();
        }

        public async Task RefreshAssetPrices()
        {
            try
            {
                await UpdateAssets();
                var pricesByOperationSymbols = (await _assetPriceRecordDataWriter.GetAsync()).ToList();
                
                foreach (var baseAsset in _assets)
                {
                    var assetPrices = new AssetPrices()
                    {
                        BaseAsset = baseAsset.Symbol,
                        BrokerId = DomainConstants.DefaultBroker,
                        CalculatingTime = DateTime.UtcNow,
                        PricesByQuoteAsset = new List<AssetPrice>()
                    };
                    var convertMap = await _baseCurrencyConverterService.GetConvertorMapToBaseCurrencyAsync(new GetConvertorMapToBaseCurrencyRequest()
                    {
                        BaseAsset = baseAsset.Symbol,
                        BrokerId = DomainConstants.DefaultBroker
                    });
                    foreach (var quoteAsset in _assets)
                    {
                        var startValue = 1m;
                        var priceByQuoteAsset = new AssetPrice()
                        {
                            Asset = quoteAsset.Symbol,
                            CurrentPrice = startValue,
                            H24 = startValue,
                            D7 = startValue,
                            M1 = startValue,
                            M3 = startValue
                        };
                        if (baseAsset == quoteAsset)
                        {
                            var pricesByOperationSymbol = pricesByOperationSymbols.FirstOrDefault(e =>
                                e.AssetPriceRecord.AssetSymbol == quoteAsset.Symbol)?.AssetPriceRecord;
                            
                            priceByQuoteAsset.CurrentPrice = pricesByOperationSymbol.CurrentPrice;
                            priceByQuoteAsset.H24 = pricesByOperationSymbol.H24.Price;
                            priceByQuoteAsset.D7 = pricesByOperationSymbol.D7.Price;
                            priceByQuoteAsset.M1*= pricesByOperationSymbol.M1.Price;
                            priceByQuoteAsset.M3 *= pricesByOperationSymbol.M3.Price;
                        }
                        else
                        {
                            var map = convertMap.Maps.FirstOrDefault(e => e.AssetSymbol == quoteAsset.Symbol);
                            if (map != null)
                            {
                                foreach (var operation in map.Operations)
                                {
                                    var pricesByOperationSymbol = pricesByOperationSymbols.FirstOrDefault(e =>
                                        e.AssetPriceRecord.AssetSymbol == operation.InstrumentPrice)?.AssetPriceRecord;
                                
                                    if (pricesByOperationSymbol != null)
                                    {
                                        if (operation.IsMultiply)
                                        {
                                            priceByQuoteAsset.CurrentPrice *= pricesByOperationSymbol.CurrentPrice;
                                            priceByQuoteAsset.H24 *= pricesByOperationSymbol.H24.Price;
                                            priceByQuoteAsset.D7 *= pricesByOperationSymbol.D7.Price;
                                            priceByQuoteAsset.M1 *= pricesByOperationSymbol.M1.Price;
                                            priceByQuoteAsset.M3 *= pricesByOperationSymbol.M3.Price;
                                        }
                                        else
                                        {
                                            priceByQuoteAsset.CurrentPrice /= pricesByOperationSymbol.CurrentPrice;
                                            priceByQuoteAsset.H24 /= pricesByOperationSymbol.H24.Price;
                                            priceByQuoteAsset.D7 /= pricesByOperationSymbol.D7.Price;
                                            priceByQuoteAsset.M1 /= pricesByOperationSymbol.M1.Price;
                                            priceByQuoteAsset.M3 /= pricesByOperationSymbol.M3.Price;
                                        }
                                    }
                                }
                            }
                        }
                        RoundPrices(priceByQuoteAsset, baseAsset.Accuracy);
                        assetPrices.PricesByQuoteAsset.Add(priceByQuoteAsset);
                    }
                    await _assetPricesDataWriter.InsertOrReplaceAsync(AssetPricesNoSqlEntity.Create(assetPrices));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void RoundPrices(AssetPrice priceByQuoteAsset, int baseAssetAccuracy)
        {
            priceByQuoteAsset.CurrentPrice = Math.Round(priceByQuoteAsset.CurrentPrice, baseAssetAccuracy);
            priceByQuoteAsset.H24 = Math.Round(priceByQuoteAsset.H24, baseAssetAccuracy);
            priceByQuoteAsset.D7 = Math.Round(priceByQuoteAsset.D7, baseAssetAccuracy);
            priceByQuoteAsset.M1 = Math.Round(priceByQuoteAsset.M1, baseAssetAccuracy);
            priceByQuoteAsset.M3 = Math.Round(priceByQuoteAsset.M3, baseAssetAccuracy);
        }

        private async Task UpdateAssets()
        {
            var assetList = _assetsDictionaryClient.GetAllAssets().Where(e => e.IsEnabled).ToList();

            if (!assetList.Any())
            {
                await Task.Delay(5000);
                assetList = _assetsDictionaryClient.GetAllAssets().Where(e => e.IsEnabled).ToList();
            }
            _assets.Clear();
            _assets = assetList;
        }

        public void Start()
        {
            //_timer.Start();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}