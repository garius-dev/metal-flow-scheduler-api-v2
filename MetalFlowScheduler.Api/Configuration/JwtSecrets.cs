namespace MetalFlowScheduler.Api.Configuration
{
    public class JwtSecrets
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public long ExpirationInMinutes { get; set; }

        public JwtSecrets(string secret, string issuer, string audience, string expirationInMinutes)
        {
            this.Secret = secret;
            this.Issuer = issuer;
            this.Audience = audience;
            this.ExpirationInMinutes = long.Parse(expirationInMinutes);
        }
    }
}
