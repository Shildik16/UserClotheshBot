using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClothesBotUser.Models;

namespace ClothesBotUser.Services
{
    public class PaymentService
    {
        private readonly HttpClient _httpClient;
        
        // ВСТАВЬТЕ ВАШИ ДАННЫЕ ИЗ ЛИЧНОГО КАБИНЕТА PLATEGA
        private readonly string _merchantId = "baa9f472-88d7-4d9a-a4d4-cc690c70edae"; 
        private readonly string _secret = "aoNFn7ADThd9nxRLtqiTjkqGEAHSvSkCI9OjDz8lame99eGIpLCQuyu7OBo9p0DQwzhHSaEIDMJa8L6v7K5ZDJb95OsSAiKWHpAj";

        public PaymentService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://app.platega.io/") };
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-MerchantId", _merchantId);
            _httpClient.DefaultRequestHeaders.Add("X-Secret", _secret);
        }

        /// <summary>
        /// Создание новой транзакции в системе Platega
        /// </summary>
        public async Task<PlategaResponse?> CreatePaymentAsync(int amount, string description, string payload, int methodId)
        {
            try 
            {
                var requestData = new
                {
                    paymentMethod = methodId, // 2 - СБП, 13 - Крипто
                    paymentDetails = new 
                    { 
                        amount = (double)amount, 
                        currency = "RUB" 
                    },
                    description = description,
                    @return = "https://t.me/YetStoreBot_bot", // Замените на юзернейм своего бота
                    failedUrl = "https://t.me/YetStoreBot_bot",
                    payload = payload, // Здесь передаем ID заказа из нашей БД
                    test = 0 // 0 - Боевой режим (транзакции видны в API), 1 - Тестовый
                };

                var options = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var response = await _httpClient.PostAsJsonAsync("transaction/process", requestData, options);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PlategaResponse>();
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка создания платежа: {response.StatusCode} - {error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка при создании платежа: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Проверка текущего статуса транзакции
        /// </summary>
        public async Task<string?> GetPaymentStatusAsync(string transactionId)
        {
            try 
            {
                // Формируем запрос согласно документации Platega
                string url = $"transaction/info/{transactionId}";
                Console.WriteLine($"--- Запрос статуса: https://app.platega.io/{url} ---");

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                    
                    // Выводим ответ для отладки в консоль
                    Console.WriteLine($"Ответ API: {json}");

                    // Извлекаем статус. Обычно он находится в корне объекта или внутри 'transaction'
                    if (json.TryGetProperty("status", out var statusProp))
                    {
                        return statusProp.GetString();
                    }
                    
                    if (json.TryGetProperty("transaction", out var trans) && 
                        trans.TryGetProperty("status", out var s))
                    {
                        return s.GetString();
                    }
                }
                else 
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка API ({response.StatusCode}): {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сети при проверке статуса: {ex.Message}");
            }
            return null;
        }
    }
}