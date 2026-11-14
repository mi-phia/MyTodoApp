using Microsoft.Data.Sqlite;
using MyTodoApp;
using Xunit;

namespace MyTodoApp.Test
{
    public class TodoRepositoryTests
    {
// 追加
        // 期日や優先度を設定して追加する
        [Fact]
        public void InsertTodo_ShouldSaveAllProperties()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 期日・優先度付きで登録
            DateTime dueDate = new DateTime(2026, 12, 31);
            int id = repository.InsertTodo("期限付きタスク", dueDate, TodoPriority.High);

            // 登録したタスクを取得
            var todo = repository.GetAllTodos().Single(t => t.Id == id);

            // 登録結果を確認
            Assert.Equal("期限付きタスク", todo.Title);
            Assert.False(todo.IsCompleted);
            Assert.Equal(dueDate, todo.DueDate);
            Assert.Equal(TodoPriority.High, todo.Priority);
        }

        // 期日・優先度を省略して追加する
        [Fact]
        public void InsertTodo_WithoutOptionalArguments_ShouldUseDefaultValues()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タイトルだけ指定して登録
            int id = repository.InsertTodo("買い物");

            // 登録したタスクを取得
            var todo = repository.GetAllTodos().Single(t => t.Id == id);

            // 初期値を確認
            Assert.False(todo.IsCompleted);
            Assert.Null(todo.DueDate);
            Assert.Equal(TodoPriority.Medium, todo.Priority);
        }

        // 登録したIDを確認する
        [Fact]
        public void InsertTodo_ShouldReturnInsertedTodoId()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを2件登録
            int firstId = repository.InsertTodo("買い物");
            int secondId = repository.InsertTodo("掃除");

            // それぞれのIDを確認
            Assert.Equal(1, firstId);
            Assert.Equal(2, secondId);
        }

// 完了
        // タスクの完了状態を更新する
        [Fact]
        public void UpdateTodoStatus_ShouldUpdateStatus()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            int id = repository.InsertTodo("買い物");

            // 完了状態に更新
            bool result = repository.UpdateTodoStatus(id, true);

            // 更新結果を確認
            var todo = repository.GetAllTodos().Single(t => t.Id == id);

            Assert.True(result);
            Assert.True(todo.IsCompleted);
        }

        // 存在しないIDを更新できないことを確認する
        // IDにはintしか入らないので、空白や文字列の入力テストは後述
        [Fact]
        public void UpdateTodoStatus_ShouldReturnFalse_WhenTodoDoesNotExist()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 存在しないIDを指定して更新
            bool result = repository.UpdateTodoStatus(999, true);

            // 更新されないことを確認
            Assert.False(result);
        }

// 削除
        // タスクを削除してみる
        [Fact]
        public void DeleteTodo_ShouldDeleteTodo()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            int id = repository.InsertTodo("買い物");

            // タスクを削除
            bool result = repository.DeleteTodo(id);

            // 削除結果を確認
            Assert.True(result);
            Assert.Empty(repository.GetAllTodos());
        }

        // 存在しないタスクを削除できないことを確認する
        [Fact]
        public void DeleteTodo_ShouldReturnFalse_WhenTodoDoesNotExist()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 存在しないIDを指定して削除
            bool result = repository.DeleteTodo(999);

            // 削除されないことを確認
            Assert.False(result);
        }

// 編集
        // タスクの詳細を更新してみる
        [Fact]
        public void UpdateTodoDetails_ShouldUpdateAllProperties()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 更新対象のタスクを登録
            int id = repository.InsertTodo("古いタスク");

            // 新しい内容に更新
            DateTime dueDate = new DateTime(2026, 12, 31);
            bool result = repository.UpdateTodoDetails(
                id, "新しいタスク", dueDate, TodoPriority.High);

            // 更新結果を確認
            var todo = repository.GetAllTodos().Single(t => t.Id == id);

            Assert.True(result);
            Assert.Equal("新しいタスク", todo.Title);
            Assert.Equal(dueDate, todo.DueDate);
            Assert.Equal(TodoPriority.High, todo.Priority);
        }

        // 存在しないIDのタスクは更新できないことを確認する
        [Fact]
        public void UpdateTodoDetails_ShouldReturnFalse_WhenTodoDoesNotExist()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 存在しないIDを指定して更新
            bool result = repository.UpdateTodoDetails(
                999, "新しいタスク", null, TodoPriority.High);

            // 更新できないことを確認
            Assert.False(result);
        }

        // 重複するタイトルには更新できないことを確認する
        [Fact]
        public void UpdateTodoDetails_ShouldThrowException_WhenTitleAlreadyExists()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 2件のタスクを登録
            int firstId = repository.InsertTodo("買い物");
            int secondId = repository.InsertTodo("掃除");

            // 重複タイトルへの更新で例外が発生することを確認
            Assert.Throws<SqliteException>(() =>
                repository.UpdateTodoDetails(
                    secondId, "買い物", null, TodoPriority.High));

            // 元のタイトルが維持されていることを確認
            var todo = repository.GetAllTodos()
                .Single(t => t.Id == secondId);

            Assert.Equal("掃除", todo.Title);
        }

