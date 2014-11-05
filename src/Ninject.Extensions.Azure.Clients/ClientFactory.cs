using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Ninject.Extensions.Azure.Clients
{
    /// <summary>
    /// Default implementation of <see cref="ICreateClientsAsync"/>.
    /// </summary>
    public class ClientFactory : ICreateClientsAsync, ICreateClients
    {
        private readonly IKernel _kernel;

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
            var client = _kernel.TryGet<CloudQueue>(queueName);
            if (client != null) return client;

            var queueClient = _kernel.Get<CloudQueueClient>();

            var queue = queueClient.GetQueueReference(queueName);
            queue.CreateIfNotExists();

            return queue;
        }

        /// <summary>
        /// Creates a queue client (Azure Service Bus Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>QueueClient</returns>
        public QueueClient CreateServicebusQueueClient(string queueName)
        {
            var client = _kernel.TryGet<QueueClient>(queueName);
            if (client != null) return client;

            var namespaceMgr = _kernel.Get<NamespaceManager>();

            if (! namespaceMgr.QueueExists(queueName))
                namespaceMgr.CreateQueue(queueName);

            var messagingFactory = _kernel.Get<MessagingFactory>();

            _kernel.Bind<QueueClient>()
                .ToMethod(context => messagingFactory.CreateQueueClient(queueName))
                .InSingletonScope()
                .Named(queueName);

            client = _kernel.Get<QueueClient>(queueName);
            return client;
        }

        /// <summary>
        /// Creates a topic client (Azure Service Bus Topic) given the topic name.
        /// </summary>
        /// <param name="topicName">Name of the topic</param>
        /// <returns>TopicClient</returns>
        public TopicClient CreateTopicClient(string topicName)
        {
            var client = _kernel.TryGet<TopicClient>(topicName);
            if (client != null) return client;

            var messagingFactory = _kernel.Get<MessagingFactory>();

            var namespaceMgr = _kernel.Get<NamespaceManager>();
            if (! namespaceMgr.TopicExists(topicName))
                namespaceMgr.CreateTopic(topicName);

            _kernel.Bind<TopicClient>()
                .ToMethod(context => messagingFactory.CreateTopicClient(topicName))
                .InSingletonScope()
                .Named(topicName);

            client = _kernel.Get<TopicClient>(topicName);
            return client;
        }

        /// <summary>
        /// Creates a subscription client (Azure Service Bus Topic) given the topic and subscription name.
        /// </summary>
        /// <param name="topicName">The topic to subscribe to</param>
        /// <param name="subscriptionName">The name of the subscription</param>
        /// <returns>SubscriptionClient</returns>
        public SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
        {
            var client = _kernel.TryGet<SubscriptionClient>(subscriptionName);
            if (client != null) return client;

            var messagingFactory = _kernel.Get<MessagingFactory>();

            var namespaceMgr = _kernel.Get<NamespaceManager>();
            if (! namespaceMgr.TopicExists(topicName))
                namespaceMgr.CreateTopic(topicName);

            if (! namespaceMgr.SubscriptionExists(topicName, subscriptionName))
                namespaceMgr.CreateSubscription(topicName, subscriptionName);

            _kernel.Bind<SubscriptionClient>()
                .ToMethod(context => messagingFactory.CreateSubscriptionClient(topicName, subscriptionName))
                .InSingletonScope()
                .Named(subscriptionName);

            client = _kernel.Get<SubscriptionClient>(subscriptionName);
            return client;
        }

        /// <summary>
        /// Creates a cloud queue (Azure Storage Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the storage queue</param>
        /// <returns>CloudQueue</returns>
        public async Task<CloudQueue> CreateStorageQueueClientAsync(string queueName)
        {
            var client = _kernel.TryGet<CloudQueue>(queueName);
            if (client != null) return client;

            var queueClient = _kernel.Get<CloudQueueClient>();

            var queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);

            return queue;
        }

        /// <summary>
        /// Creates a queue client (Azure Service Bus Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>QueueClient</returns>
        public async Task<QueueClient> CreateServicebusQueueClientAsync(string queueName)
        {
            var client = _kernel.TryGet<QueueClient>(queueName);
            if (client != null) return client;

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

        /// <summary>
        /// Creates a topic client (Azure Service Bus Topic) given the topic name.
        /// </summary>
        /// <param name="topicName">Name of the topic</param>
        /// <returns>TopicClient</returns>
        public async Task<TopicClient> CreateTopicClientAsync(string topicName)
        {
            var client = _kernel.TryGet<TopicClient>(topicName);
            if (client != null) return client;

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

        /// <summary>
        /// Creates a subscription client (Azure Service Bus Topic) given the topic and subscription name.
        /// </summary>
        /// <param name="topicName">The topic to subscribe to</param>
        /// <param name="subscriptionName">The name of the subscription</param>
        /// <returns>SubscriptionClient</returns>
        public async Task<SubscriptionClient> CreateSubscriptionClientAsync(string topicName, string subscriptionName)
        {
            var client = _kernel.TryGet<SubscriptionClient>(subscriptionName);
            if (client != null) return client;

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