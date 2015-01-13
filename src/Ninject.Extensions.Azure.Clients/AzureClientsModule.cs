using System;
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
    /// Rebind if you need different setup.
    /// </summary>
    public class AzureClientsModule : NinjectModule
    {
        private readonly string _servicebusConnection =
            CloudConfigurationManager.GetSetting("ServiceBusConnectionString");

        private readonly string _storageConnection =
            CloudConfigurationManager.GetSetting("StorageConnectionString");

        /// <summary>
        /// Will setup bindings for stuff needed by the extension
        /// </summary>
        public override void Load()
        {
            Bind<ICreateClientsAsync, ICreateClients>()
                .To<ClientFactory>()
                .InSingletonScope();

            Bind<Func<string>>()
                .ToMethod(context => () => _storageConnection)
                .Named("storageconnectionstring");

            Bind<Func<string>>()
                .ToMethod(context => () => _servicebusConnection)
                .Named("servicebusconnectionstring");

            Bind<CloudStorageAccount>()
                .ToMethod(c =>
                {
                    var storageConnection = Kernel.Get<Func<string>>("storageconnectionstring");
                    var storageAccount = CloudStorageAccount.Parse(storageConnection());
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
                    var servicebusConnection = Kernel.Get<Func<string>>("servicebusconnectionstring");
                    var namespaceManager = NamespaceManager.CreateFromConnectionString(servicebusConnection());
                    return namespaceManager;
                });

            Bind<MessagingFactory>()
                .ToMethod(c =>
                {
                    var servicebusConnection = Kernel.Get<Func<string>>("servicebusconnectionstring");
                    var fac = MessagingFactory.CreateFromConnectionString(servicebusConnection());
                    return fac;
                });
        }
    }
}