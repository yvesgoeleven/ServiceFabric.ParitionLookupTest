using System.Fabric;
using System.Threading.Tasks;
using Contract;
using NServiceBus;

namespace Stateful2
{
    public class MyNotProperlyRoutedMessageHandler : IHandleMessages<MyNotProperlyRoutedMessage>
    {
        private readonly StatefulServiceContext _context;

        public MyNotProperlyRoutedMessageHandler(StatefulServiceContext context)
        {
            _context = context;
        }

        public async Task Handle(MyNotProperlyRoutedMessage message, IMessageHandlerContext context)
        {
            ServiceEventSource.Current.ServiceMessage(_context, "Received MyNotProperlyRoutedMessage on partition " + _context.PartitionId);
        }
    }
}