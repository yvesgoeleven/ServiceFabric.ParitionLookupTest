using System.Fabric;
using System.Threading.Tasks;
using Contract;
using NServiceBus;

namespace Stateful1
{
    public class MyMessageHandler : IHandleMessages<MyMessage>
    {
        private readonly StatefulServiceContext _context;

        public MyMessageHandler(StatefulServiceContext context)
        {
            _context = context;
        }

        public async Task Handle(MyMessage message, IMessageHandlerContext context)
        {
            ServiceEventSource.Current.ServiceMessage(_context, message.Text);
        }
    }
}