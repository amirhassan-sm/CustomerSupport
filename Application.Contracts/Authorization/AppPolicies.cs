namespace Application.Contracts.Authorization
{
    public static class AppPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string Staff = "Staff";
        public const string TicketAccess = "TicketAccess";
    }
}
