﻿using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Phys.Lib.Db.Users;

namespace Phys.Lib.Admin.Api.Api.User
{
    internal static class TokenGenerator
    {
        internal static readonly byte[] SignKey = Encoding.ASCII.GetBytes("ec0620749a174722b51a847a1d0ed0a5");

        public static string CreateToken(UserDbo user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = user.Roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
            claims.Add(new Claim(ClaimTypes.Name, user.Name));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(SignKey), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
