using System;

namespace FauCap
{
    public sealed class GamePacketBuffer : IDisposable
    {
        private bool disposed;
        private ushort mask;
        private GamePacket[] buffer;
        public GamePacketBuffer(ushort size = 1024)
        {
            mask = (ushort)(size - 1);
            buffer = new GamePacket[size];
        }
        
        public bool Has(ushort i)
        {
            return buffer[i & mask]?.SequenceNumber == i;
        }

        public void Free(ushort i)
        {
            if (Has(i))
            {
                buffer[i & mask] = null;
            }
        }

        public void Clear()
        {
            for(int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != null)
                {
                    buffer[i] = null;
                }
                
            }
        }

        public GamePacket this[ushort i]
        {
            get
            {
                return Has(i) ? buffer[i & mask] : null;
            }
            set
            {
                buffer[i & mask] = value;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                Clear();
            }
            disposed = true;
        }

        ~GamePacketBuffer()
        {
            Dispose(false);
        }
    }
}