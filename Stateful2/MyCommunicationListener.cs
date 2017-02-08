using System;
using System.Fabric;
using System.Fabric.Query;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace Stateful2
{
    public class MyCommunicationListener : ICommunicationListener
    {
        private EndpointConfiguration _endpointConfiguration;
        private IEndpointInstance _endpointInstance;

        public MyCommunicationListener(StatefulServiceContext context)
        {
            _endpointConfiguration = new EndpointConfiguration(endpointName: "PartionedSpike.NamedServer");
            _endpointConfiguration.SendFailedMessagesTo("error");
            _endpointConfiguration.AuditProcessedMessagesTo("audit");
            _endpointConfiguration.UseSerialization<JsonSerializer>();
            _endpointConfiguration.EnableInstallers();
            _endpointConfiguration.UsePersistence<InMemoryPersistence>();
            _endpointConfiguration.MakeInstanceUniquelyAddressable(context.PartitionId.ToString());
            _endpointConfiguration.GetSettings().Set("PartitionId", context.PartitionId);
            _endpointConfiguration.RegisterComponents(components => components.RegisterSingleton(context));

            var transportConfig = _endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transportConfig.ConnectionString("");
            transportConfig.UseForwardingTopology();
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            _endpointInstance = await Endpoint.Start(_endpointConfiguration).ConfigureAwait(false);
            return null;
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            await _endpointInstance.Stop();
        }

        public void Abort()
        {
            //stop?
        }
    }

    /// <summary>
    ///  Idially this is shared with sender side distribution
    /// </summary>
    public static class PartitionMap
    {
        private static string[] namedPartitions;

        static PartitionMap()
        {
            namedPartitions = new[] { "a", "b", "c" };
        }

        public static object Map(object message)
        {
            var random = new Random();
            if (message is MyNotProperlyRoutedMessage)
            {
                return namedPartitions[random.Next(0, 3)];
            }
            return random.Next(0, 300);
        }
    }

    class ServerSideDistributionFeature : Feature
    {
        public ServerSideDistributionFeature()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(b => new ServerSideDistributor(
                b.Build<StatefulServiceContext>(), 
                address => context.Settings.Get<TransportInfrastructure>().ToTransportAddress(address), 
                context.Settings.LogicalAddress(),
                context.Settings.Get<Guid>("PartitionId")), "Foo");
        }
    }

    class ServerSideDistributor : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        private StatefulServiceContext statefulServiceContext;
        private Func<LogicalAddress, string> addressTranslator;
        private Guid currentPartition;
        private LogicalAddress logicalAddress;
        private readonly ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();

        public ServerSideDistributor(StatefulServiceContext context, Func<LogicalAddress, string> addressTranslator, LogicalAddress local, Guid currentPartition)
        {
            logicalAddress = local;
            this.currentPartition = currentPartition;
            this.addressTranslator = addressTranslator;
            statefulServiceContext = context;
        }

        public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            var contextMessage = context.Message;

            if (contextMessage.MessageType != typeof(MyNotProperlyRoutedMessage))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            var partitionValue = PartitionMap.Map(contextMessage.Instance);
            // Think about custom header or smart caching...
            var partitionKey = partitionValue is string
                ? new ServicePartitionKey((string) partitionValue)
                : new ServicePartitionKey((int) partitionValue);
            var partition1 =
                await this.servicePartitionResolver.ResolveAsync(statefulServiceContext.ServiceName, partitionKey,
                    CancellationToken.None).ConfigureAwait(false);


            if (partition1.Info.Id != currentPartition)
            {
                ServiceEventSource.Current.ServiceMessage(statefulServiceContext,
                    $"Received MyNotProperlyRoutedMessage on partition {currentPartition} rerouting to {partition1.Info.Id}");
                var destination =
                    addressTranslator(logicalAddress.CreateIndividualizedAddress(partition1.Info.Id.ToString()));
                await context.ForwardCurrentMessageTo(destination).ConfigureAwait(false);
                return;
            }
            await next(context).ConfigureAwait(false);
        }
    }
}