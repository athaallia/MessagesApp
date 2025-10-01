using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatClient
{
    public class NetworkClient
    {
        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        public string Username { get; private set; }

        public event EventHandler<ChatMessage>? MessageReceived;

        public NetworkClient(string username)
        {
            Username = username;
        }

        public async Task ConnectAsync(string host, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);

            var ns = _client.GetStream();
            _reader = new StreamReader(ns, Encoding.UTF8, leaveOpen: true);
            _writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true };

            // send JOIN (JSON per-line)
            var join = new ChatMessage { Type = "join", From = Username, Text = "", Ts = Now() };
            await _writer.WriteLineAsync(JsonSerializer.Serialize(join));

            _ = Task.Run(ReceiveLoop); // start background receiver
        }

       public async Task DisconnectAsync()
        {
            try
            {
                if (_writer != null)
                {
            var leave = new ChatMessage { Type = "leave", From = Username, Text = "", Ts = Now() };
            await _writer.WriteLineAsync(JsonSerializer.Serialize(leave));
                }
            }
            catch { }

            _writer?.Dispose();
            _reader?.Dispose();
            _client?.Close();
        }


        public async Task SendAsync(string text)
        {
            if (_writer == null) return;
            var msg = new ChatMessage { Type = "msg", From = Username, Text = text, Ts = Now() };
            await _writer.WriteLineAsync(JsonSerializer.Serialize(msg));
        }

        public async Task SendPmAsync(string to, string text)
        {
            if (_writer == null) return;
            var msg = new ChatMessage { Type = "pm", From = Username, To = to, Text = text, Ts = Now() };
            await _writer.WriteLineAsync(JsonSerializer.Serialize(msg));
        }

        private async Task ReceiveLoop()
        {
            if (_client == null || _reader == null) return;

            try
            {
                while (_client.Connected)
                {
                    var line = await _reader.ReadLineAsync();
                    if (line == null) break;

                    var msg = JsonSerializer.Deserialize<ChatMessage>(line);
                    if (msg != null)
                        MessageReceived?.Invoke(this, msg);
                }
            }
            catch
            {
                // swallow; UI akan handle disconnect
            }
        }

        private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
