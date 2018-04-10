using System;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WireMock.Validation;

namespace WireMock.Matchers
{
 
    /// <summary>
    /// JsonExactMatcher
    /// </summary>
    /// <seealso cref="IStringMatcher" />
    public class JsonExactMatcher : IStringMatcher, IObjectMatcher
    {
        private readonly string[] _jsons;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonExactMatcher"/> class.
        /// </summary>
        /// <param name="jsons">The json values.</param>
        public JsonExactMatcher([NotNull] params string[] jsons)
        {
            Check.HasNoNulls(jsons, nameof(jsons));
            _jsons = new string[jsons.Count()];
            int i = 0;
            foreach(string json in jsons)
            {
                _jsons[i] = "" + JsonConvert.DeserializeObject(json);
            }
        }

        /// <inheritdoc cref="IStringMatcher.IsMatch"/>
        public double IsMatch(string input)
        {
            string jsonInput = "" + JsonConvert.DeserializeObject(input);
            return MatchScores.ToScore(_jsons.Select(value => value.Equals(jsonInput)));
        }

        /// <inheritdoc cref="IObjectMatcher.IsMatch"/>
        public double IsMatch(object input)
        {
            if (input == null)
            {
                return MatchScores.Mismatch;
            }

            try
            {
                JObject jobj = JObject.FromObject(input);
                string jsonStr = "" + jobj;
                return IsMatch(jsonStr);
            }
            catch (Exception)
            {
                return MatchScores.Mismatch;
            }
        }

        /// <inheritdoc cref="IStringMatcher.GetPatterns"/>
        public string[] GetPatterns()
        {
            return _jsons;
        }

        /// <inheritdoc cref="IMatcher.GetName"/>
        public string GetName()
        {
            return "JsonExactMatcher";
        }
    }
}