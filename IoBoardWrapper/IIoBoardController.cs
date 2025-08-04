namespace IoBoardWrapper
{
    public interface IIoBoardController
    {
        bool Open(int rotarySwitchNo);
        void Close(int rotarySwitchNo);
        void WriteOutput(int rotarySwitchNo, int port, bool value);
        bool ReadInput(int rotarySwitchNo, int port);
    }
}
