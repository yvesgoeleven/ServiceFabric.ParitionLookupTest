using System;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace Stateless1
{
    public class MyBehavior : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
    {
        private readonly Func<object, object> _partitionMap;

        public MyBehavior(Func<object, object> partitionMap)
        {
            _partitionMap = partitionMap;
        }

        public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
        {
            var partitionKey = _partitionMap(context.Message.Instance);

            CallContext.LogicalSetData("selectedPartition", partitionKey.ToString());
            return next(context);
        }
    }
}