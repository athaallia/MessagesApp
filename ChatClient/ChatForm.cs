using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatClient
{
    public class ChatForm : Form
    {
        private TextBox txtUser;
        private Button btnConnect;
        private ListBox lstUsers;
        private FlowLayoutPanel chatPanel;
        private TextBox txtMessage;
        private Button btnSend;

        private NetworkClient? _client;

        public ChatForm()
        {
            this.Text = "Messages App";
            this.Width = 800;
            this.Height = 600;
            this.BackColor = Color.White;

            // ===== Input User + Connect (Top Bar) =====
            txtUser = new TextBox { Left = 10, Top = 10, Width = 150, Text = "Your name" };
            btnConnect = new Button { Left = 170, Top = 10, Width = 80, Text = "Join" };
            btnConnect.Click += BtnConnect_Click;
            this.Controls.Add(txtUser);
            this.Controls.Add(btnConnect);

            // ===== Left Panel: Members =====
            var lblMembers = new Label
            {
                Left = 10,
                Top = 50,
                Text = "Members",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true
            };

            lstUsers = new ListBox
            {
                Left = 10,
                Top = 70,
                Width = 200,
                Height = 450
            };

            this.Controls.Add(lblMembers);
            this.Controls.Add(lstUsers);

            // ===== Right Panel: Chat Bubble =====
            chatPanel = new FlowLayoutPanel
            {
                Left = 220,
                Top = 50,
                Width = 550,
                Height = 450,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.WhiteSmoke
            };
            this.Controls.Add(chatPanel);

            // ===== Bottom: Input + Send =====
            txtMessage = new TextBox
            {
                Left = 10,
                Top = 530,
                Width = 680,
                Height = 25
            };
            btnSend = new Button
            {
                Left = 700,
                Top = 530,
                Width = 70,
                Height = 25,
                Text = "Send"
            };
            btnSend.Click += BtnSend_Click;

            this.Controls.Add(txtMessage);
            this.Controls.Add(btnSend);
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            string host = "127.0.0.1"; // IP default
            int port = 8888;

            _client = new NetworkClient(txtUser.Text);
            _client.MessageReceived += Client_MessageReceived;
            await _client.ConnectAsync(host, port);

            AppendChat("System", "Connected to server");
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text) || _client == null)
                return;

            string msg = txtMessage.Text;

            if (msg.StartsWith("/w ")) // Private message
            {
                var split = msg.Split(' ', 3);
                if (split.Length >= 3)
                {
                    string toUser = split[1];
                    string text = split[2];
                    await _client.SendPmAsync(toUser, text);
                }
            }
            else // Broadcast
            {
                await _client.SendAsync(msg);
            }

            txtMessage.Clear();
        }

        private void Client_MessageReceived(object? sender, ChatMessage msg)
        {
            this.Invoke(new Action(() =>
            {
                AppendChat(msg.From, msg.Text);

                if (!lstUsers.Items.Contains(msg.From))
                    lstUsers.Items.Add(msg.From);
            }));
        }

        // ===== Add Bubble to Chat Panel =====
        private void AddBubble(string user, string message, bool isOwnMessage = false)
        {
            var bubble = new Panel
            {
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(5),
                BackColor = isOwnMessage ? Color.LightGray : Color.White,
                MaximumSize = new Size(400, 0)
            };

            // User name
            var lblUser = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Text = user
            };

            // Message
            var lblMsg = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                Text = message,
                Font = new Font("Segoe UI", 10),
            };

            bubble.Controls.Add(lblUser);
            bubble.Controls.Add(lblMsg);
            lblMsg.Top = lblUser.Bottom + 2;

            // Align bubble kanan kalau own message
            if (isOwnMessage)
                bubble.Anchor = AnchorStyles.Right;

            chatPanel.Controls.Add(bubble);
            chatPanel.ScrollControlIntoView(bubble);
        }

        private void AppendChat(string user, string message)
        {
            bool isOwn = (user == txtUser.Text);
            AddBubble(user, message, isOwn);
        }
    }
}
