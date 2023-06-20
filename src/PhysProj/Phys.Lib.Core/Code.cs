﻿using System.Text.RegularExpressions;

namespace Phys.Lib.Core
{
    internal static class Code
    {
        public static string FromString(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            return Regex.Replace(value, @"[^\w]", "-").Replace('_', '-').ToLowerInvariant().Trim();
        }
    }
}
