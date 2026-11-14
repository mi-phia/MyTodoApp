using MyTodoApp;
using Microsoft.Data.Sqlite;

/// メインプログラム
//初期化設定
TodoRepository repository = new TodoRepository();
TodoManager manager = new TodoManager();

repository.InitializeDatabase();
manager.LoadTodosFromDatabase(repository);

AppLogger.Info("アプリを起動しました。");

// メニュー画面表示
while (true)
{
    Console.WriteLine();
    Console.WriteLine("==== My Todo App ===");
    Console.WriteLine("1. タスクを追加");
    Console.WriteLine("2. タスク一覧を表示");
    Console.WriteLine("3. タスクを完了にする");
    Console.WriteLine("4. タスクを削除");
    Console.WriteLine("5. タスクを編集");
    Console.WriteLine("6. キーワード検索");
    Console.WriteLine("7. 保存先を確認");
    Console.WriteLine("8. バックアップを作成");
    Console.WriteLine("9. バックアップから復元");
    Console.WriteLine("0. 終了");

    Console.Write("番号を選択してください：");
    string input = Console.ReadLine() ?? "";

    switch (input)
    {
        // タスクを追加
        case "1":
            {
                Console.WriteLine("\n===== タスク追加 =====");

                // 諸要素の入力
                var details = ReadTodoDetails(repository);

                // 確認
                if (!ConfirmTodo(
                    "登録",
                    details.Title,
                    details.DueDate,
                    details.Priority))
                {
                    Console.WriteLine("登録をキャンセルしました。");
                    break;
                }

                // 登録
                try
                {
                    manager.AddTodo(
                            details.Title,
                            repository,
                            details.DueDate,
                            details.Priority);

                    AppLogger.Info($"タスクを登録しました。タスク名：{details.Title}");

                    Console.WriteLine("タスクを登録しました！");
                }
                catch (SqliteException ex)
                {
                    AppLogger.Error
                        ($"タスクの登録に失敗しました。タスク名：{details.Title}エラー：{ex.Message}");

                    ShowDatabaseError(ex);
                }

                break;
            }

        // タスク一覧を表示
        case "2":
            // 絞り込み
            Console.WriteLine();
            Console.WriteLine("表示方法を選んでください");
            Console.WriteLine("1.すべて表示");
            Console.WriteLine("2.未完了のみ表示");
            Console.WriteLine("3.完了済みのみ表示");
            Console.WriteLine("4. 期限切れのみ表示");

            Console.Write("選択：");
            string filterInput = Console.ReadLine() ?? "";

            int filter = -1;

            switch (filterInput)
            {
                case "1":
                    filter = 0;
                    break;

                case "2":
                    filter = 1;
                    break;

                case "3":
                    filter = 2;
                    break;

                case "4":
                    filter = 3;
                    break;

                default:
                    Console.WriteLine("無効な操作です。");
                    break;
            }

            if (filter == -1)
            {
                break;
            }

            // 並び替え
            Console.WriteLine();
            Console.WriteLine("並び替え方法を選んで下さい");
            Console.WriteLine("1. 通常順");
            Console.WriteLine("2. 期限が近い順");
            Console.WriteLine("3. 優先度が高い順");
            Console.WriteLine("4. おすすめ順");

            Console.Write("選択：");
            string sortInput = Console.ReadLine() ?? "";

            if (!int.TryParse(sortInput, out int sortOption) ||
                sortOption < 1 || sortOption > 4)
            {
                Console.WriteLine("無効な操作です。");
                break;
            }

            // 表示
            Console.WriteLine();
            if (!manager.ShowTodos(filter, sortOption))
            {
                Console.WriteLine("タスクはありません。");
            }

            break;

        case "3":
            CompleteTodo(manager, repository);
            break;

        case "4":
            DeleteTodo(manager, repository);
            break;

        case "5":
            EditTodo(manager, repository);
            break;

        case "6":
            SearchTodos(manager, repository);
            break;

        case "7":
            Console.WriteLine();
            Console.Write("現在の保存先：");
            Console.WriteLine(repository.ShowSavePath());
            break;

        case "8":
            TakeBackup(repository);
            break;

        case "9":
            RestoreBackup(repository);
            break;

        case "0":
            AppLogger.Info("アプリを終了しました。");

            Console.WriteLine("アプリを終了します。");
            return;

        default:
            Console.WriteLine("無効な操作です");
            break;
    }
}

