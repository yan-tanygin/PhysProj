﻿namespace Phys.Lib.Core.Users
{
    public interface IUsers
    {
        UserDbo Create(CreateUserData data);

        UserDbo Login(string userName, string password);
    }
}
