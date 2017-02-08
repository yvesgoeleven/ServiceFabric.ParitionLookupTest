using System.Fabric;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using NServiceBus;
using NServiceBus.Routing;

namespace Stateless1
{
    public class MyDistributionStrategy : DistributionStrategy
    {
        private readonly StatelessServiceContext _context;

        public MyDistributionStrategy(string endpoint, DistributionStrategyScope scope, StatelessServiceContext context) : base(endpoint, scope)
        {
            _context = context;
        }

        public override string SelectReceiver(string[] receiverAddresses)
        {
            var value = (string)CallContext.LogicalGetData("selectedPartition");
            if (value == null)
            {
                return receiverAddresses.First();
            }
            ServiceEventSource.Current.ServiceMessage(_context, "Going to route a message to partition " + value);
            return receiverAddresses.FirstOrDefault(a => a.EndsWith(value));
        }
    }
}