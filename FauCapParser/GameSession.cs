using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace FauCap
{
    [Serializable]
    public class GameSession
    {
        public IPAddress RemoteIp;
        public IPAddress LocalIp;

        public ushort MatrixPort;
        public uint ProtocolVersion;
        public uint SocketID;
        public ushort StreamingProtocol;
        public ushort SequenceStart;
        public ushort GameServerPort;

        public List<Packet> Packets;

        public GameSession()
        {
            Packets = new List<Packet>();
        }

        public static void Write(string file, List<GameSession> sessions)
        {
            
            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write("FCAP");
                bw.Write(sessions.Count);
                foreach (GameSession s in sessions)
                {
                    bw.Write(s.RemoteIp.ToString());
                    bw.Write(s.LocalIp.ToString());
                    bw.Write(s.MatrixPort);
                    bw.Write(s.ProtocolVersion);
                    bw.Write(s.SocketID);
                    bw.Write(s.StreamingProtocol);
                    bw.Write(s.SequenceStart);
                    bw.Write(s.GameServerPort);

                    bw.Write(s.Packets.Count);
                    foreach (Packet p in s.Packets)
                    {
                        bw.Write(p.Time.Ticks);
                        bw.Write(p.FromServer);
                        bw.Write((ushort)p.Data.Length);
                        bw.Write(p.Data);
                    }
                }
            }      
        }

        public static List<GameSession> Read(string file)
        {
            List<GameSession> sessions = new List<GameSession>();

            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
            using (BinaryReader br = new BinaryReader(fs))
            {
                if(br.ReadString() != "FCAP")
                {
                    Console.WriteLine("Could not read faucap file, magic mismatch.");
                    return null;
                }

                int sessionCount = br.ReadInt32();
                for (int i = 0; i < sessionCount; i++)
                {
                    GameSession s = new GameSession();
                    s.RemoteIp = IPAddress.Parse(br.ReadString());
                    s.LocalIp = IPAddress.Parse(br.ReadString());
                    s.MatrixPort = br.ReadUInt16();
                    s.ProtocolVersion = br.ReadUInt32();
                    s.SocketID = br.ReadUInt32();
                    s.StreamingProtocol = br.ReadUInt16();
                    s.SequenceStart = br.ReadUInt16();
                    s.GameServerPort = br.ReadUInt16();

                    int packetCount = br.ReadInt32();
                    for (int x = 0; x < packetCount; x++)
                    {
                        Packet p = new Packet();
                        p.Time = new DateTime(br.ReadInt64());
                        p.FromServer = br.ReadBoolean();
                        p.Data = br.ReadBytes(br.ReadUInt16());
                        s.Packets.Add(p);
                    }
                    sessions.Add(s);
                }
            }
            return sessions;
        }

        public class Packet
        {
            public DateTime Time;
            public bool FromServer;
            public byte[] Data;
            public Packet() { }
            public Packet(DateTime time, bool fromServer, byte[] data)
            {
                Time = time;
                FromServer = fromServer;
                Data = data;
            }
        }
    }

}
