using System;

namespace FauCap
{
    public abstract class Packet
    {
        public readonly int Id;
        public readonly Datagram Datagram;

        public Packet(int id, Datagram datagram)
        {
            Id = id;
            Datagram = datagram;
        }

        public Server Server => Datagram.Server;
        public uint SocketId => Datagram.SocketId;
        public bool FromServer => Datagram.FromServer;
        public DateTime Time => Datagram.Time;
        public abstract Span<byte> Raw { get; }
        public abstract Span<byte> Data { get; }
    }
}