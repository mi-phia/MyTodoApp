using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MyTodoApp;
using MyTodoApp.wpf;

namespace MyTodoApp.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // DB操作用
        private readonly TodoRepository _repository = new();

        // タスク管理用
        private readonly TodoManager _manager = new();

        // ViewModel用
        private readonly TodoListViewModel _viewModel;

        // 編集対象のタスク
        private TodoItem? _editingTodo;

        // 削除対象のタスク
        private TodoItem? _deletingTodo;

        public MainWindow()
        {
            // 画面の部品に関する初期化処理
            InitializeComponent();

            // DBに関する初期化処理
            _repository.InitializeDatabase();

            // DBからタスクを読み込む
            _manager.LoadTodosFromDatabase(_repository);

            // ViewModelを作成
            _viewModel = new TodoListViewModel(_manager);

            // DataContextにViewModelを設定
            DataContext = _viewModel;
        }

// 以下、イベント
        // 追加ボタンがクリックされたときの処理
        private void AddButtun_Click(
            object sender,
            RoutedEventArgs e)
        {
            // タスク名を取得
            string taskTitle = TaskInputTextBox.Text.Trim();

            // 空欄チェック
            if (string.IsNullOrWhiteSpace(taskTitle))
            {
                MessageBox.Show("タスク名を入力してください");
                return;
            }

            // 期限を取得
            DateTime? dueDate = DueDatePicker.SelectedDate;

            // 期限のバリデーション
            if (dueDate.HasValue &&
                dueDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show(
                    "期限には今日以降の日付を設定してください。",
                    "入力内容の確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // 優先度の選択番号を取得
            int priorityIndex = PriorityComboBox.SelectedIndex;

            // 優先度のバリデーション
            if (priorityIndex < 0 || priorityIndex > 2)
            {
                MessageBox.Show(
                    "優先度を選択してください。" ,
                    "入力内容の確認",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // 選択番号をTodoPriorityに変換(1ずつズレているのに注意！)
            TodoPriority priority = PriorityComboBox.SelectedIndex switch
            {
                0 => TodoPriority.Low,
                1 => TodoPriority.Medium,
                2 => TodoPriority.High,
                _ => TodoPriority.Medium
            };

            // 編集か追加かで処理を分ける
            if (_editingTodo != null)
            {
                // 既存のタスクを編集
                bool updated = _manager.UpdateTodoDetails(
                    _editingTodo.Id,
                    taskTitle,
                    dueDate,
                    priority,
                    _repository);

                if (!updated)
                {
                    MessageBox.Show("タスクを更新できませんでした。");
                    return;
                }

                TaskStatusTextBlock.Text =
                    $"「{taskTitle}」を更新しました。";
            }
            else
            {
                // DBに新しいタスクを追加
                _manager.AddTodo(
                    taskTitle,
                    _repository,
                    dueDate,
                    priority);
            }

            // 編集モードの解除
            _editingTodo = null;
            AddButton.Content = "追加";

            // 入力欄をリセット
            TaskInputTextBox.Clear();
            DueDatePicker.SelectedDate = null;
            PriorityComboBox.SelectedIndex = 1;

            // 入力欄にフォーカスを戻す
            TaskInputTextBox.Focus();
        }

        // チェックボックスがクリックされた時の処理
        private void TaskCheckBox_Changed(
            object sender,
            RoutedEventArgs e)
        {
            // チェックボックスを取得
            if (sender is not CheckBox checkBox)
            {
                return;
            }

            // チェックボックスのTodoを取得
            if (checkBox.DataContext is not TodoItem todo) 
            {
                return;
            }

            // 現在のチェック状況を取得(Checked/Unchecked == true/false)
            bool isCompleted = checkBox.IsChecked == true;

            // DBの完了状態を更新
            bool updated = _manager.UpdateTodoStatusById(
                todo.Id,
                isCompleted,
                _repository);

            if (!updated)
            {
                MessageBox.Show(
                    "タスクの完了状態を保存できませんでした。",
                    "保存エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            //操作結果を表示
            if (isCompleted)
            {
                TaskStatusTextBlock.Text =
                    $"「{todo.Title}」を完了にしました。";
            }
            else 
            {
                TaskStatusTextBlock.Text =
                    $"「{todo.Title}」を未完了にしました。";
            }

            // 完了所帯の変更を絞り込みに反映
            _viewModel.TodosView.Refresh();
        }

        // 編集ボタンがクリックされた時の処理
        private void EditButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // クリックされたボタンを取得
            if (sender is not Button button ||
                button.DataContext is not TodoItem todo)
            {
                return;
            }

            // 編集対象を保持(他のメソッドで使えるように)
            _editingTodo = todo;

            // 情報をフォームに設定
            TaskInputTextBox.Text = todo.Title;
            DueDatePicker.SelectedDate = todo.DueDate;
            PriorityComboBox.SelectedIndex = todo.Priority switch
            {
                TodoPriority.Low => 0,
                TodoPriority.Medium => 1,
                TodoPriority.High => 2,
                _ => 1
            };

            // 入力欄にフォーカスを戻す
            TaskInputTextBox.Focus();
            TaskInputTextBox.SelectAll();

            // ボタンの表示を更新に変更
            AddButton.Content = "更新";

            // 操作結果を表示
            TaskStatusTextBlock.Text =
                $"「{todo.Title}」を編集中です。";         
        }

        // 削除ボタンがクリックされた時の処理
        private void DeleteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // クリックされたボタンを取得
            if(sender is not Button button)
            {
                return;
            }

            // 削除対象のタスクを取得
            if (button.DataContext is not TodoItem todo)
            {
                return;
            }

            // 削除対象を保持
            _deletingTodo = todo;

            // 削除確認ダイアログを表示
            MessageBoxResult result = MessageBox.Show(
                $"「{todo.Title}」を削除してもよろしいですか？",
                "削除の確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            // 「はい」が選択された場合
            if (result == MessageBoxResult.Yes)
            {
                // DBから削除する
                bool deleted = _manager.DeleteTodoById(
                    todo.Id,
                    _repository);

                // 削除に失敗した場合
                if (!deleted)
                {
                    MessageBox.Show(
                        "タスクを削除できませんでした。",
                        "削除エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    _deletingTodo = null;
                    return;
                }

                // 削除に成功した場合
                TaskStatusTextBlock.Text =
                    $"「{todo.Title}」を削除しました。";

                _deletingTodo = null;
            }
            // 「いいえ」が選択された場合
            else
            {
                _deletingTodo = null;

                TaskStatusTextBlock.Text =
                    "削除をキャンセルしました。";
            }            
        }
    }
}