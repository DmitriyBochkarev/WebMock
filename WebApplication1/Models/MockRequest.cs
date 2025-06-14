using WebApplication1.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class MockRequest
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Method { get; set; } = "GET";

        public string QueryParameters { get; set; } // JSON-строка с параметрами

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