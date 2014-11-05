using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Ninject.Extensions.Azure.Clients
{
    /// <summary>
    /// Definition of the factory.
    /// </summary>
    public interface ICreateClientsAsync
    {
        /// <summary>
        /// Creates a cloud queue (Azure Storage Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the storage queue</param>
        /// <returns>CloudQueue</returns>
        Task<CloudQueue> CreateStorageQueueClientAsync(string queueName);
        /// <summary>
        /// Creates a queue client (Azure Service Bus Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>QueueClient</returns>
        Task<QueueClient> CreateServicebusQueueClientAsync(string queueName);
        /// <summary>
        /// Creates a topic client (Azure Service Bus Topic) given the topic name.
        /// </summary>
        /// <param name="topicName">Name of the topic</param>
        /// <returns>TopicClient</returns>
        Task<TopicClient> CreateTopicClientAsync(string topicName);
        /// <summary>
        /// Creates a subscription client (Azure Service Bus Topic) given the topic and subscription name.
        /// </summary>
        /// <param name="topicName">The topic to subscribe to</param>
        /// <param name="subscriptionName">The name of the subscription</param>
        /// <returns>SubscriptionClient</returns>
        Task<SubscriptionClient> CreateSubscriptionClientAsync(string topicName, string subscriptionName);
    }
}