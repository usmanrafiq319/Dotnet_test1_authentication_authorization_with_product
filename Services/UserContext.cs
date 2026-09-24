using System.Security.Claims;

namespace Dotnet_test1_authentication_authorization_with_product.Services
{
    public class UserContext(IHttpContextAccessor httpContextAccessor) :IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;


        public Guid? GetCurrentUserId()
        {
            // 1. Get the User principal from the current web request
            var user = _httpContextAccessor.HttpContext?.User;

            // 2. Extract the NameIdentifier claim string
            var userIdClaim = user?.FindFirstValue(ClaimTypes.NameIdentifier);

            // 3. Try to parse it securely into a Guid
            if (Guid.TryParse(userIdClaim, out Guid validGuid))
            {
                return validGuid;
            }

            // Return null if the claim doesn't exist or isn't a valid Guid format 
            return null;
        }

    }
}
