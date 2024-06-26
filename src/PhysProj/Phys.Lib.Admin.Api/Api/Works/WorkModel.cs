﻿namespace Phys.Lib.Admin.Api.Api.Works
{
    public class WorkModel
    {
        public required string Code { get; set; }

        public string? Publish { get; set; }

        public string? Language { get; set; }

        public List<WorkInfoModel> Infos { get; set; } = new List<WorkInfoModel>();

        public List<string> AuthorsCodes { get; set; } = new List<string>();

        public List<string> SubWorksCodes { get; set; } = new List<string>();

        public List<string> SubWorksAuthorsCodes { get; set; } = new List<string>();

        public List<string> FilesCodes { get; set; } = new List<string>();

        public bool IsPublic { get; set; }

        public class WorkInfoModel
        {
            public required string Language { get; set; }

            public string? Name { get; set; }

            public string? Description { get; set; }
        }
    }
}
