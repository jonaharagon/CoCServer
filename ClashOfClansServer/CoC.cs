using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashOfClansServer
{
    class CoC
    {
        public static String version = "0.1";
        public static String nameOfProgram = "Clash of Clans Server";
        public static int port = 9339;

        static void Main(string[] args)
        {
            TcpServer tcpServer = new TcpServer();
            tcpServer.StartThread();
            Console.WriteLine(nameOfProgram + " v" + version + " started on port " + port + "!");
            while(true)
            {
            }
        }
    }
}
