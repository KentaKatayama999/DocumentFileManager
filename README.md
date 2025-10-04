# .NET Init Template

.NET プロジェクトの初期設定を自動化するテンプレートです。CI、コード整形、ブランチ方針まで含む「動く土台」を素早く作成できます。

## 特徴

- **ブランチ方針**: main ブランチ採用
- **SDK方針**: SDK同梱の `dotnet format` 使用（追加ツール不要）
- **構成**: `src/`（本体）+ `tests/`（テスト）
- **CI**: GitHub Actions による自動化
- **設定ファイル**: `.editorconfig`, `.gitattributes`, `Directory.Build.props` など完備

## 必要環境

- .NET SDK: 8 または 9
- OS: Windows / macOS / Linux
- Git & GitHub CLI (gh) ※リポジトリ作成時

## 使用方法

### 1. テンプレートのインストール

```bash
# GitHubから直接インストール
dotnet new --install https://github.com/KentaKatayama999/dotnet-init-template.git

# インストール確認
dotnet new --list | findstr normal-init
```

### 2. 新規プロジェクト作成

```bash
# プロジェクト作成（サブフォルダに展開）
dotnet new normal-init -n YourProjectName \
  --SolutionName YourProjectName \
  --SdkMajor 9

# 作成されたプロジェクトに移動
cd YourProjectName
```

### 3. 動作確認

```bash
# 依存関係の復元
dotnet restore

# コード整形確認（差分なし確認）
dotnet format --verify-no-changes

# ビルド（Release）
dotnet build -c Release

# テスト実行
dotnet test -c Release --no-build
```

### 4. Git & GitHub連携（オプション）

```bash
# Gitリポジトリ初期化（既に初期化済み）
git status

# GitHubリポジトリ作成
gh repo create your-project --public

# 初回プッシュ
git add .
git commit -m "Initial commit"
git push -u origin main
```

## 生成されるファイル

```
YourProjectName/
├── .github/
│   └── workflows/
│       └── ci.yml              # GitHub Actions CI設定
├── .template.config/           # （元テンプレート設定、削除可）
├── src/
│   └── YourProjectName/
│       ├── Class1.cs           # サンプルクラス
│       └── YourProjectName.csproj
├── tests/
│   └── YourProjectName.Tests/
│       ├── UnitTest1.cs        # サンプルテスト
│       └── YourProjectName.Tests.csproj
├── .editorconfig              # エディタ設定
├── .gitattributes             # Git属性設定
├── .gitignore                 # Git除外設定
├── Directory.Build.props      # 共通ビルド設定
├── YourProjectName.sln        # ソリューションファイル
└── README.md                  # プロジェクトREADME
```

## テンプレート設定

### パラメータ

- `SolutionName`: ソリューション名（デフォルト: MySolution）
- `SdkMajor`: .NET SDK メジャーバージョン（デフォルト: 9）

### 使用例

```bash
# .NET 8を使用
dotnet new normal-init -n MyLibrary --SdkMajor 8

# カレントディレクトリに直接展開
dotnet new normal-init -n MyLibrary -o .
```

## アンインストール

```bash
# テンプレートのアンインストール
dotnet new --uninstall https://github.com/KentaKatayama999/dotnet-init-template.git
```

## 含まれる設定

### CI/CD (GitHub Actions)

- 自動フォーマット確認
- Release ビルド
- テスト実行
- PR および main/master ブランチへのプッシュで実行

### コード品質

- **EditorConfig**: UTF-8, LF, スペース4の統一設定
- **GitAttributes**: 改行コードの自動変換
- **Directory.Build.props**: 共通ビルド設定（Nullable, ImplicitUsings等）
- **CI警告エラー化**: GitHub Actions環境で警告をエラー扱い

### プロジェクト構成

- **ライブラリ**: `src/YourProjectName/`
- **テスト**: `tests/YourProjectName.Tests/` (IsPackable=false)
- **ソリューション**: 自動作成・プロジェクト自動追加

## ライセンス

MIT License

## 貢献

Issue や Pull Request をお待ちしています！

詳細な使用方法は [.template.config/docs/](https://github.com/KentaKatayama999/dotnet-init-template/tree/main/.template.config/docs) を参照してください。