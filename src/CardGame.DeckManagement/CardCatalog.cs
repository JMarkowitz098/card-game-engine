using System.Text.Json;

namespace CardGame.DeckManagement;

public static class CardCatalog
{
    public static List<CardTemplate> Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Card catalog file not found: {path}", path);
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<CardTemplate>>(json) ?? [];
    }

    public static void Save(string path, List<CardTemplate> templates)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(templates);
        File.WriteAllText(path, json);
    }
}
