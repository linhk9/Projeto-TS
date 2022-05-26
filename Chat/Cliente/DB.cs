using System;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Database
{
    class DB
    {
        private readonly MySqlConnection Conexao = new MySqlConnection("server=localhost;port=3306;username=root;password=;database=chatapp");

        // Função para abrir uma conexão com a Base de Dados
        public void AbrirConexao()
        {
            if (Conexao.State == System.Data.ConnectionState.Closed)
            {
                Conexao.Open();
            }
        }

        // Função para fechar a conexão com a Base de Dados
        public void FecharConexao()
        {
            if (Conexao.State == System.Data.ConnectionState.Open)
            {
                Conexao.Close();
            }
        }

        // Função para obter a conexão com a Base de Dados
        public MySqlConnection ObterConexao()
        {
            return Conexao;
        }
        // Função para obter dados de uma query
        public MySqlDataReader ExecutarLeitor(string sql)
        {
            try
            {
                MySqlDataReader leitor;
                MySqlCommand cmd = new MySqlCommand(sql, Conexao);
                leitor = cmd.ExecuteReader();
                return leitor;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }
    }
}