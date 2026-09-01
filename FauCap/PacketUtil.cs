using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace FauCap
{
    static class PacketUtil
    {
        public static bool IsControlPacket(ReadOnlySpan<byte> data)
        {
            if (data.Length >= 4)
            {
                return MemoryMarshal.Read<int>(data) == 0;
            }
            return false;
        }

        public static bool IsHandshakePacket(ReadOnlySpan<byte> data)
        {
            if (data.Length > 8)
            {
                switch (Handshake.ReadName(data))
                {
                    case "POKE":
                    case "HEHE":
                    case "KISS":
                    case "HUGG":
                    case "ABRT":
                        return true;
                    default:
                        return false;
                }

            }
            return false;
        }

        public static class Handshake
        {
            public static string ReadName(ReadOnlySpan<byte> data)
            {
                return Encoding.ASCII.GetString(data.Slice(4, 4));
            }
            public static uint ReadProtocolVersion(ReadOnlySpan<byte> data)
            {
                return MemoryMarshal.Read<uint>(data.Slice(8, 4));
            }
            public static uint ReadSocketId(ReadOnlySpan<byte> data)
            {
                return MemoryMarshal.Read<uint>(data.Slice(8, 4));
            }
            public static ushort ReadStreamingProtocol(ReadOnlySpan<byte> data)
            {
                return MemoryMarshal.Read<ushort>(data.Slice(12, 2));
            }
            public static ushort ReadSequenceStart(ReadOnlySpan<byte> data)
            {
                return ReadUInt16BigEndian(data.Slice(8, 2));
            }
            public static ushort ReadGameServerPort(ReadOnlySpan<byte> data)
            {
                return ReadUInt16BigEndian(data.Slice(10, 2));
            }
        }
    }
}
