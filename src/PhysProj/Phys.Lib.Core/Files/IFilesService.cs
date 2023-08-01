﻿namespace Phys.Lib.Core.Files
{
    public interface IFilesService
    {
        List<FileDbo> Find(string? search = null);

        FileDbo? FindByCode(string code);

        FileDbo Create(string code, string? format, long? size);

        FileDbo AddLink(FileDbo file, FileDbo.LinkDbo link);

        void Delete(FileDbo file);
    }
}