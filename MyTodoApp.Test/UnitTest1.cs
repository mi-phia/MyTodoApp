using MyTodoApp;
using Xunit;

namespace MyTodoApp.Test
{
    public class UnitTest1
    {
        [Fact]
        public void AddTodo_ShouldAddTodo()
        {
            // Arrange
            TodoManager manager = new TodoManager();

            // Act
            manager.AddTodo("テストタスク");

            // Assert
            Assert.Single(manager.Todos);
            Assert.Equal("テストタスク", manager.Todos[0].Title);
            Assert.False(manager.Todos[0].IsCompleted);
        }

        [Fact]
        public void CompleteTodo_ShouldCompleteTodo()
        {
            // Arrange
            TodoManager manager = new TodoManager();
            manager.AddTodo("テストタスク");

            // Act
            bool result = manager.CompleteTodo(1);

            // Assert
            Assert.True(result);
            Assert.True(manager.Todos[0].IsCompleted);
        }

        [Fact]
        public void DeleteTodo_ShouldDeleteTodo()
        {
            // Arrange
            TodoManager manager = new TodoManager();
            manager.AddTodo("テストタスク");

            // Act
            bool result = manager.DeleteTodo(1);

            // Assert
            Assert.True(result);
            Assert.Empty(manager.Todos);
        }

        [Fact]
        public void EditTodo_ShouldUpdateTitle()
        {
            // Arrange
            TodoManager manager = new TodoManager();
            manager.AddTodo("変更前");

            // Act
            bool result = manager.EditTodo(1, "変更後");

            // Assert
            Assert.True(result);
            Assert.Equal("変更後", manager.Todos[0].Title);
        }
    }
}