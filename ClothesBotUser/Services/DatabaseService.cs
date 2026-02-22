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
        
        
        public async Task<Item?> GetItemByIdAsync(int id, CancellationToken ct)
        {
            const string sql = "SELECT id, name, description, price_stars, photo_file_ids, availability FROM items WHERE id = @id";

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
    
            await using var reader = await command.ExecuteReaderAsync(ct);

            if (await reader.ReadAsync(ct))
            {
                return new Item
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    PriceStars = reader.GetInt32("price_stars"),
                    PhotoBytes = reader["photo_file_ids"] as byte[],
                    Availability = reader.GetString("availability")
                };
            }
            return null;
        }
        
        
        public async Task<List<(int Id, string Name)>> GetCategoriesAsync(CancellationToken ct)
        {
            var list = new List<(int, string)>();
            const string sql = "SELECT id, name FROM categories";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) 
                list.Add((reader.GetInt32("id"), reader.GetString("name")));
            return list;
        }

        public async Task CreateCategoryAsync(string name, CancellationToken ct)
        {
            const string sql = "INSERT IGNORE INTO categories (name) VALUES (@name)";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@name", name);
            await command.ExecuteNonQueryAsync(ct);
        }
        
        public async Task<List<Item>> GetItemsByCategoryIdAsync(int catId, CancellationToken ct)
        {
            var items = new List<Item>();
            const string sql = "SELECT id, name, description, price_stars, photo_file_ids, availability FROM items WHERE category_id = @catId";

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@catId", catId);

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new Item {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    PriceStars = reader.GetInt32("price_stars"),
                    PhotoBytes = reader["photo_file_ids"] as byte[],
                    Availability = reader.GetString("availability")
                });
            }
            return items;
        }
        
        
        
    }
    }
