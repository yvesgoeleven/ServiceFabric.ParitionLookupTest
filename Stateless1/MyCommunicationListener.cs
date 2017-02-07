using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Routing;

namespace Stateless1
{
    public class MyCommunicationListener : ICommunicationListener
    {
        private readonly EndpointConfiguration _endpointConfiguration;
        private IEndpointInstance _endpointInstance;

        public MyCommunicationListener(StatelessServiceContext context)
        {
            _endpointConfiguration = new EndpointConfiguration(endpointName: "PartionedSpike.Client");
            _endpointConfiguration.SendFailedMessagesTo("error");
            _endpointConfiguration.AuditProcessedMessagesTo("audit");
            _endpointConfiguration.UseSerialization<JsonSerializer>();
            _endpointConfiguration.EnableInstallers();
            _endpointConfiguration.UsePersistence<InMemoryPersistence>();
            var transportConfig = _endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transportConfig.ConnectionString("");
            transportConfig.UseForwardingTopology();
            var routingSettings = transportConfig.Routing();

            routingSettings.RouteToEndpoint(typeof(MyRangedMessage), "PartionedSpike.RangedServer");
            routingSettings.RouteToEndpoint(typeof(MyNamedMessage), "PartionedSpike.NamedServer");

            var internalSettings = _endpointConfiguration.GetSettings();
            var routingTable = internalSettings.GetOrCreate<UnicastRoutingTable>();
            var addressMap = new Dictionary<string, string>()
            {
                {"PartionedSpike.RangedServer", @"fabric:/PartitionedSpike/Stateful1"},
                {"PartionedSpike.NamedServer", @"fabric:/PartitionedSpike/Stateful2"}
            };

            var namedPartitions = new[] {"a", "b", "c"};
            Func<object, object> partitionMap = o =>
            {
                var random = new Random();
                if (o is MyNamedMessage)
                {
                    return namedPartitions[random.Next(0, 3)];
                }
                else
                {
                    return random.Next(0, 300);
                }
            };
            _endpointConfiguration.Pipeline.Register(new MyBehavior(routingTable, partitionMap, addressMap), "MyBehavior");
            
            var policy = internalSettings.GetOrCreate<DistributionPolicy>();

            policy.SetDistributionStrategy(new MyDistributionStrategy("PartionedSpike.RangedServer", DistributionStrategyScope.Send, context));
            policy.SetDistributionStrategy(new MyDistributionStrategy("PartionedSpike.NamedServer", DistributionStrategyScope.Send, context));

            var instances = internalSettings.GetOrCreate<EndpointInstances>();
            foreach (var address in addressMap)
            {
                PopulateInstances(instances, address);
            }
        }

        private void PopulateInstances(EndpointInstances instances, KeyValuePair<string, string> address)
        {
            var partitionedEndpoints = new List<EndpointInstance>();

            var serviceName = new Uri(address.Value);
            using (var client = new FabricClient())
            {
                var partitions = client.QueryManager.GetPartitionListAsync(serviceName).GetAwaiter().GetResult();

                foreach (var partition in partitions)
                {
                    partitionedEndpoints.Add(new EndpointInstance(address.Key, partition.PartitionInformation.Id.ToString()));
                }
            }

            instances.AddOrReplaceInstances(address.Value, partitionedEndpoints);
        }

        public IEndpointInstance EndpointInstance => _endpointInstance;

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
}