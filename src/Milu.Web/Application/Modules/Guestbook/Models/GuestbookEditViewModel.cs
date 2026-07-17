namespace Milu.Web.Application.Modules.Guestbook.Models;

public sealed record GuestbookEditViewModel(
    int Id,
    GuestbookInputModel Input,
    DateTime CreatedAt);
