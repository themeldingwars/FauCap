using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static FauCap.PacketUtil;

namespace FauCap
{
    public class Converter
    {
        List<GameSession> Sessions = new List<GameSession>();
        private Status    CurrentStatus;
        private int       Idx = 0;

        public List<GameSession> PcapFileToFaucap(string InFile)
        {
            Idx           = 0;
            Sessions      = new List<GameSession>();
            CurrentStatus = Status.Waiting;

            CaptureFileReaderDevice device;
            try
            {
                device = new CaptureFileReaderDevice(InFile);
                device.Open();
                Console.WriteLine($"Parsing pcap file {InFile}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not parse {InFile}, is it a valid pcap capture?");
                Console.WriteLine(e);
                return null;
            }

            device.OnPacketArrival += new PacketArrivalEventHandler(OnPacketArrival);
            device.Capture();
            device.Close();

            Console.WriteLine($"Done parsing {InFile}, " + (Sessions.Count > 0 ? $"{Sessions.Count} sessions was found, exporting faucap." : "but no game sessions was found."));

            foreach (var session in Sessions) {
                session.Reassemble(false);
            }

            return Sessions;
        }

        void OnPacketArrival(object sender, CaptureEventArgs e)
        {
            if (e.Packet.LinkLayerType == PacketDotNet.LinkLayers.Ethernet)
            {
                var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

                var udpPacket = (PacketDotNet.UdpPacket)packet.Extract<PacketDotNet.UdpPacket>();

                var ipPacket = (PacketDotNet.IPv4Packet)packet.Extract<PacketDotNet.IPv4Packet>();

                if (udpPacket == null) { return; }
                byte[] data = udpPacket.PayloadData;

                DateTime time = e.Packet.Timeval.Date;

                if (IsHandshakePacket(data))
                {
                    switch (Handshake.ReadName(data))
                    {
                        case "POKE":
                            CurrentStatus = Status.Poked;
                            GameSession session = new GameSession();
                            session.Datagrams.Add(new Datagram(Idx++, time, false, data));

                            session.LocalIp = ipPacket.SourceAddress;
                            session.RemoteIp = ipPacket.DestinationAddress;
                            session.MatrixPort = udpPacket.DestinationPort;

                            session.ProtocolVersion = Handshake.ReadProtocolVersion(data);

                            Sessions.Add(session);
                            break;

                        case "HEHE":
                            if (CurrentStatus != Status.Poked)
                            {
                                CurrentStatus = Status.Waiting;
                                break;
                            }

                            CurrentStatus = Status.Laughed;
                            Sessions.Last().SocketID = Handshake.ReadSocketId(data);
                            Sessions.Last().Datagrams.Add(new Datagram(Idx++, time, true, data));
                            break;

                        case "KISS":
                            if (CurrentStatus != Status.Laughed)
                            {
                                CurrentStatus = Status.Waiting;
                                break;
                            }

                            CurrentStatus = Status.Kissed;
                            Sessions.Last().StreamingProtocol = Handshake.ReadStreamingProtocol(data);
                            Sessions.Last().Datagrams.Add(new Datagram(Idx++, time, false, data));
                            break;

                        case "HUGG":
                            if (CurrentStatus != Status.Kissed)
                            {
                                CurrentStatus = Status.Waiting;
                                break;
                            }

                            CurrentStatus = Status.Hugged;
                            Sessions.Last().SequenceStart = Handshake.ReadSequenceStart(data);
                            Sessions.Last().GameServerPort = Handshake.ReadGameServerPort(data);
                            Sessions.Last().Datagrams.Add(new Datagram(Idx++, time, true, data));
                            break;

                        case "ABRT":
                            if (CurrentStatus != Status.Waiting && Sessions.Last().LocalIp != null)
                            {
                                Sessions.Last().Datagrams.Add(new Datagram(Idx++, time, ipPacket.DestinationAddress  == Sessions.Last().LocalIp, data));
                            }
                            break;
                    }
                }
                else if (data != null && CurrentStatus == Status.Hugged && Sessions.Last().SocketID == MemoryMarshal.Read<uint>(data))
                {
                    bool fromServer = ipPacket.DestinationAddress.Address == Sessions.Last().LocalIp.Address;
                    Sessions.Last().Datagrams.Add(new Datagram(Idx++, time, fromServer, data));
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
