using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;
using Ninject.Syntax;
using Nito.AsyncEx;

namespace Ninject.Extensions.Azure.Clients
{
    /// <summary>
    /// Default implementation of <see cref="ICreateClientsAsync"/>.
    /// </summary>
    public class ClientFactory : ICreateClientsAsync, ICreateClients
    {
        private readonly IKernel _kernel;

        #region Locks

        private readonly object _serviceBusQueueLock = new object();
        private readonly AsyncLock _serviceBusQueueLockAsync = new AsyncLock();
        private readonly object _serviceBusSubLock = new object();
        private readonly AsyncLock _serviceBusSubLockAsync = new AsyncLock();
        private readonly object _serviceBusTopicLock = new object();
        private readonly AsyncLock _serviceBusTopicLockAsync = new AsyncLock();
        private readonly object _storageQueueLock = new object();
        private readonly AsyncLock _storageQueueLockAsync = new AsyncLock();

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
            if (TryGetFromKernel(_kernel, queueName, out client)) return client;

            lock (_storageQueueLock)
            {
                if (TryGetFromKernel(_kernel, queueName, out client)) return client;

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
            if (TryGetFromKernel(_kernel, queueName, out client)) return client;

            lock (_serviceBusQueueLock)
            {
                if (TryGetFromKernel(_kernel, queueName, out client)) return client;

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
            if (TryGetFromKernel(_kernel, topicName, out client)) return client;

            lock (_serviceBusTopicLock)
            {
                if (TryGetFromKernel(_kernel, topicName, out client)) return client;

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
            if (TryGetFromKernel(_kernel, subscriptionName, out client)) return client;

            lock (_serviceBusSubLock)
            {
                if (TryGetFromKernel(_kernel, subscriptionName, out client)) return client;

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
        /// Creates a cloud queue (Azure Storage Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the storage queue</param>
        /// <returns>CloudQueue</returns>
        public async Task<CloudQueue> CreateStorageQueueClientAsync(string queueName)
        {
            CloudQueue client;
            if (TryGetFromKernel(_kernel, queueName, out client)) return client;

            using (await _storageQueueLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (TryGetFromKernel(_kernel, queueName, out client)) return client;

                var queueClient = _kernel.Get<CloudQueueClient>();

                var queue = queueClient.GetQueueReference(queueName);
                await queue.CreateIfNotExistsAsync().ConfigureAwait(false);

                return queue;
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
            if (TryGetFromKernel(_kernel, queueName, out client)) return client;

            using (await _serviceBusQueueLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (TryGetFromKernel(_kernel, queueName, out client)) return client;

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
            if (TryGetFromKernel(_kernel, topicName, out client)) return client;

            using (await _serviceBusTopicLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (TryGetFromKernel(_kernel, topicName, out client)) return client;

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
            if (TryGetFromKernel(_kernel, subscriptionName, out client)) return client;

            using (await _serviceBusSubLockAsync.LockAsync().ConfigureAwait(false))
            {
                if (TryGetFromKernel(_kernel, subscriptionName, out client)) return client;

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

        private static bool TryGetFromKernel<T>(IResolutionRoot kernel, string nameOfBinding, out T client)
            where T : class
        {
            client = kernel.TryGet<T>(nameOfBinding);
            return client != null;
        }
    }
}