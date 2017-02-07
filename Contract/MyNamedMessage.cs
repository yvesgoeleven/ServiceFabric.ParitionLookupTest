using NServiceBus;

namespace Contract
{
    public class MyNamedMessage : ICommand
    {
        public string Text { get; set; }
    }
}