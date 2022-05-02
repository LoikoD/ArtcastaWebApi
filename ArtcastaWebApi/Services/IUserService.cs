using ArtcastaWebApi.Models;

namespace ArtcastaWebApi.Services
{
    public interface IUserService
    {
        List<User> GetUsers();

        void CreateUser(User newUser);

        void UpdateUserInfo(User newUser);

        void UpdatePassword(int userId, string password);

        void DeleteUser(int userId);
    }
}
