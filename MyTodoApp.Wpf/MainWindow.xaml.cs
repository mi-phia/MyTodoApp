using MyTodoApp;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MyTodoApp.Wpf
{
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

        // ウィンドウ設定の保存先
        private readonly string _windowSettingsPath =
            System.IO.Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MyTodoApp",
                "window-settings.json");

        // コンストラクター
        public MainWindow()
        {
            // 画面の部品に関する初期化処理
            InitializeComponent();

            // DBに関する初期化処理
            _repository.InitializeDatabase();

            // 前回のウィンドウ設定を読み込む
            LoadWindowSettings();

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

            // 選択番号をTodoPriorityに変換
                // (1ずつズレているのに注意！)
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

            // 入力フォームをリセット
            ResetInputForm();
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

            // 現在のチェック状況を取得
                // (Checked/Unchecked == true/false)
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

            // 完了状態の変更を絞り込みに反映
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

            // 編集対象を保持
                // (他のメソッドで使えるように)
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
                // (ダイアログ表示後も扱えるように)
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
                TaskStatusTextBlock.Text =
                    "削除をキャンセルしました。";

                _deletingTodo = null;
            }            
        }

        // 入力リセットボタンがクリックされたときの処理
        private void ResetButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // 入力フォームをリセット
            ResetInputForm();

            // 操作結果を表示
            TaskStatusTextBlock.Text =
                "入力内容をリセットしました。";
        }

        // 入力フォームをリセットする処理
        private void ResetInputForm()
        {
            // 編集モードを解除
            _editingTodo = null;

            // タスク名を空欄にする
            TaskInputTextBox.Clear();

            // 期限を未設定にする
            DueDatePicker.SelectedDate = null;

            // 優先度を「中」に戻す
            PriorityComboBox.SelectedIndex = 1;

            // ボタンの表示を「追加」に戻す
            AddButton.Content = "追加";

            // タスク名の入力欄にフォーカスを戻す
            TaskInputTextBox.Focus();
        }

