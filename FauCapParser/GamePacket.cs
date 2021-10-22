using System;
using System.Runtime.InteropServices;

namespace FauCap
{
    public class GamePacket : Packet
    {
        private static readonly byte[] xorByte = new byte[] { 0x00, 0xFF, 0xCC, 0xAA };
        private static readonly ulong[] xorULong = new ulong [] { 0x00, 0xFFFFFFFFFFFFFFFF, 0xCCCCCCCCCCCCCCCC, 0xAAAAAAAAAAAAAAAA };
        
        public readonly int Offset;
        public readonly Channel Channel;
        public readonly int Resends;
        public readonly bool IsSplit;
        public readonly int Length;
        public readonly ushort SequenceNumber;
        public readonly bool IsSequenced;
        public readonly bool IsReliable;
        public readonly int HeaderLength;

        public GamePacket AckPacket { get; internal set; }

        public GamePacket(int id, Datagram datagram, int offset) : base(id, datagram)
        {
            Offset = offset;
                
            Span<byte> header = Datagram.Data.Slice(Offset, 4);
                
            Channel = (Channel)(header[0] >> 6);
            Resends = (header[0] >> 4) & 0x3;
            IsSplit = (header[0] & 0x8) == 0x8;
            Length = (header[1] | header[0] << 8) & 0x07FF;
            
            
            IsSequenced = Channel != Channel.Control;
            IsReliable = Channel switch { Channel.UnreliableGss => false, Channel.Control => false, _ => true };
            HeaderLength = IsSequenced ? 4 : 2;
                
            if (IsSequenced)
            {
                SequenceNumber = (ushort) (header[3] | header[2] << 8);
            }
            
            // dexor, but leave the resends for ref
            if (Resends != 0)
            {
                Span<byte> data = Data;
                int x = data.Length >> 3;
                if (x > 0)
                {
                    Span<ulong> uSpan = MemoryMarshal.Cast<byte, ulong>(data);

                    for (int i = 0; i < x; i++)
                    {
                        uSpan[i] ^= xorULong[Resends];
                    }
                }
                for (int i = x * 8; i < data.Length; i++)
                {
                    data[i] ^= xorByte[Resends];
                }
                
            }
        }

        public GamePacket() : base(0, new Datagram(0, DateTime.MinValue, true, null))
        {
            Channel     = Channel.UnreliableGss;
            IsSplit     = false;
            IsSequenced = false;
            IsReliable  = false;
        }
            
        public override Span<byte> Raw => Datagram.Data.Slice(Offset, Length);
        public override Span<byte> Data => Datagram.Data.Slice(Offset + HeaderLength, Length - HeaderLength);
            
    }
}