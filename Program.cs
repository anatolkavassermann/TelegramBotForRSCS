using System.Text;

//Telegram.Bot
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using tb_lab;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Org.BouncyCastle.Crypto;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;
using System.Net.Mail;
using System.Net;
using System.Collections;

/*
//sendCertReqToCA - отправка запроса на сертификат напрямую УЦ
//sendCertReqToAdmin - отправка запроса на сертификат админу и получение от него заключения, что запрос корректен
//getResource - получение данных от системы
подписываемые данные - JSON:
{
    "resource":"flag"
}
 */
SecureRandom r = new();
CA ca = new();
Admin admin = new();
flagGiver flagGiver = new();
Newtonsoft.Json.Linq.JObject mainConfig = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(System.IO.File.ReadAllText("config.json"));
SmtpClient smtpClient = new(mainConfig.Property("SmtpServer").Value.ToString())
{
    EnableSsl = false,
    Credentials = new NetworkCredential(mainConfig.Property("email").Value.ToString(), mainConfig.Property("password").Value.ToString())
};
var token = "";
var connectionString = "";
var botClient = new TelegramBotClient(token: token);
using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = new UpdateType[] { UpdateType.Message, UpdateType.CallbackQuery }
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    switch (update.Type)
    {
        case UpdateType.Message:
            {
                var UserName = update.Message!.From!.Username;
                if (UserName == null)
                {
                    UserName = "Человек без UserName";
                }
                Console.WriteLine($"{DateTime.UtcNow} Captured update from {update.Message!.From!.Id} - {UserName} - {update.Message!.Text!}; message type: {update.Message!.Type}");
                var isVerified = sql.IsVerified(connectionString!, update.Message!.From!.Id);

                if (isVerified == "OK")
                {
                    if (
                        (update.Message!.Type == MessageType.Text) &&
                        (update.Message!.ReplyToMessage == null) &&
                        (update.Message.ForwardFrom == null)
                    )
                        //await HandleOKMessageAsync(botClient, update, cancellationToken);
                        Task.Run(() => HandleOKMessageAsync(botClient, update, cancellationToken), cancellationToken);
                    else
                    {
                        sql.CancelAllTasks(connectionString, update.Message!.From!.Id);
                        sql.StoreCertReq(connectionString, update.Message!.From!.Id, "");
                        await botClient.SendTextMessageAsync(
                            chatId: update.Message!.Chat.Id,
                            text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, бот не обрабатывает такой контент. Только текст",
                            cancellationToken: cancellationToken,
                            replyMarkup: Keyboards.CreateTasksKeyboard()
                            );
                    }
                }
                else if (isVerified == "NV")
                {
                    if (
                        (update.Message!.Type == MessageType.Text) &&
                        (update.Message!.ReplyToMessage == null) &&
                        (update.Message.ForwardFrom == null)
                    )
                        //await HandleNVMessageAsync(botClient, update, cancellationToken);
                        Task.Run(() => HandleNVMessageAsync(botClient, update, cancellationToken), cancellationToken);
                    else
                        await botClient.SendTextMessageAsync(
                            chatId: update.Message!.Chat.Id,
                            text: $"{UserName}, бот не обрабатывает такой контент. Пожалуйста, введи код подтверждения, отправленный на почту",
                            cancellationToken: cancellationToken,
                            replyMarkup: Keyboards.CreateCancelRegistrationKeyboard()
                            );
                }
                else if (isVerified == "NR")
                {
                    if (
                        (update.Message!.Type == MessageType.Text) &&
                        (update.Message!.ReplyToMessage == null) &&
                        (update.Message.ForwardFrom == null)
                    )
                        //await HandleNRMessageAsync(botClient, update, cancellationToken);
                        Task.Run(() => HandleNRMessageAsync(botClient, update, cancellationToken), cancellationToken);
                    else
                        await botClient.SendTextMessageAsync(
                            chatId: update.Message!.Chat.Id,
                            text: $"{UserName}, бот не обрабатывает такой контент. Пожалуйста, введи свой логин от sdo.miigaik.ru",
                            cancellationToken: cancellationToken
                            );
                }
                break;
            }
        case UpdateType.CallbackQuery:
            {
                var UserName = update.CallbackQuery!.From!.Username;
                if (UserName == null)
                {
                    UserName = "Человек без UserName";
                }
                Console.WriteLine($"{DateTime.UtcNow} Captured update from {update.CallbackQuery!.From!.Id} - {UserName} - {update.CallbackQuery!.Data}");
                var isVerified = sql.IsVerified(connectionString!, update.CallbackQuery!.From!.Id);
                if (isVerified == "OK")
                {
                    //await HandleOKCallBackAsync(botClient, update, cancellationToken);
                    Task.Run(() => HandleOKCallBackAsync(botClient, update, cancellationToken), cancellationToken);
                }
                else if (isVerified == "NV")
                {
                    //await HandleNVCallBackAsync(botClient, update, cancellationToken);
                    Task.Run(() => HandleNVCallBackAsync(botClient, update, cancellationToken), cancellationToken);
                }
                else if (isVerified == "NR")
                {
                    //await HandleNRCallBackAsync(botClient, update, cancellationToken);
                    Task.Run(() => HandleNRCallBackAsync(botClient, update, cancellationToken), cancellationToken);
                }
                break;
            }
    }
    Console.WriteLine("End captured");
}

