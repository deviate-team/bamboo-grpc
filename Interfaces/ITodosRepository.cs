using bamboo_grpc.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace bamboo_grpc.Interfaces;

public interface ITodosRepository
{
    Task<IEnumerable<TodoModel>> GetTodos();
    Task<IEnumerable<TodoModel>> GetTodo(string id);
    Task InsertTodo(string title, string description, DateTime duedate, string status, string priority);
    Task UpdateTodo(string id, string title, string description, DateTime duedate, string status, string priority);
    Task DeleteTodo(string id);
}