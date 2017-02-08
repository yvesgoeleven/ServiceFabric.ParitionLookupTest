using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using NServiceBus;

namespace Stateful1
{
    using System;
    using System.Linq;

    public class MyCommunicationListener : ICommunicationListener
    {
        private EndpointConfiguration _endpointConfiguration;
        private IEndpointInstance _endpointInstance;

        public MyCommunicationListener(StatefulServiceContext context)
        {
            var client = new FabricClient();
            var servicePartitionList = client.QueryManager.GetPartitionListAsync(new Uri("fabric:/PartitionedSpike/Stateful1"), context.PartitionId).GetAwaiter().GetResult();
            var rangePartitionInformation = servicePartitionList.Select(x => x.PartitionInformation).Cast<Int64RangePartitionInformation>().Single(p => p.Id == context.PartitionId);

            _endpointConfiguration = new EndpointConfiguration(endpointName: "PartionedSpike.RangedServer");
            _endpointConfiguration.MakeInstanceUniquelyAddressable(rangePartitionInformation.HighKey.ToString());
            _endpointConfiguration.SendFailedMessagesTo("error");
            _endpointConfiguration.AuditProcessedMessagesTo("audit");
            _endpointConfiguration.UseSerialization<JsonSerializer>();
            _endpointConfiguration.EnableInstallers();
            _endpointConfiguration.UsePersistence<InMemoryPersistence>();
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
}