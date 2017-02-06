using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;

namespace Contract
{
    public class MyMessage : ICommand
    {
        public string Text { get; set; }
    }
}
