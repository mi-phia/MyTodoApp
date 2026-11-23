# MyTodoApp

C#とWPFで作成した、SQLite対応のToDo管理アプリです。
タスクの追加・編集・削除に加え、期限や優先度の管理、検索・絞り込み・並び替えができます。


## アプリの概要

Windows向けのデスクトップToDoアプリです。
SQLiteを利用してタスクを保存するため、アプリを終了して再起動してもデータを保持できます。


## 主な機能

- タスクの追加・編集・削除
- タスクの完了・未完了管理
- 期限の設定
- 優先度の設定（High・Medium・Low）
- キーワード検索
- 未完了・完了済み・期限切れの絞り込み
- 登録順・期限順・優先度順の並び替え
- SQLiteによるデータ保存
- 入力内容のバリデーション
- 優先度に応じた色分け表示


## 使用技術

| 	 技術 		|	     用途		|
|	 ---		|	     ---		|
| 	  C# 		|     アプリケーション開発 	|
|      .NET 10		|          開発基盤		|
|        WPF		|    Windowsデスクトップ画面 	|
|        XAML 		|    画面レイアウト・デザイン 	|
|       SQLite 		|         データベース		|
| Microsoft.Data.Sqlite |         SQLite接続		|
|       xUnit 		|          単体テスト		|
|     Git / GitHub 	|        バージョン管理		|


## プロジェクト構成

```text

MyTodoApp

├── MyTodoApp
│   └── コンソール版アプリケーション
│
├── MyTodoApp.Core
│   └── タスク管理・データベース処理
│
├── MyTodoApp.Test
│   └── 単体テスト
│
├── MyTodoApp.Wpf
│   └── WPFデスクトップアプリケーション
│
└── MyTodoApp.slnx

```

## 起動方法

### 開発環境

1. Windowsに.NET 10 SDKをインストールします。
2. リポジトリをクローンします。
3. ソリューションをVisual Studioで開きます。
4. `MyTodoApp.Wpf` をスタートアッププロジェクトに設定します。
5. 実行します。


### コマンドライン

パッケージの復元：

```bash
dotnet restore MyTodoApp.slnx
```

Releaseビルド：

```bash
dotnet build MyTodoApp.slnx --configuration Release

```

単体テスト：

```bash
dotnet test MyTodoApp.slnx --configuration Release

```


## データベース

タスク情報はSQLiteに保存されます。

保存先：

```text
%LOCALAPPDATA%\\MyTodoApp\\todos.db

```

アプリ本体とデータベースを分けて保存することで、アプリの更新時にもデータを保持しやすくしています。


## 配布

Windows 64bit環境向けに、Release構成・自己完結型（Self-contained）で発行できます。
発行された `win-x64` フォルダ内のファイル一式を利用します。


## GitHub Actions

GitHub Actionsを利用して、Windows環境でReleaseビルドと単体テストを実行します。


## 制作目的

C#の学習を目的として、コンソール版のToDoアプリから開発を始め、データベース対応、WPFによる画面実装、テスト、配布まで段階的に開発しました。

