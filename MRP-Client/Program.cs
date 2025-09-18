namespace MRP_Client
{
    internal class Program
    {
        public static Client HttpClient { get; set; }
        static void Main(string[] args)
        {
            HttpClient = new Client();
            SendServerRequest();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        public static async void SendServerRequest()
        {

            await HttpClient.SendRequest(); 
        }
    }
}
