namespace E_commerce_Application.Configuration;

public class EmailSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? FromName { get; set; }
    public string? FromAddress { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}


