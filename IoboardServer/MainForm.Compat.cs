// IoboardServer/MainForm.Compat.cs
using System;

namespace IoboardServer
{
    /// <summary>
    /// 既存 MainForm の後方互換メンバを補う partial クラス。
    /// （BoardManager 等が new MainForm(arg) や UpdateOutput/UpdateInput を呼ぶ想定に対応）
    /// </summary>
    public partial class MainForm
    {
        // 既存コードに new MainForm(something) がある場合の互換用
        public MainForm(object? _dummy) : this()
        {
        }

        /// <summary>
        /// 出力UIの更新（互換用・必要に応じて実装を肉付け）
        /// </summary>
        public void UpdateOutput(int port, bool value)
        {
            // TODO: 必要なら outputTable 等の実フィールドに反映する処理を実装
            // 例:
            // this.SafeInvoke(() => { /* UI反映 */ });
        }

        /// <summary>
        /// 入力UIの更新（互換用・必要に応じて実装を肉付け）
        /// </summary>
        public void UpdateInput(int port, bool value)
        {
            // TODO: 必要なら inputTable 等の実フィールドに反映する処理を実装
            // 例:
            // this.SafeInvoke(() => { /* UI反映 */ });
        }
    }
}
