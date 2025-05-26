using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.UI.WebControls;
using WebApplication1.Controllers;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        
        // Настройка сериализации JSON
        var jsonSettings = config.Formatters.JsonFormatter.SerializerSettings;
        jsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        jsonSettings.PreserveReferencesHandling = PreserveReferencesHandling.None;
        jsonSettings.Formatting = Formatting.Indented;

        config.Formatters.Remove(config.Formatters.XmlFormatter);

        // Маршрутизация атрибутов
        config.MapHttpAttributeRoutes();

        // Игнорируем маршруты для статических файлов
        config.Routes.IgnoreRoute("Html", "{*path}", new { path = @".*\.html(/.*)?" });
        //config.Routes.IgnoreRoute("Root", "");


        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = System.Web.Http.RouteParameter.Optional }
        );

        // Формат JSON
        var json = config.Formatters.JsonFormatter;
        json.SerializerSettings.PreserveReferencesHandling =
            Newtonsoft.Json.PreserveReferencesHandling.None;
        config.Formatters.Remove(config.Formatters.XmlFormatter);

    }
    
}