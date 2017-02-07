using System.Fabric;
using System.Threading.Tasks;
using Contract;
using NServiceBus;

namespace Stateful2
{
    public class MyMessageHandler : IHandleMessages<MyNamedMessage>
    {
        private readonly StatefulServiceContext _context;

        public MyMessageHandler(StatefulServiceContext context)
        {
            _context = context;
        }

        public async Task Handle(MyNamedMessage message, IMessageHandlerContext context)
        {
            ServiceEventSource.Current.ServiceMessage(_context, "Received MyNamedMessage on partition " + _context.PartitionId);
        }
    }
}