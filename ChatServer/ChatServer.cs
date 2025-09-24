using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ChatMessage
    {
        public string? Type { get; set; }   // "msg","join","leave","pm","sys"
        public string? From { get; set; }
        public string? To { get; set; }
        public string? Text { get; set; }
        public long Ts { get; set; }
    }

    public class ChatServer
    {
        private TcpListener? _listener;
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new();

        public async Task StartAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            string? username = null;
            try
            {
                using var ns = client.GetStream();
                using var reader = new StreamReader(ns, Encoding.UTF8, leaveOpen: true);
                using var writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true };

                // join
                var joinLine = await reader.ReadLineAsync();
                var joinMsg = JsonSerializer.Deserialize<ChatMessage>(joinLine);
                if (joinMsg?.Type != "join")
                {
                    return;
                }
                username = joinMsg.From;

                if (!_clients.TryAdd(username, client))
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(
                        new ChatMessage { Type = "sys", From = "Server", Text = "Username taken", Ts = Now() }));
                    return;
                }

                Console.WriteLine($"[SERVER] {username} joined");
                await BroadcastAsync(new ChatMessage { Type = "join", From = username, Text = $"{username} joined", Ts = Now() });

                // main loop
                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    var msg = JsonSerializer.Deserialize<ChatMessage>(line);
                    if (msg == null) continue;
                    msg.From = username;
                    msg.Ts = Now();

                    if (msg.Type == "msg")
                        await BroadcastAsync(msg);
                    else if (msg.Type == "pm")
                    {
                        await SendToAsync(msg.To, msg);
                        await SendToAsync(username, msg);
                    }
                }
            }
            finally
            {
                if (username != null)
                {
                    _clients.TryRemove(username, out _);
                    await BroadcastAsync(new ChatMessage { Type = "leave", From = username, Text = $"{username} left", Ts = Now() });
                }
                client.Close();
            }
        }

        private async Task BroadcastAsync(ChatMessage msg)
        {
            var json = JsonSerializer.Serialize(msg);

            foreach (var kv in _clients)
            {
                try
                {
                    var sw = new StreamWriter(kv.Value.GetStream(), new UTF8Encoding(false), leaveOpen: true)
                    {
                        AutoFlush = true
                    };
                    await sw.WriteLineAsync(json);
                }
                catch { }
            }

    // ✅ trigger ke UI Server
    MessageReceived?.Invoke(this, $"{msg.From}: {msg.Text}");
}


        private async Task SendToAsync(string user, ChatMessage msg)
        {
            if (_clients.TryGetValue(user, out var cli))
            {
                var sw = new StreamWriter(cli.GetStream(), new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
                await sw.WriteLineAsync(JsonSerializer.Serialize(msg));
            }
        }

        private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        public event EventHandler<string>? MessageReceived;

    }
}
