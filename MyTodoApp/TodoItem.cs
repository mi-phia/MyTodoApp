using System;
using System.Collections.Generic;
using System.Text;

namespace MyTodoApp
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public TodoPriority Priority { get; set; } = TodoPriority.Medium;
    }

    public enum TodoPriority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }
}