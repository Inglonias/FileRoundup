using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;

namespace FileDuplicateRoundup
{
    internal class Program
    {
        static StreamWriter sw;
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: FileRoundup.exe <Target root directory> <full path to text file for readout> <Path to dump duplicates> (Optional - won't move anything unless it's included)");
                Environment.Exit(0);
            }
            string targetDirectory = args[0];
            string targetLogFile = args[1];
            sw = new StreamWriter(targetLogFile);
            string moveDirectory = null;
            PrintLog("Target root directory: " + targetDirectory);
            if (args.Length > 2)
            {
                moveDirectory = args[2];
                PrintLog("Move mode IS active. Duplicates will be moved to " + moveDirectory);
                if (!Directory.Exists(moveDirectory))
                {
                    PrintLog("...Or they would, if that directory existed. But it doesn't. Please create it and try again.");
                    Environment.Exit(1);
                }
            }

            List<FileInfo> files = new List<FileInfo>();
            foreach (string path in GetAllFilesInDirAndSubdir(targetDirectory)) {
                files.Add(new FileInfo(path));
            }
            Dictionary<string, string> fileComparisonTable = new Dictionary<string, string>();
            List<FileInfo> duplicates = new List<FileInfo>();
            foreach (FileInfo fInfo in files)
            {
                if (fInfo.FullName.Equals(targetLogFile))
                {
                    continue; //We can't use the log file.
                }
                string fileHash = GetFileHash(fInfo);
                if (fileComparisonTable.TryGetValue(fileHash, out string? value))
                {
                    PrintLog(fInfo.FullName + " is a duplicate of " + value);
                    duplicates.Add(fInfo);
                }
                else
                {
                    fileComparisonTable.Add(fileHash, fInfo.FullName);
                }
            }
            foreach (FileInfo fInfo in duplicates)
            {
                try
                {
                    fInfo.MoveTo(moveDirectory + "\\" + fInfo.Name);
                }
                catch (Exception ex)
                {
                    PrintLog("WARNING: Could not move " + fInfo.FullName + ": " + ex.Message);
                }
            }
            sw.Close();
        }

        static string[] GetAllFilesInDirAndSubdir(string targetDirectory)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(targetDirectory));
            foreach (string dir in Directory.GetDirectories(targetDirectory))
            {
                files.AddRange(GetAllFilesInDirAndSubdir(dir));
            }
            files.Sort();
            return files.ToArray();
        }

        static void PrintLog(string message)
        {
            Console.WriteLine(message);
            sw.WriteLine(message);
        }

        static string? GetFileHash(FileInfo fInfo)
        {
            var sha256 = SHA256.Create();
            using (FileStream fileStream = fInfo.Open(FileMode.Open))
            {
                try
                {
                    // Create a fileStream for the file.
                    // Be sure it's positioned to the beginning of the stream.
                    fileStream.Position = 0;
                    // Compute the hash of the fileStream.
                    byte[] hashValue = sha256.ComputeHash(fileStream);
                    string hashString = Encoding.Default.GetString(hashValue);
                    return hashString;
                }
                catch (IOException e)
                {
                    PrintLog($"I/O Exception: {e.Message}");
                }
                catch (UnauthorizedAccessException e)
                {
                    PrintLog($"Access Exception: {e.Message}");
                }
                return null;
            }
        }
    }
}
