namespace Manager.BusinessLogic.Models;

// 
// Модель для хранения настроек электронной почты, включая хост, порт, учетные данные и адрес отправителя. 
// 
public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}