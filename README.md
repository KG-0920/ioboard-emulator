# Ioboard Emulator

このプロジェクトは、実機IOボードとの通信を模擬するための **エミュレータDLLと通信サーバ** を含むC#ベースのシステムです。  
仮想IOボードを介して、外部アプリケーション（クライアント）とNamedPipe経由で通信を行う構成です。

---

## 📦 プロジェクト構成

```
ioboard-emulator/
├── IoboardServer/          # NamedPipeでクライアントと通信するサーバアプリ
├── IoboardEmulator/        # DLL化されるエミュレータ本体
├── Common/                 # ログ出力や共通型の共有
├── ioboard-emulator.sln    # Visual Studio ソリューションファイル
```

---

## 🚀 ビルド手順

### 📌 必要環境
- Visual Studio 2022 以降
- .NET 6.0 SDK

### 🔧 ビルド方法

1. Visual Studio で `ioboard-emulator.sln` を開く
2. ソリューション構成を `Release` に設定（DLL出力の場合）
3. `ソリューションのビルド` を実行
4. 出力フォルダに以下が生成されます：
   - `IoboardServer.exe`
   - `IoboardEmulator.dll`

---

## 🧪 動作概要

1. `IoboardServer.exe` を起動 → NamedPipeサーバが待機
2. 外部アプリが `IoboardEmulator.dll` を通じて API を呼び出す
3. DLLはNamedPipeクライアントとして、サーバにコマンドを送信
4. サーバは受信・ログ記録・エコーレスポンスを返却

---

## 📝 主なAPI（IoboardEmulator.dll）

| 関数名              | 説明                     |
|---------------------|--------------------------|
| `RegisterDioHandle` | ボード名の登録           |
| `UnregisterDioHandle` | 登録解除               |
| `SetOutput`         | 出力ポートの制御         |
| `GetInput`          | 入力ポートの状態取得     |

---

## 📚 ログ出力

すべてのログはプロセス直下の `ioboard_log.txt` に出力されます。  
標準出力にも表示されます。

---

## 📄 ライセンス

本プロジェクトは [MIT License](https://opensource.org/licenses/MIT) のもとで公開されています。

---

## ✨ 今後の拡張予定（例）

- APP_A / APP_B の追加（サンプルクライアント）
- GUI付き管理ツール
- ポート毎の動作モック設定
- ボード状態の保持とシミュレーション
