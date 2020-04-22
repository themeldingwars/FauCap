using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static FauCap.PacketUtil;

namespace FauCap
{
    class Program
    {

        static List<GameSession> sessions;
        private static Status status;

        private static string usage = "";

        static void Main(string[] args)
        {

            string inFile = "";
            string outFile = "";


            if (args.Length > 0 )
            {
                if(File.Exists(args[0]))
                {
                    inFile = args[0];
                    if(args.Length > 1)
                    {
                        outFile = Path.GetFullPath(args[1]);
                    }
                    else
                    {
                        outFile = Path.ChangeExtension(args[0], "faucap");
                    }
                }
                else
                {
                    Console.WriteLine($"File {args[0]} does not exist.");
                    Console.WriteLine(usage);
                    return;
                }
            }
            else
            {
                Console.WriteLine(usage);
                return;
            }

            sessions = new List<GameSession>();
            status = Status.Waiting;

            CaptureFileReaderDevice device;
            try
            {
                device = new CaptureFileReaderDevice(inFile);
                device.Open();
                Console.WriteLine($"Parsing pcap file {inFile}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not parse {inFile}, is it a valid pcap capture?");
                return;
            }

            device.OnPacketArrival += new PacketArrivalEventHandler(OnPacketArrival);
            device.Capture();
            device.Close();

            Console.WriteLine($"Done parsing {inFile}, " + (sessions.Count > 0 ? $"{sessions.Count} sessions was found, exporting faucap." : "but no game sessions was found."));

            GameSession.Write(outFile, sessions);

        }

        static void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            if (e.Packet.LinkLayerType == PacketDotNet.LinkLayers.Ethernet)
            {
                var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

                var udpPacket = (PacketDotNet.UdpPacket)packet.Extract<PacketDotNet.UdpPacket>();

                var ipPacket = (PacketDotNet.IPv4Packet)packet.Extract<PacketDotNet.IPv4Packet>();

                if (udpPacket == null) { return; }
                byte[] data = udpPacket.PayloadData;

                DateTime time = e.Packet.Timeval.Date;
                
                if(IsHandshakePacket(data))
                {
                    switch (Handshake.ReadName(data))
                    {
                        case "POKE":
                            status = Status.Poked;
                            GameSession session = new GameSession();
                            session.Packets.Add(new GameSession.Packet(time, false, data));

                            session.LocalIp = ipPacket.SourceAddress;
                            session.RemoteIp = ipPacket.DestinationAddress;
                            session.MatrixPort = udpPacket.DestinationPort;

                            session.ProtocolVersion = Handshake.ReadProtocolVersion(data);

                            sessions.Add(session);
                            break;

                        case "HEHE":
                            if(status != Status.Poked)
                            {
                                status = Status.Waiting;
                                break;
                            }

                            status = Status.Laughed;
                            sessions.Last().SocketID = Handshake.ReadSocketId(data);
                            sessions.Last().Packets.Add(new GameSession.Packet(time, true, data));
                            break;

                        case "KISS":
                            if (status != Status.Laughed)
                            {
                                status = Status.Waiting;
                                break;
                            }

                            status = Status.Kissed;
                            sessions.Last().StreamingProtocol = Handshake.ReadStreamingProtocol(data);
                            sessions.Last().Packets.Add(new GameSession.Packet(time, false, data));
                            break;

                        case "HUGG":
                            if (status != Status.Kissed)
                            {
                                status = Status.Waiting;
                                break;
                            }

                            status = Status.Hugged;
                            sessions.Last().SequenceStart = Handshake.ReadSequenceStart(data);
                            sessions.Last().GameServerPort = Handshake.ReadGameServerPort(data);
                            sessions.Last().Packets.Add(new GameSession.Packet(time, true, data));
                            break;

                        case "ABRT":
                            if (status != Status.Waiting && sessions.Last().LocalIp != null)
                            {
                                sessions.Last().Packets.Add(new GameSession.Packet(time, ipPacket.DestinationAddress == sessions.Last().LocalIp, data));
                            }
                            break;
                    }
                }
                else if(data != null && status == Status.Hugged && sessions.Last().SocketID == MemoryMarshal.Read<int>(data))
                {
                    sessions.Last().Packets.Add(new GameSession.Packet(time, ipPacket.DestinationAddress == sessions.Last().LocalIp, data));
                }

            }
        }

        public enum Status
        {
            Waiting,
            Poked,
            Laughed,
            Kissed,
            Hugged,
        }
    }
}
