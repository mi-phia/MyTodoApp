using MyTodoApp;
using Xunit;

namespace MyTodoApp.Test
{
    public class TodoManagerTests
    {
// 空リストチェック
        // タスクが存在しない場合はfalseを返す
        [Fact]
        public void CheckTodosExist_ShouldReturnFalse_WhenNoTodosExist()
        {
            // TodoManagerを作成
            var manager = new TodoManager();

            // タスクの存在を確認
            bool result = manager.CheckTodosExist();

            // falseが返ることを確認
            Assert.False(result);
        }

        // タスクが存在する場合はtrueを返す
        [Fact]
        public void CheckTodosExist_ShouldReturnTrue_WhenTodosExist()
        {
            // テスト用データベースを作成
            using var db = new TestDatabase();
            var repository = db.Repository;

            // TodoManagerを作成
            var manager = new TodoManager();

            // データベースにタスクを登録
            repository.InsertTodo("買い物");

            // データベースの内容をTodoManagerに読み込む
            manager.LoadTodosFromDatabase(repository);

            // タスクの存在を確認
            bool result = manager.CheckTodosExist();

            // trueが返ることを確認
            Assert.True(result);
        }

// 入力値チェック
        // 指定した番号のタスクを取得できる
        [Fact]
        public void GetTodoByNumber_ShouldReturnCorrectTodo_WhenNumberIsValid()
        {
            // テスト用データベースを作成
            using var db = new TestDatabase();
            var repository = db.Repository;

            // データベースに2件のタスクを登録
            repository.InsertTodo("1番目のタスク");
            int secondId = repository.InsertTodo("2番目のタスク");

            // TodoManagerにタスクを読み込む
            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 2番目のタスクを取得
            var result = manager.GetTodoByNumber(2);

            // 取得したタスクが正しいことを確認
            Assert.NotNull(result);
            Assert.Equal(secondId, result.Id);
            Assert.Equal("2番目のタスク", result.Title);
        }

        // 存在しない番号を指定するとnullを返す
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void GetTodoByNumber_ShouldReturnNull_WhenNumberIsOutOfRange(int number)
        {
            var manager = new TodoManager();

            // 1件のタスクを登録
            using var db = new TestDatabase();
            db.Repository.InsertTodo("テストタスク");
            manager.LoadTodosFromDatabase(db.Repository);

            // 指定した番号で取得
            var result = manager.GetTodoByNumber(number);

            // nullが返ることを確認
            Assert.Null(result);
        }

// SQLとの同期
        // データベースのタスクを読み込める
        [Fact]
        public void LoadTodosFromDatabase_ShouldLoadAllTodos()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // データベースに2件登録
            repository.InsertTodo("買い物");
            repository.InsertTodo("掃除");

            // TodoManagerに読み込む
            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 読み込んだ件数を確認
            Assert.Equal(2, manager.Todos.Count);

            // タイトルを確認
            Assert.Equal("買い物", manager.Todos[0].Title);
            Assert.Equal("掃除", manager.Todos[1].Title);
        }

        // 再読み込みしてもタスクが重複しない
        [Fact]
        public void LoadTodosFromDatabase_ShouldNotDuplicateTodos_WhenCalledMultipleTimes()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            var manager = new TodoManager();

            // 最初のタスクを登録して読み込む
            repository.InsertTodo("買い物");
            manager.LoadTodosFromDatabase(repository);

            // 2件目を登録して再読み込み
            repository.InsertTodo("掃除");
            manager.LoadTodosFromDatabase(repository);

            // 重複せず2件だけ読み込まれていることを確認
            Assert.Equal(2, manager.Todos.Count);
            Assert.Equal("買い物", manager.Todos[0].Title);
            Assert.Equal("掃除", manager.Todos[1].Title);
        }

