﻿using Microsoft.Extensions.Logging;
using Phys.Queue;
using Phys.RabbitMQ;
using RabbitMQ.Client;

namespace Phys.Tests.Queue
{
    public class RabbitQueueTests : QueueTests
    {
        protected override IMessageQueue CreateQueue() => new RabbitQueue(new ConnectionFactory { HostName = "localhost" }, loggerFactory.CreateLogger<RabbitQueue>());
    }
}