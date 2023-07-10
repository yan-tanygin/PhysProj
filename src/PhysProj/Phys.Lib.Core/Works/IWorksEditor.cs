﻿namespace Phys.Lib.Core.Works
{
    public interface IWorksEditor
    {
        WorkDbo Create(string code);

        WorkDbo UpdateDate(WorkDbo work, string date);

        WorkDbo UpdateLanguage(WorkDbo work, string language);

        WorkDbo AddInfo(WorkDbo work, WorkDbo.InfoDbo info);

        WorkDbo DeleteInfo(WorkDbo work, string language);

        WorkDbo LinkAuthor(WorkDbo work, string authorCode);

        WorkDbo UnlinkAuthor(WorkDbo work, string authorCode);

        WorkDbo UpdateOriginal(WorkDbo work, string originalCode);

        WorkDbo LinkWork(WorkDbo work, string workCode);

        WorkDbo UnlinkWork(WorkDbo work, string workCode);

        void Delete(WorkDbo work);
    }
}