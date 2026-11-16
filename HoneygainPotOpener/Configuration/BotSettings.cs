namespace HoneygainPotOpener.Configuration
{
    public sealed class BotSettings
    {
        public string EmailAddress { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool HasValidCredentials
            => !string.IsNullOrWhiteSpace(EmailAddress) && !string.IsNullOrWhiteSpace(Password);
    }
}
