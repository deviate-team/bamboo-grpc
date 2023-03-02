using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace bamboo_grpc.Models;

public class UserModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("email")]
    public string Email { get; set; }

    [BsonElement("username")]
    public string Username { get; set; }

    [BsonElement("password")]
    public string Password { get; set; }

    [BsonElement("role")]
    public string Role { get; set; }
}
