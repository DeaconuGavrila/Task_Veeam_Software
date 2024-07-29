using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: <source> <replica> <interval> <logFile>");
            return;
        }

        string sourcePath = args[0];
        string replicaPath = args[1];
        int interval;
        if (!int.TryParse(args[2], out interval))
        {
            Console.WriteLine("Interval must be an integer.");
            return;
        }
        string logFilePath = args[3];

        Console.WriteLine("Synchronization started");

        while (true)
        {
            SynchronizeFolders(sourcePath, replicaPath, logFilePath);
            Thread.Sleep(interval * 1000);
        }
    }

    static void SynchronizeFolders(string sourcePath, string replicaPath, string logFilePath)
    {
        try
        {
            var sourceDir = new DirectoryInfo(sourcePath);
            var replicaDir = new DirectoryInfo(replicaPath);

            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourcePath}");
            }

            if (!replicaDir.Exists)
            {
                replicaDir.Create();
            }

            var sourceFiles = sourceDir.GetFiles();
            foreach (var file in sourceFiles)
            {
                string targetFilePath = Path.Combine(replicaPath, file.Name);
                if (!File.Exists(targetFilePath) || !FilesAreEqual(file, new FileInfo(targetFilePath)))
                {
                    file.CopyTo(targetFilePath, true);
                    LogOperation(logFilePath, $"Copied file: {file.FullName} to {targetFilePath}");
                }
            }

            var replicaFiles = replicaDir.GetFiles();
            foreach (var file in replicaFiles)
            {
                if (!File.Exists(Path.Combine(sourcePath, file.Name)))
                {
                    file.Delete();
                    LogOperation(logFilePath, $"Deleted file: {file.FullName}");
                }
            }

            var sourceDirs = sourceDir.GetDirectories();
            foreach (var dir in sourceDirs)
            {
                SynchronizeFolders(dir.FullName, Path.Combine(replicaPath, dir.Name), logFilePath);
            }

            var replicaDirs = replicaDir.GetDirectories();
            foreach (var dir in replicaDirs)
            {
                if (!Directory.Exists(Path.Combine(sourcePath, dir.Name)))
                {
                    dir.Delete(true);
                    LogOperation(logFilePath, $"Deleted directory: {dir.FullName}");
                }
            }
        }
        catch (Exception ex)
        {
            LogOperation(logFilePath, $"Error: {ex.Message}");
        }
    }

    static bool FilesAreEqual(FileInfo first, FileInfo second)
    {
        using (var firstStream = first.OpenRead())
        using (var secondStream = second.OpenRead())
        {
            return StreamsAreEqual(firstStream, secondStream);
        }
    }

    static bool StreamsAreEqual(Stream first, Stream second)
    {
        const int bufferSize = 2048 * 2;
        var firstBuffer = new byte[bufferSize];
        var secondBuffer = new byte[bufferSize];

        while (true)
        {
            int firstRead = first.Read(firstBuffer, 0, firstBuffer.Length);
            int secondRead = second.Read(secondBuffer, 0, secondBuffer.Length);

            if (firstRead != secondRead)
                return false;

            if (firstRead == 0)
                return true;

            if (!firstBuffer.Take(firstRead).SequenceEqual(secondBuffer.Take(secondRead)))
                return false;
        }
    }

    static void LogOperation(string logFilePath, string message)
    {
        string logMessage = $"{DateTime.Now}: {message}";
        Console.WriteLine(logMessage);
        File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
    }

}
