namespace Milu.Web.Application.Modules.Guestbook.Models;

public sealed class GuestbookEntry
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
