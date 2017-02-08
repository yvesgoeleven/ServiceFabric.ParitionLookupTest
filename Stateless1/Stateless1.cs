using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using NServiceBus;

namespace Stateless1
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Stateless1 : StatelessService
    {
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
            _listener = new MyCommunicationListener(this.Context);
            return new List<ServiceInstanceListener>() { new ServiceInstanceListener(context => _listener) };
        }

       
        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await Send(new MyRangedMessage()
                {
                    Text = "Hello from client"
                });

                await Send(new MyNamedMessage()
                {
                    Text = "Hello from client"
                });

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        private async Task Send(MyRangedMessage myRangedMessage)
        {
            var sendoptions = new SendOptions();

            await _listener.EndpointInstance.Send(myRangedMessage, sendoptions).ConfigureAwait(false);
        }

        private async Task Send(MyNamedMessage myNamedMessage)
        {
            var sendoptions = new SendOptions();

            await _listener.EndpointInstance.Send(myNamedMessage, sendoptions).ConfigureAwait(false);
        }
    }
}
