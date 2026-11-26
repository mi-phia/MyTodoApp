using System.Collections.ObjectModel;
using MyTodoApp;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Collections;
using System.Linq;

namespace MyTodoApp.wpf
{
    public class TodoListViewModel
    {
        // 画面に表示するタスク一覧
        public ObservableCollection<TodoItem> Todos { get; }

        // 検索用の一覧
        public ListCollectionView TodosView { get; }

        // 現在表示されているタスクの件数
        public int FilteredCount =>
            TodosView.Cast<TodoItem>().Count();

        // 検索文字列
        private string _searchText = "";

        public string SearchText 
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;

                _searchText = value;

                OnPropertyChanged();

                // 検索結果を更新
                TodosView.Refresh();

                OnPropertyChanged(nameof(FilteredCount));
            }
        }

        // 絞り込み条件
        private int _filterMode = 0;

        public int FilterMode
        {
            get => _filterMode;
            set
            {
                if(_filterMode == value) return;

                _filterMode = value;

                OnPropertyChanged();

                TodosView.Refresh();

                System.Diagnostics.Debug.WriteLine(
    $"Todosの件数：{Todos.Count}");

                System.Diagnostics.Debug.WriteLine(
                    $"TodosViewの件数：{TodosView.Cast<TodoItem>().Count()}");

                System.Diagnostics.Debug.WriteLine(
                    $"検索文字：[{SearchText}]");

                System.Diagnostics.Debug.WriteLine(
                    $"Filterの設定：{TodosView.Filter != null}");

                OnPropertyChanged(nameof(FilteredCount));
            }
        }

        // 並び替え条件
        private int _sortMode = 0;

        public int SortMode
        {
            get => _sortMode;
            set 
            {
                if (_sortMode == value) return;

                _sortMode = value;

                OnPropertyChanged();

                ApplySort();
            }
        }

        // コンストラクター
        public TodoListViewModel(TodoManager manager)
        {
            Todos = manager.Todos;

            // 一覧を検索用に設定
            TodosView = (ListCollectionView)
                CollectionViewSource.GetDefaultView(Todos);

            // 検索条件を設定
            TodosView.Filter = FilterTodos;

            // タスク一覧の追加・削除を監視
            Todos.CollectionChanged += (sender, e) =>
            {
                OnPropertyChanged(nameof(FilteredCount));
            };
        }

        // 検索処理
        private bool FilterTodos(object item)
        {
            if (item is not TodoItem todo)
            {
                return false;
            }

            // キーワード検索
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                bool matcheskeyword = todo.Title.Contains(
                    SearchText.Trim(),
                    StringComparison.CurrentCultureIgnoreCase);

                if (!matcheskeyword)
                {
                    return false;
                }
            }

            // 完了状態による絞り込み
            return FilterMode switch
            {
                0 => true,
                1 => !todo.IsCompleted,
                2 => todo.IsCompleted,
                
                3 => !todo.IsCompleted
                    && todo.DueDate.HasValue
                    && todo.DueDate.Value.Date < DateTime.Today,

                _ => true
            };            
        }

        // 並び替え処理
        private void ApplySort()
        {
            // 追加順に並べる(デフォルト)
            if (SortMode == 0)
            {
                TodosView.CustomSort = null;
            }
            // 期限が近い順に並べる
            else if (SortMode == 1)
            {
                TodosView.CustomSort = new DueDateComparer();
            }
            // 優先度が高い順にならべる
            else if (SortMode == 2)
            {
                TodosView.CustomSort = new PriorityComparer();
            }

            TodosView.Refresh();
        }

        // プロパティ変更通知
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        // 期限による並び替え
        private sealed class DueDateComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                // TodoItemを取得
                if (x is not TodoItem a ||
                    y is not TodoItem b)
                {
                    return 0;
                }

                // 両方とも期限なしの場合
                if (!a.DueDate.HasValue &&
                   !b.DueDate.HasValue)
                {
                    return a.Id.CompareTo(b.Id);
                }

                // 期限なしは最後にする
                if (!a.DueDate.HasValue)
                {
                    return 1;
                }

                if (!b.DueDate.HasValue)
                {
                    return -1;
                }

                // 期限が近い順に比較
                int result = DateTime.Compare(
                    a.DueDate.Value.Date,
                    b.DueDate.Value.Date);

                // 同じ期限の場合はID順
                return result != 0
                    ? result
                    : a.Id.CompareTo(b.Id);
            }          
        }

        // 優先度による並び替え
        private sealed class PriorityComparer : IComparer
        {
            public int Compare(object? x, object? y)
            {
                // TodoItemを取得
                if (x is not TodoItem a ||
                    y is not TodoItem b)
                {
                    return 0;
                }

                // 優先度が高い順に比較
                int result = b.Priority.CompareTo(a.Priority);

                // 同じ優先度ならID順
                return result != 0
                    ? result
                    : a.Id.CompareTo(b.Id);
            }
        }
    }
}
