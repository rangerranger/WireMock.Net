using System;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WireMock.Validation;

namespace WireMock.Matchers
{
 
    /// <summary>
    /// JsonRegExMatcher
    /// </summary>
    /// <seealso cref="IStringMatcher" />
    public class JsonRegExMatcher : IStringMatcher, IObjectMatcher
    {
        private readonly string[] _regexes;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRegExMatcher"/> class.
        /// </summary>
        /// <param name="regexs">The regex patterns.</param>
        public JsonRegExMatcher([NotNull] params string[] regexs)
        {
            Check.HasNoNulls(regexs, nameof(regexs));
            _regexes = new string[regexs.Count()];
            int i = 0;
            foreach(string regex in regexs)
            {
                _regexes[i] = regex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRegExMatcher"/> class.
        /// </summary>
        /// <param name="jsonObject">The json values (regex within).</param>
        public JsonRegExMatcher([NotNull] object jsonObject)
        {
            Check.NotNull(jsonObject, nameof(jsonObject));

            _regexes = new string[1];
            JObject jobj = JObject.FromObject(jsonObject);
            string json = JsonConvert.SerializeObject(jobj, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None, ContractResolver = null });
            _regexes[0] = System.Text.RegularExpressions.Regex.Escape(json);
        }

        /// <inheritdoc cref="IStringMatcher.IsMatch"/>
        public double IsMatch(string jsonInput)
        {
            System.Text.RegularExpressions.Regex patn = new System.Text.RegularExpressions.Regex(_regexes[0]);
            return MatchScores.ToScore(_regexes.Select(patnvalue => (new System.Text.RegularExpressions.Regex(patnvalue)).IsMatch(jsonInput)) );
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
                string jsonStr = JsonConvert.SerializeObject(jobj, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.None, ContractResolver = null });
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
            return _regexes;
        }

        /// <inheritdoc cref="IMatcher.GetName"/>
        public string GetName()
        {
            return "JsonRegExMatcher";
        }
    }
}