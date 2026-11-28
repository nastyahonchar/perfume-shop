using Pulumi.Keycloak;

namespace Nota.Infra;

public static class RealmSetup
{
    public const string Name = "DevRealmDrivers";

    // Realm DevRealmDrivers описаний кодом (IaC): реєстрація і скидання пароля дозволені, увімкнено "запам'ятати мене",
    // входом за email, використовуються стандартні теми Keycloak. Решта ресурсів (resource servers, scopes, ролі,
    // clients, users) прив'язується до цього realm через його Id.
    public static Realm Create() => new("dev-realm-drivers", new RealmArgs
    {
        RealmName = Name,
        Enabled = true,
        DisplayName = "Dev Realm Drivers",
        RegistrationAllowed = true,
        ResetPasswordAllowed = true,
        RememberMe = true,
        LoginWithEmailAllowed = true,
        VerifyEmail = false,
        LoginTheme = "keycloak",
        AccountTheme = "keycloak.v3",
        AdminTheme = "keycloak.v2",
        EmailTheme = "keycloak",
        AccessTokenLifespan = "15m",
        SslRequired = "external"
    });
}