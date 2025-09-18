using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRP_Client
{
    public class Client
    {
        private HttpClient _httpClient { get; set; }
        public Client()
        {
            Thread.Sleep(1000);
            _httpClient = new HttpClient();
        }

        public async Task SendRequest()
        {
            try
            {
                string response = await _httpClient.GetStringAsync("http://localhost:8080/");
                Console.WriteLine("Server responded: " + response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
