﻿using MongoDB.Bson;
using MongoDB.Driver;
using Phys.Lib.Core.Authors;
using Phys.Lib.Data.Utils;

namespace Phys.Lib.Data.Authors
{
    internal class AuthorsDb : Collection<AuthorDbo>, IAuthorsDb
    {
        public AuthorsDb(IMongoCollection<AuthorDbo> collection) : base(collection)
        {
            collection.Indexes.CreateOne(new CreateIndexModel<AuthorDbo>(indexBuilder.Ascending(i => i.Code), new CreateIndexOptions { Unique = true }));
        }

        public AuthorDbo Create(AuthorDbo author)
        {
            author.Id = ObjectId.GenerateNewId().ToString();
            return Insert(author);
        }

        public List<AuthorDbo> Find(AuthorsDbQuery query)
        {
            var filter = filterBuilder.Empty;
            if (query.Code != null)
                filter = filterBuilder.And(filter, filterBuilder.Eq(u => u.Code, query.Code));
            if (query.Codes != null)
                filter = filterBuilder.And(filter, filterBuilder.In(u => u.Code, query.Codes));
            if (query.SearchRegex != null)
            {
                var infoFilterBuilder = Builders<AuthorDbo.InfoDbo>.Filter;
                filter = filterBuilder.And(filter, filterBuilder.Or(
                    filterBuilder.Regex(u => u.Code, query.SearchRegex),
                    filterBuilder.ElemMatch(u => u.Infos, infoFilterBuilder.Regex(i => i.Name, query.SearchRegex)),
                    filterBuilder.ElemMatch(u => u.Infos, infoFilterBuilder.Regex(i => i.Description, query.SearchRegex))));
            }

            var sort = sortBuilder.Descending(i => i.Id);

            return collection.Find(filter).Limit(query.Limit).Sort(sort).ToList();
        }

        public AuthorDbo Get(string id)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));

            return collection.Find(filterBuilder.Eq(u => u.Id, id)).FirstOrDefault() ?? throw new ApplicationException($"author '{id}' not found in db");
        }

        public AuthorDbo GetByCode(string code)
        {
            if (code is null) throw new ArgumentNullException(nameof(code));

            return collection.Find(filterBuilder.Eq(u => u.Code, code)).FirstOrDefault() ?? throw new ApplicationException($"author '{code}' not found in db");
        }

        public AuthorDbo Update(string id, AuthorDbUpdate author)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));

            var filter = filterBuilder.Eq(i => i.Id, id);
            var update = updateBuilder.Combine();

            if (author.Born.IsEmpty())
                update = update.Unset(i => i.Born);
            else if (author.Born.HasValue())
                update = update.Set(i => i.Born, author.Born);

            if (author.Died.IsEmpty())
                update = update.Unset(i => i.Died);
            else if (author.Died.HasValue())
                update = update.Set(i => i.Died, author.Died);

            if (author.AddInfo != null)
                update = update.Push(i => i.Infos, author.AddInfo);
            if (author.DeleteInfo != null)
                update = update.PullFilter(i => i.Infos, i => i.Language == author.DeleteInfo);

            return collection.FindOneAndUpdate(filter, update, findOneAndUpdateReturnAfter)
                ?? throw new ApplicationException($"author '{id}' was not updated due to not found in db");
        }

        public void Delete(string id)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));

            collection.DeleteOne(filterBuilder.Eq(i => i.Id, id));
        }
    }
}
