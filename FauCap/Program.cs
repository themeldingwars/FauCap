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

            var sessions = new Converter().PcapFileToFaucap(inFile);
            GameSession.Write(outFile, sessions);
        }
    }
}