// 追加
        // タスクを追加できる
        [Fact]
        public void AddTodo_ShouldAddTodo_WhenTitleIsValid()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            var manager = new TodoManager();

            // タスクを追加
            manager.AddTodo("買い物", repository);

            // タスクが1件追加されたことを確認
            Assert.Single(manager.Todos);

            // 追加したタスクの内容を確認
            Assert.Equal("買い物", manager.Todos[0].Title);
            Assert.False(manager.Todos[0].IsCompleted);
            Assert.Equal(TodoPriority.Medium, manager.Todos[0].Priority);
        }

        // 期日と優先度を指定してタスクを追加できる
        [Fact]
        public void AddTodo_ShouldAddTodoWithDueDateAndPriority()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            var manager = new TodoManager();

            // 期日と優先度を指定
            DateTime dueDate = new DateTime(2026, 12, 1);

            manager.AddTodo(
                "買い物",
                repository,
                dueDate,
                TodoPriority.High);

            // 追加されたタスクを確認
            var todo = Assert.Single(manager.Todos);

            Assert.Equal("買い物", todo.Title);
            Assert.Equal(dueDate, todo.DueDate);
            Assert.Equal(TodoPriority.High, todo.Priority);
        }

// 完了
        // IDを指定してタスクを完了できる
        [Fact]
        public void CompleteTodoById_ShouldCompleteTodo_WhenIdExists()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            int id = repository.InsertTodo("買い物");

            // TodoManagerに読み込む
            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // タスクを完了する
            bool result = manager.CompleteTodoById(id, repository);

            // 処理が成功したことを確認
            Assert.True(result);

            // TodoManager側が完了状態になったことを確認
            Assert.True(manager.Todos[0].IsCompleted);

            // データベース側も完了状態になったことを確認
            Assert.True(repository.GetAllTodos()[0].IsCompleted);
        }

        // 存在しないIDでは完了処理に失敗する
        [Fact]
        public void CompleteTodoById_ShouldReturnFalse_WhenIdDoesNotExist()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            int id = repository.InsertTodo("買い物");

            // TodoManagerに読み込む
            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 存在しないIDで完了処理を実行
            bool result = manager.CompleteTodoById(999, repository);

            // 失敗することを確認
            Assert.False(result);

            // 既存のタスクが未完了のまま残っていることを確認
            var todo = Assert.Single(manager.Todos);
            Assert.Equal("買い物", todo.Title);
            Assert.False(todo.IsCompleted);

            // データベース側も変更されていないことを確認
            var savedTodo = Assert.Single(repository.GetAllTodos());
            Assert.False(savedTodo.IsCompleted);
        }

        // 番号を指定してタスクを完了できる
        [Fact]
        public void CompleteTodo_ShouldCompleteTodo_WhenNumberIsValid()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("買い物");

            // TodoManagerに読み込む
            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 1番目のタスクを完了する
            bool result = manager.CompleteTodo(1, repository);

            // 処理が成功したことを確認
            Assert.True(result);

            // 完了状態を確認
            Assert.True(manager.Todos[0].IsCompleted);
            Assert.True(repository.GetAllTodos()[0].IsCompleted);
        }

        // 範囲外の番号では完了処理に失敗する
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void CompleteTodo_ShouldReturnFalse_WhenNumberIsOutOfRange(int number)
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを1件登録
            repository.InsertTodo("買い物");

            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 範囲外の番号で完了処理を実行
            bool result = manager.CompleteTodo(number, repository);

            // 失敗することを確認
            Assert.False(result);

            // タスクが未完了のままであることを確認
            Assert.False(manager.Todos[0].IsCompleted);
        }

