using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using NServiceBus;

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
            _endpointConfiguration.RegisterComponents(components => components.RegisterSingleton(context));

            var transportConfig = _endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transportConfig.ConnectionString("Endpoint=sb://servicebus-unittesting.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=y15MuGqqMKL67kUrKPdKiq+kPBrhW+774NiDVXsjSDU=");
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