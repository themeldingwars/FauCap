using System;

namespace FauCap
{
    public class ReliableGamePacketInputQueue
    {
        private GamePacketBuffer buffer;
        private ushort size;
        private ushort tail;
        private ushort head;
        private ushort mask;
        private object mutex;

        // size must be a power of 2
        public ReliableGamePacketInputQueue(ushort sequenceStart, ushort size = 1024)
        {
            buffer = new GamePacketBuffer(size);
            head = (ushort)unchecked(sequenceStart - 1);
            this.size = size;
            tail = head;
            mask = (ushort)(size - 1);
            mutex = new object(); 
        }
        
        public bool PacketsAvailable => head != tail;

        public bool Has(ushort id)
        {
            return buffer.Has(id);
        }
        public void Free(ushort id)
        {
            buffer.Free(id);
        }

        public EnqueueResult Enqueue(GamePacket packet)
        {
            lock (mutex)
            {
                // check if the buffer would overflow if we add the packet
                if (unchecked(tail+size) == packet.SequenceNumber)
                {
                    return EnqueueResult.BufferOverflow;
                }

                // check if we already have it
                if (Has(packet.SequenceNumber))
                {
                    return EnqueueResult.Duplicate;
                }
                
                // check if the packet is within accepted range
                bool seqOk = false;
                for (int i = 1; i < size; i++)
                {
                    if (unchecked((ushort)(head + i)) == packet.SequenceNumber)
                    {
                        seqOk = true;
                        break;
                    }
                }
                if (!seqOk)
                {
                    return EnqueueResult.OutOfSequence;
                }
                
                buffer[packet.SequenceNumber] = packet;

                // move head forward
                ushort pointer = head;
                unchecked { pointer++; }
                
                while (buffer.Has(pointer))
                {
                    if (!buffer[pointer].IsSplit)
                    {
                        head = pointer;
                    }
                    unchecked { pointer++; }
                }

                return EnqueueResult.Ok;
            }
        }
        public bool TryDequeue(out GamePacket packet)
        {
            lock (mutex)
            {
                if (PacketsAvailable)
                {
                    unchecked { tail++; }
                    packet = buffer[tail];
                    //buffer.Free(tail);
                    return true;
                }
            }
            packet = null;
            return false;
        }
        public void Reset(ushort sequenceStart)
        {
            head = (ushort)unchecked(sequenceStart - 1);
            tail = head;
            buffer.Clear();
        }

        public enum EnqueueResult
        {
            Ok,
            Duplicate,
            OutOfSequence,
            BufferOverflow
        }
    }
}