// 削除
        // IDを指定してタスクを削除できる
        [Fact]
        public void DeleteTodoById_ShouldDeleteTodo_WhenIdExists()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            int id = repository.InsertTodo("買い物");

            // TodoManagerに読み込む
            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // タスクを削除
            bool result = manager.DeleteTodoById(id, repository);

            // 削除に成功したことを確認
            Assert.True(result);

            // TodoManagerから削除されたことを確認
            Assert.Empty(manager.Todos);

            // データベースからも削除されたことを確認
            Assert.Empty(repository.GetAllTodos());
        }

        // 存在しないIDでは削除処理に失敗する
        [Fact]
        public void DeleteTodoById_ShouldReturnFalse_WhenIdDoesNotExist()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを1件登録
            repository.InsertTodo("買い物");

            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 存在しないIDで削除処理を実行
            bool result = manager.DeleteTodoById(999, repository);

            // 失敗することを確認
            Assert.False(result);

            // タスクが残っていることを確認
            var remainingTodo = Assert.Single(manager.Todos);
            Assert.Equal("買い物", remainingTodo.Title);

            // データベース側にも残っていることを確認
            var savedTodo = Assert.Single(repository.GetAllTodos());
            Assert.Equal("買い物", savedTodo.Title);
        }

        // 番号を指定してタスクを削除できる
        [Fact]
        public void DeleteTodo_ShouldDeleteTodo_WhenNumberIsValid()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            repository.InsertTodo("買い物");

            // TodoManagerに読み込む
            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 1番目のタスクを削除する
            bool result = manager.DeleteTodo(1, repository);

            // 処理が成功したことを確認
            Assert.True(result);

            // 削除されたことを確認
            Assert.Empty(manager.Todos);
            Assert.Empty(repository.GetAllTodos());
        }

        // 範囲外の番号では削除処理に失敗する
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void DeleteTodo_ShouldReturnFalse_WhenNumberIsOutOfRange(int number)
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを1件登録
            repository.InsertTodo("買い物");

            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 範囲外の番号で削除処理を実行
            bool result = manager.DeleteTodo(number, repository);

            // 失敗することを確認
            Assert.False(result);

            // タスクが存在したままであることを確認
            Assert.Single(manager.Todos);
            Assert.Single(repository.GetAllTodos());
        }

// 編集
        // タスクの内容を更新できる
        [Fact]
        public void UpdateTodoDetails_ShouldUpdateTodo_WhenIdExists()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを登録
            int id = repository.InsertTodo("古いタイトル");

            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 更新内容
            string newTitle = "新しいタイトル";
            DateTime dueDate = new DateTime(2026, 12, 1);

            // タスクを更新
            bool result = manager.UpdateTodoDetails(
                id,
                newTitle,
                dueDate,
                TodoPriority.High,
                repository);

            // 更新に成功したことを確認
            Assert.True(result);

            // TodoManager側の内容を確認
            var todo = Assert.Single(manager.Todos);

            Assert.Equal(newTitle, todo.Title);
            Assert.Equal(dueDate, todo.DueDate);
            Assert.Equal(TodoPriority.High, todo.Priority);

            // データベース側の内容も確認
            var savedTodo = Assert.Single(repository.GetAllTodos());

            Assert.Equal(newTitle, savedTodo.Title);
            Assert.Equal(dueDate, savedTodo.DueDate);
            Assert.Equal(TodoPriority.High, savedTodo.Priority);
        }

        // 存在しないIDでは更新に失敗する
        [Fact]
        public void UpdateTodoDetails_ShouldReturnFalse_WhenIdDoesNotExist()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // タスクを1件登録
            repository.InsertTodo("元のタイトル");

            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 存在しないIDで更新
            bool result = manager.UpdateTodoDetails(
                999,
                "新しいタイトル",
                null,
                TodoPriority.High,
                repository);

            // 更新に失敗したことを確認
            Assert.False(result);

            // 元の内容が変わっていないことを確認
            Assert.Equal("元のタイトル", manager.Todos[0].Title);
            Assert.Equal(TodoPriority.Medium, manager.Todos[0].Priority);
        }

        // 期日を解除できる
        [Fact]
        public void UpdateTodoDetails_ShouldClearDueDate_WhenDueDateIsNull()
        {
            using var db = new TestDatabase();
            var repository = db.Repository;

            // 期日付きのタスクを登録
            int id = repository.InsertTodo(
                "買い物",
                new DateTime(2026, 12, 1));

            var manager = new TodoManager();
            manager.LoadTodosFromDatabase(repository);

            // 期日をnullにして更新
            bool result = manager.UpdateTodoDetails(
                id,
                "買い物",
                null,
                TodoPriority.Medium,
                repository);

            // 更新に成功したことを確認
            Assert.True(result);

            // TodoManager側の期日を確認
            Assert.Null(manager.Todos[0].DueDate);

            // データベース側の期日も確認
            Assert.Null(repository.GetAllTodos()[0].DueDate);
        }

