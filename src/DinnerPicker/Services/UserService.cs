using DinnerPicker.Models;
using DinnerPicker.Persistence;

namespace DinnerPicker.Services;

public class UserService : IUserService
{
    private readonly IDataStore _store;
    private readonly AppData _data;

    public UserService(IDataStore store, PantryService pantryService)
    {
        _store = store;
        _data = pantryService.GetAppData();
    }

    public string ActiveUserId => _data.ActiveUserId;
    public string ActiveUserName => _data.ActiveProfile.Name;

    public IReadOnlyList<UserProfile> GetUsers() =>
        _data.Users.Values.OrderBy(u => u.Name).ToList().AsReadOnly();

    public string AddUser(string name)
    {
        var userId = Guid.NewGuid().ToString("N")[..8];
        var profile = new UserProfile
        {
            Id = userId,
            Name = name.Trim(),
            IsFirstRun = false,
            PantryStaples = [.. PantryService.DefaultStaples]
        };
        _data.Users[userId] = profile;
        _store.Save(_data);
        return userId;
    }

    public void SwitchUser(string userId)
    {
        if (!_data.Users.ContainsKey(userId)) return;
        _data.ActiveUserId = userId;
        _store.Save(_data);
    }

    public void DeleteUser(string userId)
    {
        if (_data.Users.Count <= 1) return; // Can't delete the last user
        _data.Users.Remove(userId);
        if (_data.ActiveUserId == userId)
            _data.ActiveUserId = _data.Users.Keys.First();
        _store.Save(_data);
    }
}
