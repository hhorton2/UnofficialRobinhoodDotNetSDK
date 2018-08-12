namespace Robinhood.Domain
{
    internal class TokenResponse
    {
        public string Token { get; set; }
        public string MfaType { get; set; }
        public bool MfaRequired { get; set; }
    }
}