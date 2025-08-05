using System.Collections.Concurrent;
using Common;

namespace IoboardServer;

public class BoardManager
{
    private class BoardInstance
    {
        public int RotarySwitch { get; set; }
        public MainForm? Form { get; set; }
        public bool[] InputStates = new bool[32];
        public bool[] OutputStates = new bool[32];
        public bool FormCreated => Form != null;

        public void EnsureFormCreated(int boardId)
        {
            if (Form == null)
            {
                Form = new MainForm(boardId);
                Task.Run(() => Application.Run(Form));
            }
        }
    }

    private readonly ConcurrentDictionary<int, BoardInstance> _boards = new();

    public bool Open(int rotarySwitchNo)
    {
        if (_boards.ContainsKey(rotarySwitchNo)) return false;

        var board = new BoardInstance { RotarySwitch = rotarySwitchNo };
        _boards[rotarySwitchNo] = board;
        return true;
    }

    public void Close(int rotarySwitchNo)
    {
        if (_boards.TryRemove(rotarySwitchNo, out var board))
        {
            if (board.Form != null)
            {
                board.Form.Invoke(() => board.Form!.Close());
            }
        }
    }

    public void WriteOutput(int rotarySwitchNo, int port, bool value)
    {
        if (!_boards.TryGetValue(rotarySwitchNo, out var board)) return;

        board.OutputStates[port] = value;

        board.EnsureFormCreated(rotarySwitchNo);
        board.Form?.UpdateOutput(port, value);
    }

    public bool ReadInput(int rotarySwitchNo, int port)
    {
        if (!_boards.TryGetValue(rotarySwitchNo, out var board)) return false;

        board.EnsureFormCreated(rotarySwitchNo);
        return board.InputStates[port];
    }

    public void UpdateInput(int rotarySwitchNo, int port, bool value)
    {
        if (!_boards.TryGetValue(rotarySwitchNo, out var board)) return;

        board.InputStates[port] = value;
        board.EnsureFormCreated(rotarySwitchNo);
        board.Form?.UpdateInput(port, value);
    }
}
