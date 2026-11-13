using Microsoft.Data.Sqlite;

namespace MyTodoApp
{
    public class TodoRepository
    {
/// 以下、基本設定
        // データベースの保存位置
        private readonly string _databasePath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MyTodoApp",
                "todos.db");

        //　builder(設定)の入れ場所
        private string _connectionString = "";

        // コンストラクター(フォルダ作成、)
        public TodoRepository()
        {
            Directory.CreateDirectory(
        Path.GetDirectoryName(_databasePath)!);

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath
            };

            _connectionString = builder.ToString();
        }

        // 接続準備
        private SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

/// 以下、初期化まとめ
        // 初期化設定
        public void InitializeDatabase()
        {
            // 接続準備→開始
            using var connection = CreateConnection();
            connection.Open();

            // トランザクション(一括処理)の準備
            using var transaction = connection.BeginTransaction();

            try
            {
                // テーブル・インデックスの作成
                CreateTodoTable(connection, transaction);
                CreateTodoIndex(connection, transaction);
                transaction.Commit();
            }
            catch 
            {
                // キャンセルされたら全て巻き戻る
                transaction.Rollback();
                throw;
            }
        }

        // テーブル作成
        private void CreateTodoTable(SqliteConnection connection,SqliteTransaction transaction)
        {
            using var command = connection.CreateCommand();

            command.Transaction = transaction;

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Todos
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    DueDate TEXT NULL,
                    Priority INTEGER NOT NULL DEFAULT 2
                );
            ";

            command.ExecuteNonQuery();
        }

        // インデックス作成
        private void CreateTodoIndex(SqliteConnection connection, SqliteTransaction transaction)
        {
            using var command = connection.CreateCommand();

            command.Transaction = transaction;

            command.CommandText = @"
                CREATE UNIQUE INDEX IF NOT EXISTS
                    IX_Todos_Title_NoCase
                ON Todos (Title COLLATE NOCASE);
            ";

            command.ExecuteNonQuery();
        }

/// 操作メソッドまとめ(追加、獲得、完了、削除、編集、検索)
        // 追加
        public int InsertTodo(
            string title,
            DateTime? dueDate = null,
            TodoPriority priority = TodoPriority.Medium)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Todos(Title, IsCompleted, DueDate, Priority)
                VALUES ($title, $isCompleted, $dueDate, $priority);

                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("$title", title);
            command.Parameters.AddWithValue("$isCompleted", false);

            if (dueDate.HasValue)
            {
                command.Parameters.AddWithValue(
                    "$dueDate",
                    dueDate.Value.ToString("yyyy-MM-dd"));
            }
            else
            {
                command.Parameters.AddWithValue("$dueDate", DBNull.Value);
            }

            command.Parameters.AddWithValue("$priority", (int)priority);

            long id = (long)command.ExecuteScalar()!;

            return checked((int)id);
        }

        // リストの獲得(同期の準備)
        public List<TodoItem> GetAllTodos()
        {
            List<TodoItem> todos = new();

            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, Title, IsCompleted, DueDate, Priority
                FROM Todos;
            ";

            // 1つずつ読み込んでリストに追加
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                TodoItem todo = new TodoItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    IsCompleted = reader.GetBoolean(2),

                    DueDate = reader.IsDBNull(3)
                    ? null
                    : DateTime.Parse(reader.GetString(3)),

                    Priority =(TodoPriority)reader.GetInt32(4) 
                    };

                todos.Add(todo);
            }

            return todos;
        }

        // 完了
        public bool UpdateTodoStatus(int id, bool isCompleted)
        {
            using var connectionn = CreateConnection();
            connectionn.Open();

            using var command = connectionn.CreateCommand();

            command.CommandText = @"
                UPDATE Todos
                SET IsCompleted = $isCompleted
                WHERE Id = $id;
            ";

            command.Parameters.AddWithValue("$isCompleted", isCompleted);
            command.Parameters.AddWithValue("$id", id);

            return command.ExecuteNonQuery() == 1;
        }

        // 削除
        public bool DeleteTodo(int id)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                DELETE FROM Todos
                WHERE Id = $id;
            ";

            command.Parameters.AddWithValue("$id", id);

            return command.ExecuteNonQuery() == 1;
        }

        // 編集
        public bool UpdateTodoDetails(
            int id,
            string newTitle,
            DateTime? dueDate,
            TodoPriority priority)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE Todos
                SET Title = $title,
                    DueDate = $dueDate,
                    Priority = $priority
                WHERE Id = $id;
             ";

            command.Parameters.AddWithValue("$title", newTitle);

            if (dueDate.HasValue)
            {
                command.Parameters.AddWithValue(
                    "$dueDate", 
                    dueDate.Value.ToString("yyyy-MM-dd"));
            }
            else
            {
                command.Parameters.AddWithValue("$dueDate", DBNull.Value);
            }

            command.Parameters.AddWithValue("$priority", (int)priority);
            command.Parameters.AddWithValue("$id", id);

            return command.ExecuteNonQuery() == 1;
        }

        // 検索
        public List<TodoItem> SearchTodos(string keyword)
        {
            List<TodoItem> todos = new();

            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, Title, IsCompleted
                FROM Todos
                WHERE Title LIKE $keyword;
            ";

            command.Parameters.AddWithValue("$keyword", "%" + keyword + "%");

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                TodoItem todo = new TodoItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    IsCompleted = reader.GetBoolean(2)
                };

                todos.Add(todo);
            }

            return todos;
        }

