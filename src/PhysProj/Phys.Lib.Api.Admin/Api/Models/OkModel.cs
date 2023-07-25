﻿namespace Phys.Lib.Api.Admin.Api.Models
{
    public class OkModel
    {
        public static OkModel Ok => new OkModel();

        public DateTime Time { get; set; } = DateTime.UtcNow;

        public string Version { get; set; } = Program.Version;
    }
}
