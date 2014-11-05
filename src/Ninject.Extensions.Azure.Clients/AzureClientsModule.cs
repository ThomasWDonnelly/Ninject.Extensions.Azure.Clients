using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Ninject.Modules;

namespace Ninject.Extensions.Azure.Clients
{
    /// <summary>
    /// Module that sets up all the bindings the extension needs.
    /// It will only bind types that have not yet been bound. Recommend that you load it last.
    /// </summary>
    public class AzureClientsModule : NinjectModule
    {
        private readonly string _servicebusConnection =
            CloudConfigurationManager.GetSetting("ServiceBusConnectionString");

        private readonly string _storageConnection =
            CloudConfigurationManager.GetSetting("StorageConnectionString");

        public override void Load()
        {
            Bind<ICreateClients>().To<ClientFactory>();

            Bind<CloudStorageAccount>()
                .ToMethod(c =>
                {
                    var storageAccount = CloudStorageAccount.Parse(_storageConnection);
                    return storageAccount;
                });

            Bind<CloudQueueClient>()
                .ToMethod(c =>
                {
                    var storageAccount = Kernel.Get<CloudStorageAccount>();
                    var queueClient = storageAccount.CreateCloudQueueClient();
                    return queueClient;
                });

            Bind<NamespaceManager>()
                .ToMethod(c =>
                {
                    var namespaceManager = NamespaceManager.CreateFromConnectionString(_servicebusConnection);
                    return namespaceManager;
                });

            Bind<MessagingFactory>()
                .ToMethod(c =>
                {
                    var fac = MessagingFactory.CreateFromConnectionString(_servicebusConnection);
                    return fac;
                });
        }
    }
}