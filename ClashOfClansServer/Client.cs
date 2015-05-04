using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClashOfClansServer
{
    class Client
    {
        private TcpClient tcpClient;

        public PacketHandler packetHandler { get; private set; }
        public ulong userID { get; set; }
        public uint clientSeed { get; set; }
        public string nonce { get; internal set; }

        public Client(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            this.packetHandler = new PacketHandler(this);
            Console.WriteLine("> New client ! (" + ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString()+ ")");
        }

        public void receivedPacket(int[] packet)
        {
            this.packetHandler.handle(packet);
        }

        public void sendPacket(int packetType, PacketGenerator pg, string nonce = "nonce")
        {
            byte[] toSend = new byte[7 + pg.getBytes().Length];
            byte[] aPacketID = new byte[2] { (byte)(packetType>>8), (byte)packetType };
            PacketUtils.byteArrayCopy(aPacketID, 0, toSend, 0, 2);
            int length = pg.getBytes().Length;
            byte[] aLength = new byte[3] { (byte)(length >> 16), (byte)(length >> 8), (byte)length };
            PacketUtils.byteArrayCopy(aLength, 0, toSend, 2, 3);
            toSend[4] = 0x00;
            toSend[5] = 0xef;
            int[] bytesAsInts = pg.getBytes().Select(x => (int)x).ToArray();
            int[] encryptedData = PacketUtils.RC4(bytesAsInts, PacketUtils.baseKey + nonce);
            PacketUtils.byteArrayCopy(encryptedData, 0, toSend, 6, encryptedData.Length - 1);
            byte[] toBeSent = bytesAsInts.Select(x => (byte)x).ToArray();
            tcpClient.GetStream().Write(toBeSent, 0, toBeSent.Length);
        }
    }
}
