﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Phys.Lib.Mongo.Authors
{
    internal class AuthorModel : MongoModel
    {
        [BsonElement("code")]
        public required string Code { get; set; }

        [BsonElement("born")]
        public string? Born { get; set; }

        [BsonElement("died")]
        public string? Died { get; set; }

        [BsonElement("infos")]
        public List<InfoModel> Infos { get; set; } = new List<InfoModel>();

        public class InfoModel
        {
            [BsonElement("lang")]
            public required string Language { get; set; }

            [BsonElement("name")]
            public string? FullName { get; set; }

            [BsonElement("desc")]
            public string? Description { get; set; }
        }
    }
}
