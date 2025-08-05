using System.Xml.Linq;

namespace SharedConfig;

public class ConfigLocator
{
    public static string GetConfigFilePath(string fileName)
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null)
        {
            string fullPath = Path.Combine(directory, fileName);
            if (File.Exists(fullPath))
                return fullPath;

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new FileNotFoundException($"{fileName} が見つかりませんでした。親ディレクトリも確認しました。", fileName);
    }

    public static XDocument LoadConfigXml(string fileName)
    {
        string path = GetConfigFilePath(fileName);
        return XDocument.Load(path);
    }
}
