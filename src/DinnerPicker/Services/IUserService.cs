using DinnerPicker.Models;

namespace DinnerPicker.Services;

public interface IUserService
{
    string ActiveUserId { get; }
    string ActiveUserName { get; }
    IReadOnlyList<UserProfile> GetUsers();
    string AddUser(string name);
    void SwitchUser(string userId);
    void DeleteUser(string userId);
}
