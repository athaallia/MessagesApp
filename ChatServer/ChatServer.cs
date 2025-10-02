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

        // ===== Logging =====
        private readonly string logFile = "server.log";
        private void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logEntry);
            try
            {
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch
            {
                // ignore error menulis file log
            }
        }

        public async Task StartAsync(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            Log($"=== TCP Chat Server started on port {port} ===");

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
                if (string.IsNullOrEmpty(joinLine)) return;

                var joinMsg = JsonSerializer.Deserialize<ChatMessage>(joinLine);
                if (joinMsg?.Type != "join" || string.IsNullOrEmpty(joinMsg.From))
                    return;

                username = joinMsg.From;

                if (!_clients.TryAdd(username, client))
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(
                        new ChatMessage { Type = "sys", From = "Server", Text = "Username taken", Ts = Now() }));
                    Log($"[SYSTEM] Username '{username}' already taken.");
                    return;
                }

                // --- Informasi join & broadcast ---
                Log($"[SERVER] {username} joined");
                await BroadcastAsync(new ChatMessage { Type = "join", From = username, Text = $"{username} joined", Ts = Now() });

                // --- Kirim daftar user online ke client baru ---
                var currentUsers = _clients.Keys;
                await SendToAsync(username, new ChatMessage
                {
                    Type = "sys",
                    From = "Server",
                    Text = string.Join(",", currentUsers),
                    Ts = Now()
                });

                // main loop
                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) break;

                    var msg = JsonSerializer.Deserialize<ChatMessage>(line);
                    if (msg == null) continue;

                    msg.From = username;
                    msg.Ts = Now();

                    if (msg.Type == "msg")
                    {
                        await BroadcastAsync(msg);
                        Log($"[MSG] {username}: {msg.Text}");
                    }
                    else if (msg.Type == "pm" && !string.IsNullOrEmpty(msg.To))
                    {
                        await SendToAsync(msg.To, msg);
                        await SendToAsync(username, msg);
                        Log($"[PM] {username} -> {msg.To}: {msg.Text}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] {username}: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(username))
                {
                    _clients.TryRemove(username, out _);
                    await BroadcastAsync(new ChatMessage { Type = "leave", From = username, Text = $"{username} left", Ts = Now() });
                    Log($"[SERVER] {username} left");
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

            // trigger ke UI Server jika ada
            MessageReceived?.Invoke(this, $"{msg.From}: {msg.Text}");
        }

        private async Task SendToAsync(string? user, ChatMessage msg)
        {
            if (string.IsNullOrEmpty(user)) return;

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
