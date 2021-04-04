using System;
using System.Collections.Generic;
using System.Diagnostics;
using FauCap;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string file = @"sadface.faucap"; // path to some faucap

            Stopwatch sw = Stopwatch.StartNew();
            
            List<GameSession> sessions = GameSession.Read(file, false);
            
            Console.WriteLine("Parse time: " + sw.ElapsedMilliseconds + "ms");
            

            int x = 0;
            foreach (GameSession session in sessions)
            {
                Console.WriteLine("Session: " + x);
                Console.WriteLine("Dgrm: " + session.Datagrams.Count);
                Console.WriteLine("Pckt: " + session.Packets.Count);
                Console.WriteLine("Msgs: " + session.Messages.Count);
                Console.WriteLine("------------------------");
                x++;
            }
            
        }
    }
}