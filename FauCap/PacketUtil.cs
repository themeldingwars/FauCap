using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FauCap
{
    static class PacketUtil
    {
        public static bool IsControlPacket(Span<byte> data)
        {
            if (data.Length >= 4)
            {
                return MemoryMarshal.Read<int>(data) == 0;
            }
            return false;
        }

        public static bool IsHandshakePacket(Span<byte> data)
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
            public static string ReadName(Span<byte> data)
            {
                return Encoding.ASCII.GetString(data.Slice(4, 4));
            }
            public static uint ReadProtocolVersion(Span<byte> data)
            {
                return MemoryMarshal.Read<uint>(data.Slice(8, 4));
            }
            public static uint ReadSocketId(Span<byte> data)
            {
                return MemoryMarshal.Read<uint>(data.Slice(8, 4));
            }
            public static ushort ReadStreamingProtocol(Span<byte> data)
            {
                return MemoryMarshal.Read<ushort>(data.Slice(12, 2));
            }
            public static ushort ReadSequenceStart(Span<byte> data)
            {
                return MemoryMarshal.Read<ushort>(data.Slice(8, 2));
            }
            public static ushort ReadGameServerPort(Span<byte> data)
            {
                return MemoryMarshal.Read<ushort>(data.Slice(10, 2));
            }
        }
    }
}
