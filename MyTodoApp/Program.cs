using MyTodoApp;

TodoRepository repository = new TodoRepository();
TodoManager manager = new TodoManager();

repository.InitializeDatabase();
manager.LoadTodosFromDatabase(repository);

while (true)
{
    Console.WriteLine();
    Console.WriteLine("==== My Todo App ===");
    Console.WriteLine("1. タスクを追加");
    Console.WriteLine("2. タスク一覧を表示");
    Console.WriteLine("3. タスクを完了にする");
    Console.WriteLine("4. タスクを削除");
    Console.WriteLine("5. タスクを編集");
    Console.WriteLine("0. 終了");
    Console.WriteLine("番号を選択してください");

    string? input = Console.ReadLine();

    switch (input)
    {
        case "1":
            AddTodo(manager, repository);
            break;

        case "2":
            Console.WriteLine("表示方法を選んでください");
            Console.WriteLine("1.すべて表示");
            Console.WriteLine("2.未完了のみ表示");
            Console.WriteLine("3.完了済みのみ表示");
            Console.Write("選択：");

            string? filterInput = Console.ReadLine();

            switch (filterInput)
            {
                case "1":
                    manager.ShowTodos(0);
                    break;

                case "2":
                    manager.ShowTodos(1);
                    break;

                case "3":
                    manager.ShowTodos(2);
                    break;

                default:
                    Console.WriteLine("無効な操作です。");
                    break;
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

        case "0":
            Console.WriteLine("アプリを終了します。");
            return;

        default:
            Console.WriteLine("無効な操作です");
            break;
    }
}

static void AddTodo(TodoManager manager, TodoRepository repository)
{
    Console.Write("タスク名を入力してください(終了するには空欄)：");
    string? title = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(title))
    {
        Console.WriteLine("タスク名が入力されていません");
        return;
    }

    manager.AddTodo(title.Trim(), repository);

    Console.WriteLine("タスクを追加しました！");
}

static void CompleteTodo(TodoManager manager, TodoRepository repository)
{
    Console.Write("完了にするタスクの番号を入力してください(省略するには空欄)：");
    string? input = Console.ReadLine();

    if (int.TryParse(input, out int number)
        && manager.CompleteTodo(number, repository))
    {
        Console.WriteLine("タスクを完了にしました！");
    }
    else
    {
        Console.WriteLine("無効な番号です。");
    }
}

static void DeleteTodo(TodoManager manager, TodoRepository repository)
{
    manager.ShowTodos();

    Console.Write("削除するタスクの番号を入力してください：");
    string? input = Console.ReadLine();

    if (int.TryParse(input, out int number)
        && manager.DeleteTodo(number, repository))
    {
        Console.WriteLine("操作を完了しました！");
    }
    else
    {
        Console.WriteLine("無効な番号です。");
    }
}

static void EditTodo(TodoManager manager, TodoRepository repository)
{
    manager.ShowTodos();

    Console.Write("編集するタスクの番号を入力してください：");
    string? input = Console.ReadLine();

    if (!int.TryParse(input, out int number))
    {
        Console.WriteLine("番号を正しく入力してください");
        return;
    }

    Console.Write("新しいタスク名を入力してください：");
    string? newTitle = Console.ReadLine();

    if (string.IsNullOrEmpty(newTitle))
    {
        Console.WriteLine("タスク名が空欄です");
        return;
    }

    if (manager.EditTodo(number, newTitle, repository))
    {
        Console.WriteLine("タスクを更新しました！");
    }
    else
    {
        Console.WriteLine("無効な番号です。");
    }
}
