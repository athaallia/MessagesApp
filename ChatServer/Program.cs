using System;
using System.Threading.Tasks;

namespace ChatServer
{
    class Program
    {
        static async Task Main()
        {
            var server = new ChatServer();
            Console.WriteLine("=== TCP Chat Server ===");
            Console.WriteLine("Listening on port 8888...");
            await server.StartAsync(8888);
        }
    }
}