async Task HandleNRMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    string PotentiaStudentID = update.Message!.Text!;
    if (sql.DoesStudentExist(connectionString, PotentiaStudentID))
    {
        if (!sql.IsStudentTIDALreadyBinded(connectionString, PotentiaStudentID))
        {
            string StudentID = PotentiaStudentID;

            string nonce = "";
            for (int i = 0; i < 64; i++)
            {
                nonce += Encoding.ASCII.GetString(new byte[] { (byte)r.Next(65, 91) });
            }
            sql.SetTID(connectionString, update.Message!.From!.Id, StudentID);
            var studentEmail = sql.GetStudentEmail(connectionString, update.Message!.From!.Id);
            bool flag = false;
            for (int i = 0; i < 4; i++)
            {
                if (flag)
                {
                    break;
                }
                try
                {
                    smtpClient.Send(
                    new MailMessage(
                        from: mainConfig.Property("email").Value.ToString(),
                        to: studentEmail,
                        subject: "Одноразовый код для регистрации в телеграм боте",
                        body: nonce)
                    );
                    sql.SetNonce(connectionString, update.Message!.From!.Id, nonce);
                    var UserName = update.Message!.From!.Username;
                    if (UserName == null) 
                    {
                        UserName = "Человек без UserName";
                    }
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message!.Chat.Id,
                        text: $"{UserName}, на почту {studentEmail} отправлен код подтверждения. Отправь его для завершения регистрации",
                        cancellationToken: cancellationToken,
                        replyMarkup: Keyboards.CreateCancelRegistrationKeyboard()
                        );
                    flag = true;
                }
                catch
                {

                }
            }
            
            if (!flag)
            {
                var UserName = update.Message!.From!.Username;
                if (UserName == null)
                {
                    UserName = "Человек без UserName";
                }
                sql.UnsetTID(connectionString, StudentID);
                await botClient.SendTextMessageAsync(
                    chatId: update.Message!.Chat.Id,
                    text: $"{UserName}, почтовый сервер МИИГАиК недоступен. Пожалуйста, повтори попытку позднее",
                    cancellationToken: cancellationToken
                    );
            }
        }
        else
        {
            var UserName = update.Message!.From!.Username;
            if (UserName == null)
            {
                UserName = "Человек без UserName";
            }
            await botClient.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: $"{UserName}, этот логин уже используется. Если это ошибка - обратись к преподавателю: @anatolkabasurman, или перепроверь свой логин",
            cancellationToken: cancellationToken
            );
        }

    }
    else if (update.Message!.Text! == "/start")
    {
        var UserName = update.Message!.From!.Username;
        if (UserName == null)
        {
            UserName = "Человек без UserName";
        }
        await botClient.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: $"Приветствую тебя, {UserName} !) Сперва тебе необходимо пройти регистрацию. Введи свой логин от sdo.miigaik.ru",
            cancellationToken: cancellationToken
            );
    }
    else
    {
        var UserName = update.Message!.From!.Username;
        if (UserName == null)
        {
            UserName = "Человек без UserName";
        }
        await botClient.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: $"{UserName}, такой логин не принадлежит ни одному из слушателей курса. Повтори ввод своего логина от sdo.miigaik.ru",
            cancellationToken: cancellationToken
            );
    }
}

