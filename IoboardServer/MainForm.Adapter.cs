namespace IoboardServer
{
    /// <summary>
    /// 既存コードの MainForm 参照を生かすための互換アダプタ。
    /// 実体は IoboardForm を継承。必要なメソッド名を肩代わりする。
    /// </summary>
    public class MainForm : IoboardForm
    {
        // 既存で new MainForm(arg) が呼ばれるケースに備えて形だけ用意
        public MainForm() : base() { }
        public MainForm(object? _) : this() { }

        // 既存コードが呼ぶ想定メソッド。IoboardForm側に同等が無い場合は no-op にする。
        public void UpdateOutput(int port, bool value)
        {
            TryForwardUpdateOutput(port, value);
        }

        public void UpdateInput(int port, bool value)
        {
            TryForwardUpdateInput(port, value);
        }

        // IoboardForm に同等の内部実装がある前提なら、ここで呼び出すように変更してください。
        // 無い場合は、とりあえず no-op（ビルド優先）。
        private void TryForwardUpdateOutput(int port, bool value)
        {
            // 例: this.UpdateOutputInternal(port, value); // 実装があればこちらに接続
        }

        private void TryForwardUpdateInput(int port, bool value)
        {
            // 例: this.UpdateInputInternal(port, value); // 実装があればこちらに接続
        }
    }
}
