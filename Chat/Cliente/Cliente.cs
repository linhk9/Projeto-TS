using EI.SI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Database;
using System.Data;

namespace Cliente
{
    public partial class Cliente : Form
    {
        private bool exit = false;
        private bool connected = false;
        private bool UserAuthed = false;

        private const int PORT = 10000;
        ProtocolSI protocolSI;
        NetworkStream networkStream;
        TcpClient client;

        public Cliente()
        {
            InitializeComponent();
            protocolSI = new ProtocolSI();
        }

        private void Log(string msg = "") // clear the log if message is not supplied or is empty
        {
            if (!exit)
            {
                logTextBox.Invoke((MethodInvoker)delegate
                {
                    if (msg.Length > 0)
                    {
                        logTextBox.AppendText(string.Format("[ {0} ] {1}{2}", DateTime.Now.ToString("HH:mm"), msg, Environment.NewLine));
                    }
                    else
                    {
                        logTextBox.Clear();
                    }
                });
            }
        }

        private string ErrorMsg(string msg)
        {
            return string.Format("ERRO: {0}", msg);
        }

        private string SystemMsg(string msg)
        {
            return string.Format("SISTEMA: {0}", msg);
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            CheckAuth();

            if (UserAuthed)
            {
                if (connected)
                {
                    CloseClient();
                } else if (client == null || !client.Connected) {
                    string address = addrTextBox.Text.Trim();
                    bool error = false;

                    if (address.Length < 1)
                    {
                        error = true;
                        Log(ErrorMsg("Endereço necessário"));
                    }
                    else
                    {
                        try
                        {
                            IPAddress ip = Dns.GetHostEntry(address).AddressList[0];
                        }
                        catch
                        {
                            error = true;
                            Log(ErrorMsg("Endereço inválido"));
                        }
                    }

                    if (!error)
                    {
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, PORT);
                        client = new TcpClient();
                        client.Connect(endPoint);
                        networkStream = client.GetStream();
                        protocolSI = new ProtocolSI();
                        Connected(true);
                    }
                }
            }
            
            
        }

        private void SendTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && UserAuthed)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (sendTextBox.Text.Length > 0)
                {
                    string msg = sendTextBox.Text;
                    sendTextBox.Clear();
                    Log(string.Format("{0} (Eu): {1}", usernameTextBox.Text, msg));
                    if (connected)
                    {
                        byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, msg);
                        networkStream.Write(packet, 0, packet.Length);

                        // existe algum problema neste loop que faz crashar o cliente ou então é problema do servidor
                        while (protocolSI.GetCmdType() != ProtocolSICmdType.ACK)
                        {
                            networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                        }
                    }
                }
            }
        }

        private void Connected(bool status)
        {
            if (!exit)
            {
                connectButton.Invoke((MethodInvoker)delegate
                {
                    connected = status;
                    if (status)
                    {
                        addrTextBox.Enabled = false;
                        usernameTextBox.Enabled = false;
                        passwordTextBox.Enabled = false;
                        connectButton.Text = "Disconectar";
                        Log(SystemMsg("Estás agora conectado"));
                    }
                    else
                    {
                        addrTextBox.Enabled = true;
                        usernameTextBox.Enabled = true;
                        passwordTextBox.Enabled = true;
                        connectButton.Text = "Conectar";
                        Log(SystemMsg("Estás agora disconectado"));
                    }
                });
            }
        }

        private void CloseClient()
        {
            if (connected)
            {
                byte[] eot = protocolSI.Make(ProtocolSICmdType.EOT);
                networkStream.Write(eot, 0, eot.Length);
                // existe algum problema na parte do .Read que faz crashar o cliente ou então é problema do servidor
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                networkStream.Close();
                client.Close();
                Connected(false);
                Log();
            }
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            exit = true;
            if (connected)
            {
                CloseClient();
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            Log();
        }

        private void CheckAuth()
        {
            DB db = new DB();

            string username = usernameTextBox.Text;
            string password = passwordTextBox.Text;

            DataTable table = new DataTable();

            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE `username` = @usn and `password` = @pass", db.ObterConexao());

            db.AbrirConexao();

            command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = username;
            command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = password;

            adapter.SelectCommand = command;
            adapter.Fill(table);

            if (db.ObterConexao().State == ConnectionState.Open)
            {
                db.FecharConexao();
            }

            // Verificar se o utilizador existe ou não
            if (table.Rows.Count > 0)
            {
                UserAuthed = true;
            }
            else
            {
                // Verificar se as informações de login estão existem e estão corretas
                if (username.Trim().Equals(""))
                {
                    Log(ErrorMsg("Username necessário"));
                }
                else if (password.Trim().Equals(""))
                {
                    Log(ErrorMsg("Password necessária"));
                }
                else
                {
                    Log(ErrorMsg("Username/Password errada ou a conta não existe"));
                }
            }
        }

        private void RegistarButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            Registo registarform = new Registo();
            registarform.Show();
        }
    }
}
