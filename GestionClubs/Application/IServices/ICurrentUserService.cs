namespace GestionClubs.Application.IServices;

public interface ICurrentUserService
{
    string? GetEmail();
    Task CheckUserIsAdminForClub(int clubId);
}
