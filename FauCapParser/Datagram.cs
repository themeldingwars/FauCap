using System;
using System.Buffers.Binary;

namespace FauCap
{
    public class Datagram
    {
        public readonly int Id;
        public readonly Server Server;
        public readonly DateTime Time;
        public readonly bool FromServer;
        public readonly byte[] Raw;

        public Datagram(int id, DateTime time, bool fromServer, byte[] raw)
        {
            Id = id;
            Raw = raw;
            
            Server = SocketId == 0 ? Server.Matrix : Server.Game;
            Time = time;
            FromServer = fromServer;
        }
        
        public uint SocketId => BinaryPrimitives.ReadUInt32BigEndian(Raw);
        public Span<byte> Data => Raw.AsSpan().Slice(4);
    }
}