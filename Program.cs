using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace CheckFileMD5
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new FileInfo("checkLog.txt");
            using var logWriter = log.Exists ? log.AppendText() : log.CreateText();

            var checkListJson = new FileInfo("checkList.json");
            var checkList = JsonDocument.Parse(checkListJson.OpenRead()).RootElement.EnumerateArray();

            foreach(var check in checkList)
            {
                var fileName = check.GetProperty("FileName").GetString();
                var rightMD5 = check.GetProperty("MD5").GetString();
                var rename = check.TryGetProperty("Rename", out var renameElement) && 
                    renameElement.GetBoolean();

                var checkFileInfo = new FileInfo(fileName);

                if (checkFileInfo.Exists)
                {
                    using var md5 = MD5.Create();
                    var fileStream = checkFileInfo.OpenRead();
                    var md5Text = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", string.Empty).ToLower();

                    if (rightMD5.Equals(md5Text))
                    {
                        Console.WriteLine($"正確的 {checkFileInfo.Name}");
                        logWriter.WriteLine($"{Environment.MachineName}    |    正確的 {checkFileInfo.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"可疑的 {checkFileInfo.Name}");
                        logWriter.WriteLine($"{Environment.MachineName}    |    可疑的 {checkFileInfo.Name}");
                        
                        Directory.CreateDirectory($".//{Environment.MachineName}");
                        checkFileInfo.CopyTo($@".//{Environment.MachineName}//{checkFileInfo.Name}.copy", true);

                        try
                        {
                            if (rename)
                                checkFileInfo.MoveTo($"{checkFileInfo.FullName}.bak");
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Console.Error.WriteLine($"權限不足，無法為 {checkFileInfo.FullName} 重新命名!");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"不存在 {checkFileInfo.Name}");
                    logWriter.WriteLine($"{Environment.MachineName}    |    不存在 {checkFileInfo.Name}");
                }
            }
            
            logWriter.Close();

            Console.WriteLine();
            Console.WriteLine("Press any key to exit!");
            Console.Read();
        }
    }
}
