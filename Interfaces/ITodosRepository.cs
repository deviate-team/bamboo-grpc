using bamboo_grpc.Models;

namespace bamboo_grpc.Interfaces;

public interface ITodosRepository
{
    Task<IEnumerable<TodoModel>> GetTodos();
    Task<IEnumerable<TodoModel>> GetTodosByUserId(string userId);
    Task<TodoModel> GetTodoById(string id);
    Task InsertTodo(string title, string description, string due_date, string status, string priority, string userId);
    Task UpdateTodo(string id, string title, string description, string due_date, string status, string priority, string userId);
    Task DeleteTodoById(string id, string userId);
}