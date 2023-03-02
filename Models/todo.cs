using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace bamboo_grpc.Models;

public class TodoModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }

    [BsonElement("dueDate")]
    public string DueDate { get; set; }

    [BsonElement("status")]
    public string Status { get; set; }

    [BsonElement("priority")]
    public string Priority { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("userId")]
    public string UserId { get; set; }
}
