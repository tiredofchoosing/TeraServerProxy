namespace TeraServerProxy.MessageProcessor
{
    public interface ITeraMessageProcessor
    {
        public event Action? MessageProcessed;
        public void Process();
    }
}
