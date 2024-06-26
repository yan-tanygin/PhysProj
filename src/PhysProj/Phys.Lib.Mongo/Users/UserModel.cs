﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Phys.Lib.Mongo.Users
{
    internal class UserModel : MongoModel
    {
        [BsonElement("name")]
        public required string Name { get; set; }

        [BsonElement("nameLc")]
        public required string NameLowerCase { get; set; }

        [BsonElement("pwdHash")]
        public string? PasswordHash { get; set; }

        [BsonElement("roles")]
        public List<string> Roles { get; set; } = new List<string>();
    }
}
