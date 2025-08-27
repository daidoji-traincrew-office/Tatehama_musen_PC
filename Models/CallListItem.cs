namespace Tatehama_musen_PC.Models;

public class CallListItem
{
    public string DisplayName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public override string ToString()
    {
        return DisplayName;
    }
}
