﻿using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Phys.Queue
{
    public class JsonQueue : IObjectQueue
    {
        private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.WriteAsString | System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        };

        private readonly ILogger<JsonQueue> log;
        private readonly IMessageQueue queue;

        public JsonQueue(IMessageQueue queue, ILogger<JsonQueue> log)
        {
            this.queue = queue;
            this.log = log;
        }

        public IDisposable Consume<T>(string queueName, IObjectConsumer<T> consumer)
        {
            ArgumentNullException.ThrowIfNull(queueName);
            ArgumentNullException.ThrowIfNull(consumer);

            return queue.Consume(queueName, new JsonConsumer<T>(consumer, log));
        }

        public void Publish<T>(string queueName, T message)
        {
            ArgumentNullException.ThrowIfNull(queueName);
            ArgumentNullException.ThrowIfNull(message);

            queue.Publish(queueName, JsonSerializer.Serialize(message, serializerOptions));
        }

        private class JsonConsumer<T> : IMessageConsumer
        {
            private readonly ILogger<JsonQueue> log;
            private readonly IObjectConsumer<T> consumer;

            public JsonConsumer(IObjectConsumer<T> consumer, ILogger<JsonQueue> log)
            {
                this.consumer = consumer;
                this.log = log;
            }

            void IMessageConsumer.Consume(string message)
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<T>(message, serializerOptions)!;
                    consumer.Consume(obj);
                }
                catch (Exception e)
                {
                    log.LogError(e, $"failed consume {typeof(T)}");
                }
            }
        }
    }
}
