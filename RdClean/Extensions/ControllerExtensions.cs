namespace RdClean.Extensions
{
    public static class ControllerExtensions
    {
        public static string? GetUserName(this HttpRequest request)
        {
            return request.HttpContext.User.Identity?.IsAuthenticated == true 
                ? request.HttpContext.User.Identity?.Name
                : null;
        }
    }
}