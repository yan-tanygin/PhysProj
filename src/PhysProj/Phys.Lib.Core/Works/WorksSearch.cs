﻿using NLog;

namespace Phys.Lib.Core.Works
{
    public class WorksSearch : IWorksSearch
    {
        private static readonly ILogger log = LogManager.GetLogger("works-search");

        private readonly IWorksDb db;

        public WorksSearch(IWorksDb db)
        {
            this.db = db;
        }

        public WorkDbo? FindByCode(string code)
        {
            ArgumentNullException.ThrowIfNull(code);

            return db.Find(new WorksDbQuery { Code = code }).FirstOrDefault();
        }

        public List<WorkDbo> FindByText(string search)
        {
            ArgumentNullException.ThrowIfNull(search);

            return db.Find(new WorksDbQuery { Search = search });
        }

        public List<WorkDbo> FindByAuthor(string authorCode)
        {
            ArgumentNullException.ThrowIfNull(authorCode);

            return db.Find(new WorksDbQuery { AuthorCode = authorCode });
        }

        public List<WorkDbo> FindTranslations(string originalCode)
        {
            ArgumentNullException.ThrowIfNull(originalCode);

            return db.Find(new WorksDbQuery { OriginalCode = originalCode });
        }

        public List<WorkDbo> FindCollected(string subWorkCode)
        {
            ArgumentNullException.ThrowIfNull(subWorkCode);

            return db.Find(new WorksDbQuery { SubWorkCode = subWorkCode });
        }
    }
}
