using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
   
        Console.WriteLine("Enter the directory path:");
        string directoryPath = Console.ReadLine();

        Console.WriteLine("Enter the old value to replace:");
        string oldValue = Console.ReadLine();

        Console.WriteLine("Enter the new value to replace with:");
        string newValue = Console.ReadLine();
 
        if (Directory.Exists(directoryPath))
        {
     
            await RenameDirectoriesAsync(directoryPath, oldValue, newValue);
             
            await RenameFilesAndContentsAsync(directoryPath, oldValue, newValue);
        }
        else
        {
            Console.WriteLine("The directory does not exist.");
        }

        Console.WriteLine("Process completed.");
        Console.ReadKey();
    }

    // تابع برای تغییر نام دایرکتوری‌ها به صورت ناهمگام
    static async Task RenameDirectoriesAsync(string directoryPath, string oldValue, string newValue)
    {
        try
        {
             var directories = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);

             Array.Reverse(directories);

            foreach (var dirPath in directories)
            {
                string dirName = Path.GetFileName(dirPath);
                string parentDirPath = Path.GetDirectoryName(dirPath);
                string newDirPath = dirPath;

                if (dirName.Contains(oldValue))
                {
                    string newDirName = dirName.Replace(oldValue, newValue);
                    newDirPath = Path.Combine(parentDirPath, newDirName);

                     await Task.Run(() => Directory.Move(dirPath, newDirPath));
                    Console.WriteLine($"Renamed directory: {dirName} -> {newDirName}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while renaming directories: {ex.Message}");
        }
    }

     static async Task RenameFilesAndContentsAsync(string directoryPath, string oldValue, string newValue)
    {
        try
        {
             var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

            await Parallel.ForEachAsync(files, async (filePath, cancellationToken) =>
            {
                string fileName = Path.GetFileName(filePath);
                string currentFilePath = filePath;
 
                if (fileName.Contains(oldValue))
                {
                    string newFileName = fileName.Replace(oldValue, newValue);
                    string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);
 
                    if (newFilePath != currentFilePath)
                    {
                        await Task.Run(() => File.Move(currentFilePath, newFilePath));
                        Console.WriteLine($"Renamed file: {fileName} -> {newFileName}");
                        currentFilePath = newFilePath;
                    }
                }

 
                string fileExtension = Path.GetExtension(currentFilePath);
                if (await IsTextFile(fileExtension))
                {
                    await ReplaceTextInFileAsync(currentFilePath, oldValue, newValue);
                }
            });

        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

     static async Task ReplaceTextInFileAsync(string filePath, string oldValue, string newValue)
    {
        try
        {
            string content = await File.ReadAllTextAsync(filePath);

            if (content.Contains(oldValue))
            {
                 string newContent = content.Replace(oldValue, newValue);
                await File.WriteAllTextAsync(filePath, newContent);
                Console.WriteLine($"Replaced content in: {filePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not process file: {filePath}. Error: {ex.Message}");
        }
    }

     static async Task<bool> IsTextFile(string extension)
    {
        string[] textFileExtensions =await  LoadExtensionsFromJson();
        return textFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
    static async Task<string[]> LoadExtensionsFromJson()
    {
        string buildDirectory = AppContext.BaseDirectory;

        string projectRoot =await  GetProjectRootPath();

        string filePath = Path.Combine(projectRoot, "textFileExtensions.json");
 
        string jsonString = File.ReadAllText(filePath);

        using JsonDocument doc = JsonDocument.Parse(jsonString);
        JsonElement root = doc.RootElement;   
        JsonElement extensionsElement = root.GetProperty("textFileExtensions");

        return extensionsElement.EnumerateArray()
                                .Select(e => e.GetString())
                                .ToArray();
    }
    static async Task<string> GetProjectRootPath()
    {
        string baseDirectory = AppContext.BaseDirectory;
        DirectoryInfo directory = new DirectoryInfo(baseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, $"{directory.Name}.csproj")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? baseDirectory; 
    }
}
