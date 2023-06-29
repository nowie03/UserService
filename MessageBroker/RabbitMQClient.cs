using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using UserService.Constants;
using UserService.Context;
using UserService.Models;

namespace UserService.MessageBroker
{
    public class RabbitMQClient : IMessageBrokerClient , IDisposable
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private string _queueName = "service-queue";
        private readonly IServiceProvider _serviceProvider;
        

        public RabbitMQClient(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
                _channel.QueueDeclare(_queueName,exclusive: false);

                _channel.ConfirmSelect();

                _channel.BasicAcks += (sender, eventArgs) => HandleMessageAcknowledge(eventArgs.DeliveryTag, eventArgs.Multiple);


            }
            catch(BrokerUnreachableException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public ulong GetNextSequenceNumer()
        {
            return _channel.NextPublishSeqNo;
        }

        private async void HandleMessageAcknowledge(ulong currentSequenceNumber,bool multiple)
        {
            //if multiple is true all messages with sequenceNumber <currentSequenceNumber has been acknowledged
            if(multiple)
            {
                try
                {
                    Console.WriteLine("publish handler");
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ServiceContext>();


                    //retrieve all messages with sequenceNumber < currentSequenceNumber
                    //and set state from not acknowledged and acknowledged
                    await dbContext.Outbox
                        .Where(message => message.SequenceNumber <= currentSequenceNumber)
                        .ExecuteUpdateAsync(
                        entity=>entity.SetProperty(
                            message=>message.State,
                            EventStates.EVENT_ACK_COMPLETED));

                    await dbContext.SaveChangesAsync();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }


               

            }
            

        }


        public void SendMessage(Message message)
        {

            //Serialize the message

            if (_channel == null)
                return;

           string serializedOutBoxMessage = JsonConvert.SerializeObject(message);

           var body = Encoding.UTF8.GetBytes(serializedOutBoxMessage);

            
                //put the data on to the product queue
           _channel.BasicPublish(exchange: "", routingKey: _queueName, body: body);

            
           

        }

     
    }
}
