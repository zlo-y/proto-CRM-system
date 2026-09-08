namespace Manager.BusinessLogic.Interfaces;

public interface ICurrentUserProvider
{
    int GetCurrentUserId();
    int GetEmployeeId();
    bool IsAdmin();
}