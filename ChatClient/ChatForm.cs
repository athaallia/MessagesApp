using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatClient
{
    public class ChatForm : Form
    {
        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtUser;
        private Button btnConnect;
        private Button btnDisconnect;

        private ListBox lstUsers;
        private FlowLayoutPanel chatPanel;
        private TextBox txtMessage;
        private Button btnSend;
        private CheckBox chkDarkMode;
        private NetworkClient? _client;

        public ChatForm()
        {
            this.Text = "Messages App";
            this.Width = 900;
            this.Height = 600;
            this.BackColor = Color.White;

            // ===== Input User + IP/Port + Connect/Disconnect =====
            txtIp = new TextBox { Left = 10, Top = 10, Width = 120, Text = "127.0.0.1" };
            txtPort = new TextBox { Left = 140, Top = 10, Width = 60, Text = "8888" };
            txtUser = new TextBox { Left = 210, Top = 10, Width = 120, Text = "Your name" };

            btnConnect = new Button { Left = 340, Top = 10, Width = 80, Text = "Connect" };
            btnDisconnect = new Button { Left = 430, Top = 10, Width = 90, Text = "Disconnect", Enabled = false };

            btnConnect.Click += BtnConnect_Click;
            btnDisconnect.Click += BtnDisconnect_Click;

            this.Controls.Add(txtIp);
            this.Controls.Add(txtPort);
            this.Controls.Add(txtUser);
            this.Controls.Add(btnConnect);
            this.Controls.Add(btnDisconnect);

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
                Width = 650,
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
                Width = 780,
                Height = 25
            };
            btnSend = new Button
            {
                Left = 800,
                Top = 530,
                Width = 70,
                Height = 25,
                Text = "Send"
            };
            btnSend.Click += BtnSend_Click;

            this.Controls.Add(txtMessage);
            this.Controls.Add(btnSend);

            chkDarkMode = new CheckBox
            {
                Left = 10,
                Top = 508,
                Width = 120,
                Text = "Dark Mode"
            };

            chkDarkMode.CheckedChanged += (s, e) => ToggleTheme(chkDarkMode.Checked);
            this.Controls.Add(chkDarkMode);

        }

        // === Event Connect ===
        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            try
            {
                string ip = txtIp.Text;
                int port = int.Parse(txtPort.Text);

                _client = new NetworkClient(txtUser.Text);
                _client.MessageReceived += Client_MessageReceived;
                await _client.ConnectAsync(ip, port);

                AddBubble("System", $"Connected to {ip}:{port}");

                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
            }
            catch (Exception ex)
            {
                AddBubble("System", $"Error: {ex.Message}");
            }
        }

        // === Event Disconnect ===
        private async void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            if (_client != null)
            {
                await _client.DisconnectAsync();
                AddBubble("System", "Disconnected from server");
            }

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
        }


        // === Event Send ===
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

        // === Receive handler ===
        private void Client_MessageReceived(object? sender, ChatMessage msg)
        {
            this.Invoke(new Action(() =>
            {
                AppendChat(msg.From, msg.Text);

                if (!lstUsers.Items.Contains(msg.From))
                    lstUsers.Items.Add(msg.From);
            }));
        }

        // === Add Bubble to Chat Panel ===
        private void AddBubble(string user, string message, bool isOwnMessage = false)
        {
            var bubble = new Panel
            {
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(5),
                BackColor = isOwnMessage ? Color.LightGray : Color.White,
                MaximumSize = new Size(500, 0)
            };

            var lblUser = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Text = user
            };

            var lblMsg = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(480, 0),
                Text = message,
                Font = new Font("Segoe UI", 10),
            };

            bubble.Controls.Add(lblUser);
            bubble.Controls.Add(lblMsg);
            lblMsg.Top = lblUser.Bottom + 2;

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

        private void ToggleTheme(bool dark)
        {
            if (dark)
            {
                this.BackColor = Color.Black;
                chatPanel.BackColor = Color.DimGray;
                txtMessage.BackColor = Color.Black;
                txtMessage.ForeColor = Color.White;
                lstUsers.BackColor = Color.Black;
                lstUsers.ForeColor = Color.White;
            }
            else
            {
                this.BackColor = Color.White;
                chatPanel.BackColor = Color.WhiteSmoke;
                txtMessage.BackColor = Color.White;
                txtMessage.ForeColor = Color.Black;
                lstUsers.BackColor = Color.White;
                lstUsers.ForeColor = Color.Black;
            }
        }

    }
}
