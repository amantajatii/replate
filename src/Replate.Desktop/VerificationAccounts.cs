namespace Replate.Desktop;

internal static class VerificationAccounts
{
    internal static DemoSession AsCustomer(this DemoSession session)
    {
        session.SignIn(DemoAccounts.CustomerEmail, DemoAccounts.Password);
        return session;
    }

    internal static DemoSession AsOwner(this DemoSession session, string restaurantName = "Dapur Bumi")
    {
        session.SignIn(DemoAccounts.OwnerEmail(restaurantName), DemoAccounts.Password);
        return session;
    }
}