// 表記
        // 完了状態と期日に応じた表示文字列を確認
        [Theory]
        [InlineData(true, "2001-01-01", "完了")]
        [InlineData(false, "2001-01-01", "期限切れ")]
        [InlineData(false, "2099-12-31", "未完了")]
        [InlineData(false, null, "未完了")]
        public void GetIsCompletedText_ShouldReturnCorrectText(
            bool isCompleted,
            string? dueDateText,
            string expected)
        {
            DateTime? dueDate = dueDateText == null
                ? null
                : DateTime.Parse(dueDateText);

            var todo = new TodoItem
            {
                IsCompleted = isCompleted,
                DueDate = dueDate
            };

            var manager = new TodoManager();

            string result = manager.GetIsCompletedText(todo);

            Assert.Equal(expected, result);
        }

        // 期日の状態に応じた表示文字列を確認
        [Theory]
        [InlineData(true, -1, "")]
        [InlineData(false, -1, "期限切れ")]
        [InlineData(false, 0, "本日が期限")]
        [InlineData(false, 1, "あと1日")]
        [InlineData(false, 2, "あと2日")]
        [InlineData(false, 3, "")]
        [InlineData(false, null, "未設定")]
        public void GetDueDateText_ShouldReturnCorrectText(
          bool isCompleted,
          int? remainingDays,
          string expectedSuffix)
        {
            DateTime? dueDate = remainingDays.HasValue
                ? DateTime.Today.AddDays(remainingDays.Value)
                : null;

            var todo = new TodoItem
            {
                IsCompleted = isCompleted,
                DueDate = dueDate
            };

            var manager = new TodoManager();

            string result = manager.GetDueDateText(todo);

            string expected = dueDate.HasValue
                ? dueDate.Value.ToString("yyyy-MM-dd") +
                  (expectedSuffix == "" ? "" : $"({expectedSuffix})")
                : "未設定";

            Assert.Equal(expected, result);
        }

        // 優先度の状態に応じた表示文字列を確認(privateのため、ShowTodos経由)
        [Theory]
        [InlineData(TodoPriority.Low, "★ 低")]
        [InlineData(TodoPriority.Medium, "★★ 中")]
        [InlineData(TodoPriority.High, "★★★ 高")]
        [InlineData((TodoPriority)999, "不明")]
        public void GetPriorityText_ShouldReturnCorrectText(
            TodoPriority priority,
            string expected)
        {
            using var db = new TestDatabase();

            var repository = db.Repository;
            var manager = new TodoManager();

            manager.AddTodo(
                "テストタスク",
                repository,
                priority: priority
            );

            // コンソールの出力先を保存
            // 画面に表示するのではなく、文字列として保存するための準備
            TextWriter originalOutput = Console.Out;

            using var writer = new StringWriter();

            try
            {
                // コンソールの出力先をテスト用に変更
                Console.SetOut(writer);

                // タスクを表示
                manager.ShowTodos();
            }
            finally
            {
                // コンソールの出力先を元に戻す
                Console.SetOut(originalOutput);
            }

            // 期待する優先度が表示されているか確認
            Assert.Contains($"優先度：{expected}", writer.ToString());
        }

