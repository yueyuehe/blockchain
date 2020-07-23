using System;
namespace QMRaftCore.Infrastructure
{

    public abstract class Message
    {
        public Message(Guid messageId)
        {
            MessageId = messageId;
        }

        public Guid MessageId { get; private set; }
    }
}