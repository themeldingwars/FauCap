using System;

namespace FauCap
{
    public abstract class Message
    {
        public readonly int Id;
        public Message(int id)
        {
            Id = id;
        }

        public abstract Server Server { get; }
        public abstract bool FromServer { get; }
        public abstract DateTime Time { get; }
        public abstract Span<byte> Raw { get; }
        public abstract Span<byte> Data { get; }
    }
}