// 表示
        // タスクを表示できる
        [Fact]
        public void ShowTodos_ShouldDisplayAllTodoDetails()
        {
            // テスト用データベースを作成
            using var db = new TestDatabase();

            var repository = db.Repository;
            var manager = new TodoManager();

            // テスト用タスクを追加
            manager.AddTodo(
                "テストタスク",
                repository,
                DateTime.Today.AddDays(1),
                TodoPriority.High
            );

            // 追加したタスクのIDを取得
            int todoId = manager.Todos[0].Id;

            // タスクを完了済みにする
            manager.CompleteTodoById(todoId, repository);

            // コンソールの出力先を保存
            TextWriter originalOutput = Console.Out;

            using var writer = new StringWriter();

            try
            {
                Console.SetOut(writer);

                manager.ShowTodos();
            }
            finally
            {
                Console.SetOut(originalOutput);
            }

            string output = writer.ToString();

            // タスク名を確認
            Assert.Contains("テストタスク", output);

            // 完了状態を確認
            Assert.Contains("[完了]", output);

            // 期日を確認
            Assert.Contains(
                DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                output
            );

            // 優先度を確認
            Assert.Contains("★★★ 高", output);
        }

        // タスクが存在しない場合の表示確認
        [Fact]
        public void ShowTodos_ShouldReturnFalseWhenNoTodosExist()
        {
            // テスト用データベースを作成
            using var db = new TestDatabase();

            var manager = new TodoManager();

            // タスク一覧を表示
            bool result = manager.ShowTodos();

            // タスクが存在しない場合、falseが返ることを確認
            Assert.False(result);
        }

        // フィルター機能による表示変更を確認
        [Theory]
        [InlineData(0, true, true, true)]   // すべて表示
        [InlineData(1, true, false, false)] // 未完了のみ
        [InlineData(2, false, true, false)] // 完了済みのみ
        [InlineData(3, false, false, true)] // 期限切れのみ
        public void ShowTodos_ShouldFilterTodosCorrectly(
            int filter,
            bool expectIncomplete,
            bool expectCompleted,
            bool expectExpired)
        {
            // テスト用データベースを作成
            using var db = new TestDatabase();

            var repository = db.Repository;
            var manager = new TodoManager();

            // 未完了タスクを追加
            manager.AddTodo("未完了タスク", repository);

            // 完了済みタスクを追加
            manager.AddTodo("完了済みタスク", repository);

            // 完了にする
            int todoId = manager.Todos[1].Id;
            manager.CompleteTodoById(todoId, repository);

            // 期限切れタスクを追加
            manager.AddTodo(
                "期限切れタスク",
                repository,
                dueDate: DateTime.Today.AddDays(-1)
            );

            // 出力を保存するための準備
            TextWriter originalOutput = Console.Out;

            using var writer = new StringWriter();

            try
            {
                Console.SetOut(writer);

                // 指定したフィルターでタスクを表示
                manager.ShowTodos(filter: filter);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }

            string output = writer.ToString();

            // 未完了タスクの表示を確認
            Assert.Equal(expectIncomplete, output.Contains("未完了タスク"));

            // 完了済みタスクの表示を確認
            Assert.Equal(expectCompleted, output.Contains("完了済みタスク"));

            // 期限切れタスクの表示を確認
            Assert.Equal(expectExpired, output.Contains("期限切れタスク"));
        }

        // ソート機能による表示変更を確認
        [Theory]
        [InlineData(0, "Aタスク", "Bタスク", "Cタスク")] // 追加順
        [InlineData(2, "Bタスク", "Cタスク", "Aタスク")] // 期限順
        [InlineData(3, "Cタスク", "Aタスク", "Bタスク")] // 優先度順
        [InlineData(4, "Cタスク", "Bタスク", "Aタスク")] // おすすめ
        public void ShowTodos_ShouldSortTodosCorrectly(
            int sortOption,
            string first,
            string second,
            string third)
        {
            // テスト用データベースを作成
            using var db = new TestDatabase();

            var repository = db.Repository;
            var manager = new TodoManager();

            // 1番目のタスクを追加（期限：2日後、優先度：中）
            manager.AddTodo(
                "Aタスク",
                repository,
                DateTime.Today.AddDays(2),
                TodoPriority.Medium
            );

            // 2番目のタスクを追加（期限：明日、優先度：低）
            manager.AddTodo(
                "Bタスク",
                repository,
                DateTime.Today.AddDays(1),
                TodoPriority.Low
            );

            // 3番目のタスクを追加（期限：明日、優先度：高）
            manager.AddTodo(
                "Cタスク",
                repository,
                DateTime.Today.AddDays(1),
                TodoPriority.High
            );

            // 出力を保存するための準備
            TextWriter originalOutput = Console.Out;

            using var writer = new StringWriter();

            try
            {
                Console.SetOut(writer);

                // 指定した方法で並び替えて表示
                manager.ShowTodos(sortOption: sortOption);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }

            string output = writer.ToString();

            // 各タスクの表示位置を取得
            int firstIndex = output.IndexOf(first);
            int secondIndex = output.IndexOf(second);
            int thirdIndex = output.IndexOf(third);

            // 期待した順番で表示されているか確認
            Assert.True(firstIndex < secondIndex);
            Assert.True(secondIndex < thirdIndex);
        }

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