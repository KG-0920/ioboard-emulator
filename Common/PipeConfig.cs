namespace Common;
public static class PipeConfig
{
    // ※必要なら名称は1か所で変更。Server/APPで必ず一致させること。
    public const string PipeName = "IoboardBus";

    // 行指向メッセージ
    public const string CmdWrite = "WRITE"; // WRITE <port> <0|1>
    public const string CmdInput = "INPUT"; // INPUT <port> <0|1>
}
