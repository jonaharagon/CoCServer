using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClashOfClansServer
{
    class TcpServer
    {
        private TcpListener _server;
        private Boolean _isRunning;

        public void StartThread()
        {
            Thread a = new Thread(new ThreadStart(Start));
            a.Start();
        }

        public void Start()
        {
            _server = new TcpListener(IPAddress.Any, CoC.port);
            _server.Start();

            _isRunning = true;

            LoopClients();
        }

        public void LoopClients()
        {
            while (_isRunning)
            {
                TcpClient newClient = _server.AcceptTcpClient();

                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(newClient);
            }
        }

        public void HandleClient(object obj)
        {
            TcpClient client = (TcpClient) obj;
            Client clientInstance = new Client(client);
            Boolean bClientConnected = true;

            while (bClientConnected)
            {
                int[] data = readData(client.GetStream());
                if (data != null)
                {
                    clientInstance.packetHandler.handle(data);
                }
            }
        }

        private int[] readData(NetworkStream networkStream)
        {
            int[] _tmp = new int[8192];
            bool _loop = networkStream.DataAvailable;
            int count = 0, __tmp, _zeroCount=0;
            while (_loop)
            {
                __tmp = networkStream.ReadByte();
                if (__tmp == 0)
                    _zeroCount++;
                if (__tmp != -1)
                {
                    _tmp[count] = __tmp;
                    count++;
                    _loop = networkStream.DataAvailable;
                } else
                {
                    _tmp = null;
                    break;
                }
            }
            if (_zeroCount == count)
                return null;
            return _tmp;
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}
