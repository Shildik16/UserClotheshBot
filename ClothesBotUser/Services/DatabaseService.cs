using System.Data;
using MySql.Data.MySqlClient;
using ClothesBotUser.Models;

namespace ClothesBotUser.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Item>> GetAllItemsAsync(CancellationToken ct)
        {
            
            var items = new List<Item>();
            // Оставляем ваше название колонки
            const string sql = "SELECT id, name, description, price_stars, photo_file_ids, availability FROM items";

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(ct);
            Console.WriteLine("hul");
            while (await reader.ReadAsync(ct))
            {
                string rawPhotoId = reader.GetString("photo_file_ids"); 

                // Чистим мусор (скобки и кавычки), который мешает Телеграму
                string cleanPhotoId = rawPhotoId
                    .Replace("[", "")
                    .Replace("]", "")
                    .Replace("\"", "")
                    .Trim();
                Console.WriteLine("hul2");
                items.Add(new Item
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    PriceStars = reader.GetInt32("price_stars"),
                    PhotoId = cleanPhotoId,
                    Availability = reader.GetString("availability")
                });
                Console.WriteLine("hul3");
            }
            return items;
        }
        
        
    }
    }
