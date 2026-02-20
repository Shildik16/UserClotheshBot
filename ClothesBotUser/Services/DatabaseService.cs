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
            // Оставляем ваш SQL запрос
            const string sql = "SELECT id, name, description, price_stars, photo_file_ids, availability FROM items";

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                items.Add(new Item
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    PriceStars = reader.GetInt32("price_stars"),
            
                    // ИСПРАВЛЕНИЕ ТУТ: Читаем колонку как массив байтов
                    // Используем имя вашей колонки "photo_file_ids"
                    PhotoBytes = reader["photo_file_ids"] as byte[], 
            
                    Availability = reader.GetString("availability")
                });
            }
            return items;
        }
        
        
    }
    }
