using System;
using System.IO;
using log4net.Config;
using Newtonsoft.Json;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace WireMock.Net.StandAlone.Net452
{
    public class Program
    {
        private static int sleepTime = 30000;
        private static FluentMockServer _server;
        private static FluentMockServer _apiRecordingProxy;

        static void Main(params string[] args)
        {
            XmlConfigurator.Configure(new FileInfo("log4net.config"));

            _server = StandAloneApp.Start(args);
            //SetupRoutes();

            Console.WriteLine("Press any key to stop the server");
            Console.ReadKey();
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
            Request.Create().WithPath("/api/beta02*").UsingPost()
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