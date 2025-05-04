using GigaChatApiTest.GigaChatModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

class Program
{
    private const string ClientId = "ZTNiN2MzZjItYTA2Zi00YzgzLTlmMGEtNmQxNWViNGYyZjBhOmJkMjE1MWE2LWE5YTQtNDc4Ni04Mzg2LWJjNjNiYTY2NjQ3ZA==";
    private const string UploadFileUrl = "https://gigachat.devices.sberbank.ru/api/v1/files";
    private const string FilePath = @"C:\Users\danil\OneDrive\Рабочий стол\test.txt";

   

    static void Main(string[] args)
    {
        try
        {
            var token = GetAccessToken().Result;
            Console.WriteLine("Токен получен.");

            var fileId = UploadFile(token, File.ReadAllBytes(FilePath));
            Console.WriteLine($"ID файла: {fileId}");

            var fileContent = GetFileContent(token, fileId);
            Console.WriteLine("Содержимое файла получено:");

            // Инициализация истории сообщений
            var messages = new List<ChatMessage>
            {
                new ChatMessage { role = "system", content = "Ты помощник, который отвечает на вопросы." },
                new ChatMessage { role = "user", content = $"Ответ верный? \n\n{fileContent}" }
            };

            // Первый запрос к модели
            var response = SendToGigaChat(token, messages);
            Console.WriteLine("Ответ модели:");
            Console.WriteLine(response);
            messages.Add(new ChatMessage { role = "assistant", content = response });

            // Цикл для продолжения диалога
            while (true)
            {
                Console.Write("Вы: ");
                string userMessage = Console.ReadLine();
                if (userMessage.ToLower() == "exit")
                    break;

                messages.Add(new ChatMessage { role = "user", content = userMessage });

                try
                {
                    string modelResponse = SendToGigaChat(token, messages);
                    Console.WriteLine("Модель: " + modelResponse);
                    messages.Add(new ChatMessage { role = "assistant", content = modelResponse });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при получении ответа от модели: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка:");
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    static async Task<string> GetAccessToken()
    {
        var authHeaderValue = ClientId;
        var content = "scope=GIGACHAT_API_PERS";

        var request = (HttpWebRequest)WebRequest.Create("https://ngw.devices.sberbank.ru:9443/api/v2/oauth");
        request.Method = "POST";
        request.Headers.Add("Authorization", $"Basic {authHeaderValue}");
        request.Headers.Add("RqUID", Guid.NewGuid().ToString());
        request.ContentType = "application/x-www-form-urlencoded";

        using (var requestStream = request.GetRequestStream())
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            requestStream.Write(contentBytes, 0, contentBytes.Length);
        }

        using (var response = (HttpWebResponse)request.GetResponse())
        using (var reader = new StreamReader(response.GetResponseStream()))
        {
            var responseBody = await reader.ReadToEndAsync();
            var json = JsonDocument.Parse(responseBody);
            return json.RootElement.GetProperty("access_token").GetString();
        }
    }

    static Guid UploadFile(string token, byte[] fileData)
    {
        string boundary = Guid.NewGuid().ToString();
        var request = (HttpWebRequest)WebRequest.Create(UploadFileUrl);
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.Headers.Add("Authorization", "Bearer " + token);
        request.Accept = "application/json";

        using (var requestStream = request.GetRequestStream())
        {
            string fileName = Path.GetFileName(FilePath);
            WriteMultipartFormData(requestStream, boundary, "file", fileName, fileData);
            WriteMultipartFormData(requestStream, boundary, "purpose", "general");
            WriteMultipartFormDataEnd(requestStream, boundary);
        }

        try
        {
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                var result = reader.ReadToEnd();
                var uploadFileResponse = JsonSerializer.Deserialize<UploadFileResponse>(result);
                return Guid.Parse(uploadFileResponse.id);
            }
        }
        catch (WebException ex)
        {
            using var stream = ex.Response?.GetResponseStream();
            using var reader = new StreamReader(stream);
            var error = reader.ReadToEnd();
            throw new Exception($"Ошибка загрузки файла: {error}");
        }
    }

    static string GetFileContent(string token, Guid fileId)
    {
        string url = $"https://gigachat.devices.sberbank.ru/api/v1/files/{fileId}/content";

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.Accept = "application/json";
        request.Headers.Add("Authorization", $"Bearer {token}");

        try
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        catch (WebException ex)
        {
            using (var stream = ex.Response?.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string errorText = reader.ReadToEnd();
                throw new Exception($"Ошибка сервера: {errorText}");
            }
        }
    }

    static string SendToGigaChat(string token, List<ChatMessage> messages)
    {
        var payload = new
        {
            model = "GigaChat",
            messages = messages.ToArray(),
            temperature = 0.7,
            max_tokens = 500
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var request = (HttpWebRequest)WebRequest.Create("https://gigachat.devices.sberbank.ru/api/v1/chat/completions");
        request.Method = "POST";
        request.Headers.Add("Authorization", "Bearer " + token);
        request.ContentType = "application/json";

        using (var requestStream = request.GetRequestStream())
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonPayload);
            requestStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        try
        {
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                var responseBody = reader.ReadToEnd();
                var json = JsonDocument.Parse(responseBody);
                return json.RootElement.GetProperty("choices")[0]
                           .GetProperty("message")
                           .GetProperty("content")
                           .GetString();
            }
        }
        catch (WebException ex)
        {
            using var stream = ex.Response?.GetResponseStream();
            using var reader = new StreamReader(stream);
            var error = reader.ReadToEnd();
            throw new Exception($"Ошибка запроса к модели: {error}");
        }
    }

    // --- Вспомогательные методы для Multipart/Form-данных ---
    static void WriteMultipartFormData(Stream stream, string boundary, string name, string value)
    {
        string header = $"\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n";
        byte[] headerBytes = Encoding.UTF8.GetBytes(header);
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);

        stream.Write(headerBytes, 0, headerBytes.Length);
        stream.Write(valueBytes, 0, valueBytes.Length);
    }

    static void WriteMultipartFormData(Stream stream, string boundary, string name, string fileName, byte[] fileData)
    {
        string header = $"\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"; filename=\"{fileName}\"\r\nContent-Type: text/plain\r\n\r\n";
        byte[] headerBytes = Encoding.UTF8.GetBytes(header);

        stream.Write(headerBytes, 0, headerBytes.Length);
        stream.Write(fileData, 0, fileData.Length);
    }

    static void WriteMultipartFormDataEnd(Stream stream, string boundary)
    {
        string footer = $"\r\n--{boundary}--\r\n";
        byte[] footerBytes = Encoding.UTF8.GetBytes(footer);
        stream.Write(footerBytes, 0, footerBytes.Length);
    }
}