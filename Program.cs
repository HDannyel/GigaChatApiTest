using GigaChatApiTest.GigaChatModels;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private const string clientId = "ZTNiN2MzZjItYTA2Zi00YzgzLTlmMGEtNmQxNWViNGYyZjBhOmJkMjE1MWE2LWE5YTQtNDc4Ni04Mzg2LWJjNjNiYTY2NjQ3ZA==";

    static async Task Main(string[] args)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            Console.WriteLine("Токен получен.");

            var fileContent = File.ReadAllBytes("C:\\Users\\danil\\OneDrive\\Рабочий стол\\Документ Microsoft Word (2).txt");
            var fileId = await UploadFile(token, "Документ Microsoft Word(2).txt", fileContent);
            Console.WriteLine($"Id файла: {fileId}");

            var response = await SendToGigaChatAsync(token);
            Console.WriteLine("Ответ модели:");
            Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    

    static async Task<string> GetAccessTokenAsync()
    {
        var authHeaderValue = clientId;

        // Создаем контент с правильным Content-Type
        var content = new StringContent("scope=GIGACHAT_API_PERS", Encoding.UTF8, "application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://ngw.devices.sberbank.ru:9443/api/v2/oauth")
        {
            Headers =
            {
                { "Authorization", $"Basic {authHeaderValue}" },
                { "RqUID", Guid.NewGuid().ToString() }
            },
            Content = content // Content-Type теперь внутри content
        };


        //-H 'Content-Type: application/x-www-form-urlencoded' \
        //-H 'Accept: application/json' \
        //-H 'RqUID: <идентификатор_запроса>' \
        //-H 'Authorization: Basic ключ_авторизации' \
        //--data - urlencode 'scope=GIGACHAT_API_PERS'


        var response = await client.SendAsync(request);
        //response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(responseBody);
        return json.RootElement.GetProperty("access_token").GetString();
    }

    static async Task<string> SendToGigaChatAsync(string token)
    {
        var payload = new
        {
            model = "GigaChat",
            messages = new[]
            {
                new { role = "system", content = "Ты помощник, который отвечает на вопросы." },
                new { role = "user", content = "Как работает эта модель?" }
            },
            temperature = 0.7,
            max_tokens = 100
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"); // Content-Type здесь

        var request = new HttpRequestMessage(HttpMethod.Post, "https://gigachat.devices.sberbank.ru/api/v1/chat/completions")
        {
            Headers =
            {
                { "Authorization", $"Bearer {token}" }
            },
            Content = content // Content-Type теперь внутри content
        };

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(responseBody);
        return json.RootElement.GetProperty("choices")[0]
                   .GetProperty("message")
                   .GetProperty("content")
                   .GetString();
    }
    //application/msword


    static async Task<Guid> UploadFile(string token, string filename, byte[] body)
    {

        var requestUrl = "https://gigachat.devices.sberbank.ru/api/v1/files";

        using (var client = new HttpClient())
        {
            // Установка заголовка авторизации
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Подготовка данных формы
            using (var content = new MultipartFormDataContent())
            {
                // Чтение файла
                var fileStream = new FileStream("C:\\Users\\danil\\OneDrive\\Рабочий стол\\Документ Microsoft Word (2).txt", FileMode.Open, FileAccess.Read);
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "file", filename); // "file" - имя поля формы для файла



                //var fileContent = new ByteArrayContent(body);
                ////fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

                //// Добавление файла в форму
                //content.Add(fileContent, "file", filename);

                // Добавление текстового параметра "purpose"
                var purposeContent = new StringContent("general", Encoding.UTF8);
                content.Add(purposeContent, "purpose");

                // Отправка запроса
                var response = await client.PostAsync(requestUrl, content);

                // Проверка ответа
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status Code: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");
                var uploadFileResponse = JsonSerializer.Deserialize<UploadFileResponse>(responseBody);

                return Guid.Parse(uploadFileResponse!.id);
            }
        }

  
        //        curl - L - X POST 'https://gigachat.devices.sberbank.ru/api/v1/files' \
        //-H 'Content-Type: multipart/form-data' \
        //-H 'Accept: application/json' \
        //-H 'Authorization: Bearer <TOKEN>'
    }
}