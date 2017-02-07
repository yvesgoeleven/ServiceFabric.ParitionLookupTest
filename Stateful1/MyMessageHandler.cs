using System.Fabric;
using System.Threading.Tasks;
using Contract;
using NServiceBus;

namespace Stateful1
{
    public class MyMessageHandler : IHandleMessages<MyRangedMessage>
    {
        private readonly StatefulServiceContext _context;

        public MyMessageHandler(StatefulServiceContext context)
        {
            _context = context;
        }

        public async Task Handle(MyRangedMessage message, IMessageHandlerContext context)
        {
            ServiceEventSource.Current.ServiceMessage(_context, "Received MyRangedMessage on partition " + _context.PartitionId);
        }
    }
}