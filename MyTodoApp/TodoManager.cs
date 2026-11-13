using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Net.NetworkInformation;

namespace MyTodoApp
{
    public class TodoManager
    {
        private readonly List<TodoItem> _todos = new();
        public IReadOnlyList<TodoItem> Todos => _todos;

/// タスクの操作(追加、表示、完了、削除、編集)
        // 追加
        public void AddTodo(
            string trimmedTitle,
            TodoRepository repository,
            DateTime? dueDate = null,
            TodoPriority priority = TodoPriority.Medium
            )
        { 
            // SQLへの追加(IDが返ってくる)
            int id = repository.InsertTodo(
                trimmedTitle,
                dueDate,
                priority);

            // Listへの追加
            _todos.Add(new TodoItem
            {
                Id = id,
                Title = trimmedTitle,
                IsCompleted = false,
                DueDate = dueDate,
                Priority = priority
            });
        }

        // 表示
        public bool ShowTodos(int filter = 0, int sortOption = 0)
        {
            // フィルターによる絞り込み
            var filteredTodos = _todos
                .Where(todo =>
                  filter == 0 ||
                  (filter == 1 && !todo.IsCompleted) ||
                  (filter == 2 && todo.IsCompleted) ||
                  (filter == 3 &&
                        !todo.IsCompleted &&
                        todo.DueDate.HasValue &&
                        todo.DueDate.Value.Date < DateTime.Today))
                .ToList();

            // ソートによる並び替え
            switch (sortOption)
            {
                case 2:
                    filteredTodos = filteredTodos
                        .OrderBy(todo => todo.DueDate ?? DateTime.MaxValue)
                        .ToList();
                    break;

                case 3:
                    filteredTodos = filteredTodos
                        .OrderByDescending(todo => todo.Priority)
                        .ToList();
                    break;

                case 4:
                    filteredTodos = filteredTodos
                        .OrderBy(todo => todo.DueDate ?? DateTime.MaxValue)
                        .ThenByDescending(todo => todo.Priority)
                        .ToList();
                    break;
            }

            if (filteredTodos.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < filteredTodos.Count; i++)
            {
                var todo = filteredTodos[i];

                int originalNumber = _todos.IndexOf(todo) + 1;

                string status = GetIsCompletedText(todo);

                string dueDateText = GetDueDateText(todo);

                string priorityText = GetPriorityText(todo);

                Console.WriteLine($"{originalNumber}. [{status}]{todo.Title}");
                Console.WriteLine($"　期限：{dueDateText}");
                Console.WriteLine($"　優先度：{priorityText}");
            }

            return true;
        }

        // 完了
        public bool CompleteTodo(int number, TodoRepository repository)
        {
            TodoItem todo = _todos[number - 1];

            if (!repository.UpdateTodoStatus(todo.Id, true))
            {
                return false;
            }

            _todos[number - 1].IsCompleted = true;
            return true;
        }

        // 削除
        public bool DeleteTodo(int number, TodoRepository repository)
        {
            TodoItem todo = _todos[number - 1];

            if (!repository.DeleteTodo(todo.Id))
            {
                return false;
            }

            _todos.RemoveAt(number - 1);
            return true;
        }

        // 編集
        public bool UpdateTodoDetails(
            int id,
            string newTitle,
            DateTime? dueDate,
            TodoPriority priority,
            TodoRepository repository)
        {
            bool success = repository.UpdateTodoDetails(
                id,
                newTitle,
                dueDate,
                priority
                );

            if (!success)
            {
                return false;
            }

            LoadTodosFromDatabase(repository);

            return true;
        }

/// 表記方法まとめ(完了、期日、優先度)
        // 完了
        public string GetIsCompletedText(TodoItem todo)
        {
            string status;

            if (todo.IsCompleted)
            {
                status = "完了";
            }
            else if (todo.DueDate.HasValue &&
                    todo.DueDate.Value.Date < DateTime.Today)
            {
                status = "期限切れ";
            }
            else
            {
                status = "未完了";
            }

            return status;
        }


        // 期日
        public string GetDueDateText(TodoItem todo)
        {
            string dueDateText;

            if (!todo.DueDate.HasValue)
            {
                dueDateText = "未設定";
            }
            else
            {
                DateTime today = DateTime.Today;
                DateTime dueDate = todo.DueDate.Value.Date;

                int remainingDays = (dueDate - today).Days;

                if (todo.IsCompleted)
                {
                    dueDateText = dueDate.ToString("yyyy-MM-dd");
                }
                else if (remainingDays < 0)
                {
                    dueDateText = $"{dueDate:yyyy-MM-dd}(期限切れ)";
                }
                else if (remainingDays == 0)
                {
                    dueDateText = $"{dueDate:yyyy-MM-dd}(本日が期限)";
                }
                else if (remainingDays < 3)
                {
                    dueDateText = $"{dueDate:yyyy-MM-dd}(あと{remainingDays}日)";
                }
                else
                {
                    dueDateText = dueDate.ToString("yyyy-MM-dd");
                }
            }

            return dueDateText;
        }

        // 優先度
        private string GetPriorityText(TodoItem todo)
        {
            return todo.Priority switch
            {
                TodoPriority.Low => "★ 低",
                TodoPriority.Medium => "★★ 中",
                TodoPriority.High => "★★★ 高",
                _ => "不明"
            };
        }

/// その他まとめ
        // SQLとListの同期
        public void LoadTodosFromDatabase(
            TodoRepository repository)
        {
            List<TodoItem> loadedTodos =
                repository.GetAllTodos();

            _todos.Clear();

            _todos.AddRange(loadedTodos);
        }

        //入力値チェック
        public TodoItem? GetTodoByNumber(int number)
        {
            if (number < 1 || number > _todos.Count)
            {
                return null;
            }

            return _todos[number - 1];
        }

        // 空リストチェック
        public bool CheckTodosExist()
        {
            if (_todos.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

    }
}