using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Database;

namespace Cliente
{
    public partial class Registo : Form
    {
        public Registo()
        {
            InitializeComponent();
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

        private void LoginButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            Cliente loginForm = new Cliente();
            loginForm.Show();
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
    }
}
