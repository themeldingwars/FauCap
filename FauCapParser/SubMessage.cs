using System;

namespace FauCap
{
    public class SubMessage : GameMessage
    {
        public GameMessage ParentMessage;
        public int         ParentMessageIdx;
        public byte[]      SubMessageData;
        public ulong       EntityId;

        public SubMessage(int id, GameMessage parentMessage, Span<byte> data, ulong entityId) : base(id)
        {
            ParentMessage    = parentMessage;
            ParentMessageIdx = parentMessage.Id;
            SubMessageData   = data.ToArray();
            EntityId         = entityId;
        }

        public override Server     Server     => ParentMessage.Server;
        public override bool       FromServer => ParentMessage.FromServer;
        public override DateTime   Time       => ParentMessage.Time;
        public override Span<byte> Raw        => SubMessageData;
        public override Span<byte> Data       => SubMessageData;
            
        public override Channel Channel     => ParentMessage.Channel;
        public override bool    IsSplit     => ParentMessage.IsSplit;
        public override bool    IsReliable  => ParentMessage.IsReliable;
        public override bool    IsSequenced => ParentMessage.IsSequenced;
    }
}