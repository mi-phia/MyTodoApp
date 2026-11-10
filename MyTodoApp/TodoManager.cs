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

        public void AddTodo(string title, TodoRepository repository)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine("タスク名を入力してください。");
                return;
            }

            int id = repository.InsertTodo(title);

            _todos.Add(new TodoItem
            {
                Id = id,
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

        public bool CompleteTodo(int number, TodoRepository repository)
        {
            if (number < 1 || number > _todos.Count)
            {
                return false;
            }

            TodoItem todo = _todos[number - 1];

            if (!repository.UpdateTodoStatus(todo.Id, true))
            {
                return false;
            }

            _todos[number - 1].IsCompleted = true;
            return true;
        }

        public bool DeleteTodo(int number, TodoRepository repository)
        {
            if (number < 1 || number > _todos.Count)
            {
                return false;
            }

            TodoItem todo = _todos[number - 1];

            if (!repository.DeleteTodo(todo.Id))
            {
                return false;
            }

            _todos.RemoveAt(number - 1);
            return true;
        }

        public bool EditTodo(
            int number, 
            string newTitle,
            TodoRepository repository)
        {
            if (number < 1 || number > _todos.Count)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(newTitle))
            {
                return false;
            }

            TodoItem todo = _todos[number - 1];
            string trimmedTitle = newTitle.Trim();

            if (repository.UpdateTodoTitle(todo.Id, trimmedTitle))
            {
                return false;
            }

            todo.Title = trimmedTitle;
            return true;
        }

        public void LoadTodosFromDatabase(
            TodoRepository repository)
        {
            List<TodoItem> loadedTodos =
                repository.GetAllTodos();

            _todos.Clear();

            _todos.AddRange(loadedTodos);

            Console.WriteLine("SQLiteからタスクを読み込みました");
        }
    }
}