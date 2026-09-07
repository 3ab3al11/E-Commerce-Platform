using System.ComponentModel.DataAnnotations;

namespace ECommerce522.ViewModels
{
    public class LoginVM
    {
        public int Id { get; set; }

        public string EmailOrUserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
