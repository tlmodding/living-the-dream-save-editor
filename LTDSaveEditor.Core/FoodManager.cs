using System.Text.Json;

namespace LTDSaveEditor.Core;

public static class FoodManager
{
    public static Dictionary<uint, string> FoodHashes { get; private set; } = [];
    public static bool IsInitialized => FoodHashes.Count > 0;

    public static void Initialize(string jsonPath)
    {
        FoodHashes.Clear();

        if (!File.Exists(jsonPath))
            return;

        try
        {
            var json = File.ReadAllText(jsonPath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    if (uint.TryParse(kvp.Key, out uint hash))
                    {
                        FoodHashes[hash] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading food hashes: {ex.Message}");
        }
    }

    public static string GetFoodName(uint hash)
    {
        if (FoodHashes.TryGetValue(hash, out var name))
            return name;
        return hash.ToString();
    }
}
