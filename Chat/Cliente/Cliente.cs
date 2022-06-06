using EI.SI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Database;
using System.Data;
using System.Threading;

namespace Cliente
{
    public partial class Cliente : Form
    {
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

        private void Log(string msg = "")
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
                } 
                else if (client == null || !client.Connected) 
                {
                    try
                    {
                        IPAddress ip = Dns.GetHostEntry("127.0.0.1").AddressList[0];
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, PORT);

                        client = new TcpClient();
                        client.Connect(endPoint);

                        networkStream = client.GetStream();

                        Thread thread = new Thread(GetMessage);
                        thread.Start();

                        Connected(true);
                    }
                    catch
                    {
                        Log(ErrorMsg("Erro ao connectar ao servidor!"));
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
                    }
                }
            }
        }

        private void GetMessage()
        {
            while(true)
            {
                if(connected)
                {
                    try
                    {
                        networkStream = client.GetStream();
                        networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                        string mensagem = protocolSI.GetStringFromData();
                        Log(string.Format("Outro User: {0}", mensagem));
                    }
                    catch
                    {
                        Log(ErrorMsg("Erro ao receber mensagem!"));
                    }
                }
                
            }
            
        }

        private void Connected(bool status)
        {
            connectButton.Invoke((MethodInvoker)delegate
            {
                connected = status;
                if (status)
                {
                    usernameTextBox.Enabled = false;
                    passwordTextBox.Enabled = false;
                    connectButton.Text = "Disconectar";
                    Log(SystemMsg("Estás agora conectado"));
                }
                else
                {
                    usernameTextBox.Enabled = true;
                    passwordTextBox.Enabled = true;
                    connectButton.Text = "Conectar";
                    Log(SystemMsg("Estás agora disconectado"));
                }
            });
        }

        private void CloseClient()
        {
            if (connected)
            {
                byte[] eot = protocolSI.Make(ProtocolSICmdType.EOT);
                networkStream.Write(eot, 0, eot.Length);
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                networkStream.Close();
                client.Close();

                Connected(false);
                Log();
            }
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
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

        public Boolean VerificarUsername()
        {
            DB db = new DB();

            String username = usernameTextBox.Text;

            DataTable table = new DataTable();

            MySqlDataAdapter adapter = new MySqlDataAdapter();

            MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE `username` = @usn", db.ObterConexao());

            command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = username;

            adapter.SelectCommand = command;
            adapter.Fill(table);

            // Verifica se o utilizador já existe
            if (table.Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public Boolean VerificarValoresDasCaixasDeTexto()
        {
            String uname = usernameTextBox.Text;
            String pass = passwordTextBox.Text;

            // Verificar se as caixas de texto não têm os seguintes nomes ou estão vazias
            if (uname.Equals("username") || pass.Equals("password") || uname.Equals("") || pass.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RegistarButton_Click(object sender, EventArgs e)
        {
            DB db = new DB();
            MySqlCommand command = new MySqlCommand("INSERT INTO `users`(`id`, `username`, `password`) VALUES (@id, @usn, @pass)", db.ObterConexao());

            //Parametros a serem inseridos na base de dados
            command.Parameters.Add("@id", MySqlDbType.VarChar).Value = Guid.NewGuid().ToString(); // ID unico que cada conta tem
            command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = usernameTextBox.Text; // Username
            command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = passwordTextBox.Text; // Password

            // Abrir conexão com a base de dados
            db.AbrirConexao();

            // Verifica se a caixa de texto tem valores 
            if (!VerificarValoresDasCaixasDeTexto())
            {
                // Verifica se o utilizador já existe e mostra mensagem
                if (VerificarUsername())
                {
                    //Mostra mensagem de erro
                    MessageBox.Show(
                        "Esse utilizador já existe seleciona um diferente",
                        "Utilizador duplicado",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Error
                    );
                }
                else
                {
                    // Executa a query e mostra mensagem
                    if (command.ExecuteNonQuery() == 1)
                    {
                        //Mostra mensagem de informação
                        MessageBox.Show(
                            "A tua conta foi criada",
                            "Conta Criada",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        //Mostra mensagem de erro
                        MessageBox.Show("ERRO");
                    }
                }
            }
            else
            {
                //Mostra mensagem de erro
                MessageBox.Show(
                    "Insere as primeiras informações",
                    "Informação vazia",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Error
                );
            }

            // Fecha a conexão com a base de dados
            db.FecharConexao();
        }
    }
}
