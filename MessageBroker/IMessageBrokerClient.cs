using UserService.Constants;
using UserService.Models;

namespace UserService.MessageBroker
{
    public interface IMessageBrokerClient
    {
        public void SendMessage(Message message);
        public ulong GetNextSequenceNumer();
    }
}