// 検索
        // キーワードに一致するタスクを検索する
        [Fact]
        public void SearchTodos_ShouldReturnMatchingTodos()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("買い物");
            repository.InsertTodo("掃除");
            repository.InsertTodo("買い物リスト");

            // 「買い物」で検索
            var todos = repository.SearchTodos("買い物");

            // 検索結果を確認
            Assert.Equal(2, todos.Count);
            Assert.All(todos, todo =>
                Assert.Contains("買い物", todo.Title));
        }

        // 該当するタスクがない場合は空の結果を返す
        [Fact]
        public void SearchTodos_ShouldReturnEmpty_WhenNoMatch()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("買い物");
            repository.InsertTodo("掃除");

            // 該当しないキーワードで検索
            var todos = repository.SearchTodos("勉強");

            // 検索結果が空であることを確認
            Assert.Empty(todos);
        }

        // 大文字・小文字を区別せず検索できることを確認する
        [Fact]
        public void SearchTodos_ShouldIgnoreCase()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("Shopping");

            // 小文字で検索
            var todos = repository.SearchTodos("shopping");

            // 検索結果を確認
            Assert.Single(todos);
            Assert.Equal("Shopping", todos[0].Title);
        }

        // 検索結果にすべてのプロパティが含まれることを確認する
        [Fact]
        public void SearchTodos_ShouldReturnAllProperties()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // すべての情報を指定してタスクを登録
            DateTime dueDate = new DateTime(2026, 12, 31);
            int id = repository.InsertTodo(
                "買い物", dueDate, TodoPriority.High);

            repository.UpdateTodoStatus(id, true);

            // タスクを検索
            var todo = Assert.Single(repository.SearchTodos("買い物"));

            // すべてのプロパティを確認
            Assert.Equal(id, todo.Id);
            Assert.Equal("買い物", todo.Title);
            Assert.True(todo.IsCompleted);
            Assert.Equal(dueDate, todo.DueDate);
            Assert.Equal(TodoPriority.High, todo.Priority);
        }

        // 特殊文字を通常の文字として検索できることを確認する
        [Theory]
        [InlineData("%", "割引100%")]
        [InlineData("_", "task_list")]
        [InlineData("\\", "C:\\Todo")]
        public void SearchTodos_ShouldTreatSpecialCharactersAsLiteral(
            string keyword, string title)
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo(title);

            // 特殊文字で検索
            var todos = repository.SearchTodos(keyword);

            // 検索結果を確認
            Assert.Single(todos);
            Assert.Equal(title, todos[0].Title);
        }

// 重複
        // 登録済みのタイトルが存在することを確認する
        [Fact]
        public void ExistsTodoTitle_ShouldReturnTrue_WhenTitleExists()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("買い物");

            // タイトルの存在を確認
            bool result = repository.ExistsTodoTitle("買い物");

            // 存在することを確認
            Assert.True(result);
        }

        // 未登録のタイトルは存在しないことを確認する
        [Fact]
        public void ExistsTodoTitle_ShouldReturnFalse_WhenTitleDoesNotExist()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 別のタスクを登録
            repository.InsertTodo("買い物");

            // 未登録のタイトルを確認
            bool result = repository.ExistsTodoTitle("掃除");

            // 存在しないことを確認
            Assert.False(result);
        }

        // 指定したIDのタスクを重複チェックから除外する
        [Fact]
        public void ExistsTodoTitle_ShouldIgnoreExcludedId()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            int id = repository.InsertTodo("買い物");

            // 自分自身のIDを除外して確認
            bool result = repository.ExistsTodoTitle("買い物", id);

            // 重複扱いにならないことを確認
            Assert.False(result);
        }

        // 大文字・小文字を区別せず重複を確認する
        [Fact]
        public void ExistsTodoTitle_ShouldIgnoreCase()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("Shopping");

            // 小文字で重複確認
            bool result = repository.ExistsTodoTitle("shopping");

            // 重複していることを確認
            Assert.True(result);
        }

