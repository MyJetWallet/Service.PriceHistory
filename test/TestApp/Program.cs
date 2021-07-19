using System;
using System.Threading.Tasks;
using ProtoBuf.Grpc.Client;
using Service.PriceHistory.Client;
using Service.PriceHistory.Grpc.Models;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();


            var factory = new PriceHistoryClientFactory("http://localhost:5001");
            var client = factory.GetBasePriceService();

            var resp = await  client.GetPricesByAsset(new BasePriceRequest(){BrokerId = "jetwallet", InstrumentId = "BTC"});
            Console.WriteLine(resp?.CurrentPrice);

            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}
