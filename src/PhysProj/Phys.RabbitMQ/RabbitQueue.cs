﻿using Microsoft.Extensions.Logging;
using Phys.Queue;
using RabbitMQ.Client;

namespace Phys.RabbitMQ
{
    public class RabbitQueue : IMessageQueue
    {
        private readonly ILogger<RabbitQueue> log;
        private readonly Lazy<IConnection> connection;
        private readonly string prefix;

        public RabbitQueue(IConnectionFactory connectionFactory, string prefix, ILogger<RabbitQueue> log)
        {
            this.log = log;
            this.prefix = prefix;
            connection = new Lazy<IConnection>(connectionFactory.CreateConnection);
        }

        public IDisposable Consume(string queueName, IMessageConsumer consumer)
        {
            queueName = GetFullQueueName(queueName);
            var channel = EnsureChannel(queueName);
            var rabbitConsumer = new RabbitConsumer(channel, consumer, log);
            channel.BasicConsume(queueName, autoAck: false, rabbitConsumer);
            log.LogInformation($"BasicConsume '{queueName}'");
            return rabbitConsumer;
        }

        public void Publish(string queueName, ReadOnlyMemory<byte> message)
        {
            queueName = GetFullQueueName(queueName);
            using var channel = EnsureChannel(queueName);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            channel.BasicPublish(exchange: string.Empty, routingKey: queueName, body: message, basicProperties: properties);
            log.LogInformation($"BasicPublish '{queueName}'");
        }

        private IModel EnsureChannel(string queueName)
        {
            var channel = connection.Value.CreateModel();
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            log.LogInformation($"QueueDeclare '{queueName}'");
            return channel;
        }

        private string GetFullQueueName(string queueName)
        {
            return $"{prefix}.{queueName}";
        }

        private class RabbitConsumer : DefaultBasicConsumer, IDisposable
        {
            private readonly ILogger<RabbitQueue> log;
            private readonly IMessageConsumer consumer;
            private readonly IModel channel;

            public RabbitConsumer(IModel channel, IMessageConsumer consumer, ILogger<RabbitQueue> log)
            {
                this.channel = channel;
                this.consumer = consumer;
                this.log = log;
            }

            public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
            {
                log.LogInformation($"HandleBasicDeliver: routingKey {routingKey}");

                try
                {
                    consumer.Consume(body);
                    channel.BasicAck(deliveryTag, false);
                }
                catch (Exception e)
                {
                    log.LogError(e, "consume failed");
                    channel.BasicNack(deliveryTag, false, false);
                    throw;
                }
            }

            public override void HandleModelShutdown(object model, ShutdownEventArgs reason)
            {
                log.LogInformation($"HandleModelShutdown: {reason.ReplyText}");
                base.HandleModelShutdown(model, reason);
            }

            public void Dispose()
            {
                channel.Dispose();
            }
        }
    }
}