async Task HandleNVMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var UserName = update.Message!.From!.Username;
    if (UserName == null)
    {
        UserName = "Человек без UserName";
    }
    string verificationString = update.Message!.Text!;
    if (sql.VerifyStudent(connectionString, update.Message!.From!.Id, verificationString))
        await botClient.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, поздравляю с успешным завершением регистрации! Можешь приступать к выполнению заданий!",
            cancellationToken: cancellationToken,
            replyMarkup: Keyboards.CreateTasksKeyboard()
            );
    else
        await botClient.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: $"{UserName}, ты ввел некорректный код проверки. Пожалуйста, проверь введенное значение",
            cancellationToken: cancellationToken
            );
}

async Task HandleNRCallBackAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var UserName = update.CallbackQuery!.From!.Username;
    if (UserName == null)
    {
        UserName = "Человек без UserName";
    }
    if (update.CallbackQuery!.Data == "/cancelregistration")
        await botClient.SendTextMessageAsync(
            chatId: update.CallbackQuery!.Message!.Chat.Id,
            text: $"{UserName}, ты уже отменил процесс регистрации",
            cancellationToken: cancellationToken
            );
    else
    {
        await botClient.SendTextMessageAsync(
            chatId: update.CallbackQuery!.Message!.Chat.Id,
            text: $"{UserName}, ты не можешь выполнять задания до прохождения регистрации. Пожалуйста, заверши его",
            cancellationToken: cancellationToken
            );
    }
}

async Task HandleNVCallBackAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var UserName = update.CallbackQuery!.From!.Username;
    if (UserName == null)
    {
        UserName = "Человек без UserName";
    }
    if (update.CallbackQuery!.Data == "/cancelregistration")
    {
        sql.CancelRegistration(connectionString, update.CallbackQuery!.From!.Id);
        await botClient.SendTextMessageAsync(
            chatId: update.CallbackQuery!.Message!.Chat.Id,
            text: $"{UserName}, процесс регистрации успешно отменен. Для повторной регистрации введи свой логин от sdo.miigaik.ru",
            cancellationToken: cancellationToken
            );
    }
}

