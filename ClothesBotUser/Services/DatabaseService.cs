using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
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

        // --- МЕТОДЫ КАТАЛОГА ---

        public async Task<List<Item>> GetAllItemsAsync(CancellationToken ct)
        {
            var items = new List<Item>();
            const string sql = "SELECT id, name, description, price_stars, photo_file_ids, availability FROM items";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
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

        public async Task<Item?> GetItemByIdAsync(int id, CancellationToken ct)
        {
            const string sql = "SELECT id, name, description, price_stars, photo_file_ids, availability, category_id FROM items WHERE id = @id";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                return new Item {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    Description = reader.GetString("description"),
                    PriceStars = reader.GetInt32("price_stars"),
                    PhotoBytes = reader["photo_file_ids"] as byte[], 
                    Availability = reader.GetString("availability"),
                    CategoryId = reader.IsDBNull(reader.GetOrdinal("category_id")) ? null : reader.GetInt32("category_id")
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
            while (await reader.ReadAsync(ct)) list.Add((reader.GetInt32("id"), reader.GetString("name")));
            return list;
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

        // --- МЕТОДЫ ЗАКАЗОВ ---

        public async Task<int> CreateOrderAndGetIdAsync(long userId, string username, int itemId, string comment, CancellationToken ct)
        {
            const string sql = @"INSERT INTO orders (user_telegram_id, user_name, item_id, customer_comment, status, is_notified) 
                                 VALUES (@uid, @uname, @iid, @comment, 'pending', 0);
                                 SELECT LAST_INSERT_ID();";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@uid", userId);
            command.Parameters.AddWithValue("@uname", (object)username ?? DBNull.Value);
            command.Parameters.AddWithValue("@iid", itemId);
            command.Parameters.AddWithValue("@comment", (object)comment ?? DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        }

        public async Task UpdateOrderExternalIdAsync(int orderId, string externalId, CancellationToken ct)
        {
            const string sql = "UPDATE orders SET external_id = @extId WHERE id = @orderId";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@extId", externalId);
            command.Parameters.AddWithValue("@orderId", orderId);
            await command.ExecuteNonQueryAsync(ct);
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken ct)
        {
            const string sql = "UPDATE orders SET status = @status WHERE id = @orderId";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@orderId", orderId);
            await command.ExecuteNonQueryAsync(ct);
        }

        public async Task<string?> GetExternalIdByOrderIdAsync(int orderId, CancellationToken ct)
        {
            const string sql = "SELECT external_id FROM orders WHERE id = @id";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", orderId);
            return (string?)await command.ExecuteScalarAsync(ct);
        }

        public async Task<List<dynamic>> GetUserOrdersAsync(long userId, CancellationToken ct)
        {
            var orders = new List<dynamic>();
            const string sql = @"
                SELECT o.id, o.status, o.customer_comment, i.name as item_name, i.price_stars 
                FROM orders o
                JOIN items i ON o.item_id = i.id
                WHERE o.user_telegram_id = @uid
                ORDER BY o.id DESC LIMIT 10";

            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@uid", userId);

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                orders.Add(new {
                    Id = reader.GetInt32("id"),
                    Status = reader.GetString("status"),
                    ItemName = reader.GetString("item_name"),
                    Price = reader.GetInt32("price_stars"),
                    Comment = reader.IsDBNull(reader.GetOrdinal("customer_comment")) ? "-" : reader.GetString("customer_comment")
                });
            }
            return orders;
        }

        public async Task<List<dynamic>> GetPendingOrdersAsync(long userId, CancellationToken ct)
        {
            var orders = new List<dynamic>();
            const string sql = "SELECT id, external_id FROM orders WHERE user_telegram_id = @uid AND status = 'pending' AND external_id IS NOT NULL";
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@uid", userId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                orders.Add(new { Id = reader.GetInt32("id"), ExternalId = reader.GetString("external_id") });
            }
            return orders;
        }
    }
}