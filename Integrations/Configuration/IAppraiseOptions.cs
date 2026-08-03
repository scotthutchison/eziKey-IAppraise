namespace Integrations.Configuration
{
    public class IAppraiseOptions
    {
        public const string SectionName = "IAppraise";

        public string BaseUrl { get; set; } = "https://www.testdriveloan.com.au/api/";

        public string? Username { get; set; }
        public string? Password { get; set; }

        public string? StaticToken { get; set; }

        public int DefaultDealershipId { get; set; } = 251;
    }
}