async Task HandleOKMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    Hashtable activeTask = sql.GetStudentActiveTask(connectionString, update.Message!.From!.Id);
    if (activeTask.Count == 0)
    {
        await botClient.SendTextMessageAsync(
            chatId: update.Message!.Chat.Id,
            text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, тебе сперва надо выбрать операцию, прежде чем отправлять ответы",
            cancellationToken: cancellationToken,
            replyMarkup: Keyboards.CreateTasksKeyboard()
            );
    }
    else
    {
        if (activeTask.ContainsKey("sendCertReqToCA"))
        {
            if ((int)activeTask["sendCertReqToCA"]! == 1)
            {
                sql.StoreCertReq(connectionString, update.Message!.From!.Id, update.Message!.Text!);
                await botClient.SendTextMessageAsync(
                    chatId: update.Message!.Chat.Id,
                    text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, у тебя есть подтверждение админа, что запрос прошел его проверку?",
                    cancellationToken: cancellationToken,
                    replyMarkup: Keyboards.CreateIfStudentHasAdminCertificationKeyboard()
                    );
            }
            else if ((int)activeTask["sendCertReqToCA"]! == 2)
            {
                string resultX509 = ca.validateCertReq(sql.GetStoredCertReq(connectionString, update.Message!.From!.Id), update.Message!.Text!);
                if (resultX509 == "")
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message!.Chat.Id,
                        text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, запрос на сертификат содержит ошибки или не прошел проверку",
                        cancellationToken: cancellationToken,
                        replyMarkup: Keyboards.CreateTasksKeyboard()
                        );

                else
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message!.Chat.Id,
                        text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, твой сертификат:\n{resultX509}",
                        cancellationToken: cancellationToken,
                        replyMarkup: Keyboards.CreateTasksKeyboard()
                        );
                sql.CancelAllTasks(connectionString, update.Message!.From!.Id);
                sql.StoreCertReq(connectionString, update.Message!.From!.Id, "");
            }
        }
        else if (activeTask.ContainsKey("sendCertReqToAdmin"))
        {
            string adminCertification = admin.checkRequest(update.Message!.Text!);
            if (adminCertification == "")
                await botClient.SendTextMessageAsync(
                    chatId: update.Message!.Chat.Id,
                    text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, запрос на сертификат содержит ошибки или не прошел проверку",
                    cancellationToken: cancellationToken,
                    replyMarkup: Keyboards.CreateTasksKeyboard()
                    );

            else
                await botClient.SendTextMessageAsync(
                    chatId: update.Message!.Chat.Id,
                    text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, подтверждение администратора: {adminCertification}",
                    cancellationToken: cancellationToken,
                    replyMarkup: Keyboards.CreateTasksKeyboard()
                    );
            sql.CancelAllTasks(connectionString, update.Message!.From!.Id);
        }
        else if (activeTask.ContainsKey("getResource"))
        {
            string flag = "";
            try
            {
                flag = flagGiver.GimmeResource(update.Message!.Text!, update.Message!.From!.Id);
                if (flag == "OK")
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message!.Chat.Id,
                        text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, твой флаг: {sql.GetFlag(connectionString, update.Message!.From!.Id)}",
                        cancellationToken: cancellationToken,
                        replyMarkup: Keyboards.CreateTasksKeyboard()
                        );
            }
            catch (Exception ex)
            {
                if (ex.Message == "You are not authorized to do that")
                    await botClient.SendTextMessageAsync(
                        chatId: update.Message!.Chat.Id,
                        text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, у тебя недостаточно прав для получения флага",
                        cancellationToken: cancellationToken,
                        replyMarkup: Keyboards.CreateTasksKeyboard()
                        );

                else
                    await botClient.SendTextMessageAsync(
                            chatId: update.Message!.Chat.Id,
                            text: $"{sql.GetStudentName(connectionString, update.Message!.From!.Id)}, возникла ошибка при проверке ЭП. Маленькая подсказка: не все, что подписано, защищено !)",
                            cancellationToken: cancellationToken,
                            replyMarkup: Keyboards.CreateTasksKeyboard()
                            );
            }
            sql.CancelAllTasks(connectionString, update.Message!.From!.Id);
        }
    }
}

