using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;
using Nito.AsyncEx;

namespace Ninject.Extensions.Azure.Clients
{
    /// <summary>
    /// Default implementation of <see cref="ICreateClientsAsync"/>.
    /// </summary>
    public sealed partial class ClientFactory : ICreateClientsAsync
    {
        #region Locks

        private readonly AsyncLock _serviceBusQueueLockAsync = new AsyncLock();
        private readonly AsyncLock _serviceBusSubLockAsync = new AsyncLock();
        private readonly AsyncLock _serviceBusTopicLockAsync = new AsyncLock();
        private readonly AsyncLock _storageQueueLockAsync = new AsyncLock();

        #endregion

        /// <summary>
        /// Creates an event processor host.
        /// </summary>
        /// <param name="eventHubPath">The path to the Event Hub from which to start receiving event data.</param>
        /// <param name="hostname">Base name for an instance of the host.</param>
        /// <param name="consumerGroupName">The name of the Event Hubs consumer group from which to start receiving event data.</param>
        /// <returns>A new EventProcessorHost</returns>
        public async Task<EventProcessorHost> CreateEventProcessorHostAsync(string eventHubPath, string hostname, string consumerGroupName = EventHubConsumerGroup.DefaultGroupName)
        {
            return await Task.Factory.StartNew(() => 
            {
                var combinedHostname = hostname + Guid.NewGuid();
                var storageConnectionString = _kernel.Get<Func<string>>("storageconnectionstring");
                var servicebusConnectionString = _kernel.Get<Func<string>>("servicebusconnectionstring");

                var eventProcessorHost = new EventProcessorHost(
                    combinedHostname, eventHubPath, consumerGroupName,
                    servicebusConnectionString(), storageConnectionString());
                return eventProcessorHost;
            });
        }

        /// <summary>
        /// Creates a cloud queue (Azure Storage Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the storage queue</param>
        /// <returns>CloudQueue</returns>
        public async Task<CloudQueue> CreateStorageQueueClientAsync(string queueName)
        {
            CloudQueue client;
            if (_kernel.TryGetFromKernel(queueName, out client)) return client;

            using (await _storageQueueLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (_kernel.TryGetFromKernel(queueName, out client)) return client;

                var queueClient = _kernel.Get<CloudQueueClient>();

                var queue = queueClient.GetQueueReference(queueName);
                await queue.CreateIfNotExistsAsync().ConfigureAwait(false);

                return queue;
            }
        }

        /// <summary>
        /// Creates an event hub client  given the hubs name.
        /// </summary>
        /// <param name="eventHubName">Name of the queue</param>
        /// <returns>EventHubClient</returns>
        public async Task<EventHubClient> CreateEventHubClientAsync(string eventHubName)
        {
            EventHubClient client;
            if (_kernel.TryGetFromKernel(eventHubName, out client)) return client;

            using (await _serviceBusQueueLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (_kernel.TryGetFromKernel(eventHubName, out client)) return client;

                var messagingFactory = _kernel.Get<MessagingFactory>();

                _kernel.Bind<EventHubClient>()
                    .ToMethod(context => messagingFactory.CreateEventHubClient(eventHubName))
                    .InSingletonScope()
                    .Named(eventHubName);

                client = _kernel.Get<EventHubClient>(eventHubName);
                return client;
            }
        }

        /// <summary>
        /// Creates a queue client (Azure Service Bus Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>QueueClient</returns>
        public async Task<QueueClient> CreateServicebusQueueClientAsync(string queueName)
        {
            QueueClient client;
            if (_kernel.TryGetFromKernel(queueName, out client)) return client;

            using (await _serviceBusQueueLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (_kernel.TryGetFromKernel(queueName, out client)) return client;

                var namespaceMgr = _kernel.Get<NamespaceManager>();

                if (!await namespaceMgr.QueueExistsAsync(queueName).ConfigureAwait(false))
                    await namespaceMgr.CreateQueueAsync(queueName).ConfigureAwait(false);

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
        public async Task<TopicClient> CreateTopicClientAsync(string topicName)
        {
            TopicClient client;
            if (_kernel.TryGetFromKernel(topicName, out client)) return client;

            using (await _serviceBusTopicLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (_kernel.TryGetFromKernel(topicName, out client)) return client;

                var messagingFactory = _kernel.Get<MessagingFactory>();

                var namespaceMgr = _kernel.Get<NamespaceManager>();
                if (!await namespaceMgr.TopicExistsAsync(topicName).ConfigureAwait(false))
                    await namespaceMgr.CreateTopicAsync(topicName).ConfigureAwait(false);

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
        public async Task<SubscriptionClient> CreateSubscriptionClientAsync(string topicName, string subscriptionName)
        {
            SubscriptionClient client;
            if (_kernel.TryGetFromKernel(subscriptionName, out client)) return client;

            using (await _serviceBusSubLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (_kernel.TryGetFromKernel(subscriptionName, out client)) return client;

                var messagingFactory = _kernel.Get<MessagingFactory>();

                var namespaceMgr = _kernel.Get<NamespaceManager>();
                if (!await namespaceMgr.TopicExistsAsync(topicName).ConfigureAwait(false))
                    await namespaceMgr.CreateTopicAsync(topicName).ConfigureAwait(false);

                if (!await namespaceMgr.SubscriptionExistsAsync(topicName, subscriptionName).ConfigureAwait(false))
                    await namespaceMgr.CreateSubscriptionAsync(topicName, subscriptionName).ConfigureAwait(false);

                _kernel.Bind<SubscriptionClient>()
                    .ToMethod(context => messagingFactory.CreateSubscriptionClient(topicName, subscriptionName))
                    .InSingletonScope()
                    .Named(subscriptionName);

                client = _kernel.Get<SubscriptionClient>(subscriptionName);
                return client;
            }
        }
    }
}