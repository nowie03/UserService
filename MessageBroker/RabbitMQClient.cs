using Microsoft.AspNetCore.Connections;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;
using UserService.Models;

namespace UserService.MessageBroker
{
    public class RabbitMQClient
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private string _queueName="service-queue";
        private void  _setupCleint() {
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
        }
        public void SendProductMessage<T>(T message,string eventType)
        {
            _setupCleint();

            //Serialize the message


            Message<T> eventMessage=new Message<T>(eventType, message);

            string json = JsonConvert.SerializeObject(eventMessage);

            var body = Encoding.UTF8.GetBytes(json);


            //put the data on to the product queue
            _channel.BasicPublish(exchange: "", routingKey: _queueName, body: body);
        }

        public void ReceiveMessage()
        {
            _setupCleint();
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Product message received: {message}");
            };
            //read the message
            _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);

        }
    }
}
