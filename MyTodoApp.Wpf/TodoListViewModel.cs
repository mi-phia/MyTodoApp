using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Collections;
using System.Linq;
using MyTodoApp;

namespace MyTodoApp.Wpf
{
    public class TodoListViewModel : INotifyPropertyChanged
    {
        // 表示専用のタスク一覧
        public ObservableCollection<TodoItem> Todos { get; }

        // 検索用の一覧
        public ListCollectionView TodosView { get; }

        // 検索・絞り込み後のタスク件数
        public int FilteredCount =>
            TodosView.Cast<TodoItem>().Count();

        // 検索用の文字列
        private string _searchText = "";

        public string SearchText 
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;

                _searchText = value;

                // 検索欄の変更を通知
                OnPropertyChanged();

                // 検索結果の画面を更新
                TodosView.Refresh();
                
                // 検索件数の変更を通知
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

                // 並び替えメソッドの呼び出し
                ApplySort();
            }
        }

        // コンストラクター
        public TodoListViewModel(TodoManager manager)
        {
            // 表示用のタスク一覧を取得
            Todos = manager.Todos;

            // 表示・検索・並び替えに対応できる型に変更
            TodosView = (ListCollectionView)
                CollectionViewSource.GetDefaultView(Todos);

            // 検索メソッドを設定
                // ()がないメソッドは必要回数呼び出しが可能
            TodosView.Filter = FilterTodos;

            // 検索件数の変更の監視を追加
            Todos.CollectionChanged += (sender, e) =>
            {
                OnPropertyChanged(nameof(FilteredCount));
            };
        }

// 以下、処理メソッド
        // 検索処理
        private bool FilterTodos(object item)
        {
            if (item is not TodoItem todo)
            {
                return false;
            }

            // 検索欄が空なら全件表示
                // Trueなら表示、Falseなら非表示
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                // 検索文字が含まれるならTrue(表示)
                bool matcheskeyword = todo.Title.Contains(
                    SearchText.Trim(),
                    StringComparison.CurrentCultureIgnoreCase);

                if (!matcheskeyword)
                {
                    return false;
                }
            }

            // 完了状態による絞り込み
                // 1:未完了 l 2:完了 l 3:期限切れ
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

        // 期限による並び替え
        private sealed class DueDateComparer : IComparer
        {
            // 正の数なら前者が、負の数なら後者が後に並ぶ
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
                if (x is not TodoItem a ||
                    y is not TodoItem b)
                {
                    return 0;
                }

                int result = b.Priority.CompareTo(a.Priority);

                return result != 0
                    ? result
                    : a.Id.CompareTo(b.Id);
            }
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
    }
}