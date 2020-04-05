namespace Ringo.Api.Models
{
    public class AuthorizationResult
    {
        public string UserId { get; set; }
        public bool Authorized { get; set; }
        public string AuthorizationUrl { get; set; }
    }
}
