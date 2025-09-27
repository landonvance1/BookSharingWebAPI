namespace BookSharingApp.Common
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RateLimitAttribute : Attribute
    {
        public string LimitName { get; }
        public RateLimitScope Scope { get; }

        public RateLimitAttribute(string limitName, RateLimitScope scope = RateLimitScope.User)
        {
            LimitName = limitName;
            Scope = scope;
        }
    }
}