/// 以下、メソッド一覧
/// タスク操作(完了、削除、編集、検索)
// 完了
static void CompleteTodo(TodoManager manager, TodoRepository repository)
{
    Console.WriteLine();
    manager.ShowTodos();

    // 空チェック
    if (!manager.CheckTodosExist())
    {
        Console.WriteLine("タスクがありません。");
        return;
    }

    Console.WriteLine();
    Console.Write("完了にするタスクの番号を入力してください：");
    string input = Console.ReadLine() ?? "";
    Console.WriteLine();

    // 入力値のチェック
    if (!int.TryParse(input, out int number) ||
         manager.GetTodoByNumber(number) is not TodoItem selectedTodo)
    {
        Console.WriteLine("無効な番号です。");
        return;
    }

    // 実際の完了作業
    try
    {
        if (manager.CompleteTodo(number, repository))
        {
            AppLogger.Info($"タスクを完了にしました。タスク番号：{number}");

            Console.WriteLine("タスクを完了にしました！");
        }
        else
        {
            AppLogger.Error($"タスクの完了に失敗しました。入力番号：{input}");

            Console.WriteLine("タスクの完了に失敗しました。");
        }
    }
    catch (SqliteException ex)
    {
        AppLogger.Error($"タスクの更新中にデータベースエラーが発生しました。詳細：{ex.Message}");

        ShowDatabaseError(ex);
    }
}

// 削除
static void DeleteTodo(TodoManager manager, TodoRepository repository)
{
    Console.WriteLine();
    manager.ShowTodos();

    if (!manager.CheckTodosExist())
    {
        Console.WriteLine("タスクがありません。");
        return;
    }

    Console.WriteLine();
    Console.Write("削除するタスクの番号を入力してください：");
    string input = Console.ReadLine() ?? "";

    if (!int.TryParse(input, out int number) ||
         manager.GetTodoByNumber(number) is not TodoItem selectedTodo)
    {
        Console.WriteLine("無効な番号です。");
        return;
    }

    // 最終確認
    Console.WriteLine();
    Console.Write("このタスクを本当に削除しますか(y/n):");
    string answer = Console.ReadLine() ?? "";
    Console.WriteLine();

    if (answer.ToLower() != "y")
    {
        Console.WriteLine("削除をキャンセルしました。");
        return;
    }

    try
    {
        if (manager.DeleteTodo(number, repository))
        {
            AppLogger.Info($"タスクを削除しました。タスク番号：{number}");

            Console.WriteLine("操作を完了しました！");
        }
        else
        {
            AppLogger.Error($"タスクの削除に失敗しました。入力番号：{input}");

            Console.WriteLine("タスクの削除に失敗しました。");
        }
    }
    catch (SqliteException ex)
    {
        AppLogger.Error($"タスクの更新中にデータベースエラーが発生しました。詳細：{ex.Message}");

        ShowDatabaseError(ex);
    }
}

