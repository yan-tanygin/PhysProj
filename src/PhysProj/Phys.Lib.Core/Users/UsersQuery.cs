﻿namespace Phys.Lib.Core.Users
{
    public class UsersQuery
    {
        public string NameLowerCase { get; set; }

        public string Code { get; set; }

        public int Limit { get; set; } = 20;
    }
}
