namespace Common
{
    /// <summary>
    /// 共通定義用の型や定数をまとめたクラス。
    /// 必要に応じて追加してください。
    /// </summary>
    public static class SharedTypes
    {
        public const string Version = "1.0.0";

        // IOボードで共通使用されるパラメータなどを定義
        public enum BoardStatus
        {
            Unknown = 0,
            Ready = 1,
            Busy = 2,
            Error = 3
        }
    }
}
