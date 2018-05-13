using System;
using System.Threading.Tasks;
using WireMock.Logging;
using WireMock.Matchers.Request;
using System.Linq;
using WireMock.Matchers;
using WireMock.Util;
using Newtonsoft.Json;
using WireMock.Http;
using System.IO;
using Newtonsoft.Json.Linq;
#if !NETSTANDARD
using Microsoft.Owin;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace WireMock.Owin
{
#if !NETSTANDARD
    internal class WireMockMiddleware : OwinMiddleware
#else
    internal class WireMockMiddleware
#endif
    {
        private static readonly Task CompletedTask = Task.FromResult(false);
        private readonly WireMockMiddlewareOptions _options;

        private readonly OwinRequestMapper _requestMapper = new OwinRequestMapper();
        private readonly OwinResponseMapper _responseMapper = new OwinResponseMapper();

        private System.IO.StreamWriter _logTrafficStream;
        private Object _lockTrafficStream = new Object();

#if !NETSTANDARD
        public WireMockMiddleware(OwinMiddleware next, WireMockMiddlewareOptions options) : base(next)
        {
            _options = options;
            _logTrafficStream = null;
            if(_options.TrafficLogFile != null) {
                if(!Directory.Exists(Path.GetDirectoryName(_options.TrafficLogFile)))
                {
                    _options.Logger.Error("TrafficLogFile Folder - {0} - does NOT exist. Not logging traffic to {1}.", Path.GetDirectoryName(_options.TrafficLogFile), _options.TrafficLogFile);
                } else
                {
                    try
                    {
                        _logTrafficStream = File.AppendText(_options.TrafficLogFile);
                    } 
                    catch(Exception e)
                    {
                        _options.Logger.Error("Cannot create or access TrafficLogFile Path - {0} - does NOT exist. Not logging traffic. Exception: {1}", _options.TrafficLogFile, e.Message);
                    }
                }

            }
        }
#else
        public WireMockMiddleware(RequestDelegate next, WireMockMiddlewareOptions options)
        {
            _options = options;
            _logTrafficStream = null;
            if(_options.TrafficLogFile != null) {
                if(!Directory.Exists(Path.GetDirectoryName(_options.TrafficLogFile)))
                {
                    _options.Logger.Error("TrafficLogFile Folder - {0} - does NOT exist. Not logging traffic to {1}.", Path.GetDirectoryName(_options.TrafficLogFile), _options.TrafficLogFile);
                } else
                {
                    try
                    {
                        _logTrafficStream = File.AppendText(_options.TrafficLogFile);
                    } 
                    catch(Exception e)
                    {
                        _options.Logger.Error("Cannot create or access TrafficLogFile Path - {0} - does NOT exist. Not logging traffic. Exception: {1}", _options.TrafficLogFile, e.Message);
                    }
                }

            }
        }
#endif

#if !NETSTANDARD
        public override async Task Invoke(IOwinContext ctx)
#else
        public async Task Invoke(HttpContext ctx)
#endif
        {
            _options.Logger.Debug("New Task Invoke with Request: '{0}'", JsonConvert.SerializeObject(new { Headers = ctx.Request.Headers, Body = "Body not logged here."}));
            
            var request = await _requestMapper.MapAsync(ctx.Request);

            var logRequestStr = JsonConvert.SerializeObject(request);

            _options.Logger.Debug("Start Finding matching mapping for Request: '{0}'", logRequestStr);

            bool logRequest = false;
            ResponseMessage response = null;
            Mapping targetMapping = null;
            RequestMatchResult requestMatchResult = null;
            System.Collections.Generic.Dictionary<Mapping, RequestMatchResult> log_mappings = new System.Collections.Generic.Dictionary<Mapping, RequestMatchResult>();
            try
            {
                _options.Logger.Debug("Start Scenario mappings for Request: '{0}'", logRequestStr);
                foreach (var mapping in _options.Mappings.Values.Where(m => m?.Scenario != null))
                {   
                    // Set start
                    if (!_options.Scenarios.ContainsKey(mapping.Scenario) && mapping.IsStartState)
                    {
                        _options.Scenarios.Add(mapping.Scenario, null);
                    }
                }
                _options.Logger.Debug("Done Getting Scenario mappings for Request: '{0}'", logRequestStr);

                _options.Logger.Debug("Start iterating mapping matchings for Request: '{0}'", logRequestStr);
                var mappings = _options.Mappings.Values
                    .Select(m => new
                    {
                        Mapping = m,
                        MatchResult = m.GetRequestMatchResult(request, m.Scenario != null && _options.Scenarios.ContainsKey(m.Scenario) ? _options.Scenarios[m.Scenario] : null)
                    })
                    .ToList();
                _options.Logger.Debug("Done iterating mapping matchings for Request: '{0}'", logRequestStr);

                foreach (var mapping in mappings) {
                    if(mapping.MatchResult.LogThis)
                    {
                        log_mappings.Add(mapping.Mapping, mapping.MatchResult);
                    }
                }

                if (_options.AllowPartialMapping)
                {
                    _options.Logger.Debug("Start AllowPartialMapping steps for Request: '{0}'", logRequestStr);
                    var partialMappings = mappings
                        .Where(pm => pm.Mapping.IsAdminInterface && pm.MatchResult.IsPerfectMatch || !pm.Mapping.IsAdminInterface)
                        .OrderBy(m => m.MatchResult)
                        .ThenBy(m => m.Mapping.Priority)
                        .ToList();

                    var bestPartialMatch = partialMappings.FirstOrDefault(pm => pm.MatchResult.AverageTotalScore > 0.0);

                    targetMapping = bestPartialMatch?.Mapping;
                    requestMatchResult = bestPartialMatch?.MatchResult;
                    _options.Logger.Info("Got PartialMapping steps for Request: '{0}', Mapping: '{1}'", logRequestStr, JsonConvert.SerializeObject(requestMatchResult));
                    _options.Logger.Debug("Done AllowPartialMapping steps for Request: '{0}'", logRequestStr);
                }
                else
                {
                    _options.Logger.Debug("Start Perfect mapping steps for Request: '{0}'", logRequestStr);

                    var perfectMatch = mappings
                        .OrderBy(m => m.Mapping.Priority)
                        .FirstOrDefault(m => m.MatchResult.IsPerfectMatch);

                    targetMapping = perfectMatch?.Mapping;
                    requestMatchResult = perfectMatch?.MatchResult;

                    _options.Logger.Debug("Done Perfect mapping steps for Request: '{0}'", logRequestStr);
                }

                if (targetMapping == null)
                {
                    logRequest = true;
                    _options.Logger.Warn("HttpStatusCode set to 404 : No matching mapping found for request: '{0}'", logRequestStr);
                    response = new ResponseMessage { StatusCode = 404, Body = JsonConvert.SerializeObject(new {Error = "No matching mapping found", RequestMessage = request, LoggedMapFailures = log_mappings}) };
                    response.AddHeader("Content-Type", "application/json");
                    return;
                }

                logRequest = !targetMapping.IsAdminInterface;

                if (targetMapping.IsAdminInterface && _options.AuthorizationMatcher != null)
                {
                    bool present = request.Headers.TryGetValue(HttpKnownHeaderNames.Authorization, out WireMockList<string> authorization);
                    if (!present || _options.AuthorizationMatcher.IsMatch(authorization.ToString()) < MatchScores.Perfect)
                    {
                        _options.Logger.Error("HttpStatusCode set to 401 : For request: '{0}'", logRequestStr);
                        response = new ResponseMessage { StatusCode = 401 };
                        return;
                    }
                }

                if (!targetMapping.IsAdminInterface && _options.RequestProcessingDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_options.RequestProcessingDelay.Value);
                }

                response = await targetMapping.ResponseToAsync(request);

                if (targetMapping.Scenario != null)
                {
                    _options.Scenarios[targetMapping.Scenario] = targetMapping.NextState;
                }

                if (targetMapping != null)
                {
                    _options.Logger.Info("Matching mapping found for Request: '{0}', Mapping: '{1}'", logRequestStr, JsonConvert.SerializeObject(targetMapping));
                }

                _options.Logger.Debug("Done Finding matching mapping for Request: '{0}'", logRequestStr);
            }
            catch (Exception ex)
            {
                _options.Logger.Error("Exception thrown: HttpStatusCode set to 500, Exception: '{0}'", ex.ToString());
                response = new ResponseMessage { StatusCode = 500, Body = JsonConvert.SerializeObject(ex) };
            }
            finally
            {
                foreach(Mapping mapping in log_mappings.Keys)
                {
                    var faillog = new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Guid = Guid.NewGuid(),
                        RequestMessage = request,
                        ResponseMessage = response,
                        MappingGuid = mapping?.Guid,
                        MappingTitle = mapping?.Title,
                        RequestMatchResult = log_mappings[mapping]
                    };
                    LogMatch(faillog, true, "");
                }
                var log = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Guid = Guid.NewGuid(),
                    RequestMessage = request,
                    ResponseMessage = response,
                    MappingGuid = targetMapping?.Guid,
                    MappingTitle = targetMapping?.Title,
                    RequestMatchResult = requestMatchResult
                };

                LogRequest(log, logRequest);

                await _responseMapper.MapAsync(response, ctx.Response);
            }

            await CompletedTask;
        }

        private void LogMatch(LogEntry entry, bool addRequest, string message)
        {
            if (addRequest)
            {
                if (_logTrafficStream != null)
                {
                    lock (_lockTrafficStream)
                    {
                        _logTrafficStream.WriteLine(JsonConvert.SerializeObject(new { Logtype = "MatchFail", LogMessage = message, LogEntry = entry }));
                        _logTrafficStream.Flush();
                    }
                }
            }
        }

        private void LogRequest(LogEntry entry, bool addRequest)
        {
            if (addRequest)
            {
                _options.LogEntries.Add(entry);
                if(_logTrafficStream != null)
                {
                    lock (_lockTrafficStream)
                    {
                        _logTrafficStream.WriteLine(JsonConvert.SerializeObject(new { Logtype = "RequestResponse", LogMessage = "", LogEntry = entry }));
                        _logTrafficStream.Flush();
                    }
                }
            }

            if (_options.MaxRequestLogCount != null)
            {
                var amount = _options.LogEntries.Count - _options.MaxRequestLogCount.Value;
                for (int i = 0; i < amount; i++)
                {
                    _options.LogEntries.RemoveAt(0);
                }
            }

            if (_options.RequestLogExpirationDuration != null)
            {
                var checkTime = DateTime.Now.AddHours(-_options.RequestLogExpirationDuration.Value);

                for (var i = _options.LogEntries.Count - 1; i >= 0; i--)
                {
                    var le = _options.LogEntries[i];
                    if (le.RequestMessage.DateTime <= checkTime)
                    {
                        _options.LogEntries.RemoveAt(i);
                    }
                }
            }
        }
    }
}