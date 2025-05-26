using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class MockResponse
    {
        public int Id { get; set; }
        public int StatusCode { get; set; } = 200;
        public string Body { get; set; }
        public string HeadersJson { get; set; }

        public virtual ICollection<MockRequest> Requests { get; set; }

        public Dictionary<string, string> Headers
        {
            get => JsonConvert.DeserializeObject<Dictionary<string, string>>(HeadersJson ?? "{}");
            set => HeadersJson = JsonConvert.SerializeObject(value);
        }
    }
}