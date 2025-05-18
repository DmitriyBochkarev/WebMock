using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace MockService.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class MockController : ApiController
    {
        private static readonly Dictionary<string, MockResponse> MockResponses = new Dictionary<string, MockResponse>();
        private static readonly object LockObject = new object();

        // Настройка мока
        [HttpPost]
        [Route("api/mock/configure")]
        public IHttpActionResult ConfigureMock([FromBody] MockRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Path) || request.Response == null)
            {
                return BadRequest("Неверный запрос");
            }

            var key = $"{request.Method}_{request.Path}";

            lock (LockObject)
            {
                MockResponses[key] = request.Response;
            }

            return Ok();
        }

        // Обработка всех входящих запросов
        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        [HttpPatch]
        [Route("api/{*path}")]
        public HttpResponseMessage HandleRequest(string path)
        {
            // Игнорируем запросы к корню и к самому хосту
            if (string.IsNullOrEmpty(path) || path.Equals("/") || path.Equals(""))
            {
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Message = "Mock Service is running",
                    UiUrl = "/index.html"
                });
            }


            var method = Request.Method.Method;
            var key = $"{method}_/{path}";

            lock (LockObject)
            {
                if (MockResponses.TryGetValue(key, out var mockResponse))
                {
                    var response = Request.CreateResponse(
                        (HttpStatusCode)mockResponse.StatusCode,
                        mockResponse.Body);

                    foreach (var header in mockResponse.Headers)
                    {
                        response.Headers.Add(header.Key, header.Value);
                    }

                    return response;
                }
            }

            return Request.CreateResponse(HttpStatusCode.NotFound, new
            {
                Message = "Для этого пути и метода не настроен мок",
                Path = path,
                Method = method,
                ConfigureUrl = "/index.html"
            });
        }

        // Получение списка всех моков
        [HttpGet]
        [Route("api/mock/configurations")]
        public IHttpActionResult GetConfigurations()
        {
            lock (LockObject)
            {
                var configs = MockResponses.Select(kvp => new
                {
                    Key = kvp.Key,
                    Method = kvp.Key.Split('_')[0],
                    Path = kvp.Key.Split('_')[1],
                    kvp.Value
                }).ToList();

                return Ok(configs);
            }
        }

        // Очистка всех моков
        [HttpDelete]
        [Route("api/mock/clear")]
        public IHttpActionResult ClearAll()
        {
            lock (LockObject)
            {
                MockResponses.Clear();
            }
            return Ok();
        }
    }

    public class MockRequest
    {
        public string Path { get; set; }
        public string Method { get; set; } = "GET";
        public MockResponse Response { get; set; }
    }

    public class MockResponse
    {
        public int StatusCode { get; set; } = 200;
        public object Body { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}