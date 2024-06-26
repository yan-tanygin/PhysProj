﻿using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using Phys.Lib.Mongo.Utils;
using Phys.Lib.Db.Authors;
using Microsoft.Extensions.Logging;
using Phys.Lib.Db;

namespace Phys.Lib.Mongo.Authors
{
    internal class AuthorsDb : Collection<AuthorModel>, IAuthorsDb, IDbReader<AuthorDbo>
    {
        public AuthorsDb(Lazy<IMongoCollection<AuthorModel>> collection, string name, ILogger<AuthorsDb> logger) : base(collection, logger)
        {
            Name = name;
        }

        protected override void Init(IMongoCollection<AuthorModel> collection)
        {
            collection.Indexes.CreateOne(new CreateIndexModel<AuthorModel>(IndexBuilder.Ascending(i => i.Code), new CreateIndexOptions { Unique = true }));
        }

        public string Name { get; }

        public void Create(string code)
        {
            ArgumentNullException.ThrowIfNull(code);

            var author = new AuthorModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Code = code
            };

            Insert(author);
        }

        public List<AuthorDbo> Find(AuthorsDbQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            var filter = FilterBuilder.Empty;
            if (query.Code != null)
                filter = FilterBuilder.And(filter, FilterBuilder.Eq(u => u.Code, query.Code));
            if (query.Codes != null)
                filter = FilterBuilder.And(filter, FilterBuilder.In(u => u.Code, query.Codes));
            if (query.Search != null)
            {
                var regex = new Regex(Regex.Escape(query.Search), RegexOptions.IgnoreCase);
                var infoFilterBuilder = Builders<AuthorModel.InfoModel>.Filter;
                filter = FilterBuilder.And(filter, FilterBuilder.Or(
                    FilterBuilder.Regex(u => u.Code, regex),
                    FilterBuilder.ElemMatch(u => u.Infos, infoFilterBuilder.Regex(i => i.FullName, regex)),
                    FilterBuilder.ElemMatch(u => u.Infos, infoFilterBuilder.Regex(i => i.Description, regex))));
            }

            var sort = SortBuilder.Descending(i => i.Id);

            return MongoCollection.Find(filter).Limit(query.Limit).Sort(sort).ToList().Select(AuthorMapper.Map).ToList();
        }

        public void Update(string code, AuthorDbUpdate author)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentNullException.ThrowIfNull(author);

            var filter = FilterBuilder.Eq(i => i.Code, code);
            var update = UpdateBuilder.Set(i => i.UpdatedAt, DateTime.UtcNow);

            if (author.Born.IsEmpty())
                update = update.Unset(i => i.Born);
            else if (author.Born.HasValue())
                update = update.Set(i => i.Born, author.Born);

            if (author.Died.IsEmpty())
                update = update.Unset(i => i.Died);
            else if (author.Died.HasValue())
                update = update.Set(i => i.Died, author.Died);

            if (author.AddInfo != null)
                update = update.Push(i => i.Infos, AuthorMapper.Map(author.AddInfo));
            if (author.DeleteInfo != null)
                update = update.PullFilter(i => i.Infos, i => i.Language == author.DeleteInfo);

            if (MongoCollection.UpdateOne(filter, update).MatchedCount == 0)
                throw new PhysDbException($"author '{code}' update failed");
        }

        public void Delete(string code)
        {
            ArgumentNullException.ThrowIfNull(code);

            MongoCollection.DeleteOne(FilterBuilder.Eq(i => i.Code, code));
        }

        IEnumerable<List<AuthorDbo>> IDbReader<AuthorDbo>.Read(int batchSize)
        {
            return Read(batchSize, AuthorMapper.Map);
        }
    }
}
