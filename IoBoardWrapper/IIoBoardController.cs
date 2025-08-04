namespace IoBoardWrapper
{
    public interface IIoBoardController
    {
        bool Register(string boardName);
        void Unregister();
        void SetOutput(int port, int value);
        int GetInput(int port);
    }
}
