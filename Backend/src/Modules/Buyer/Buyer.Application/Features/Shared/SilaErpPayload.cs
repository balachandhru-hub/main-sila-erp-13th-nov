using System.Text.RegularExpressions;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Prepares an ERP request or response body for storage and display: values of credential-like fields (password,
    /// secret, token, api key, authorization, client secret) are masked in JSON, form and XML bodies.
    /// Display truncate is <see cref="MAX_LENGTH"/>; persistence uses <see cref="MAX_PERSIST_LENGTH"/>.
    /// </summary>
    public static class SilaErpPayload
    {
        public const int MAX_LENGTH = 4000;
        /// <summary>Persistence ceiling for SAP request/response bodies (secrets still masked).</summary>
        public const int MAX_PERSIST_LENGTH = 200_000;
        private const string MASK = "***";
        private const string SECRET_KEYS = "password|passwd|pwd|secret|client_?secret|token|access_?token|refresh_?token|api_?key|apikey|authorization|credential[s]?";

        private static readonly Regex JsonSecret = new Regex(
            @"(""(?:" + SECRET_KEYS + @")""\s*:\s*)(""(?:[^""\\]|\\.)*""|[^,}\]\s]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        private static readonly Regex FormSecret = new Regex(
            @"((?:^|[?&])(?:" + SECRET_KEYS + @")=)[^&\s]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        private static readonly Regex XmlSecret = new Regex(
            @"(<(" + SECRET_KEYS + @")>)[^<]*(</\2>)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        public static string? Sanitise(string? body) => SanitiseCore(body, MAX_LENGTH);

        /// <summary>Mask credentials for DB persistence without the short tracker display truncate.</summary>
        public static string? SanitiseForPersist(string? body) => SanitiseCore(body, MAX_PERSIST_LENGTH);

        private static string? SanitiseCore(string? body, int maxLength)
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            int preLimit = Math.Max(maxLength * 4, maxLength);
            string text = body.Length > preLimit ? body[..preLimit] : body;
            try
            {
                text = JsonSecret.Replace(text, "$1\"" + MASK + "\"");
                text = FormSecret.Replace(text, "$1" + MASK);
                text = XmlSecret.Replace(text, "$1" + MASK + "$3");
            }
            catch (RegexMatchTimeoutException)
            {
                return "[payload not shown: it could not be checked for credentials]";
            }

            return text.Length > maxLength ? text[..maxLength] : text;
        }

        /// <summary>
        /// Pulls the ERP response JSON/XML out of an error like
        /// "The ERP returned HTTP 400. {\"error\":...}" when ErpResponsePayload was not stored.
        /// </summary>
        public static string? HttpBodyFromError(string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return null;
            }

            string text = errorMessage.Trim();
            int json = text.IndexOf('{');
            int xml = text.IndexOf('<');
            int start = json < 0 ? xml : xml < 0 ? json : Math.Min(json, xml);
            if (start < 0)
            {
                return null;
            }

            return SanitiseForPersist(text[start..]);
        }
    }
}
