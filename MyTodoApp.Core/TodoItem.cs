using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodoApp
{
    public class TodoItem : INotifyPropertyChanged
    {
        // プロパティの値を保存する変数
        private int _id;
        private string _title = "";
        private bool _isCompleted;
        private DateTime? _dueDate;
        private TodoPriority _priority = TodoPriority.Medium;

        // プロパティ変更通知イベント
        public event PropertyChangedEventHandler? PropertyChanged;

        // ID
        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        // タスク名
        public string Title
        {
            get => _title;
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        // 完了状態
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted == value) return;

                _isCompleted = value;
                OnPropertyChanged();

                // 期限切れ状態も更新する
                OnPropertyChanged(nameof(IsOverdue));
            }
        }

        // 期限
        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                if (_dueDate == value) return;

                _dueDate = value;
                OnPropertyChanged();

                // 期限切れ状態も更新する
                OnPropertyChanged(nameof(IsOverdue));
            }
        }

        // 優先度
        public TodoPriority Priority
        {
            get => _priority;
            set
            {
                if (_priority == value) return;
                _priority = value;
                OnPropertyChanged();
            }
        }

        // プロパティ変更通知
        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        // 期限切れかどうか
        public bool IsOverdue =>
            !IsCompleted &&
            DueDate.HasValue &&
            DueDate.Value.Date < DateTime.Today;

    }

    // 優先度
    public enum TodoPriority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }
}