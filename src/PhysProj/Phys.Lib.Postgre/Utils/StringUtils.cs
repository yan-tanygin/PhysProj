﻿namespace Phys.Lib.Postgres.Utils
{
    internal static class StringUtils
    {
        public static bool IsEmpty(this string? value)
        {
            return value == string.Empty;
        }

        public static bool HasValue(this string? value)
        {
            return value?.Length > 0;
        }
    }
}
