using System;
using static System.Buffers.Binary.BinaryPrimitives;


namespace FauCap
{
    public static class ControlMessage
    {
        public static MessageType GetMessageType(Span<byte> packet)  => (MessageType)packet[0];
        public static ushort GetAckFor(Span<byte> packet) => ReadUInt16BigEndian(packet.Slice(3, 2));
        public enum MessageType : byte
        {
            CloseConnection = 0,
            MatrixAck = 2,
            GSSAck = 3,
            TimeSyncRequest = 4,
            TimeSyncResponse = 5,
            MTUProbe = 6
        }
    }
}