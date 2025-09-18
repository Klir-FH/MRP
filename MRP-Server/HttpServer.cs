using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Server
{
    public class HttpServer
    {
        private const int Port = 8080;
        private HttpListener _listener = null;


        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + Port.ToString() + "/");
            _listener.Start();
            Recieve();
        }

        public void Stop()
        {
            _listener.Stop();
        }
        public void Recieve()
        {
            _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
        }

        public void ListenerCallback(IAsyncResult result)
        {
            if (_listener.IsListening)
            {
                var context = _listener.EndGetContext(result);
                var request = context.Request;

                var response = context.Response;
                string responseText = RequestHandler(request);
                byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                response.ContentLength64 = buffer.Length;
                using (var output = response.OutputStream)
                {
                    output.Write(buffer, 0, buffer.Length);
                }


                Recieve();
            }
        }

        private string RequestHandler(HttpListenerRequest request)
        {
            return "i responded";
        }
    }
}
