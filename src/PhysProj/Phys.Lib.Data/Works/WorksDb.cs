﻿using MongoDB.Bson;
using MongoDB.Driver;
using Phys.Lib.Core.Works;
using Phys.Lib.Data.Utils;

namespace Phys.Lib.Data.Works
{
    internal class WorksDb : Collection<WorkDbo>, IWorksDb
    {
        public WorksDb(IMongoCollection<WorkDbo> collection) : base(collection)
        {
            collection.Indexes.CreateOne(new CreateIndexModel<WorkDbo>(indexBuilder.Ascending(i => i.Code), new CreateIndexOptions { Unique = true }));
        }

        public WorkDbo Create(WorkDbo work)
        {
            work.Id = ObjectId.GenerateNewId().ToString();
            return Insert(work);
        }

        public void Delete(string id)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));

            collection.DeleteOne(filterBuilder.Eq(i => i.Id, id));
        }

        public List<WorkDbo> Find(WorksDbQuery query)
        {
            var filter = filterBuilder.Empty;
            if (query.Code != null)
                filter = filterBuilder.And(filter, filterBuilder.Eq(u => u.Code, query.Code));
            if (query.AuthorCode != null)
                filter = filterBuilder.And(filter, filterBuilder.AnyEq(u => u.AuthorsCodes, query.AuthorCode));
            if (query.Search != null)
                filter = filterBuilder.And(filter, filterBuilder.Regex(u => u.Code, query.Search));

            var sort = sortBuilder.Descending(i => i.Id);

            return collection.Find(filter).Limit(query.Limit).Sort(sort).ToList();
        }

        public WorkDbo Get(string id)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));

            return collection.Find(filterBuilder.Eq(u => u.Id, id)).FirstOrDefault() ?? throw new ApplicationException($"work '{id}' not found in db");
        }

        public WorkDbo Update(string id, WorkDbUpdate work)
        {
            if (id is null) throw new ArgumentNullException(nameof(id));

            var filter = filterBuilder.Eq(i => i.Id, id);
            var update = updateBuilder.Combine();

            if (work.Publish.IsEmpty())
                update = update.Unset(i => i.Publish);
            else if (work.Publish.HasValue())
                update = update.Set(i => i.Publish, work.Publish);

            if (work.Language.IsEmpty())
                update = update.Unset(i => i.Language);
            else if (work.Language.HasValue())
                update = update.Set(i => i.Language, work.Language);

            if (work.AddInfo != null)
                update = update.Push(i => i.Infos, work.AddInfo);
            if (work.DeleteInfo.HasValue())
                update = update.PullFilter(i => i.Infos, i => i.Language == work.DeleteInfo);

            if (work.AddAuthor.HasValue())
                update = update.AddToSet(i => i.AuthorsCodes, work.AddAuthor);
            if (work.DeleteAuthor.HasValue())
                update = update.Pull(i => i.AuthorsCodes, work.DeleteAuthor);

            if (work.AddSubWork.HasValue())
                update = update.AddToSet(i => i.SubWorksCodes, work.AddSubWork);
            if (work.DeleteSubWork.HasValue())
                update = update.Pull(i => i.SubWorksCodes, work.DeleteSubWork);

            if (work.Original.IsEmpty())
                update = update.Unset(i => i.OriginalCode);
            else if (work.Original.HasValue())
                update = update.Set(i => i.OriginalCode, work.Original);

            return collection.FindOneAndUpdate(filter, update, findOneAndUpdateReturnAfter)
                ?? throw new ApplicationException($"work '{id}' was not updated due to not found in db");
        }
    }
}