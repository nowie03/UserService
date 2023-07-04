﻿using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using UserService.Context;
using UserService.MessageBroker;
using UserService.Models;

namespace UserService.BackgroundServices
{
    public class PublishMessageToQueue : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<Guid, IMessageBrokerClient> _rabbitMQConnections;
        private readonly Guid _scopeKey = Guid.NewGuid();


        public PublishMessageToQueue(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _rabbitMQConnections = new ConcurrentDictionary<Guid, IMessageBrokerClient>();
        }

        private IMessageBrokerClient GetScopedMessageBrokerClient(IServiceProvider serviceProvider)
        {
            if (!_rabbitMQConnections.TryGetValue(_scopeKey, out var messageBrokerClient))
            {
                // Create and cache the scoped message broker client
                messageBrokerClient = serviceProvider.GetRequiredService<IMessageBrokerClient>();
                _rabbitMQConnections.TryAdd(_scopeKey, messageBrokerClient);
            }

            return messageBrokerClient;
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var scope = _serviceProvider.CreateScope();
                try
                {


                    //cache the services so that they dont get created everytime
                    var dbContext = scope.ServiceProvider.GetRequiredService<ServiceContext>();

                    var messageBrokerClient = GetScopedMessageBrokerClient(scope.ServiceProvider);

                    //get pending ack messages and publish it to queue
                    IEnumerable<Message> pendingMessages = await dbContext.Outbox.Where(message => message.State == Constants.EventStates.EVENT_ACK_PENDING)
                        .ToListAsync();



                    foreach (var pendingMessage in pendingMessages)
                    {

                        messageBrokerClient.SendMessage(pendingMessage);

                    }




                }


                catch (Exception ex)
                {
                    Console.WriteLine($"error when publishing messages to queue {ex.Message}");
                }


                await Task.Delay(5000, stoppingToken);

            }
        }
    }
}
