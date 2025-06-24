using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApplication1.Models;
using System.Xml.Linq;
using System.Xml;
using System.Text;


namespace WebApplication1.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class MockController : ApiController
    {
        private readonly MockDbContext _db = new MockDbContext();

        [HttpPost]
        [Route("webmocks/mock/configure")]
        public IHttpActionResult ConfigureMock([FromBody] MockRequestDto requestDto)
        {
            if (requestDto == null || string.IsNullOrEmpty(requestDto.Path) || requestDto.Response == null)
            {
                return BadRequest("Invalid request");
            }
            using (var transaction = _db.Database.BeginTransaction())
            
            {
                try
                {


                    // Сохраняем ответ
                    var response = new MockResponse
                    {
                        StatusCode = requestDto.Response.StatusCode,
                        Body = requestDto.Response.Body is string ?
                            requestDto.Response.Body.ToString() :
                            JsonConvert.SerializeObject(requestDto.Response.Body),
                        Headers = requestDto.Response.Headers
                    };

                    _db.MockResponses.Add(response);
                    _db.SaveChanges();


                    // Сохраняем запрос
                    var request = new MockRequest
                    {
                        Path = requestDto.Path,
                        Method = requestDto.Method,
                        QueryParams = requestDto.QueryParams ?? new Dictionary<string, string>(),
                        BodyParameters = requestDto.BodyParameters != null
            ? JsonConvert.SerializeObject(requestDto.BodyParameters)
            : null,
                        MockResponseId = response.Id
                    };
                    


                    _db.MockRequests.Add(request);
                    _db.SaveChanges();

                    transaction.Commit();
                    return Ok();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return InternalServerError(ex);
                }
            }
        }

        [HttpGet]
        [HttpPost]
        [HttpPut]
        [HttpDelete]
        [HttpPatch]
        [Route("webmocks/{*path}")]
        public async Task<HttpResponseMessage> HandleRequestWithBody(string path)
        {
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

            var queryParams = Request.GetQueryNameValuePairs()
        .ToDictionary(kv => kv.Key, kv => kv.Value);
            var queryParamsDeserialize = JsonConvert.SerializeObject(queryParams);

            // Читаем тело запроса
            var requestBody = Request;

            // Получаем ВСЕ возможные заглушки для этого пути и метода
            var potentialMocks = _db.MockRequests
                .Include("Response")
                .Where(r => r.Method == method && r.Path == path)
                .ToList(); // Материализуем запрос

            var exactMatch = potentialMocks.FirstOrDefault(r =>
                MatchBodyParameters(r, requestBody) &&
                           r.QueryParameters == queryParamsDeserialize);

            foreach (var mock in potentialMocks)
            {
                bool matchbody = MatchBodyParameters(mock, requestBody);
                
                if (matchbody == true)
                {
                    exactMatch = mock;
                }
                else
                {
                    exactMatch = potentialMocks.FirstOrDefault(r =>
                MatchBodyParameters(r, requestBody) &&
                           r.QueryParameters == queryParamsDeserialize);
                }
            }




            // Если нет точного совпадения, ищем совпадение только по методу и пути
            var mockRequest = exactMatch ?? _db.MockRequests
                .Include("Response")
                .FirstOrDefault(r => r.Method == method &&
                                   r.Path == path &&
                                   string.IsNullOrEmpty(r.QueryParameters));

            if (mockRequest != null)
            {
                // Определяем формат ответа
                bool isXml = 
                            mockRequest.Response.Body.TrimStart().StartsWith("<");

                // Создаём базовый ответ
                var response = new HttpResponseMessage((HttpStatusCode)mockRequest.Response.StatusCode);

                // Обрабатываем тело ответа
                if (isXml)
                {
                    // Для XML
                    try
                    {
                        var xmlDoc = XDocument.Parse(mockRequest.Response.Body);
                        response.Content = new StringContent(xmlDoc.ToString(), Encoding.UTF8, "application/xml");
                    }
                    catch
                    {
                        response.Content = new StringContent(mockRequest.Response.Body, Encoding.UTF8, "text/plain");
                    }
                }
                else
                {
                    // Для JSON или plain text
                    try
                    {
                        var json = JsonConvert.DeserializeObject(mockRequest.Response.Body);
                        response.Content = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");
                    }
                    catch
                    {
                        response.Content = new StringContent(mockRequest.Response.Body, Encoding.UTF8, "text/plain");
                    }
                }

                var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    mockRequest.Response.HeadersJson ?? "{}");

                foreach (var header in headers)
                {
                    if (!response.Headers.TryAddWithoutValidation(header.Key, header.Value))
                    {
                        // Если не удалось добавить как заголовок ответа, пробуем добавить как заголовок контента
                        response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                return response;
            }

            return Request.CreateResponse(HttpStatusCode.NotFound, new
            {
                Message = "Для этого пути и метода не настроен мок",
                Path = path,
                Method = method,
                QueryParameters = queryParams,
                ConfigureUrl = "/index.html"
            });
        }

        

        [HttpGet]
        [Route("webmocks/mock/configurations")]
        public IHttpActionResult GetConfigurations()
        {
            var configs = _db.MockRequests
                .Include("Response") // Используем строку вместо лямбда-выражения
                .ToList()
                .Select(r => new
                {
                    Id = r.Id,
                    Method = r.Method,
                    Path = r.Path,
                    QueryParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                            r.QueryParameters ?? "{}"),
                    BodyParameters = JsonConvert.DeserializeObject<JObject>(
                            r.BodyParameters ?? "{}"),
                    Response = new
                    {
                        r.Response.StatusCode,
                        Body = TryParseJson(r.Response.Body),
                        Headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                            r.Response.HeadersJson ?? "{}")
                    }
                });

            return Ok(configs);
        }

        private object TryParseJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject(json);
            }
            catch
            {
                return json;
            }
        }

        [HttpDelete]
        [Route("webmocks/mock/clear")]
        public IHttpActionResult ClearAll()
        {
            try
            {
                _db.Database.ExecuteSqlCommand("DELETE FROM MockRequests");
                _db.Database.ExecuteSqlCommand("DELETE FROM MockResponses");
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("webmocks/mock/delete/{id}")]
        public IHttpActionResult DeleteMock(int id)
        {
            var mockRequest = _db.MockRequests
                .Include("Response") // Используем строку вместо лямбда-выражения
                .FirstOrDefault(r => r.Id == id);

            if (mockRequest == null)
            {
                return NotFound();
            }

            try
            {
                //_db.MockRequests.Remove(mockRequest);
                //_db.MockResponses.Remove(mockRequest.Response);
                _db.Database.ExecuteSqlCommand($"DELETE FROM MockRequests WHERE Id= {id}");
                _db.Database.ExecuteSqlCommand($"DELETE FROM MockResponses WHERE Id= {id}");
                _db.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("webmocks/mock/update/{id}")]
        public IHttpActionResult UpdateMock(int id, [FromBody] MockRequestDto requestDto)
        {
            if (requestDto == null || string.IsNullOrEmpty(requestDto.Path) || requestDto.Response == null) 
            {
                return BadRequest("Invalid request data");
            }


            using (var transaction =  _db.Database.BeginTransaction())
            {
                try
                {
                    

                    var existingRequest = _db.MockRequests
                        .Include("Response")
                        .FirstOrDefault(r =>r.Id == id);

                    if (existingRequest == null)
                    {
                        return NotFound();
                    }

                    // Обновляем данные запроса

                    existingRequest.Path = requestDto.Path;
                    existingRequest.Method = requestDto.Method;
                    existingRequest.QueryParameters = JsonConvert.SerializeObject(requestDto.QueryParams ?? new Dictionary<string, string>());
                    existingRequest.BodyParameters = requestDto.BodyParameters != null
            ? JsonConvert.SerializeObject(requestDto.BodyParameters)
            : null;
                    string setBodyParameters = requestDto.BodyParameters != null
            ? JsonConvert.SerializeObject(requestDto.BodyParameters)
            : null;


                    // Обновляем данные ответа
                    existingRequest.Response.StatusCode = requestDto.Response.StatusCode;
                    existingRequest.Response.Body = requestDto.Response.Body is string
                ? requestDto.Response.Body.ToString()
                : JsonConvert.SerializeObject(requestDto.Response.Body);
                    existingRequest.Response.HeadersJson = JsonConvert.SerializeObject(requestDto.Response.Headers ?? new Dictionary<string, string>());

                    _db.SaveChanges();
                    transaction.Commit();

                    return Ok(new { Success = true, Message = $"Заглушка успешно обновлена " +
                        $"{setBodyParameters}" +
                    //$"{JsonConvert.SerializeObject(requestDto.QueryParams)}" +
                    $"" }); ;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return InternalServerError(ex);
                }
            }

            
        }
        [HttpGet]
        [Route("webmocks/mock/configurations/{id}")]
        public IHttpActionResult GetMockConfiguration(int id)
        {
            var mock = _db.MockRequests
                .Include("Response")
                .FirstOrDefault(r => r.Id == id);

            if (mock == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                mock.Id,
                mock.Method,
                mock.Path,
                QueryParameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(mock.QueryParameters ?? "{}"),
                BodyParameters = JsonConvert.DeserializeObject<JObject>(mock.BodyParameters ?? "{}"),
                Response = new
                {
                    mock.Response.StatusCode,
                    Body = TryParseJson(mock.Response.Body),
                    Headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(mock.Response.HeadersJson ?? "{}")
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        // Конвертация XML в JObject (для сравнения с JSON-заглушкой)
        private JObject ConvertXmlToJObject(XDocument xmlDoc)
        {
            var json = new JObject();

            foreach (var element in xmlDoc.Root.Elements())
            {
                json.Add(element.Name.LocalName, element.Value);

                // Обработка атрибутов
                foreach (var attr in element.Attributes())
                {
                    json.Add($"@{attr.Name.LocalName}", attr.Value);
                }
            }

            return json;
        }


        private bool MatchBodyParameters(MockRequest mock, HttpRequestMessage request)
        {
            if (string.IsNullOrEmpty(mock.BodyParameters))
                return true;

            try
            {
                var requestBody = request.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(requestBody))
                    return true;

                //var requestJson = JToken.Parse(requestBody);
                var requestJson = XDocument.Parse(requestBody);

                var mockJson = mock.BodyParametersJson;

                //foreach (var prop in mockJson.Properties())
                //{
                //    if (requestJson[prop.Name]?.ToString() != prop.Value.ToString())
                //        return false;
                //}

                var requestXml = XDocument.Parse(requestBody);
                var requestBodyJson = ConvertXmlToJObject(requestXml);

                // Сравниваем все элементы

                foreach (var prop in mockJson.Properties())
                {
                    if (requestBodyJson[prop.Name]?.ToString() != prop.Value.ToString())
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class MockRequestDto
    {
        public int? Id { get; set; }  // Добавляем ID для редактирования

        public string Path { get; set; }
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> QueryParams { get; set; } = new Dictionary<string, string>();
        public JObject BodyParameters { get; set; } // Для сложных тел
        public MockResponseDto Response { get; set; }
    }

    public class MockResponseDto
    {
        public int StatusCode { get; set; } = 200;
        public object Body { get; set; }
        
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }


}

