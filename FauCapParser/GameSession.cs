using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace FauCap
{
    [Serializable]
    public class GameSession
    {
        public IPAddress RemoteIp;
        public IPAddress LocalIp;

        public ushort MatrixPort;
        public uint   ProtocolVersion;
        public uint   SocketID;
        public ushort StreamingProtocol;
        public ushort SequenceStart;
        public ushort GameServerPort;

        public List<Datagram> Datagrams;
        public List<Packet>   Packets;
        public List<Message>  Messages;
        
        private Dictionary<ushort, ulong> ReffIdLookup = new Dictionary<ushort, ulong>();

        public GameSession()
        {
            Datagrams = new List<Datagram>();
        }

        public static void Write(string file, List<GameSession> sessions)
        {
            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs)) {
                bw.Write("FCAP");
                bw.Write(sessions.Count);
                foreach (GameSession s in sessions) {
                    bw.Write(s.RemoteIp.ToString());
                    bw.Write(s.LocalIp.ToString());
                    bw.Write(s.MatrixPort);
                    bw.Write(s.ProtocolVersion);
                    bw.Write(s.SocketID);
                    bw.Write(s.StreamingProtocol);
                    bw.Write(s.SequenceStart);
                    bw.Write(s.GameServerPort);

                    bw.Write(s.Datagrams.Count);
                    foreach (Datagram p in s.Datagrams) {
                        bw.Write(p.Time.Ticks);
                        bw.Write(p.FromServer);
                        bw.Write((ushort) p.Raw.Length);
                        bw.Write(p.Raw);
                    }
                }
            }
        }

        public static List<GameSession> Read(string file, bool verboseLog = false)
        {
            List<GameSession> sessions = new List<GameSession>();

            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
            using (BinaryReader br = new BinaryReader(fs)) {
                if (br.ReadString() != "FCAP") {
                    Console.WriteLine("Could not read faucap file, magic mismatch.");
                    return null;
                }

                int sessionCount = br.ReadInt32();
                for (int i = 0; i < sessionCount; i++) {
                    GameSession s = new GameSession();
                    s.RemoteIp          = IPAddress.Parse(br.ReadString());
                    s.LocalIp           = IPAddress.Parse(br.ReadString());
                    s.MatrixPort        = br.ReadUInt16();
                    s.ProtocolVersion   = br.ReadUInt32();
                    s.SocketID          = br.ReadUInt32();
                    s.StreamingProtocol = br.ReadUInt16();
                    s.SequenceStart     = br.ReadUInt16();
                    s.GameServerPort    = br.ReadUInt16();

                    int datagramCount = br.ReadInt32();
                    for (int x = 0; x < datagramCount; x++) {
                        Datagram d = new Datagram
                        (
                            x,
                            new DateTime(br.ReadInt64()),
                            br.ReadBoolean(),
                            br.ReadBytes(br.ReadUInt16())
                        );
                        s.Datagrams.Add(d);
                    }

                    s.Reassemble(verboseLog);
                    sessions.Add(s);
                }
            }

            return sessions;
        }

        public static string ReadName(Span<byte> data)
        {
            return Encoding.ASCII.GetString(data.Slice(4, 4));
        }

        public ushort SwapBytes(ushort x)
        {
            return (ushort) ((ushort) ((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        public void Reassemble(bool verboseLog)
        {
            Packets  = new List<Packet>();
            Messages = new List<Message>();

            foreach (Datagram datagram in Datagrams) {
                if (datagram.Server == Server.Matrix) {
                    Packets.Add(new MatrixPacket(Packets.Count, datagram));
                }
                else {
                    int        consumed = 0;
                    Span<byte> data     = datagram.Data;

                    while (consumed < data.Length) {
                        GamePacket packet = new GamePacket(Packets.Count, datagram, consumed);
                        Packets.Add(packet);
                        consumed += packet.Length;
                    }
                }
            }

            //SequenceStart = SwapBytes(SequenceStart); // was the wrong endianness in the old fileformat, fixed now
            ReliableGamePacketInputQueue   sMatrix  = new ReliableGamePacketInputQueue(SequenceStart);
            ReliableGamePacketInputQueue   sGSS     = new ReliableGamePacketInputQueue(SequenceStart);
            ReliableGamePacketInputQueue   cMatrix  = new ReliableGamePacketInputQueue(SequenceStart);
            ReliableGamePacketInputQueue   cGSS     = new ReliableGamePacketInputQueue(SequenceStart);
            List<GamePacket>               sMBuffer = new List<GamePacket>();
            List<GamePacket>               sGBuffer = new List<GamePacket>();
            List<GamePacket>               cMBuffer = new List<GamePacket>();
            List<GamePacket>               cGBuffer = new List<GamePacket>();
            Dictionary<ushort, GamePacket> sMAck    = new Dictionary<ushort, GamePacket>();
            Dictionary<ushort, GamePacket> cMAck    = new Dictionary<ushort, GamePacket>();
            Dictionary<ushort, GamePacket> sGAck    = new Dictionary<ushort, GamePacket>();
            Dictionary<ushort, GamePacket> cGAck    = new Dictionary<ushort, GamePacket>();

            foreach (Packet packet in Packets) {
                if (packet.Server == Server.Matrix) {
                    Messages.Add(new MatrixMessage(Messages.Count, packet as MatrixPacket));
                    continue;
                }

                GamePacket gPacket = packet as GamePacket;

                Dictionary<ushort, GamePacket> needsAck = null;
                ReliableGamePacketInputQueue   queue    = null;
                List<GamePacket>               buffer   = null;

                switch (gPacket.Channel) {
                    case Channel.Control:
                        ControlMessage.MessageType type = ControlMessage.GetMessageType(packet.Data);
                        if (type == ControlMessage.MessageType.MatrixAck) {
                            needsAck = packet.FromServer ? cMAck : sMAck;
                        }
                        else if (type == ControlMessage.MessageType.GSSAck) {
                            needsAck = packet.FromServer ? cGAck : sGAck;
                        }

                        if (needsAck == null) {
                            Messages.Add(new GameMessage(Messages.Count, gPacket));
                            break;
                        }

                        ushort ackFor = ControlMessage.GetAckFor(gPacket.Data);

                        if (needsAck.ContainsKey(ackFor)) {
                            needsAck[ackFor].AckPacket = gPacket;
                            gPacket.AckPacket          = needsAck[ackFor];
                            needsAck.Remove(ackFor);
                            Messages.Add(new GameMessage(Messages.Count, gPacket));
                        }
                        else {
                            if (verboseLog)
                                Console.WriteLine("Duplicate " + type + ", seq: " + ackFor + " - " + (gPacket.FromServer ? "Server -> Client" : "Client -> Server"));
                        }

                        break;
                    case Channel.UnreliableGss:
                        var message = new GameMessage(Messages.Count, gPacket);
                        Messages.Add(message);
                        SplitOutRoutedMessages(message);
                        break;

                    case Channel.Matrix:
                        queue    = (gPacket.FromServer ? sMatrix : cMatrix);
                        buffer   = (gPacket.FromServer ? sMBuffer : cMBuffer);
                        needsAck = packet.FromServer ? sMAck : cMAck;
                        break;
                    case Channel.ReliableGss:
                        queue    = (gPacket.FromServer ? sGSS : cGSS);
                        buffer   = (gPacket.FromServer ? sGBuffer : cGBuffer);
                        needsAck = packet.FromServer ? sGAck : cGAck;
                        break;
                }

                if (queue == null)
                    continue;


                ReliableGamePacketInputQueue.EnqueueResult result = queue.Enqueue(gPacket);

                if (result == ReliableGamePacketInputQueue.EnqueueResult.Ok) {
                    needsAck.Add(gPacket.SequenceNumber, gPacket);
                }
                else {
                    if (verboseLog)
                        Console.WriteLine(result + " packet, seq: " + gPacket.SequenceNumber + ", channel: " + gPacket.Channel + " - " + (gPacket.FromServer ? "Server -> Client" : "Client -> Server"));
                }

                GamePacket qPacket;
                while (queue.TryDequeue(out qPacket)) {
                    buffer.Add(qPacket);

                    if (!qPacket.IsSplit) {
                        if (buffer.Count > 1) {
                            if (verboseLog) {
                                StringBuilder sb = new StringBuilder();
                                sb.Append("Reassembled split packet, seq: ");
                                sb.Append(buffer[0].SequenceNumber);
                                sb.Append("->");
                                sb.Append(buffer[buffer.Count - 1].SequenceNumber);
                                sb.Append(", channel ");
                                sb.Append(qPacket.Channel);
                                sb.Append(" - ");
                                sb.Append((qPacket.FromServer ? "Server -> Client" : "Client -> Server"));

                                Console.WriteLine(sb.ToString());
                            }
                        }

                        Messages.Add(new GameMessage(Messages.Count, buffer.ToArray()));
                        buffer.Clear();
                    }
                }
            }
        }

        public void SplitOutRoutedMessages(GameMessage gameMessage)
        {
            var controllerId = gameMessage.Data[0];
            var messageId    = gameMessage.Data[8];
            int offset       = 9;
            if (messageId == 8) { // Routed multiple
                do {
                    bool   has2ByteLen = (((gameMessage.Data[offset] & 0x80) >> 7) == 1);
                    int    length      = has2ByteLen ? gameMessage.Data[offset + 1] | (gameMessage.Data[offset] ^ 0x80) << 8 : gameMessage.Data[offset];
                    offset += has2ByteLen ? 2 : 1;
                    
                    ushort reffId      = BinaryPrimitives.ReadUInt16LittleEndian(gameMessage.Data.Slice(offset, 2));
                    offset += 2;

                    Span<byte> msgData  = null;
                    ulong  entityId = 0;

                    if (reffId == 0xFFFF) {
                        entityId = BinaryPrimitives.ReadUInt64LittleEndian(gameMessage.Data.Slice(offset, 8));
                        offset  += 8;
                        msgData =  gameMessage.Data.Slice(offset, length - 10).ToArray(); // minus 10 for the length of the reff id and the entityId
                        offset  += msgData.Length;
                        
                        // Check if the message is a ref id assign
                        if (msgData[0] == 9) {
                            var reffIdToAssign = BinaryPrimitives.ReadUInt16LittleEndian(msgData.Slice(1, 2));

                            if (ReffIdLookup.ContainsKey(reffIdToAssign)) {
                                if (ReffIdLookup[reffIdToAssign] != entityId) {
                                    Console.WriteLine($"ReffId ({reffIdToAssign}) already in look up for EntityId: {ReffIdLookup[reffIdToAssign]}, reassigning to {entityId}");
                                }

                                ReffIdLookup[reffIdToAssign] = entityId;
                            }
                            else {
                                if (ReffIdLookup.TryAdd(reffIdToAssign, entityId)) {
                                    Console.WriteLine($"[Error] Couldn't add ReffId {reffIdToAssign} to EntityId {entityId}");
                                }
                            }
                        }
                    }
                    else {
                        msgData =  gameMessage.Data.Slice(offset, length - 2).ToArray(); // minus 2 for the reff id
                        offset  += msgData.Length;
                        
                        // Try and get the entity id for this from the reff id
                        if (ReffIdLookup.TryGetValue(reffId, out var entId)) {
                            entityId = entId;
                        }
                        else {
                            Console.WriteLine($"Couldn't find EntityId in lookup for: {reffId}");
                        }
                    }

                    var subMessage = new SubMessage(Messages.Count, gameMessage, msgData, entityId);
                    Messages.Add(subMessage);

                } while (offset < gameMessage.Data.Length);
            }
        }
    }
}