// 表示
        // タスクがない場合は空のリストを返す
        [Fact]
        public void GetAllTodos_ShouldReturnEmpty_WhenNoTodosExist()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // すべてのタスクを取得
            var todos = repository.GetAllTodos();

            // 結果が空であることを確認
            Assert.Empty(todos);
        }

        // 登録したすべてのタスクを取得する
        [Fact]
        public void GetAllTodos_ShouldReturnAllTodos()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("買い物");
            repository.InsertTodo("掃除");
            repository.InsertTodo("勉強");

            // すべてのタスクを取得
            var todos = repository.GetAllTodos();

            // 取得結果を確認
            Assert.Equal(3, todos.Count);
            Assert.Equal(
                new[] { "買い物", "掃除", "勉強" },
                todos.Select(t => t.Title));
        }

        // 取得したタスクのすべてのプロパティを確認する
        [Fact]
        public void GetAllTodos_ShouldReturnAllProperties()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            DateTime dueDate = new DateTime(2026, 12, 31);
            int id = repository.InsertTodo(
                "買い物", dueDate, TodoPriority.High);

            repository.UpdateTodoStatus(id, true);

            // タスクを取得
            var todo = Assert.Single(repository.GetAllTodos());

            // すべてのプロパティを確認
            Assert.Equal(id, todo.Id);
            Assert.Equal("買い物", todo.Title);
            Assert.True(todo.IsCompleted);
            Assert.Equal(dueDate, todo.DueDate);
            Assert.Equal(TodoPriority.High, todo.Priority);
        }

        // 完了状態が正しく取得できることを確認する
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetAllTodos_ShouldReturnCorrectStatus(bool isCompleted)
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            int id = repository.InsertTodo("買い物");

            // 完了状態を設定
            repository.UpdateTodoStatus(id, isCompleted);

            // 登録したタスクを取得
            var todo = Assert.Single(repository.GetAllTodos());

            // 完了状態を確認
            Assert.Equal(isCompleted, todo.IsCompleted);
        }

        // 期日が未設定の場合も正しく取得できることを確認する
        [Fact]
        public void GetAllTodos_ShouldReturnNullDueDate()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 期日を指定せず登録
            int id = repository.InsertTodo("買い物");

            // 登録したタスクを取得
            var todo = Assert.Single(repository.GetAllTodos());

            // 期日が未設定であることを確認
            Assert.Equal(id, todo.Id);
            Assert.Null(todo.DueDate);
        }

        // 指定した優先度が正しく取得できることを確認する
        [Theory]
        [InlineData(TodoPriority.Low)]
        [InlineData(TodoPriority.Medium)]
        [InlineData(TodoPriority.High)]
        public void GetAllTodos_ShouldReturnCorrectPriority(TodoPriority priority)
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 優先度を指定して登録
            repository.InsertTodo("買い物", null, priority);

            // 登録したタスクを取得
            var todo = Assert.Single(repository.GetAllTodos());

            // 優先度を確認
            Assert.Equal(priority, todo.Priority);
        }

// バックアップ
        // バックアップが作成され、データも保存されることを確認する
        [Fact]
        public void BackupDatabase_ShouldCreateBackupWithData()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("C#の勉強");

            // バックアップを作成
            string backupPath = repository.BackupDatabase();

            try
            {
                // バックアップの存在を確認
                Assert.True(File.Exists(backupPath));

                // バックアップを接続してデータを取得
                using var connection = new SqliteConnection(
                    $"Data Source={backupPath}");

                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT Title FROM Todos;";

                // 保存されたデータを確認
                Assert.Equal("C#の勉強", command.ExecuteScalar());
            }
            finally
            {
                // SQLiteの接続プールをクリア
                SqliteConnection.ClearAllPools();

                // バックアップファイルを削除
                File.Delete(backupPath);
            }
        }

        // バックアップからデータを復元できることを確認する
        [Fact]
        public void RestoreDatabase_ShouldRestoreBackupData()
        {
            // テスト用フォルダーを作成
            string testDirectory = Path.Combine(
                Path.GetTempPath(), Guid.NewGuid().ToString());

            string databasePath = Path.Combine(testDirectory, "test.db");
            string sourcePath = Path.Combine(testDirectory, "Source", "source.db");
            string? backupPath = null;

            var repository = new TodoRepository(databasePath);
            var sourceRepository = new TodoRepository(sourcePath);

            try
            {
                // データベースを初期化
                repository.InitializeDatabase();
                sourceRepository.InitializeDatabase();

                // 復元前後のデータを登録
                repository.InsertTodo("古いタスク");
                sourceRepository.InsertTodo("新しいタスク");

                // バックアップを作成して復元
                backupPath = sourceRepository.BackupDatabase();
                repository.RestoreDatabase(backupPath);

                // 復元結果を確認
                var todos = repository.GetAllTodos();

                Assert.Single(todos);
                Assert.Equal("新しいタスク", todos[0].Title);
            }
            finally
            {
                // SQLiteの接続プールを解放
                SqliteConnection.ClearAllPools();

                // バックアップファイルを削除
                if (backupPath != null && File.Exists(backupPath))
                    File.Delete(backupPath);

                // テスト用フォルダーを削除
                try
                {
                    Directory.Delete(testDirectory, true);
                }
                catch (IOException)
                {
                }
            }
        }

        // データベースの保存先を取得できることを確認する
        [Fact]
        public void ShowSavePath_ShouldReturnDatabasePath()
        {
            using var db = new TestDatabase();

            // 保存先を取得
            string path = db.Repository.ShowSavePath();

            // 保存先が正しいことを確認
            Assert.Equal(db.DatabasePath, path);
        }

        // 接続準備
        private sealed class TestDatabase : IDisposable
        {
            // テスト用データベースの保存先
            public string DatabasePath { get; } =
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");

            // テストで使用するRepository
            public TodoRepository Repository { get; }

            // データベースを作成・初期化
            public TestDatabase()
            {
                Repository = new TodoRepository(DatabasePath);
                Repository.InitializeDatabase();
            }

            // テスト終了後にデータベースを削除
            public void Dispose()
            {
                try
                {
                    File.Delete(DatabasePath);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}