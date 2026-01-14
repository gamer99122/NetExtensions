using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetExtensions.Extensions
{
    public static class DirectoryExtensions
    {
        /// <summary>
        /// 確保指定的資料夾存在，如果不存在則創建它。
        /// </summary>
        /// <param name="path">資料夾目錄</param>
        public static void pDirectoryEnsureExists(this string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
