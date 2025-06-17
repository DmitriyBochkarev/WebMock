using WebApplication1.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json.Linq;

namespace WebApplication1.Models
{
    public class MockRequest
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Method { get; set; } = "GET";

        public string QueryParameters { get; set; } // JSON-строка с параметрами
        public string BodyParameters { get; set; }  // Для параметров в теле
        [NotMapped]
        public JObject BodyParametersJson
        {
            get => string.IsNullOrEmpty(BodyParameters)
                ? new JObject()
                : JObject.Parse(BodyParameters);
            set => BodyParameters = value.ToString();
        }
        public Dictionary<string, string> QueryParams
        {
            get => JsonConvert.DeserializeObject<Dictionary<string, string>>(QueryParameters ?? "{}");
            set => QueryParameters = JsonConvert.SerializeObject(value);
        }

        public string GetCacheKey()
        {
            var queryString = QueryParams.Any()
                ? "?" + string.Join("&", QueryParams.Select(kv => $"{kv.Key}={kv.Value}"))
                : "";
            return $"{Method}_{Path}{queryString}";
        }
    

    public int MockResponseId { get; set; }
        public virtual MockResponse Response { get; set; }

        public string GetKey() => $"{Method}_/{Path}";
    }
}