using MyTodoApp;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace MyTodoApp
{
    public class TodoManager
    {
        private readonly List<TodoItem> _todos = new();

        public IReadOnlyList<TodoItem> Todos => _todos;

        private static readonly string FilePath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MyTodoApp",
                "todos.json");

        public void AddTodo(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine("タスク名を入力してください。");
                return;
            }

            _todos.Add(new TodoItem
            {
                Title = title,
                IsCompleted = false
            });
        }

        public void ShowTodos(int filter = 0)
        {
            var filteredTodos = _todos
                .Where(todo =>
                  filter == 0 ||
                  (filter == 1 && !todo.IsCompleted) ||
                  (filter == 2 && todo.IsCompleted))
                .ToList();

            if (filteredTodos.Count == 0)
            {
                Console.WriteLine("タスクはありません");
                return;
            }

            for (int i = 0; i < filteredTodos.Count; i++)
            {
                var todo = filteredTodos[i];

                int originalNumber = _todos.IndexOf(todo) + 1;

                string status = todo.IsCompleted ? "完了" : "未完了";

                Console.WriteLine($"{originalNumber}. [{status}]{todo.Title}");
            }
        }

        public bool CompleteTodo(int number)
        {
            if (number < 1 || number > _todos.Count)
            {
                return false;
            }

            _todos[number - 1].IsCompleted = true;
            return true;
        }

        public bool DeleteTodo(int number)
        {
            if (number < 1 || number > _todos.Count)
            {
                return false;
            }

            _todos.RemoveAt(number - 1);
            return true;
        }

        public bool EditTodo(int number, string newTitle)
        {
            if (number < 1 || number > _todos.Count)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(newTitle))
            {
                return false;
            }

            _todos[number - 1].Title = newTitle.Trim();
            return true;
        }

        public void SaveTodos()
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(FilePath)!);

                string json = JsonSerializer.Serialize(_todos);

                File.WriteAllText(FilePath, json);

                Console.WriteLine("タスクを保存しました。");
            }
            catch (IOException)
            {
                Console.WriteLine("ファイルへの保存に失敗しました。");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("フェイルへのアクセスが許可されていません。");
            }
        }

        public void LoadTodos()
        {
            if (!File.Exists(FilePath))
            {
                Console.WriteLine("保存データがありません。");
                return;
            }
            try
            {

                string json = File.ReadAllText(FilePath);

                var loadedTodos =
                    JsonSerializer.Deserialize<List<TodoItem>>(json);

                if (loadedTodos != null)
                {
                    _todos.Clear();
                    _todos.AddRange(loadedTodos);
                }

                Console.WriteLine("タスクを読み込みました。");
            }
            catch (JsonException)
            {
                Console.WriteLine("保存データの形式が正しくありません。");
            }
            catch (IOException)
            {
                Console.WriteLine("ファイルの読み込みに失敗しました。");
            }
        }

        public void ShowSavePath()
        {
            Console.WriteLine("現在の保存先：");
            Console.WriteLine(FilePath);
        }
    }
}