// 編集
static void EditTodo(TodoManager manager, TodoRepository repository)
{
    Console.WriteLine();
    manager.ShowTodos();

    if (!manager.CheckTodosExist())
    {
        Console.WriteLine("まだタスクはありません。");
        return;
    }

    Console.WriteLine();
    Console.Write("編集するタスクの番号を入力してください：");
    string input = Console.ReadLine() ?? "";
    Console.WriteLine();

    if (!int.TryParse(input, out int number) ||
        manager.GetTodoByNumber(number) is not TodoItem selectedTodo)
    {
        Console.WriteLine("無効な番号です。");
        return;
    }

    // 注意：numberではなく、selectedTodo.Idを使用する
    var details = ReadTodoDetails(repository, selectedTodo.Id);

    if (!ConfirmTodo(
        "更新",
        details.Title,
        details.DueDate,
        details.Priority))
    {
        Console.WriteLine();
        Console.WriteLine("編集をキャンセルしました。");
        return;
    }

    Console.WriteLine();

    try
    {
        if (manager.UpdateTodoDetails(
            selectedTodo.Id,
            details.Title,
            details.DueDate,
            details.Priority,
            repository))
        {
            AppLogger.Info($"タスクを更新しました。タスク名：{details.Title}");

            Console.WriteLine("タスクを更新しました！");
        }
        else
        {
            AppLogger.Error($"タスクの更新に失敗しました。タスク番号：{selectedTodo.Id}");

            Console.WriteLine("タスクの更新に失敗しました。");
        }
    }
    catch (SqliteException ex)
    {
        AppLogger.Error($"タスクの更新中にデータベースエラーが発生しました。詳細：{ex.Message}");

        ShowDatabaseError(ex);
    }
}

// 検索
static void SearchTodos(TodoManager manager, TodoRepository repository)
{
    Console.WriteLine();
    Console.Write("検索キーワードを入力して下さい：");
    string keyword = Console.ReadLine() ?? "";
    Console.WriteLine();

    if (string.IsNullOrWhiteSpace(keyword))
    {
        Console.WriteLine("検索キーワードが入力されていません。");
        return;
    }

    Console.WriteLine("===== 検索結果 =====");

    List<TodoItem> results = repository.SearchTodos(keyword);

    if (results.Count == 0)
    {
        Console.WriteLine("該当するタスクはありません。");
        return;
    }

    for (int i = 0; i < results.Count; i++)
    {
        TodoItem todo = results[i];

        // 完了/未完了/期限切れの表記獲得
        string status = manager.GetIsCompletedText(todo);

        Console.WriteLine($"{i + 1}.[{status}]{todo.Title}");
    }
    // 以上、===== 検索結果 =====の表示

    // 以下、編集作業
    Console.Write("\n操作するタスクの番号を入力してください（0で戻る）：");
    string input = Console.ReadLine() ?? "";
    Console.WriteLine();

    // 注意：_todosではなく、検索結果resultsを使う
    if (!int.TryParse(input, out int number) ||
    number < 0 ||
    number > results.Count)
    {
        Console.WriteLine("無効な番号です。");
        return;
    }

    if (number == 0)
    {
        return;
    }

    TodoItem selectedTodo = results[number - 1];

    Console.WriteLine($"選択したタスク：{selectedTodo.Title}");
    Console.WriteLine("1. 完了にする");
    Console.WriteLine("2. 編集する");
    Console.WriteLine("3. 削除する");
    Console.WriteLine("0. 戻る");

    Console.Write("操作を入力してください：");
    string operation = Console.ReadLine() ?? "";
    Console.WriteLine();

    switch (operation)
    {
        case "1":
            try
            {
                if (manager.CompleteTodoById(selectedTodo.Id, repository))
                {
                    AppLogger.Info($"タスクを完了にしました。タスク名：{selectedTodo.Title}");

                    Console.WriteLine("タスクを完了にしました。");
                }
                else
                {
                    AppLogger.Error($"タスクの更新に失敗しました。タスク名：{selectedTodo.Title}");

                    Console.WriteLine("タスクの更新に失敗しました。");
                }
            }
            catch (SqliteException ex)
            {
                AppLogger.Error($"タスクの更新中にデータベースエラーが発生しました。詳細：{ex.Message}");

                ShowDatabaseError(ex);
            }

            break;

        case "2":
            var details = ReadTodoDetails(
                repository,
                selectedTodo.Id);

            if (!ConfirmTodo(
                "更新",
                details.Title,
                details.DueDate,
                details.Priority))
            {
                Console.WriteLine("タスクの更新をキャンセルしました。");
                break;
            }

            Console.WriteLine();

            try
            {
                bool success = manager.UpdateTodoDetails(
                    selectedTodo.Id,
                    details.Title,
                    details.DueDate,
                    details.Priority,
                    repository);

                if (success)
                {
                    AppLogger.Info($"タスクを更新しました。タスク名：{details.Title}");

                    Console.WriteLine("タスクを更新しました！");
                }
                else
                {
                    AppLogger.Error($"タスクの更新に失敗しました。タスク名：{selectedTodo.Title}");

                    Console.WriteLine("タスクの更新に失敗しました。");
                }
            }
            catch (SqliteException ex)
            {
                AppLogger.Error($"タスクの更新中にデータベースエラーが発生しました。詳細：{ex.Message}");

                ShowDatabaseError(ex);
            }

            break;

        case "3":
            Console.Write("本当に削除しますか(y/n):");
            string answer = Console.ReadLine() ?? "";
            Console.WriteLine();

            if (answer.ToLower() != "y")
            {
                Console.WriteLine("削除をキャンセルしました。");
                break;
            }

            try
            {
                if (manager.DeleteTodoById(selectedTodo.Id, repository))
                {
                    AppLogger.Info($"タスクを削除しました。タスク名：{selectedTodo.Title}");

                    Console.WriteLine("タスクを削除しました。");
                }
                else
                {
                    AppLogger.Error($"タスクの削除に失敗しました。タスク名：{selectedTodo.Title}");

                    Console.WriteLine("タスクの削除に失敗しました。");
                }
            }
            catch (SqliteException ex)
            {
                AppLogger.Error($"タスクの更新中にデータベースエラーが発生しました。詳細：{ex.Message}");

                ShowDatabaseError(ex);
            }

            break;

        case "0":
            Console.WriteLine("検索画面に戻ります。");
            break;

        default:
            Console.WriteLine("正しい番号を入力してください。");
            break;
    }
}

