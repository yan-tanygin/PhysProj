﻿using FluentValidation;
using NLog;

namespace Phys.Lib.Core.Works
{
    internal class WorksService : IWorksService
    {
        private static readonly ILogger log = LogManager.GetLogger("works");

        private readonly IWorksDb db;

        public WorksService(IWorksDb db)
        {
            this.db = db;
        }

        public WorkDbo Create(string code)
        {
            code = Code.NormalizeAndValidate(code);

            var work = db.Create(new WorkDbo
            {
                Code = code,
            });

            log.Info($"created work {work}");
            return work;
        }

        public void Delete(WorkDbo work)
        {
            if (work is null) throw new ArgumentNullException(nameof(work));

            db.Delete(work.Id);
            log.Info($"deleted work {work}");
        }

        public WorkDbo? GetByCode(string code)
        {
            if (code is null) throw new ArgumentNullException(nameof(code));

            return db.Find(new WorksQuery { Code = code }).FirstOrDefault();
        }

        public List<WorkDbo> Search(string search)
        {
            if (search is null) throw new ArgumentNullException(nameof(search));

            return db.Find(new WorksQuery { Search = search });
        }

        public WorkDbo Update(WorkDbo work, WorkUpdate update)
        {
            if (work is null) throw new ArgumentNullException(nameof(work));
            if (update is null) throw new ArgumentNullException(nameof(update));

            if (update.Original != null)
            {
                if (update.Original.Code == work.Code)
                    throw ValidationError($"original work can not be set to itself");
            }

            work = db.Update(work.Id, update);
            log.Info($"updated work {work}");
            return work;
        }

        private Exception ValidationError(string message)
        {
            return new ValidationException(message);
        }
    }
}
