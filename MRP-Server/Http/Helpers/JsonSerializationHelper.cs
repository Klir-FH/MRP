using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Server.Http.Helpers
{
    public class JsonSerializationHelper
    {
        public static async Task<T?> ReadJsonAsync<T>(HttpListenerRequest httpRequest)
        {
            using var reader = new StreamReader(httpRequest.InputStream, httpRequest.ContentEncoding);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(body);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        public static async Task WriteJsonAsync(HttpListenerResponse httpRequest, object obj)
        {
            httpRequest.ContentType = "application/json";
            string json = JsonConvert.SerializeObject(obj);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await httpRequest.OutputStream.WriteAsync(bytes);
        }
    }
}
