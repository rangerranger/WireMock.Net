using System;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Config;
using log4net.Repository;
using Newtonsoft.Json;
using WireMock.Logging;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace WireMock.Net.StandAlone.NETCoreApp
{
    static class Program
    {
        private static readonly ILoggerRepository LogRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        // private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        private static int sleepTime = 30000;
        private static FluentMockServer _server;
        private static FluentMockServer _apiRecordingProxy;
        static void Main(string[] args)
        {
            XmlConfigurator.Configure(LogRepository, new FileInfo("log4net.config"));

            //_server = StandAloneApp.Start(args, new WireMockLog4NetLogger());

            _server = StandAloneApp.Start(args, new WireMockConsoleLogger());

            SetupRoutes();

            //SetupProxy();

            Console.WriteLine($"{DateTime.UtcNow} Press Ctrl+C to shut down");

            Console.CancelKeyPress += (s, e) =>
            {
                Stop("CancelKeyPress");
            };

            System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += ctx =>
            {
                Stop("AssemblyLoadContext.Default.Unloading");
            };

            while (true)
            {
                Console.WriteLine($"{DateTime.UtcNow} WireMock.Net server running : {_server.IsStarted}");
                Thread.Sleep(sleepTime);
            }
        }

        private static void Stop(string why)
        {
            Console.WriteLine($"{DateTime.UtcNow} WireMock.Net server stopping because '{why}'");
            _server.Stop();
            Console.WriteLine($"{DateTime.UtcNow} WireMock.Net server stopped");
        }

        private static void SetupRoutes()
        {
            /*
            _server.Given(
            Request.Create().WithPath("/api/beta02").UsingPost()
                .WithBody(new JsonExactMatcher("{Scope:[{Sources:[{Shard:\"beta02\"}]}]}"))
                .WithHeader("Prefer", "exchange.behavior=\"CustomSearchAction,CustomSearchActionNewSchemas,OpenComplexTypeExtensions\"", true)
                .WithHeader("Content-Type", "application/json", true)
                .WithHeader("Host", "localhost:9091")
                .WithHeader("Content-Length","118")
            ).RespondWith(Response.Create()
                                    .WithBodyAsJson(JsonConvert.DeserializeObject("{    Id: \"deadbeef-0002-494e-af71-b5e1f217b685\",    SessionId: \"315ab117-b311-494e-af71-b5e1f217b685\"}"),true)
                                    .WithHeader("Content-Type", "application/json")
                         );
                         */

            _server.Given(
            Request.Create().WithPath("/api/beta02").UsingPost()
                .WithBody(new JsonExactMatcher("{Scope:[{Sources:[{Shard:\"beta02\"}]}]}"))
            ).RespondWith(Response.Create()
                                    .WithBodyAsJson(JsonConvert.DeserializeObject("{    Id: \"deadbeef-0002-494e-af71-b5e1f217b685\",    SessionId: \"315ab117-b311-494e-af71-b5e1f217b685\"}"), true)
                                    .WithHeader("Content-Type", "application/json")
                         );

            _server.Given(
            Request.Create().WithPath("/api/beta03").UsingPost()
                .WithBody(new JsonPathMatcher(@"{Scope:[{Sources:[{Shard:""beta03""}]}]}"))
            ).RespondWith(Response.Create()
                                    .WithBodyAsJson(JsonConvert.DeserializeObject("{    Id: \"deadbeef-0003-494e-af71-b5e1f217b685\",    SessionId: \"315ab117-b311-494e-af71-b5e1f217b685\"}"), true)
                                    .WithHeader("Content-Type", "application/json")
                         );

            _server.Given(
            Request.Create().WithPath("/api/beta04").UsingPost()
                .WithBody(new JsonExactMatcher(@"{Scope:[{Sources:[{Shard:""beta04""}]}]}"))
            ).RespondWith(Response.Create()
                                    .WithBodyAsJson(JsonConvert.DeserializeObject("{    Id: \"deadbeef-0004-494e-af71-b5e1f217b685\",    SessionId: \"315ab117-b311-494e-af71-b5e1f217b685\"}"), true)
                                    .WithHeader("Content-Type", "application/json")
                         );

        }

        private static void SetupProxy()
        {
             _apiRecordingProxy = FluentMockServer.Start(new FluentMockServerSettings
            {
                Urls = new[] { "http://+:5002" }, //different port
                ProxyAndRecordSettings = new ProxyAndRecordSettings()
                {
                    Url = "https://outlook.office.com/",
                    SaveMapping = true,
                    SaveMappingToFile = true
                }
            });
        }
    }
}