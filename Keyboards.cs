using Telegram.Bot.Types.ReplyMarkups;

namespace tb_lab
{
    internal class Keyboards
    {
        static public InlineKeyboardMarkup CreateCancelRegistrationKeyboard()
        {
            var cancelButton = new InlineKeyboardButton("Отмена регистрации")
            {
                CallbackData = "/cancelregistration"
            };
            return new InlineKeyboardMarkup(cancelButton);
        }
        static public InlineKeyboardMarkup CreateCancelTaskKeyboard()
        {
            var cancelButton = new InlineKeyboardButton("Отмена выполнения задания")
            {
                CallbackData = "/cancelSolvingTask"
            };
            return new InlineKeyboardMarkup(cancelButton);
        }
        static public InlineKeyboardMarkup CreateTasksKeyboard()
        {
            var sendCertReqToCAButton = new InlineKeyboardButton("Отправить запрос на сертификат в УЦ")
            {
                CallbackData = "/sendCertReqToCA"
            };
            var sendCertReqToAdminButton = new InlineKeyboardButton("Отправить запрос на сертификат на проверку админу")
            {
                CallbackData = "/sendCertReqToAdmin"
            };
            var getResourceButton = new InlineKeyboardButton("Отправить запрос на предоставление защищаемых ресурсов")
            {
                CallbackData = "/getResource"
            };
            List<List<InlineKeyboardButton>> keyBoard = new List<List<InlineKeyboardButton>>();
            keyBoard.Add(new List<InlineKeyboardButton> { sendCertReqToCAButton });
            keyBoard.Add(new List<InlineKeyboardButton> { sendCertReqToAdminButton });
            keyBoard.Add(new List<InlineKeyboardButton> { getResourceButton });
            return new InlineKeyboardMarkup(keyBoard);
        }

        static public InlineKeyboardMarkup CreateIfStudentHasAdminCertificationKeyboard()
        {
            var yesButton = new InlineKeyboardButton("Да")
            {
                CallbackData = "/sendAdminCertification"
            };
            var noButton = new InlineKeyboardButton("Нет")
            {
                CallbackData = "/doNotSendAdminCertification"
            };
            var cancelButton = new InlineKeyboardButton("Отмена выполнения задания")
            {
                CallbackData = "/cancelSolvingTask"
            };
            List<List<InlineKeyboardButton>> keyBoard = new List<List<InlineKeyboardButton>>();
            keyBoard.Add(new List<InlineKeyboardButton> { yesButton, noButton });
            keyBoard.Add(new List<InlineKeyboardButton> { cancelButton });
            return new InlineKeyboardMarkup(keyBoard);
        }
    }
}