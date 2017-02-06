using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using NServiceBus;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Routing;

namespace Stateless1
{
    public class MyCommunicationListener : ICommunicationListener
    {
        private EndpointConfiguration _endpointConfiguration;
        private IEndpointInstance _endpointInstance;

        public MyCommunicationListener()
        {
            _endpointConfiguration = new EndpointConfiguration(endpointName: "PartionedSpike.Client");
            _endpointConfiguration.SendFailedMessagesTo("error");
            _endpointConfiguration.AuditProcessedMessagesTo("audit");
            _endpointConfiguration.UseSerialization<JsonSerializer>();
            _endpointConfiguration.EnableInstallers();
            _endpointConfiguration.UsePersistence<InMemoryPersistence>();
            var transportConfig = _endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transportConfig.ConnectionString("Endpoint=sb://servicebus-unittesting.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=y15MuGqqMKL67kUrKPdKiq+kPBrhW+774NiDVXsjSDU=");
            transportConfig.UseForwardingTopology();
            var routingSettings = transportConfig.Routing();
            routingSettings.RouteToEndpoint(typeof(MyMessage), "PartionedSpike.RangedServer");

            var internalSettings = _endpointConfiguration.GetSettings();
            var policy = internalSettings.Get<DistributionPolicy>();
            policy.SetDistributionStrategy(new MyDistributionStrategy("PartionedSpike.RangedServer", DistributionStrategyScope.Send));
            // var instances = internalSettings.GetOrCreate<EndpointInstances>();

            //PopulateInstances(instances).GetAwaiter().GetResult();

            // routingSettings.InstanceMappingFile();
        }

        //private async Task PopulateInstances(EndpointInstances instances)
        //{
        //    var partitionedEndpoints = new List<EndpointInstance>();

        //    var serviceName = new Uri(@"fabric:/PartitionedSpike/Stateful1");
        //    using (var client = new FabricClient())
        //    {
        //        var partitions = await client.QueryManager.GetPartitionListAsync(serviceName);

        //        foreach (var partition in partitions)
        //        {
        //            var partitionInformation = (Int64RangePartitionInformation)partition.PartitionInformation;
        //            partitionedEndpoints.Add(new EndpointInstance(partitionInformation.Id.ToString(), "PartionedSpike.RangedServer")
        //            {
                        
        //            });
        //        }
        //    }

        //    instances.AddOrReplaceInstances("Partitions", partitionedEndpoints);
        //}

        public IEndpointInstance EndpointInstance
        {
            get { return _endpointInstance; }
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

        /*

    

    // Use JSON to serialize and deserialize messages (which are just
    // plain classes) to and from message queues
    

    // Ask NServiceBus to automatically create message queues
    

    // Store information in memory for this example, rather than in
    // a database. In this sample, only subscription information is stored
    

    // Initialize the endpoint with the finished configuration
    var endpointInstance = await Endpoint.Start(endpointConfiguration)
        .ConfigureAwait(false);
    try
    {
        await SendOrder(endpointInstance);
    }
    finally
    {
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
         
         */
    }

    //public class ListInstances: FeatureStartupTask
    //{
    //    protected override Task OnStart(IMessageSession session)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    protected override Task OnStop(IMessageSession session)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}

    //public class ListInstancesFeature : Feature
    //{
    //    protected override void Setup(FeatureConfigurationContext context)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
}