using Microsoft.AspNetCore.Mvc;

namespace STAT_Academy.Api.Models
{
    public class LoginModel
    {
        public string email { get; set; }
        public string password { get; set; }
    }
}