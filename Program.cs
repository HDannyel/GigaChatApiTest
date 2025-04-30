using GigaChatAdapter;
using System.Text;

//Настройка для работы консоли с кириллицей
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.InputEncoding = Encoding.GetEncoding(1251);
Console.OutputEncoding = Encoding.GetEncoding(1251);

//Укажите аутентификационные данные из личного кабинета
string authData = "ZTNiN2MzZjItYTA2Zi00YzgzLTlmMGEtNmQxNWViNGYyZjBhOmJkMjE1MWE2LWE5YTQtNDc4Ni04Mzg2LWJjNjNiYTY2NjQ3ZA==";

//Запуск авторизации в гигачате
Authorization auth = new Authorization(authData, GigaChatAdapter.Auth.RateScope.GIGACHAT_API_PERS);
var authResult = await auth.SendRequest();

if (authResult.AuthorizationSuccess)
{
    Completion completion = new Completion();
    Console.WriteLine("Напишите запрос к модели. В ином случае закройте окно, если дальнейшую работу с чатботом необходимо прекратить."); //RU

    while (true)
    {
        //Чтение промпта с консоли
        var prompt = Console.ReadLine();

        //Обновление токена, если он просрочился
        await auth.UpdateToken();

        //Установка доп.настроек
        CompletionSettings settings = new CompletionSettings("GigaChat:latest", 2, null, 4, 100);

        //Отправка промпта (с историей)
        var result = await completion.SendRequest(auth.LastResponse.GigaChatAuthorizationResponse?.AccessToken, prompt, true, settings);

        if (result.RequestSuccessed)
        {
            foreach (var it in result.GigaChatCompletionResponse.Choices)
            {
                Console.WriteLine(it.Message.Content);
            }
        }
        else
        {
            Console.WriteLine(result.ErrorTextIfFailed);
        }
    }
}
else
{
    Console.WriteLine(authResult.ErrorTextIfFailed);
}