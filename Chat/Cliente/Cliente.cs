using EI.SI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Database;
using System.Data;
using System.Threading;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Linq;

namespace Cliente
{
    public partial class Cliente : Form
    {
        private bool connected = false;
        private bool UserAuthed = false;

        private const int NUMBER_OF_ITERATIONS = 50000;

        private const int PORT = 10000;
        NetworkStream networkStream;
        ProtocolSI protocolSI;
        TcpClient client;

        public Cliente()
        {
            InitializeComponent();

            protocolSI = new ProtocolSI();
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
                    if (connected)
                    {
                        string msg = sendTextBox.Text;
                        sendTextBox.Clear();

                        var utilizador = new Utilizador
                        {
                            Username = usernameTextBox.Text,
                            Mensagem = msg
                        };

                        string jsonString = JsonConvert.SerializeObject(utilizador);

                        byte[] packet = protocolSI.Make(ProtocolSICmdType.DATA, jsonString);
                        networkStream.Write(packet, 0, packet.Length);

                        Log(string.Format("{0} (Eu): {1}", usernameTextBox.Text, msg));
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
                        networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);

                        string jsonString = protocolSI.GetStringFromData();
                        Utilizador utilizador = JsonConvert.DeserializeObject<Utilizador>(jsonString);

                        if (utilizador.Username != usernameTextBox.Text)
                        {
                            Log(string.Format("{0}: {1}", utilizador.Username, utilizador.Mensagem));
                        }
                    }
                    catch
                    {
                        Log(ErrorMsg("Erro ao receber mensagem!"));
                    }
                }

                Thread.Sleep(1000);
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
                    UserAuthed = false;
                    connectButton.Text = "Conectar";
                    Log(SystemMsg("Estás agora disconectado"));
                }
            });
        }

        private void CloseClient()
        {
            if (connected)
            {
                var utilizador = new Utilizador
                {
                    Username = usernameTextBox.Text
                };

                string jsonString = JsonConvert.SerializeObject(utilizador);
                byte[] eot = protocolSI.Make(ProtocolSICmdType.EOT, jsonString);

                networkStream.Write(eot, 0, eot.Length);
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
            MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE `username` = @usn", db.ObterConexao());

            db.AbrirConexao();

            command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = username;

            adapter.SelectCommand = command;
            adapter.Fill(table);

            MySqlDataReader reader;
            reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                Log(ErrorMsg("A conta com esses dados não existe"));
            } 
            else
            {
                var SaltPassword = new SaltPassword();

                // Ler resultado da pesquisa
                while (reader.Read())
                {
                    // Obter Hash (password + salt)
                    SaltPassword.Password = (byte[])reader.GetValue(3);

                    // Obter salt
                    SaltPassword.Salt = (byte[])reader.GetValue(2);
                }

                reader.Close();

                byte[] hash = GenerateSaltedHash(password, SaltPassword.Salt);

                // Verificar se o utilizador existe ou não
                if (SaltPassword.Password.SequenceEqual(hash))
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
                        Log(ErrorMsg("Username/Password errada"));
                    }
                }
            }

            if (db.ObterConexao().State == ConnectionState.Open)
            {
                db.FecharConexao();
            }
        }

        public Boolean VerificarUsername()
        {
            DB db = new DB();

            string username = usernameTextBox.Text;

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
            string uname = usernameTextBox.Text;
            string pass = passwordTextBox.Text;

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
            MySqlCommand command = new MySqlCommand("INSERT INTO `users`(`id`, `username`, `salt`, `password`) VALUES (@id, @usn, @salt, @pass)", db.ObterConexao());

            // Gerar Salt
            byte[] salt = GenerateSalt(8);
            // Gerar Salted Password
            byte[] saltedPassword = GenerateSaltedHash(passwordTextBox.Text, salt);

            // Parametros a serem inseridos na base de dados
            command.Parameters.Add("@id", MySqlDbType.VarChar).Value = Guid.NewGuid().ToString(); // ID unico que cada conta tem
            command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = usernameTextBox.Text; // Username

            command.Parameters.Add("@salt", MySqlDbType.Binary).Value = salt; // salt
            command.Parameters.Add("@pass", MySqlDbType.Binary).Value = saltedPassword; // Salted Password

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

        private static byte[] GenerateSalt(int size)
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);
            return buff;
        }

        private static byte[] GenerateSaltedHash(string plainText, byte[] salt)
        {
            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(plainText, salt, NUMBER_OF_ITERATIONS);
            return rfc2898.GetBytes(32);
        }

        public void Log(string msg = "")
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

        public string ErrorMsg(string msg)
        {
            return string.Format("ERRO: {0}", msg);
        }

        public string SystemMsg(string msg)
        {
            return string.Format("SISTEMA: {0}", msg);
        }
    }

    public class SaltPassword
    {
        public byte[] Salt { get; set; }
        public byte[] Password { get; set; }
    }

    public class Utilizador
    {
        public string Username { get; set; }
        public string Mensagem { get; set; }
    }
}
