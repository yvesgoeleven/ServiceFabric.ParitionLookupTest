using System.Fabric;
using System.Runtime.Remoting.Messaging;
using NServiceBus;
using NServiceBus.Routing;

namespace Stateless1
{
    using System.Linq;

    public class MyDistributionStrategy : DistributionStrategy
    {
        private readonly StatelessServiceContext _context;

        public MyDistributionStrategy(string endpoint, DistributionStrategyScope scope, StatelessServiceContext context) : base(endpoint, scope)
        {
            _context = context;
        }

        public override string SelectReceiver(string[] receiverAddresses)
        {
            var discriminator = (string)CallContext.LogicalGetData("selectedPartition");
            ServiceEventSource.Current.ServiceMessage(_context, "Going to route a message to partition " + discriminator);

            var logicalAddress = LogicalAddress.CreateRemoteAddress(new EndpointInstance(Endpoint, discriminator));

            return receiverAddresses.FirstOrDefault(a => a == logicalAddress.ToString()); //199 vs 99
        }
    }
}