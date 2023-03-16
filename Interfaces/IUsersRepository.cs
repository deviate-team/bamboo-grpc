using bamboo_grpc.Models;

namespace bamboo_grpc.Interfaces;

public interface IUsersRepository
{
    Task<IEnumerable<UserModel>> GetUsers();
    Task<UserModel> GetUserById(string id);
    Task<UserModel> GetUserByUsername(string username);
    Task<UserModel> GetUserByEmail(string email);
    Task InsertUser(string username, string password, string email, string role);
    Task UpdateUser(string id, string username, string password, string email, string role);
    Task DeleteUserById(string id);
}