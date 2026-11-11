namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Запрос в Честный знак ушёл, но ответ не получен: документ мог быть принят,
/// поэтому повторная отправка создала бы второй документ.
/// </summary>
public class GisMtSendUnconfirmedException(string message, Exception? innerException = null)
    : Exception(message, innerException);