/// 入力操作(名前、期限、優先度、確認)
// 入力まとめ
static (string Title, DateTime? DueDate, TodoPriority Priority)
    ReadTodoDetails(TodoRepository repository, int excludeId = 0)
{
    string title = ReadTodoTitle(repository, excludeId);

    DateTime? dueDate = ReadDueDate();

    TodoPriority priority = ReadPriority();

    return (title, dueDate, priority);
}

// 名前入力
static string ReadTodoTitle(TodoRepository repository, int excludeId = 0)
{
    while (true)
    {
        Console.Write("タスク名を入力してください：");
        string title = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("タスク名は空欄にできません。");
            continue;
        }

        title = title.Trim();

        if (title.Length > 50)
        {
            Console.WriteLine("タスク名は50文字以内で入力してください。");
            continue;
        }

        if (repository.ExistsTodoTitle(title, excludeId))
        {
            Console.WriteLine("同じ名前のタスクが存在します。");
            Console.WriteLine("別の名前を入力してください。");
            continue;
        }

        return title;
    }
}

// 期限入力
static DateTime? ReadDueDate()
{
    while (true)
    {
        Console.Write("期限を入力してください(yyyy-MM-dd、省略可)：");
        string input = Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (DateTime.TryParseExact(
            input,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime date))
        {
            return date;
        }

        Console.WriteLine("日付の形式が正しくありません。");
        Console.WriteLine("例：2026-11-11の形式で入力してください。");
    }
}

// 優先度入力
static TodoPriority ReadPriority()
{
    while (true)
    {
        Console.WriteLine("優先度を選んでください");
        Console.WriteLine("1. 低");
        Console.WriteLine("2. 中");
        Console.WriteLine("3. 高");

        Console.Write("番号を入力してください：");
        string input = Console.ReadLine() ?? "";

        if (int.TryParse(input, out int number)
            && number >= 1
            && number <= 3)
        {
            return (TodoPriority)number;
        }

        Console.WriteLine("1～3の数字を入力してください。");
    }
}

