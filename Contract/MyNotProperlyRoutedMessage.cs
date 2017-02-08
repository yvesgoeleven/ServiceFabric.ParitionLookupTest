using NServiceBus;

namespace Contract
{
    public class MyNotProperlyRoutedMessage : ICommand
    {
        public string Text { get; set; }
    }
}