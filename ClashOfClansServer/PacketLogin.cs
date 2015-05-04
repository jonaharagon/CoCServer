using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashOfClansServer
{
    class PacketLogin : Packet, IAnswerable
    {
        public PacketLogin(int[] data, Client c) : base(data, c)
        {}

        internal void completeFields()
        {
            // Assuming the nonce is "nonce" since it's a Login packet
            decrypted = PacketUtils.RC4(encryptedData, PacketUtils.baseKey + "nonce");
            byte[] bDecrypted = new byte[length];
            PacketUtils.byteArrayCopy(decrypted, 0, bDecrypted, 0, length);
            userID(bDecrypted);
            clientSeed(bDecrypted);
            userToken(bDecrypted);
        }

        private void userToken(byte[] bDecrypted)
        {
            uint howLong = BitConverter.ToUInt32(bDecrypted, 8);
            string userToken = "";
            for (int a = 12; a < howLong; a++)
            {
                userToken += Encoding.UTF8.GetString(new byte[1] { bDecrypted[a] });
            }
            Console.WriteLine("Usertoken : " + userToken);
        }

        private void clientSeed(byte[] bDecrypted)
        {
            this.from.clientSeed = BitConverter.ToUInt32(bDecrypted, bDecrypted.Length-4);
            Console.WriteLine(from.clientSeed);
        }

        private void userID(byte[] bDecrypted)
        {
            this.from.userID = BitConverter.ToUInt64(bDecrypted, 0);
        }

        internal static PacketLogin convertFrom(int[] data, Client c)
        {
            // Alternative to the (PacketLogin) this
            // 'Cause this F@!#ing C# doesn't "recommend" casting!
            return new PacketLogin(data, c);
        }

        public void answer()
        {
            answerEncryption();
            calculateNonce();
            sendLoginOK();
        }

        private void sendLoginOK()
        {
            PacketGenerator pg = new PacketGenerator().qword(from.userID).qword(from.userID);
        }

        internal void answerEncryption()
        {
            PacketGenerator pg = new PacketGenerator().utf8(PacketUtils.getRandom()).dword(1);
            from.sendPacket((int)PacketType.ENCRYPTION, pg);
        }

        // Calculating the scramble (v7)
        internal void calculateNonce()
        {
            Scramble7 s = new Scramble7(from.clientSeed);
            uint hundredthByte = 0;
            for (int i = 0; i < 100; i++)
            {
                hundredthByte = s.getByte();
            }
            char[] tmp = new char[PacketUtils.getRandom().Length];
            int b = 0;
            foreach (char a in PacketUtils.getRandom())
            {
                tmp[b] = (char)(a ^ (s.getByte() & hundredthByte));
                b++;
            }
            from.nonce = String.Join("", tmp);
            //DEBUG
            Console.WriteLine("nonce = " + from.nonce);
        }


    }
}
