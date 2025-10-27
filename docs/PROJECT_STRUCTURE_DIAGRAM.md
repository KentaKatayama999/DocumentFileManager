# フォルダ／ファイル構成図

## ルートリポジトリ（`DocumentFileManager/`）

```
DocumentFileManager/
├─ src/
│  ├─ DocumentFileManager/                # ドメイン層（エンティティ／値オブジェクト）
│  ├─ DocumentFileManager.Infrastructure/ # リポジトリ・サービス層
│  ├─ DocumentFileManager.UI/             # WPF クライアント本体
│  └─ DocumentFileManager.Viewer/         # ビューア（外部プロセスで資料を表示）
├─ tests/
│  ├─ DocumentFileManager.UI.Test/        # テスト用 WPF サンプルアプリ
│  └─ DocumentFileManager.UI.UnitTests/   # 追加したユニットテスト
├─ docs/                                  # 設計資料・手順書
├─ scripts/                               # PowerShell スクリプト
├─ packages/                              # NuGet ローカルキャッシュ
├─ local-packages/                        # `dotnet pack` の成果物置き場
├─ README.md
├─ USAGE.md
└─ FOLDER_STRUCTURE.md
```

## プロジェクトルート（`documentRootPath/`）

アプリ起動時に必須サブフォルダを自動生成し、各種データを所定の場所に保存します。

```
documentRootPath/
├─ config/                                # 設定／チェックリストのローカルコピー
│  ├─ checklist.json                      # 選択されたチェックリストを複製
│  └─ appsettings.json                    # 設定画面で保存した PathSettings 等（任意）
├─ documents/                             # UI から登録した資料ファイル
│  └─ ...                                 # 例: 仕様書.pdf、設計書.xlsx など
├─ captures/                              # チェックリスト画面で撮影したキャプチャ
│  ├─ document_1/
│  │  └─ capture_20251020_001.png
│  └─ document_2/
├─ logs/                                  # Serilog ログ（1 日単位のローリング）
│  └─ app-20251020.log
├─ workspace.db                           # SQLite データベース（自動マイグレーション）
└─ appsettings.json                       # 初期設定テンプレート（存在しない場合は自動生成）
```

### チェックリスト運用フロー

- 初回選択時に共有フォルダ上の `checklist.json` を `config/` 配下へコピー。
- `PathSettings.SelectedChecklistFile`／`ChecklistFile` は常にローカル相対パス（既定は `config/checklist.json`）を指します。
- 元の共有フォルダは `ChecklistDefinitionsFolder` に記録され、次回のダイアログ初期位置として利用します。

### 資料ファイルのコピー方針

- UI から登録したファイルは `documents/` へ必ずコピーし、DB には `documents/<ファイル名>` 形式で相対パスを保存します。
- これによりプロジェクトフォルダを別環境に移してもリンク切れが発生しません。

## テスト用ハーネス（`DocumentFileManagerTest/`）

```
DocumentFileManagerTest/
├─ config/
│  └─ checklist.json                      # 本番パッケージから展開されたローカルコピー
├─ documents/                             # テスト実行時に使う資料サンプル
├─ captures/                              # 回帰テストで生成されたキャプチャ
├─ logs/
├─ workspace.db
└─ appsettings.json
```

> ローカルの `config/checklist.json` を削除してからテストアプリを再起動すると、共有フォルダを再選択して最新のフォーマットを取り込めます。
