﻿using Microsoft.Extensions.Logging;
using Phys.Shared.Queue;
using System.Text.Json;

namespace Phys.Queue
{
    public class JsonQueue : IQueue
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

        public IDisposable Consume<T>(IQueueConsumer<T> consumer)
        {
            ArgumentNullException.ThrowIfNull(consumer);

            return queue.Consume(consumer.QueueName, new JsonConsumer<T>(consumer, log));
        }

        public void Send<T>(T message) where T : IQueueMessage
        {
            ArgumentNullException.ThrowIfNull(message);

            queue.Publish(message.QueueName, JsonSerializer.SerializeToUtf8Bytes(message, serializerOptions));
        }

        private class JsonConsumer<T> : IMessageConsumer
        {
            private readonly ILogger<JsonQueue> log;
            private readonly IQueueConsumer<T> consumer;

            public JsonConsumer(IQueueConsumer<T> consumer, ILogger<JsonQueue> log)
            {
                this.consumer = consumer;
                this.log = log;
            }

            void IMessageConsumer.Consume(ReadOnlyMemory<byte> message)
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<T>(message.Span, serializerOptions)!;
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
