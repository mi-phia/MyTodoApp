using Microsoft.Data.Sqlite;

namespace MyTodoApp
{
    public class TodoRepository
    {
        private readonly string _databasePath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MyTodoApp",
                "todos.db");

        private string _connectionString = "";

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

        public void InitializeDatabase()
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Todos
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0
                );
            ";

            command.ExecuteNonQuery();

            Console.WriteLine("データベースを初期化しました。");
        }

        public int InsertTodo(string title)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Todos(Title, IsCompleted)
                VALUES ($title, $isCompleted);

                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("$title", title);
            command.Parameters.AddWithValue("$isCompleted", false);

            long id = (long)command.ExecuteScalar()!;

            return checked((int)id);
        }

        public List<TodoItem> GetAllTodos()
        {
            List<TodoItem> todos = new();

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT Id, Title, IsCompleted
                FROM Todos;
            ";

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

        public void ImportTodos(IEnumerable<TodoItem> todos)
        {
            using var connection = new SqliteConnection();
            connection.Open();

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "SELECT COUNT(*) FROM Todos;";

            long count = (long)checkCommand.ExecuteScalar()!;

            if (count > 0)
            {
                Console.WriteLine("SQLiteに既にデータがあるため、以降をスキップします。");
                return;
            }

            using var transaction = connection.BeginTransaction();

            foreach(TodoItem todo in todos)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;

                command.CommandText = @"
                    INSERT INTO Todos(Title, IsCompleted)
                    VALUES ($title, $isCompleted);
                ";

                command.Parameters.AddWithValue("$title", todo.Title);
                command.Parameters.AddWithValue("$isCompleted", todo.IsCompleted);

                command.ExecuteNonQuery();
            }

            transaction.Commit();

            Console.WriteLine("JSONからSQLiteへのデータ移行が完了しました。");
        }

        public bool UpdateTodoStatus(int id, bool isCompleted)
        {
            using var connectionn = new SqliteConnection(_connectionString);
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

        public bool UpdateTodoTitle(int id, string newTitle)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Todos
                SET Title = $title
                WHERE Id = $id;
            ";

            command.Parameters.AddWithValue("$title", newTitle);
            command.Parameters.AddWithValue("$id", id);

            return command.ExecuteNonQuery() == 1;
        }

        public bool DeleteTodo(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
        DELETE FROM Todos
        WHERE Id = $id;
    ";

            command.Parameters.AddWithValue("$id", id);

            return command.ExecuteNonQuery() == 1;
        }
    }
}
