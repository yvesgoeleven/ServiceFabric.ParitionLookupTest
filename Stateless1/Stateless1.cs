using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

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

        public Stateless1(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
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

                var partitionName = namedPartitions[random.Next(0, 3)];
                var partitionValue = random.Next(0, 300);

                ServicePartitionKey partitionKey = new ServicePartitionKey(partitionValue);
                ResolvedServicePartition partition1 = await this.servicePartitionResolver.ResolveAsync(rangedServiceUri, partitionKey, cancellationToken);

                ServiceEventSource.Current.ServiceMessage(this.Context, "Partition Value: {0} maps to {1}", partitionValue, partition1.Info.Id);


                ServicePartitionKey partitionByNameKey = new ServicePartitionKey(partitionName);
                ResolvedServicePartition partition2 = await this.servicePartitionResolver.ResolveAsync(namedServiceUri, partitionByNameKey, cancellationToken);

                ServiceEventSource.Current.ServiceMessage(this.Context, "Partition Name: {0} maps to {1}", partitionName, partition2.Info.Id);


                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
