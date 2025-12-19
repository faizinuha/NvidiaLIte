using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NvidiaCi
{
    public class GameDataManager
    {
        private readonly string _filePath;

        public GameDataManager()
        {
            // Menentukan path: C:\Users\<User>\AppData\Local\NvidiaCi\games.json
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folderPath = Path.Combine(appDataPath, "NvidiaCi");
            
            // Pastikan folder sudah dibuat
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            _filePath = Path.Combine(folderPath, "games.json");
        }

        public void SaveGames(List<GameItem> games)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(games, options);
                File.WriteAllText(_filePath, jsonString);
            }
            catch (Exception ex)
            {
                // Dalam aplikasi nyata, Anda mungkin ingin melakukan logging di sini
                Console.WriteLine($"Failed to save games: {ex.Message}");
            }
        }

        public List<GameItem> LoadGames()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new List<GameItem>();
                }

                string jsonString = File.ReadAllText(_filePath);
                var games = JsonSerializer.Deserialize<List<GameItem>>(jsonString);
                
                return games ?? new List<GameItem>();
            }
            catch (Exception)
            {
                // Jika file korup atau ada error, kembalikan list kosong
                return new List<GameItem>();
            }
        }
    }
}