/// バックアップまとめ(作成、復元)
        // 作成
        public string BackupDatabase()
        {
            string backupDirectory = Path.Combine(
                Path.GetDirectoryName(_databasePath)!,
                "Backups");

            Directory.CreateDirectory(backupDirectory);

            string fileName = $"todos_{DateTime.Now:yyyyMMdd_HHmmss}.db";

            string backupPath = Path.Combine(
                backupDirectory, 
                fileName);

            // 参照元の接続
            using var sourceConnection = CreateConnection();
            sourceConnection.Open();

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = backupPath
            };

            // 作成先の接続
            using var destinationConnection =
                new SqliteConnection(builder.ToString());

            destinationConnection.Open();

            sourceConnection.BackupDatabase(destinationConnection);

            return backupPath;
        }

        // 復元
        public void RestoreDatabase(string backupPath)
        {
            // 参照元の入力エラー
            if (string.IsNullOrWhiteSpace(backupPath))
            {
                throw new ArgumentException("バックアップファイルパスが指定されていません。",
                    nameof(backupPath));
            }

            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("バックアップファイルが見当たりません。", backupPath);
            }

            // 接続準備
            string fullBackupPath = Path.GetFullPath(backupPath);
            string fullDatabasePath = Path.GetFullPath(_databasePath);

            if (string.Equals(
                fullBackupPath, 
                fullDatabasePath,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("現在のデータベース自体は復元元に指定できません",
                    nameof(backupPath));
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = fullBackupPath
            };

            // 現在データのバックアップ
            using var sourceConnection = new SqliteConnection(builder.ToString());
            sourceConnection.Open();

            BackupDatabase();

            // 参照元の復元
            using var destinationConnection = CreateConnection();
            destinationConnection.Open();

            sourceConnection.BackupDatabase(destinationConnection);
        }

/// その他まとめ
        // データベースの保存先表示
        public string ShowSavePath()
        {
            return _databasePath;
        }

        // 重複チェック
        public bool ExistsTodoTitle(string title, int excludeId = 0)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM Todos
                    WHERE Title = $title COLLATE NOCASE
                        AND Id != $excludeId
                );
            ";

            command.Parameters.AddWithValue("$title", title);
            command.Parameters.AddWithValue("$excludeId", excludeId);

            long result = (long)command.ExecuteScalar()!;

            return result == 1;
        }
    }
}
