using NServiceBus;

namespace Contract
{
    public class MyRangedMessage : ICommand
    {
        public string Text { get; set; }
    }
}