async Task HandleOKCallBackAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    Hashtable activeTask = sql.GetStudentActiveTask(connectionString, update.CallbackQuery!.From!.Id);
    if (activeTask.Count == 0)
    {
        if (update.CallbackQuery!.Data == "/sendCertReqToCA")
        {
            sql.SetActiveTask(connectionString, update.CallbackQuery!.From!.Id, "sendCertReqToCA", 1);
            await botClient.SendTextMessageAsync(
                chatId: update.CallbackQuery!.Message!.Chat.Id,
                text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, отправь мне свой PEM запрос на сертификат. Затем он будет отправлен в УЦ. В случае успешных проверкок тебе будет отправлен x509 сертификат",
                cancellationToken: cancellationToken,
                replyMarkup: Keyboards.CreateCancelTaskKeyboard()
                );
        }
        else if (update.CallbackQuery!.Data == "/sendCertReqToAdmin")
        {
            sql.SetActiveTask(connectionString, update.CallbackQuery!.From!.Id, "sendCertReqToAdmin", 1);
            await botClient.SendTextMessageAsync(
                chatId: update.CallbackQuery!.Message!.Chat.Id,
                text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, отправь мне свой PEM запрос на сертификат. Он будет отправлен администратору, который убедится, что все поля в запросе в норме, и вернет тебе свое подтверждение.",
                cancellationToken: cancellationToken,
                replyMarkup: Keyboards.CreateCancelTaskKeyboard()
                );
        }
        else if (update.CallbackQuery!.Data == "/getResource")
        {
            sql.SetActiveTask(connectionString, update.CallbackQuery!.From!.Id, "getResource", 1);
            await botClient.SendTextMessageAsync(
                chatId: update.CallbackQuery!.Message!.Chat.Id,
                text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, подписанное JSON сообщение с указанием ресурса, к которому хочешь получить доступ. Доступен 1 ресурс: flag",
                cancellationToken: cancellationToken,
                replyMarkup: Keyboards.CreateCancelTaskKeyboard()
                );
        }
        else if (update.CallbackQuery!.Data == "/cancelSolvingTask")
        {
            await botClient.SendTextMessageAsync(
                chatId: update.CallbackQuery!.Message!.Chat.Id,
                text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, ни одно задание не активно. Отменять нечего !)",
                cancellationToken: cancellationToken,
                replyMarkup: Keyboards.CreateTasksKeyboard()
                );
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: update.CallbackQuery!.Message!.Chat.Id,
                text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, нет текущих активных заданий !)",
                cancellationToken: cancellationToken,
                replyMarkup: Keyboards.CreateTasksKeyboard()
                );
        }
    }
    else
    {
        if (update.CallbackQuery!.Data == "/cancelSolvingTask")
        {
            sql.StoreCertReq(connectionString, update.CallbackQuery!.From!.Id, "");
            sql.CancelAllTasks(connectionString, update.CallbackQuery!.From!.Id);
            await botClient.SendTextMessageAsync(
                    chatId: update.CallbackQuery!.Message!.Chat.Id,
                    text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, выполнение задания отменено",
                    cancellationToken: cancellationToken,
                    replyMarkup: Keyboards.CreateTasksKeyboard()
                    );
        }
        else if (activeTask.ContainsKey("sendCertReqToCA"))
        {
            if (update.CallbackQuery!.Data == "/sendAdminCertification")
            {
                sql.SetActiveTask(connectionString, update.CallbackQuery!.From!.Id, "sendCertReqToCA", 2);
                await botClient.SendTextMessageAsync(
                    chatId: update.CallbackQuery!.Message!.Chat.Id,
                    text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, ожидаю от тебя подтверждение администратора",
                    cancellationToken: cancellationToken,
                    replyMarkup: Keyboards.CreateCancelTaskKeyboard()
                    );
            }
            else if (update.CallbackQuery!.Data == "/doNotSendAdminCertification")
            {
                string resultX509 = ca.validateCertReq(sql.GetStoredCertReq(connectionString, update.CallbackQuery!.From!.Id), null);
                if (resultX509 == "")
                    await botClient.SendTextMessageAsync(
                        chatId: update.CallbackQuery!.Message!.Chat.Id,
                        text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, запрос на сертификат содержит ошибки или не прошел проверку",
                        cancellationToken: cancellationToken,
                        replyMarkup: Keyboards.CreateTasksKeyboard()
                        );
                else
                    await botClient.SendTextMessageAsync(
                        chatId: update.CallbackQuery!.Message!.Chat.Id,
                        text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, твой сертификат: {resultX509}",
                        cancellationToken: cancellationToken,
                        replyMarkup: Keyboards.CreateTasksKeyboard()
                        );
                sql.CancelAllTasks(connectionString, update.CallbackQuery!.From!.Id);
                sql.StoreCertReq(connectionString, update.CallbackQuery!.From!.Id, "");
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.CallbackQuery!.Message!.Chat.Id,
                    text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, сперва закончи выполение задания, или отмени текущее",
                    cancellationToken: cancellationToken,
                    replyMarkup: Keyboards.CreateCancelTaskKeyboard()
                    );
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(
                    chatId: update.CallbackQuery!.Message!.Chat.Id,
                    text: $"{sql.GetStudentName(connectionString, update.CallbackQuery!.From!.Id)}, сперва закончи выполение задания, или отмени текущее",
                    cancellationToken: cancellationToken,
                    replyMarkup: Keyboards.CreateCancelTaskKeyboard()
                    );
        }
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}