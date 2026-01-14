using NetExtensions.Extensions.Data;

namespace NetExtensions.Tools
{
    /// <summary>
    /// 連線字串加密工具
    /// 使用方式：
    /// dotnet run --project NetExtensions.Tools encrypt "Server=localhost;..." "your-key"
    /// dotnet run --project NetExtensions.Tools decrypt "ENCRYPTED:..." "your-key"
    /// </summary>
    public class EncryptConnectionString
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            var command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "encrypt":
                    case "enc":
                        EncryptCommand(args);
                        break;

                    case "decrypt":
                    case "dec":
                        DecryptCommand(args);
                        break;

                    case "encrypt-password":
                    case "enc-pwd":
                        EncryptPasswordCommand(args);
                        break;

                    case "help":
                    case "-h":
                    case "--help":
                        ShowHelp();
                        break;

                    default:
                        Console.WriteLine($"未知的命令: {command}");
                        ShowHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"錯誤: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        private static void EncryptCommand(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("用法: encrypt <連線字串> <加密金鑰>");
                return;
            }

            var connectionString = args[1];
            var key = args[2];

            var encrypted = ConnectionStringProtector.Encrypt(connectionString, key);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ 加密成功！");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("加密後的連線字串：");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(encrypted);
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("請將此字串複製到 appsettings.json 的 ConnectionStrings 區段");
        }

        private static void DecryptCommand(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("用法: decrypt <加密的連線字串> <加密金鑰>");
                return;
            }

            var encryptedConnectionString = args[1];
            var key = args[2];

            var decrypted = ConnectionStringProtector.Decrypt(encryptedConnectionString, key);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ 解密成功！");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("明文連線字串：");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(decrypted);
            Console.ResetColor();
        }

        private static void EncryptPasswordCommand(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("用法: encrypt-password <連線字串> <加密金鑰>");
                return;
            }

            var connectionString = args[1];
            var key = args[2];

            var encrypted = ConnectionStringProtector.EncryptPassword(connectionString, key);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ 密碼加密成功！");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("密碼已加密的連線字串：");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(encrypted);
            Console.ResetColor();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("  NetExtensions 連線字串加密工具");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("用法：");
            Console.WriteLine("  dotnet run --project Tools <命令> [參數]");
            Console.WriteLine();
            Console.WriteLine("命令：");
            Console.WriteLine("  encrypt <連線字串> <金鑰>");
            Console.WriteLine("    加密整個連線字串");
            Console.WriteLine();
            Console.WriteLine("  decrypt <加密的連線字串> <金鑰>");
            Console.WriteLine("    解密連線字串");
            Console.WriteLine();
            Console.WriteLine("  encrypt-password <連線字串> <金鑰>");
            Console.WriteLine("    只加密連線字串中的密碼部分");
            Console.WriteLine();
            Console.WriteLine("範例：");
            Console.WriteLine("  # 加密整個連線字串");
            Console.WriteLine("  dotnet run encrypt \"Server=localhost;Database=MyDb;User=sa;Password=123\" \"my-secret-key\"");
            Console.WriteLine();
            Console.WriteLine("  # 只加密密碼");
            Console.WriteLine("  dotnet run encrypt-password \"Server=localhost;Database=MyDb;User=sa;Password=123\" \"my-secret-key\"");
            Console.WriteLine();
            Console.WriteLine("  # 解密");
            Console.WriteLine("  dotnet run decrypt \"ENCRYPTED:CfDJ8N7x9...\" \"my-secret-key\"");
            Console.WriteLine();
            Console.WriteLine("注意事項：");
            Console.WriteLine("  • 加密金鑰請妥善保管，不要提交到版控");
            Console.WriteLine("  • 建議使用環境變數 DB_ENCRYPTION_KEY 儲存金鑰");
            Console.WriteLine("  • 加密後的連線字串可以安全地加入版控");
            Console.WriteLine();
        }
    }
}
