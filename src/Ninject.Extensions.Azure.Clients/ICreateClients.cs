using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Ninject.Extensions.Azure.Clients
{
    /// <summary>
    /// Definition of the factory.
    /// </summary>
    public interface ICreateClients
    {
        /// <summary>
        /// Creates a cloud queue (Azure Storage Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the storage queue</param>
        /// <returns>CloudQueue</returns>
        CloudQueue CreateStorageQueueClient(string queueName);

        /// <summary>
        /// Creates a queue client (Azure Service Bus Queue) given the queue name.
        /// </summary>
        /// <param name="queueName">Name of the queue</param>
        /// <returns>QueueClient</returns>
        QueueClient CreateServicebusQueueClient(string queueName);

        /// <summary>
        /// Creates a topic client (Azure Service Bus Topic) given the topic name.
        /// </summary>
        /// <param name="topicName">Name of the topic</param>
        /// <returns>TopicClient</returns>
        TopicClient CreateTopicClient(string topicName);

        /// <summary>
        /// Creates a subscription client (Azure Service Bus Topic) given the topic and subscription name.
        /// </summary>
        /// <param name="topicName">The topic to subscribe to</param>
        /// <param name="subscriptionName">The name of the subscription</param>
        /// <returns>SubscriptionClient</returns>
        SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName);

        /// <summary>
        /// Creates an Event Hub client, given eventHubName
        /// </summary>
        /// <param name="eventHubName"></param>
        /// <returns></returns>
        EventHubClient CreatEventHubClient(string eventHubName);
    }
}