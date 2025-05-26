using WebApplication1.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication1.Models
{
    public class MockRequest
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Method { get; set; } = "GET";

        public int MockResponseId { get; set; }
        public virtual MockResponse Response { get; set; }

        public string GetKey() => $"{Method}_/{Path}";
    }
}