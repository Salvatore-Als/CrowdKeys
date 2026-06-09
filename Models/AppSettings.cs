namespace CrowdKeys.Models;

public class AppSettings
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<RedemptionBinding> Bindings { get; set; } = [];
}
