using EI.SI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
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
                Logs.Escrever(Logs.SystemMsg(string.Format("Cliente {0} conectado", clientCounter)));

                //definicao da variavel
                //clientHandler do tipo ClientHandler
                ClientHandler clientHandler = new(client, clientCounter);
                clientHandler.Handle();
            }
        }
    }

    class ClientHandler
    {
        private readonly TcpClient client;
        private readonly int clientID;

        public ClientHandler(TcpClient client, int clientID)
        {
            this.client = client;
            this.clientID = clientID;
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
                byte[] ack;

                switch (protocolSI.GetCmdType())
                {
                    case ProtocolSICmdType.DATA:
                        Logs.Escrever(string.Format("{0}: {1}", clientID, protocolSI.GetStringFromData()));

                        ack = protocolSI.Make(ProtocolSICmdType.ACK);
                        networkStream.Write(ack, 0, ack.Length);
                        break;
                    case ProtocolSICmdType.EOT:
                        Logs.Escrever(Logs.SystemMsg(string.Format("A acabar thread para o cliente {0}", clientID)));
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
        public static void Escrever(string msg = "") // clear the log if message is not supplied or is empty
        {
            Console.WriteLine(string.Format("[ {0} ] {1}{2}", DateTime.Now.ToString("HH:mm"), msg, Environment.NewLine));
        }

        public static string ErrorMsg(string msg)
        {
            return string.Format("ERRO: {0}", msg);
        }

        public static string SystemMsg(string msg)
        {
            return string.Format("SISTEMA: {0}", msg);
        }
    }
}
