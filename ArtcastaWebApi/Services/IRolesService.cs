using ArtcastaWebApi.Models;

namespace ArtcastaWebApi.Services
{
    public interface IRolesService
    {
        List<Role> GetRoles();
        List<AccessCategory> GetAccessCategoriesByRoleId(int roleId);
        //List<int> GetAccessTablesByRoleId(int roleId);
        List<int> GetAccessPointsByRoleId(int roleId);
        void UpdateRole(Role role);
        void CreateRole(Role role);
        List<int> GetRoleIdsByAccessPointId(int accessPointId);
        void AddAccessCategoryToRole(AccessCategory accessCategory, int roleId);
        void DeleteAccessCategoriesByCategoryId(int categoryId);
        void DeleteRole(int roleId);
        int IsUsersHasRoleId(int roleId);
    }
}
