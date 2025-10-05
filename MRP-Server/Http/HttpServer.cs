using MRP_DAL.Interfaces;
using MRP_Server.Http.Controllers;
using MRP_Server.Http.Helpers;
using MRP_Server.Services;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MRP_Server.Http
{
    public class HttpServer
    {
        private readonly HttpListener _listener = new();
        private readonly UserController _userController;
        private readonly MediaController _mediaController;

        public HttpServer(UserController userController, MediaController mediaController)
        {
            _userController = userController;
            _mediaController = mediaController;
        }

        public async Task StartAsync(string prefix)
        {
            _listener.Prefixes.Add(prefix);
            _listener.Start();


            while (true)
            {
                _ = HandleRequestAsync(await _listener.GetContextAsync());
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext listenerContext)
        {
            var path = listenerContext.Request.Url.AbsolutePath;

            if (path.StartsWith("/api/users"))
                await _userController.HandleAsync(listenerContext);
            else if (path.StartsWith("/api/media"))
                await _mediaController.HandleAsync(listenerContext);
            else
            {
                listenerContext.Response.StatusCode = 404;
                await  JsonSerializationHelper.WriteJsonAsync(listenerContext.Response, new { error = "Endpoint not found" });
                listenerContext.Response.Close();
            }
        }
    }
}
