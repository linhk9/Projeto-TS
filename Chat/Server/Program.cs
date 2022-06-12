using EI.SI;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class Utilizador
    {
        public string? Username { get; set; }
        public string? Mensagem { get; set; }
    }

    internal class Program
    {
        //criar novamente uma constante tal como no lado do cliente
        private const int PORT = 10000;

        static void Main()
        {
            //Definição das variáveis na funcao principal
            IPEndPoint endPoint = new(IPAddress.Any, PORT);
            TcpListener listener = new(endPoint);

            //Iniciar o listener
            //apresentacao da primeira mensagem na linha de comandos
            // e inicializacao do contador
            listener.Start();
            Logs.Escrever(Logs.SystemMsg("Servidor Iniciado"));
            int clientCounter = 0;

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                clientCounter++;
                Logs.Escrever(Logs.SystemMsg(string.Format("Cliente #{0} conectado", clientCounter)));

                //definicao da variavel
                //clientHandler do tipo ClientHandler
                ClientHandler clientHandler = new(client);
                clientHandler.Handle();
            }
        }
    }

    class ClientHandler
    {
        private readonly TcpClient client;

        public ClientHandler(TcpClient client)
        {
            this.client = client;
        }

        public void Handle()
        {
            Thread thread = new(ThreadHandler);
            thread.Start();
        }

        private void ThreadHandler()
        {
            NetworkStream networkStream = client.GetStream();
            ProtocolSI protocolSI = new();

            while (protocolSI.GetCmdType() != ProtocolSICmdType.EOT)
            {
                networkStream.Read(protocolSI.Buffer, 0, protocolSI.Buffer.Length);
                byte[] ack;

                string jsonString = protocolSI.GetStringFromData();
                Utilizador? utilizador = JsonConvert.DeserializeObject<Utilizador>(jsonString);

                switch (protocolSI.GetCmdType())
                {
                    case ProtocolSICmdType.DATA:
                        Logs.Escrever(string.Format("O Cliente {0} enviou a mensagem: {1}", utilizador?.Username, utilizador?.Mensagem));

                        ack = protocolSI.Make(ProtocolSICmdType.DATA, jsonString);
                        networkStream.Write(ack, 0, ack.Length);
                        break;
                    case ProtocolSICmdType.EOT:
                        Logs.Escrever(Logs.SystemMsg(string.Format("A acabar thread para o cliente {0}", utilizador?.Username)));

                        ack = protocolSI.Make(ProtocolSICmdType.ACK);
                        networkStream.Write(ack, 0, ack.Length);
                        break;
                }
            }

            networkStream.Close();
            client.Close();
        }
    }

    class Logs
    {
        public static void Escrever(string msg = "") 
        {
            Console.WriteLine(string.Format("[ {0} ] {1}{2}", DateTime.Now.ToString("HH:mm"), msg, Environment.NewLine));

            using StreamWriter w = File.AppendText("log.txt");
            WriteInFile(msg, w);
        }

        public static string ErrorMsg(string msg)
        {
            return string.Format("ERRO: {0}", msg);
        }

        public static string SystemMsg(string msg)
        {
            return string.Format("SISTEMA: {0}", msg);
        }

        public static void WriteInFile(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog: ");
            w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
            w.WriteLine($"  :{logMessage}");
            w.WriteLine("-------------------------------");
        }
    }
}
