using System;

namespace FauCap
{
    public class MatrixPacket : Packet
    {
        public MatrixPacket(int id, Datagram datagram) : base(id, datagram)
        {
        }

        public override Span<byte> Raw => Datagram.Raw;
        public override Span<byte> Data => Datagram.Data;
    }
}