// 入力確認
static bool ConfirmTodo(
    string action,
    string title,
    DateTime? dueDate,
    TodoPriority priority)
{
    Console.WriteLine();
    Console.WriteLine($"===== {action}内容の確認 =====");

    Console.WriteLine($"タスク名：{title}");

    string dueDateText = dueDate.HasValue
        ? dueDate.Value.ToString("yyyy-MM-dd")
        : "未設定";

    Console.WriteLine($"期限：{dueDateText}");

    string priorityText = priority switch
    {
        TodoPriority.Low => "★ 低",
        TodoPriority.Medium => "★★ 中",
        TodoPriority.High => "★★★ 高",
        _ => "不明"
    };

    Console.WriteLine($"優先度：{priorityText}");

    Console.WriteLine();
    Console.WriteLine($"この内容で{action}しますか？");
    Console.WriteLine($"1. {action}する");
    Console.WriteLine("2. キャンセル");

    Console.Write("選択：");
    string input = Console.ReadLine() ?? "";

    return input == "1";
}

/// エラー処理
// データベース
static void ShowDatabaseError(SqliteException ex)
{
    if (ex.SqliteExtendedErrorCode == 2067)
    {
        Console.WriteLine("同じ名前のタスクがすでに存在します。");
    }
    else
    {
        Console.WriteLine($"データベースにエラーが発生しました：{ex.Message}");
    }
}

/// バックアップ(取得、復元)
// バックアップ取得
static void TakeBackup(TodoRepository repository)
{
    try
    {
        string backupPath = repository.BackupDatabase();

        AppLogger.Info($"バックアップを作成しました。保存先：{backupPath}");

        Console.WriteLine();
        Console.WriteLine("バックアップが完了しました！");
        Console.WriteLine($"保存先：{backupPath}");
    }
    catch (SqliteException ex)
    {
        AppLogger.Error($"データベースのバックアップに失敗しました。エラー：{ex.Message}");

        Console.WriteLine("データベースのバックアップに失敗しました。");
    }
    catch (IOException ex)
    {
        AppLogger.Error($"データベースのバックアップに失敗しました。エラー：{ex.Message}");

        Console.WriteLine("バックアップファイルの作成に失敗しました。");
    }
    catch (UnauthorizedAccessException ex)
    {
        AppLogger.Error($"データベースのバックアップに失敗しました。エラー：{ex.Message}");

        Console.WriteLine("バックアップ先へのアクセスが許可されていません。");
    }
}

// バックアップ復元
static void RestoreBackup(TodoRepository repository)
{
    Console.WriteLine();
    Console.Write("復元するバックアップファイルのパスを入力してください：");
    string backupPath = Console.ReadLine() ?? "";
    Console.WriteLine();

    backupPath = backupPath.Trim('"');

    if (string.IsNullOrWhiteSpace(backupPath))
    {
        Console.WriteLine("ファイルパスが入力されていません。");
        return;
    }

    Console.WriteLine("指定したバックアップからデータを復元します。");
    Console.WriteLine("現在のデータが上書きされる可能性があります。");
    Console.WriteLine("本当に復元しますか？");
    Console.WriteLine("1. 復元する");
    Console.WriteLine("2. キャンセル");

    Console.Write("選択：");
    string confirmation = Console.ReadLine() ?? "";
    Console.WriteLine();

    if (confirmation != "1")
    {
        Console.WriteLine("復元をキャンセルしました。");
        return;
    }

    try
    {
        repository.RestoreDatabase(backupPath);

        AppLogger.Info($"バックアップからデータを復元しました。復元元：{backupPath}");

        Console.WriteLine("データの復元が完了しました！");
    }
    catch (FileNotFoundException ex)
    {
        AppLogger.Error($"バックアップファイルが見つかりません。指定先：{backupPath} エラー：{ex.Message}");

        Console.WriteLine("指定されたバックアップファイルが見つかりません。");
    }
    catch (SqliteException ex)
    {
        AppLogger.Error($"データベースの復元に失敗しました。エラー：{ex.Message}");

        Console.WriteLine("データベースの復元に失敗しました。");
    }
    catch (IOException ex)
    {
        AppLogger.Error($"データベースの復元に失敗しました。エラー：{ex.Message}");

        Console.WriteLine("ファイルの読み書きに失敗しました。");
    }
    catch (UnauthorizedAccessException ex)
    {
        AppLogger.Error($"データベースの復元に失敗しました。エラー：{ex.Message}");

        Console.WriteLine("ファイルへのアクセスが許可されていません。");
    }
}