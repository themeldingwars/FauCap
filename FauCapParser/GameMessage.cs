using System;
using System.Linq;

namespace FauCap
{
    public class GameMessage : Message
    {
        private int    last;
        private byte[] raw;
        private byte[] data;

        public readonly GamePacket[] Packets;

        public GameMessage(int id, GamePacket packet) : base(id)
        {
            Packets = new[] {packet};
        }

        public GameMessage(int id, GamePacket[] packets) : base(id)
        {
            Packets = packets;
            last    = packets.Length - 1;
        }

        public GameMessage(int id) : base(id)
        {
        }

        public virtual  Channel Channel     => Packets[0].Channel;
        public virtual  bool    IsSplit     => Packets[0].IsSplit;
        public virtual  bool    IsReliable  => Packets[0].IsReliable;
        public virtual  bool    IsSequenced => Packets[0].IsSequenced;
        public override Server  Server      => Server.Game;

        public override bool     FromServer => Packets[last].FromServer;
        public override DateTime Time       => Packets[last].Time;

        public override Span<byte> Raw
        {
            get
            {
                if (last == 0)
                    return Packets[0].Raw;

                if (raw == null) {
                    raw = new byte[Packets.Sum(packet => packet.Length)];
                    Span<byte> span = raw;

                    int offset = 0;
                    foreach (GamePacket packet in Packets) {
                        packet.Raw.CopyTo(span.Slice(offset));
                        offset += packet.Length;
                    }
                }

                return raw;
            }
        }

        public override Span<byte> Data
        {
            get
            {
                if (last == 0)
                    return Packets[0].Data;

                if (data == null) {
                    data = new byte[Packets.Sum(packet => packet.Length - packet.HeaderLength)];
                    Span<byte> span = data;

                    int offset = 0;
                    foreach (GamePacket packet in Packets) {
                        packet.Data.CopyTo(span.Slice(offset));
                        offset += packet.Length - packet.HeaderLength;
                    }
                }

                return data;
            }
        }
    }
}