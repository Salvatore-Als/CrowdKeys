namespace CrowdKeys.Models;

public class AppSettings
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public List<RedemptionBinding> Bindings { get; set; } = [];
}
