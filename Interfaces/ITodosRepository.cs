namespace bamboo_grpc.Interfaces;

public interface ITodosRepository
{
    IEnumerable<(int id, string description)> GetTodos();
    string GetTodo(int id);
    void InsertTodo(string description);
    void UpdateTodo(int id, string description);
    void DeleteTodo(int id);
}
