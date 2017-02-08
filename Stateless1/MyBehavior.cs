using System;
using System.Collections.Generic;
using System.Fabric;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Microsoft.ServiceFabric.Services.Client;
using NServiceBus.Pipeline;
using NServiceBus.Routing;

namespace Stateless1
{
    public class MyBehavior : IBehavior<IOutgoingSendContext, IOutgoingSendContext>
    {
        private readonly UnicastRoutingTable _unicastRoutingTable;
        private readonly Func<object, object> _partitionMap;
        private readonly Dictionary<string, string> _addressMap;
        private readonly ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();

        public MyBehavior(UnicastRoutingTable unicastRoutingTable, Func<object, object> partitionMap , Dictionary<string, string> addressMap)
        {
            _unicastRoutingTable = unicastRoutingTable;
            _partitionMap = partitionMap;
            _addressMap = addressMap;
        }

        public Task Invoke(IOutgoingSendContext context, Func<IOutgoingSendContext, Task> next)
        {
            if (context.Message.MessageType == typeof(MyNotProperlyRoutedMessage))
            {
                return next(context);
            }

            //context.Message.MessageType
            // how to get destination endpoint here, to resolve @"fabric:/PartitionedSpike/Stateful1"
            //unicastRoutingTable
            var type = typeof(UnicastRoutingTable);
            var methodInfo = type.GetMethod("GetRouteFor", BindingFlags.NonPublic | BindingFlags.Instance);
            var route = (UnicastRoute) methodInfo.Invoke(_unicastRoutingTable, new object[] { context.Message.MessageType });

            var uri = _addressMap[route.Endpoint];

            var partitionValue = _partitionMap(context.Message.Instance);

            var partitionKey = partitionValue is string ? new ServicePartitionKey((string)partitionValue) : new ServicePartitionKey((int)partitionValue);
            var partition1 = this.servicePartitionResolver.ResolveAsync(new Uri(uri), partitionKey, CancellationToken.None).GetAwaiter().GetResult();
            
            CallContext.LogicalSetData("selectedPartition", partition1.Info.Id.ToString());
            return next(context);
        }
    }
}