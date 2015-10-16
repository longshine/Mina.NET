using System;
using System.Net;
using System.Windows.Forms;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Filter.Logging;
using Mina.Filter.Ssl;
using Mina.Transport.Socket;

namespace Mina.Example.Chat.Client
{
    public partial class FormChat : Form
    {
        IoConnector connector = new AsyncSocketConnector();
        IoSession session;

        public FormChat()
        {
            InitializeComponent();

            textBoxUser.Text = "user" + Math.Round(new Random().NextDouble() * 10);

            connector.FilterChain.AddLast("logger", new LoggingFilter());
            connector.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory()));

            connector.SessionClosed += (o, e) => Append("Connection closed.");
            connector.MessageReceived += OnMessageReceived;

            SetState(false);
        }

        private void OnMessageReceived(object sender, IoSessionMessageEventArgs e)
        {
            String theMessage = (String)e.Message;
            String[] result = theMessage.Split(new Char[] { ' ' }, 3);
            String status = result[1];
            String theCommand = result[0];

            if ("OK".Equals(status))
            {
                if (String.Equals("BROADCAST", theCommand, StringComparison.OrdinalIgnoreCase))
                {
                    if (result.Length == 3)
                        Append(result[2]);
                }
                else if (String.Equals("LOGIN", theCommand, StringComparison.OrdinalIgnoreCase))
                {
                    SetState(true);
                    Append("You have joined the chat session.");
                }
                else if (String.Equals("QUIT", theCommand, StringComparison.OrdinalIgnoreCase))
                {
                    SetState(false);
                    Append("You have left the chat session.");
                }
            }
            else
            {
                if (result.Length == 3)
                {
                    MessageBox.Show(result[2]);
                }
            }
        }

        private void SetState(Boolean loggedIn)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Boolean>(SetState), loggedIn);
                return;
            }
            buttonConnect.Enabled = textBoxUser.Enabled = textBoxServer.Enabled = !loggedIn;
            buttonDisconnect.Enabled = buttonSend.Enabled = buttonQuit.Enabled = textBoxChat.Enabled = textBoxInput.Enabled = loggedIn;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            String server = textBoxServer.Text;
            if (String.IsNullOrEmpty(server))
                return;

            if (checkBoxSSL.Checked)
            {
                if (!connector.FilterChain.Contains("ssl"))
                    connector.FilterChain.AddFirst("ssl", new SslFilter("TempCert", null));
            }
            else if (connector.FilterChain.Contains("ssl"))
            {
                connector.FilterChain.Remove("ssl");
            }

            IPEndPoint ep;
            String[] parts = server.Trim().Split(':');
            if (parts.Length > 0)
            {
                ep = new IPEndPoint(IPAddress.Parse(parts[0]), Int32.Parse(parts[1]));
            }
            else
            {
                ep = new IPEndPoint(IPAddress.Loopback, Int32.Parse(parts[0]));
            }

            IConnectFuture future = connector.Connect(ep).Await();

            if (future.Connected)
            {
                session = future.Session;
                session.Write("LOGIN " + textBoxUser.Text);
            }
            else
            {
                MessageBox.Show("Could not connect to " + server);
            }
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            Quit();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            Broadcast(textBoxInput.Text);
        }

        private void buttonQuit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            connector.Dispose();
        }

        public void Broadcast(String message)
        {
            if (session != null)
                session.Write("BROADCAST " + message);
        }

        public void Quit()
        {
            if (session != null)
            {
                session.Write("QUIT");
                // session will be closed by the server
                session = null;
            }
        }

        public void Append(String line)
        {
            if (textBoxChat.InvokeRequired)
            {
                textBoxChat.Invoke(new Action<String>(Append), line);
                return;
            }

            textBoxChat.AppendText(line);
            textBoxChat.AppendText(Environment.NewLine);
        }
    }
}
