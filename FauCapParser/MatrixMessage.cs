using System;

namespace FauCap
{
    public class MatrixMessage : Message
    {
        public readonly MatrixPacket Packet;
        public MatrixMessage(int id, MatrixPacket packet) : base(id)
        {
            Packet = packet;
        }

        public override Server Server => Server.Matrix;
        public override bool FromServer => Packet.FromServer;
        public override DateTime Time => Packet.Time;
        public override Span<byte> Raw => Packet.Data;
        public override Span<byte> Data => Packet.Data;
    }
}