using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetExtensions.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 取得字串左邊指定長度的子字串。
        /// </summary>
        public static string pLeft(this string source, int length)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            return source.Length <= length ? source : source.Substring(0, length);
        }

        /// <summary>
        /// 取得字串右邊指定長度的子字串。
        /// </summary>
        public static string pRight(this string source, int length)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            return source.Length <= length ? source : source.Substring(source.Length - length, length);
        }

        /// </summary>
        /// <param name="source"></param>
        /// <param name="format">日期格式</param>
        /// <returns></returns>
        public static DateTime pParseDateTime(this string source, string format = "yyyy/MM/dd")
        {
            return DateTime.ParseExact(source, format, CultureInfo.InvariantCulture);
        }
    }
}
