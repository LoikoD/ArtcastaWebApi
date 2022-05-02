namespace ArtcastaWebApi.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public List<string> AccessPoints { get; set; } = new List<string>();
    }
}
