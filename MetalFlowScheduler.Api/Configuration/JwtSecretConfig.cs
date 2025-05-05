namespace MetalFlowScheduler.Api.Configuration
{
    public class JwtSecretConfig
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpirationInMinutes { get; set; }

        public JwtSecretConfig()
        {
            
        }

        public JwtSecretConfig(string secret, string issuer, string audience, string expirationInMinutes)
        {
            this.Secret = secret;
            this.Issuer = issuer;
            this.Audience = audience;
            this.ExpirationInMinutes = int.Parse(expirationInMinutes);
        }
    }
}
