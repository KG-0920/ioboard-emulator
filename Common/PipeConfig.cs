// Common/PipeConfig.cs
using System;

namespace Common
{
    /// <summary>
    /// パイプ名とコマンド名の正準定義（V1 正）。
    /// ※ 旧パイプ名（RSW 別名）は廃止方向。直書きは CI で検出しビルド失敗させる。
    /// </summary>
    public static class PipeConfig
    {
        // === 正準（Canonical） ===
        public const string PipeName = "IoboardBus"; // これ以外は使用禁止（CI ガードが検出）

        // 行コマンド（UTF-8 + LF）
        public const string CmdWrite = "WRITE";
        public const string CmdInput = "INPUT";
    }
}
