namespace Myrmex.AspNetCore.Security;

public static class MyrmexAuthenticationSchemes
{
    public const string WebAppIdentity = "Identity.Application";

    public const string ApiSession = "Myrmex.ApiSession";

    public const string DevelopmentActor = "DevelopmentActor";

    public const string WebAppIdentityCookieName = ".Myrmex.Identity.Application";

    public const string ApiSessionCookieName = "Myrmex.ApiSession";
}
