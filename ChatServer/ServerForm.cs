using System;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ServerForm : Form
    {
        private Button btnStart;
        private TextBox txtPort;
        private DataGridView dgvChat;
        private ChatServer? _server;

        public ServerForm()
        {
            // Inisialisasi UI manual
            this.Text = "TCP Chat Server";
            this.Width = 600;
            this.Height = 400;

            txtPort = new TextBox { Left = 10, Top = 10, Width = 100, Text = "8888" };
            btnStart = new Button { Left = 120, Top = 10, Width = 75, Text = "Start" };
            dgvChat = new DataGridView
            {
                Left = 10,
                Top = 50,
                Width = 560,
                Height = 300,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvChat.Columns.Add("User", "User");
            dgvChat.Columns.Add("Message", "Message");

            this.Controls.Add(txtPort);
            this.Controls.Add(btnStart);
            this.Controls.Add(dgvChat);

            btnStart.Click += BtnStart_Click;
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            _server = new ChatServer();
            _server.MessageReceived += Server_MessageReceived;

            int port = int.Parse(txtPort.Text);
            await Task.Run(() => _server.StartAsync(port));
            dgvChat.Rows.Add("System", $"Server started on port {port}");
        }

        private void Server_MessageReceived(object? sender, string msg)
        {
            this.Invoke(new Action(() =>
            {
                var parts = msg.Split(':', 2);
                string user = parts.Length > 1 ? parts[0] : "System";
                string text = parts.Length > 1 ? parts[1] : msg;

                dgvChat.Rows.Add(user.Trim(), text.Trim());
            }));
        }
    }
}