// バックアップ関連
        // バックアップ作成ボタンがクリックされたときの処理
        private void BackupButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                // データベースのバックアップを作成
                string backupPath = _repository.BackupDatabase();

                // 操作結果を表示
                TaskStatusTextBlock.Text =
                    "バックアップを作成しました。";

                // 成功メッセージを表示
                MessageBox.Show(
                    $"バックアップを作成しました。\n\n保存先：\n{backupPath}",
                    "バックアップ完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // 失敗した場合
                TaskStatusTextBlock.Text =
                    "バックアップの作成に失敗しました。";

                MessageBox.Show(
                    $"バックアップの作成に失敗しました。\n\n詳細：\n{ex.Message}",
                    "バックアップエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // バックアップ復元ボタンがクリックされたときの処理
        private void RestoreButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // バックアップ保存フォルダを取得
            string databaseDirectory =
                System.IO.Path.GetDirectoryName(
                    _repository.ShowSavePath())!;

            string backupDirectory =
                System.IO.Path.Combine(
                    databaseDirectory,
                    "Backups");

            // ファイル選択画面を作成
            var dialog = new OpenFileDialog
            {
                Title = "復元するバックアップを選択",

                // SQLiteファイルを選択
                Filter =
                    "バックアップファイル (*.db)|*.db|すべてのファイル (*.*)|*.*",

                // バックアップフォルダを最初に表示
                InitialDirectory = Directory.Exists(backupDirectory)
                    ? backupDirectory
                    : databaseDirectory,

                // ファイル選択時の設定
                CheckFileExists = true,
                Multiselect = false
            };

            // ファイル選択画面を表示
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            // 復元前の確認
            MessageBoxResult result = MessageBox.Show(
                $"次のバックアップから復元します。\n\n" +
                $"{dialog.FileName}\n\n" +
                "現在のタスクは置き換えられます。\n" +
                "復元を実行してもよろしいですか？",

                "バックアップ復元の確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            // 「いいえ」の場合は終了
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                // データベースを復元
                _repository.RestoreDatabase(dialog.FileName);

                // タスク一覧を再読み込み
                _manager.LoadTodosFromDatabase(_repository);

                // 一覧を更新
                _viewModel.TodosView.Refresh();

                // 検索・絞り込みを初期状態に戻す
                _viewModel.SearchText = "";
                _viewModel.FilterMode = 0;

                // 入力フォームをリセット
                ResetInputForm();

                // 操作結果を表示
                TaskStatusTextBlock.Text =
                    "バックアップから復元しました。";

                // 復元成功メッセージ
                MessageBox.Show(
                    "バックアップから復元しました。\n\n" +
                    "タスク一覧を更新しました。",
                    "復元完了",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // 復元失敗
                TaskStatusTextBlock.Text =
                    "バックアップの復元に失敗しました。";

                MessageBox.Show(
                    $"バックアップの復元に失敗しました。\n\n" +
                    $"詳細：\n{ex.Message}",
                    "復元エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

// ウィンドウ情報関連
        // ウィンドウを閉じるときの処理
        private void Window_Closing(
            object? sender,
            System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowSettings();
        }

        // ウィンドウ設定を読み込む処理
        private void LoadWindowSettings()
        {
            // 保存ファイルが存在しない場合は何もしない
            if (!File.Exists(_windowSettingsPath))
            {
                return;
            }

            try
            {
                // 保存ファイルを読み込む
                string json = File.ReadAllText(_windowSettingsPath);

                // JSONをオブジェクトに変換
                WindowSettings? settings =
                    JsonSerializer.Deserialize<WindowSettings>(json);

                if (settings == null)
                {
                    return;
                }

                // 画面からはみ出さないようにサイズを調整
                double maxWidth =
                    Math.Max(MinWidth, SystemParameters.WorkArea.Width);

                double maxHeight =
                    Math.Max(MinHeight, SystemParameters.WorkArea.Height);

                // 幅を復元
                if (double.IsFinite(settings.Width) &&
                    settings.Width >= MinWidth)
                {
                    Width = Math.Clamp(
                        settings.Width,
                        MinWidth,
                        maxWidth);
                }

                // 高さを復元
                if (double.IsFinite(settings.Height) &&
                    settings.Height >= MinHeight)
                {
                    Height = Math.Clamp(
                        settings.Height,
                        MinHeight,
                        maxHeight);
                }

                // ウィンドウの位置を復元
                if (settings.Left.HasValue &&
                    settings.Top.HasValue &&
                    double.IsFinite(settings.Left.Value) &&
                    double.IsFinite(settings.Top.Value))
                {
                    // 現在の仮想デスクトップの範囲を取得
                    double virtualLeft = SystemParameters.VirtualScreenLeft;
                    double virtualTop = SystemParameters.VirtualScreenTop;

                    double virtualRight =
                        virtualLeft + SystemParameters.VirtualScreenWidth;

                    double virtualBottom =
                        virtualTop + SystemParameters.VirtualScreenHeight;

                    // 画面からはみ出さない位置に調整
                    double maxLeft = Math.Max(
                        virtualLeft,
                        virtualRight - Width);

                    double maxTop = Math.Max(
                        virtualTop,
                        virtualBottom - Height);

                    Left = Math.Clamp(
                        settings.Left.Value,
                        virtualLeft,
                        maxLeft);

                    Top = Math.Clamp(
                        settings.Top.Value,
                        virtualTop,
                        maxTop);

                    // 保存した位置を優先する
                    WindowStartupLocation = WindowStartupLocation.Manual;
                }

                // 最大化状態を復元
                if (settings.IsMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ウィンドウ設定の読み込みに失敗しました: {ex.Message}");
            }
        }

        // ウィンドウ設定を保存する処理
        private void SaveWindowSettings()
        {
            try
            {
                // 保存先のフォルダを作成
                Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(_windowSettingsPath)!);

                // 通常サイズのウィンドウ情報を取得
                Rect bounds = WindowState == WindowState.Normal
                    ? new Rect(Left, Top, Width, Height)
                    : RestoreBounds;

                // 保存する設定を作成
                var settings = new WindowSettings
                {
                    Width = bounds.Width > 0 ? bounds.Width : Width,
                    Height = bounds.Height > 0 ? bounds.Height : Height,

                    Left = bounds.Left,
                    Top = bounds.Top,

                    IsMaximized = WindowState == WindowState.Maximized
                };

                // JSONに変換
                string json = JsonSerializer.Serialize(
                    settings,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                // ファイルに保存
                File.WriteAllText(_windowSettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ウィンドウ設定の保存に失敗しました: {ex.Message}");
            }
        }

        // ウィンドウ設定を保存するためのクラス
        private sealed class WindowSettings
        {
            public double Width { get; set; }

            public double Height { get; set; }

            // ウィンドウの表示位置
            public double? Left { get; set; }

            public double? Top { get; set; }

            public bool IsMaximized { get; set; }
        }
    }
}