using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.Managers
{
    public static class StringExtensions
    {
        public static bool ContainsIgnoreCase(this string value, string testValue)
        {
            var val1 = value == null ? "" : value.Trim().ToUpper();
            var val2 = testValue == null ? "" : testValue.Trim().ToUpper();
            return val1.Contains(val2);
        }
        public static bool EqualsIgnoreCase(this string value, string testValue)
        {
            var val1 = value == null ? "" : value.Trim().ToUpper();
            var val2 = testValue == null ? "" : testValue.Trim().ToUpper();
            return val1 == val2;
        }
    }
}
