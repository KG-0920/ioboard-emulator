using System;
using Common;

namespace IoboardServer
{
    public class BoardManager
    {
        public void Initialize()
        {
            Logger.Log("BoardManager initialized.");
        }

        public void ExecuteCommand(string command)
        {
            Logger.Log($"Executing command: {command}");
            // ここに実際のボード制御ロジックを記述
        }

        public void Shutdown()
        {
            Logger.Log("BoardManager shutting down.");
        }
    }
}
