﻿using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Ninject.Extensions.Azure.Clients
{
    /// <summary>
    /// Default implementation of <see cref="ICreateClients"/>.
    /// </summary>
    public sealed partial class ClientFactory : ICreateClients
    {
        private readonly IKernel _kernel;

        #region Locks

        private readonly object _serviceBusQueueLock = new object();
        private readonly object _serviceBusSubLock = new object();
        private readonly object _serviceBusTopicLock = new object();
        private readonly object _storageQueueLock = new object();

        #endregion

        /// <summary>
        /// Takes in the IKernel, required to resolve and add bindings.
        /// </summary>
        /// <param name="kernel"></param>
        public ClientFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// Creates a cloud queue (Azure Storage Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the storage queue</param>
        /// <returns>CloudQueue</returns>
        public CloudQueue CreateStorageQueueClient(string queueName)
        {
            CloudQueue client;
            if (_kernel.TryGetFromKernel(queueName, out client)) return client;

            lock (_storageQueueLock)
            {
                if (_kernel.TryGetFromKernel(queueName, out client)) return client;

                var queueClient = _kernel.Get<CloudQueueClient>();

                var queue = queueClient.GetQueueReference(queueName);
                queue.CreateIfNotExists();

                return queue;
            }
        }

        /// <summary>
        /// Creates a queue client (Azure Service Bus Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>QueueClient</returns>
        public QueueClient CreateServicebusQueueClient(string queueName)
        {
            QueueClient client;
            if (_kernel.TryGetFromKernel(queueName, out client)) return client;

            lock (_serviceBusQueueLock)
            {
                if (_kernel.TryGetFromKernel(queueName, out client)) return client;

                var namespaceMgr = _kernel.Get<NamespaceManager>();

                if (!namespaceMgr.QueueExists(queueName))
                    namespaceMgr.CreateQueue(queueName);

                var messagingFactory = _kernel.Get<MessagingFactory>();

                _kernel.Bind<QueueClient>()
                    .ToMethod(context => messagingFactory.CreateQueueClient(queueName))
                    .InSingletonScope()
                    .Named(queueName);

                client = _kernel.Get<QueueClient>(queueName);
                return client;
            }
        }

        /// <summary>
        /// Creates a topic client (Azure Service Bus Topic) given the topic name.
        /// </summary>
        /// <param name="topicName">Name of the topic</param>
        /// <returns>TopicClient</returns>
        public TopicClient CreateTopicClient(string topicName)
        {
            TopicClient client;
            if (_kernel.TryGetFromKernel(topicName, out client)) return client;

            lock (_serviceBusTopicLock)
            {
                if (_kernel.TryGetFromKernel(topicName, out client)) return client;

                var messagingFactory = _kernel.Get<MessagingFactory>();

                var namespaceMgr = _kernel.Get<NamespaceManager>();
                if (!namespaceMgr.TopicExists(topicName))
                    namespaceMgr.CreateTopic(topicName);

                _kernel.Bind<TopicClient>()
                    .ToMethod(context => messagingFactory.CreateTopicClient(topicName))
                    .InSingletonScope()
                    .Named(topicName);

                client = _kernel.Get<TopicClient>(topicName);
                return client;
            }
        }

        /// <summary>
        /// Creates a subscription client (Azure Service Bus Topic) given the topic and subscription name.
        /// </summary>
        /// <param name="topicName">The topic to subscribe to</param>
        /// <param name="subscriptionName">The name of the subscription</param>
        /// <returns>SubscriptionClient</returns>
        public SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
        {
            SubscriptionClient client;
            if (_kernel.TryGetFromKernel(subscriptionName, out client)) return client;

            lock (_serviceBusSubLock)
            {
                if (_kernel.TryGetFromKernel(subscriptionName, out client)) return client;

                var messagingFactory = _kernel.Get<MessagingFactory>();

                var namespaceMgr = _kernel.Get<NamespaceManager>();
                if (!namespaceMgr.TopicExists(topicName))
                    namespaceMgr.CreateTopic(topicName);

                if (!namespaceMgr.SubscriptionExists(topicName, subscriptionName))
                    namespaceMgr.CreateSubscription(topicName, subscriptionName);

                _kernel.Bind<SubscriptionClient>()
                    .ToMethod(context => messagingFactory.CreateSubscriptionClient(topicName, subscriptionName))
                    .InSingletonScope()
                    .Named(subscriptionName);

                client = _kernel.Get<SubscriptionClient>(subscriptionName);
                return client;
            }
        }

        /// <summary>
        /// Will create an event hub with default service bus credentials.
        /// </summary>
        /// <param name="eventHubName">Name of the hub</param>
        /// <returns>EventHubClient</returns>
        public EventHubClient CreatEventHubClient(string eventHubName)
        {
            EventHubClient client;
            if (_kernel.TryGetFromKernel(eventHubName, out client)) return client;

            lock (_storageQueueLock)
            {
                if (_kernel.TryGetFromKernel(eventHubName, out client)) return client;

                _kernel.Bind<EventHubClient>()
                    .ToMethod(context =>
                    {
                        var factory = _kernel.Get<MessagingFactory>();
                        return factory.CreateEventHubClient(eventHubName);
                    })
                    .InSingletonScope()
                    .Named(eventHubName);

                client = _kernel.Get<EventHubClient>(eventHubName);
                return client;
            }
        }
    }
}