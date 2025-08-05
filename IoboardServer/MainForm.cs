using System.Drawing;
using System.Windows.Forms;

namespace IoboardServer;

public partial class MainForm : Form
{
    private readonly int _boardId;
    private readonly Label[] _inputLabels = new Label[32];
    private readonly Label[] _outputLabels = new Label[32];

    public MainForm(int boardId)
    {
        _boardId = boardId;
        InitializeComponent();
        Text = $"I/O Board RSW {_boardId}";
        Size = new Size(600, 300);
        InitControls();
    }

    private void InitControls()
    {
        var inputPanel = new FlowLayoutPanel
        {
            Location = new Point(10, 10),
            Size = new Size(270, 240),
            BorderStyle = BorderStyle.FixedSingle
        };
        var outputPanel = new FlowLayoutPanel
        {
            Location = new Point(300, 10),
            Size = new Size(270, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        Label CreateLabel(string title)
        {
            return new Label
            {
                Text = title,
                AutoSize = false,
                Width = 60,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.LightGray,
                Margin = new Padding(3)
            };
        }

        for (int i = 0; i < 32; i++)
        {
            _inputLabels[i] = CreateLabel($"IN{i:D2}");
            inputPanel.Controls.Add(_inputLabels[i]);

            _outputLabels[i] = CreateLabel($"OUT{i:D2}");
            outputPanel.Controls.Add(_outputLabels[i]);
        }

        Controls.Add(inputPanel);
        Controls.Add(outputPanel);
    }

    public void UpdateInput(int port, bool value)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateInput(port, value));
            return;
        }
        _inputLabels[port].BackColor = value ? Color.Lime : Color.LightGray;
    }

    public void UpdateOutput(int port, bool value)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateOutput(port, value));
            return;
        }
        _outputLabels[port].BackColor = value ? Color.OrangeRed : Color.LightGray;
    }
}
