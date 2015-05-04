using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashOfClansServer
{
    class PacketGenerator
    {
        private byte[] packet = new byte[2048]; // To be changed, has to be dynamic
        private int offset;

        public PacketGenerator()
        {
        }

        public int getLength()
        {
            return offset;
        }

        private PacketGenerator(byte[] packet, int offset)
        {
            this.packet = packet;
            this.offset = offset;
        }

        public PacketGenerator qword(ulong qword)
        {
            byte[] tmp = BitConverter.GetBytes(qword);
            this.offset = PacketUtils.transferBytes(tmp, this.packet, this.offset);
            return new PacketGenerator(this.packet, this.offset);
        }

        public PacketGenerator dword(uint dword)
        {
            byte[] tmp = BitConverter.GetBytes(dword);
            this.offset = PacketUtils.transferBytes(tmp, this.packet, this.offset);
            return new PacketGenerator(this.packet, this.offset);
        }

        internal byte[] getBytes()
        {
            return packet;
        }

        public PacketGenerator utf8(string a)
        {
            PacketGenerator tmp = dword((uint) a.Length);
            foreach (char c in a)
            {
                tmp = tmp.bit((byte)c);
            }
            return tmp;
        }

        public PacketGenerator bit(byte a)
        {
            byte[] tmp = new byte[1] { a };
            this.offset = PacketUtils.transferBytes(tmp, this.packet, this.offset);
            return new PacketGenerator(this.packet, this.offset);
        }

        public PacketGenerator none()
        {
            return null;
        }

        public PacketGenerator boolean(bool a)
        {
            byte[] tmp = BitConverter.GetBytes(a);
            this.offset = PacketUtils.transferBytes(tmp, this.packet, this.offset);
            return new PacketGenerator(this.packet, this.offset);
        }
    }

    class Packet
    {
        protected int[] data;
        protected int[] decrypted;
        protected int[] encryptedData;
        protected int length;
        protected int packetID;
        protected Client from;

        public Packet(int[] data, Client from)
        {
            this.data = data;
            packetID = ((data[0] & 0xff) << 8) | (data[1] & 0xff);
            length = ((data[2] & 0xff) << 16) | ((data[3] & 0xff) << 8) | (data[4] & 0xff);
            encryptedData = new int[length];
            for (int i = 7; i <= length; i++)
            {
                encryptedData[i - 7] = this.data[i];
            }
            Console.WriteLine("Packet received : " + packetID);
            this.from = from;
        }

        public Packet getSpecificPacket()
        {
            switch (packetID)
            {
                case (int) PacketType.LOGIN:
                    PacketLogin tmp = PacketLogin.convertFrom(data, from);
                    tmp.completeFields();
                    return tmp;
                default:
                    return null;
            }
        }
    }

    class PacketUtils
    {
        public static String baseKey = "fhsd6f86f67rt8fw78fw789we78r9789wer6re";

        public static int[] RC4(int[] input, string key)
        {
            int[] tmp = new int[input.Length];
            int x, y, j = 0;
            int[] box = new int[256];

            for (int i = 0; i < 256; i++)
            {
                box[i] = i;
            }

            for (int i = 0; i < 256; i++)
            {
                j = (key[i % key.Length] + box[i] + j) % 256;
                x = box[i];
                box[i] = box[j];
                box[j] = x;
            }

            for (int i = 0; i < input.Length; i++)
            {
                y = i % 256;
                j = (box[y] + j) % 256;
                x = box[y];
                box[y] = box[j];
                box[j] = x;

                tmp[i] = (input[i] ^ box[(box[y] + box[j]) % 256]);
            }
            return tmp;
        }

        public static int transferBytes(byte[] tmp, byte[] packet, int o)
        {
            int i = 0;
            foreach (byte a in tmp)
            {
                packet[o + i] = a;
                i++;
            }
            return o + i;
        }

        public static void byteArrayCopy(byte[] a, int v1, byte[] b, int v2, int v3)
        {
            for (int i = v1; i < v3; i++)
            {
                b[v2 + i] = a[i];
            }
        }

        public static void byteArrayCopy(int[] a, int v1, byte[] b, int v2, int length)
        {
            for (int i = v1; i < length; i++)
            {
                b[v2 + i] = (byte)a[i];
            }
        }

        internal static string getRandom()
        {
            // This is static, not the best...
            return "2tdziru1asé&@f*";
        }
    }

    class PacketHandler
    {
        private Client c;
        private Packet last;

        public PacketHandler(Client c)
        {
            this.c = c;
        }

        internal Packet handle(int[] packet)
        {
            Packet tmp = new Packet(packet, c);
            this.last = tmp.getSpecificPacket();
            if (this.last is IAnswerable)
            {
               ((IAnswerable)this.last).answer();
            }
            return this.last;
        }
    }

    enum PacketType {
        LOGIN = 10101,
        ENCRYPTION = 20000
    }



    public interface IAnswerable
    {
        void answer();
    }

    class Scramble7
    {
        private uint[] buffer;
        private int ix;

        public Scramble7(uint seed)
        {
            this.ix = 0;
            initBuffer();
            this.seedBuffer(seed);
        }

        private void initBuffer()
        {
            this.buffer = new uint[624];
            for (int i = 0; i<624; i++)
            {
                buffer[i] = 0;
            }
        }

        private void seedBuffer(uint seed)
        {
            for (int i = 0; i<624; i++)
            {
                this.buffer[i] = seed;
                seed = (1812433253 * ((seed ^ rshift(seed, 30)) + 1)) & 0xFFFFFFFF;
            }
        }

        public uint getByte()
        {
            uint x = this.getInt();
            if (isNegative(x) != 0)
            {
                x = negate(x);
            }
            return (x % 256);
        }

        private uint negate(uint x)
        {
            return (~x) + 1;
        }

        private uint isNegative(uint x)
        {
            return x & (2 ^ 31);
        }

        private uint getInt()
        {
            if (this.ix == 0)
            {
                this.mixBuffer();
            }
            uint val = this.buffer[this.ix];
            this.ix = (this.ix + 1) % 624;
            val ^= rshift(val, 11) ^ lshift((val ^ rshift(val, 11)), 7) & 0x9D2C5680;
            return rshift((val ^ lshift(val, 15) & 0xEFC60000), 18) ^ val ^ lshift(val, 15) & 0xEFC60000;
        }

        private uint lshift(uint a, int b)
        {
            return ((uint)(a * (2 ^ b)) % (2 ^ 32));
        }

        private void mixBuffer()
        {
            int i = 0, j = 0;
            uint a, b;
            while (i < 624)
            {
                i++;
                a = (this.buffer[i % 624] & 0x7FFFFFFF) + (this.buffer[j] & 0x80000000);
                b = rshift(a, 1)^this.buffer[(i+394)%624];
                if ((a&1)!= 0)
                {
                    b ^= 0x9908B0DF;
                }
                this.buffer[j] = b;
                j++;
            }
        }

        private uint rshift(uint num, int n)
        {
            int highbits = 0;
            if ((num & (2 ^ 31)) != 0)
            {
                highbits = (2 ^ (n - 1)) * (2 ^ (32 - n));
            }
            return Convert.ToUInt32((num / (2 ^ n)) | highbits);
        }
    }
}
