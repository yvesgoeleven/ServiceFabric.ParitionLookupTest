using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using NServiceBus;
using NServiceBus.Routing;

namespace Stateless1
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Stateless1 : StatelessService
    {
        private static readonly Uri rangedServiceUri = new Uri(@"fabric:/PartitionedSpike/Stateful1");
        private static readonly Uri namedServiceUri = new Uri(@"fabric:/PartitionedSpike/Stateful2");
        private readonly ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();
        private MyCommunicationListener _listener;

        public Stateless1(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            _listener = new MyCommunicationListener();
            return new List<ServiceInstanceListener>() { new ServiceInstanceListener(context => _listener) };
        }

       
        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var random = new Random();
            var namedPartitions = new[] { "a", "b", "c"};

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //var partitionName = namedPartitions[random.Next(0, 3)];
                var partitionValue = random.Next(0, 300);

                ServicePartitionKey partitionKey = new ServicePartitionKey(partitionValue);
                ResolvedServicePartition partition1 = await this.servicePartitionResolver.ResolveAsync(rangedServiceUri, partitionKey, cancellationToken);

                ServiceEventSource.Current.ServiceMessage(this.Context, "Partition Value: {0} maps to {1}", partitionValue, partition1.Info.Id);


                //ServicePartitionKey partitionByNameKey = new ServicePartitionKey(partitionName);
                //ResolvedServicePartition partition2 = await this.servicePartitionResolver.ResolveAsync(namedServiceUri, partitionByNameKey, cancellationToken);

                //ServiceEventSource.Current.ServiceMessage(this.Context, "Partition Name: {0} maps to {1}", partitionName, partition2.Info.Id);

                await Send(new MyMessage()
                {
                    Text = "Hello from client"
                }, partition1.Info.Id);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private async Task Send(MyMessage myMessage, Guid id)
        {
            var sendoptions = new SendOptions();
           // sendoptions.SetDestination("PartionedSpike.RangedServer");
            sendoptions.RouteToSpecificInstance(id.ToString());

            await _listener.EndpointInstance.Send(myMessage, sendoptions).ConfigureAwait(false);
        }
    }

    public class MyDistributionStrategy : DistributionStrategy
    {
        private readonly string _endpoint;
        //   private readonly ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();

        public MyDistributionStrategy(string endpoint, DistributionStrategyScope scope) : base(endpoint, scope)
        {
            _endpoint = endpoint;
        }

        public override string SelectReceiver(string[] receiverAddresses)
        {
            return receiverAddresses.First();

            //return endpoint 

            //var random = new Random();
            //  var rangedServiceUri = new Uri(@"fabric:/PartitionedSpike/Stateful1");
            //var partitionValue = random.Next(0, 300);

            //ServicePartitionKey partitionKey = new ServicePartitionKey(partitionValue);
            //ResolvedServicePartition partition1 = this.servicePartitionResolver.ResolveAsync(rangedServiceUri, partitionKey, CancellationToken.None).GetAwaiter().GetResult();

            //return _endpoint + "-" + partition1.Info.Id;

            //using (var client = new FabricClient())
            //{
            //    var partitions = await client.QueryManager.GetPartitionListAsync(serviceName);

            //    foreach (var partition in partitions)
            //    {
            //        var partitionInformation = (Int64RangePartitionInformation)partition.PartitionInformation;
            //        partitionedEndpoints.Add(new EndpointInstance(partitionInformation.Id.ToString(), "PartionedSpike.RangedServer")
            //        {

            //        });
            //    }
            //}

        }
    }
}
