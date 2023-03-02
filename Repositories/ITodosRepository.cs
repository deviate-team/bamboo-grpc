using bamboo_grpc.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace bamboo_grpc.Repositories;

public interface ITodosRepository
{
    Task<IEnumerable<TodoModel>> GetTodos();
    Task<TodoModel> GetTodoById(string id);
    Task InsertTodo(string title, string description, string due_date, string status, string priority);
    Task UpdateTodo(string id, string title, string description, string due_date, string status, string priority);
    Task DeleteTodoById(string id);
}