﻿namespace Phys.Lib.Core.Works
{
    public class WorksDbQuery
    {
        public string? SearchRegex { get; set; }

        public string? Code { get; set; }

        public string? AuthorCode { get; set; }

        public string? OriginalCode { get; set; }

        public string? SubWorkCode { get; set; }

        public int Limit { get; set; } = 20;
    }
}
