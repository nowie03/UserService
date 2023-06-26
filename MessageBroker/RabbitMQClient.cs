using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using UserService.Models;

namespace UserService.MessageBroker
{
    public class RabbitMQClient : IMessageBrokerClient , IDisposable
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private string _queueName = "service-queue";

        public RabbitMQClient()
        {
            SetupClient();
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        private void SetupClient()
        {
            try
            {
                //Here we specify the Rabbit MQ Server. we use rabbitmq docker image and use it
                _connectionFactory = new ConnectionFactory
                {
                    HostName = "localhost"
                };
                //Create the RabbitMQ connection using connection factory details as i mentioned above
                _connection = _connectionFactory.CreateConnection();
                //Here we create channel with session and model
                _channel = _connection.CreateModel();
                //declare the queue after mentioning name and a few property related to that
                _channel.QueueDeclare(_queueName, exclusive: false);
            }catch(BrokerUnreachableException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public void SendMessage<T>(T message, string eventType)
        {

            //Serialize the message

            if (_channel == null)
                return;


            Message<T> eventMessage = new Message<T>(eventType, message);

            string json = JsonConvert.SerializeObject(eventMessage);

            var body = Encoding.UTF8.GetBytes(json);


            //put the data on to the product queue
            _channel.BasicPublish(exchange: "", routingKey: _queueName, body: body);
        }

     
    }
}
