namespace UserService.MessageBroker
{
    public interface IMessageBrokerClient
    {
        public void SendProductMessage<T>(T message, string eventType);

        public void ReceiveMessage();
    }
}
