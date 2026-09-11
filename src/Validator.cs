using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Concurrent;

sealed class Validator(HttpClient http, bool fixture)
{
    static readonly HashSet<string> Countries = "AT BE BG CY CZ DE DK EE EL ES FI FR HR HU IE IT LT LU LV MT NL PL PT RO SE SI SK XI".Split(' ').ToHashSet();
    readonly ConcurrentDictionary<string, Evidence> cache = new();
    readonly SemaphoreSlim gate = new(1);
    int failures;
    DateTimeOffset openUntil;
    static string Clean(string? s) => Regex.Replace((s ?? "").ToUpperInvariant(), "[ .-]", "");
    static string Mask(string s) => s.Length <= 4 ? new string ('•', s.Length) : new string ('•', s.Length - 4) + s[^4..];
    public async Task<Audit> Check(Check input)
    {
        if ((input.Vat?.Length ?? 0) > 40 || (input.Iban?.Length ?? 0) > 80)
            throw new ArgumentException("Identifier is too long.");
        var country = Clean(input.Country);
        if (country == "GR")
            country = "EL";
        var vat = Clean(input.Vat);
        if (vat.StartsWith(country) && country.Length == 2)
            vat = vat[2..];
        var iban = Clean(input.Iban);
        var now = DateTimeOffset.UtcNow;
        Evidence evidence;
        if (vat.Length == 0)
            evidence = new("not_checked", "No VAT number provided.", "Local input validation", now);
        else if (!Countries.Contains(country) || !Regex.IsMatch(vat, "^[A-Z0-9]{2,12}$") || (country == "DE" && !Regex.IsMatch(vat, "^[0-9]{9}$")))
            evidence = new("invalid_input", "Unsupported VIES country or malformed VAT identifier.", "Local input validation", now);
        else
            evidence = await Vat(country, vat);
        var ibanEvidence = new Evidence(iban.Length == 0 ? "not_checked" : Iban.Valid(iban) ? "valid" : "invalid_input", iban.Length == 0 ? "No IBAN provided." : Iban.Valid(iban) ? "Country length and MOD-97 checksum pass. Account existence and ownership are not checked." : "Country, length, characters or MOD-97 checksum failed.", "Local ISO 13616 structure / MOD-97", now);
        return new(now, country, Mask(vat), Mask(iban), evidence, ibanEvidence);
    }

    async Task<Evidence> Vat(string country, string vat)
    {
        var key = country + vat;
        await gate.WaitAsync();
        try
        {
            if (cache.TryGetValue(key, out var hit) && hit.CheckedAt > DateTimeOffset.UtcNow.AddMinutes(-15))
                return hit with
                {
                    Cached = true
                };
            if (openUntil > DateTimeOffset.UtcNow)
                return new("provider_unavailable", "Circuit open after repeated failures. Retry after 30 seconds.", "VIES", DateTimeOffset.UtcNow);
            Evidence result;
            try
            {
                string xml;
                if (fixture)
                    xml = vat == "999999999" ? "<Envelope><Fault><faultstring>MS_UNAVAILABLE</faultstring></Fault></Envelope>" : $"<Envelope><checkVatResponse><valid>{(vat == "123456789" ? "true" : "false")}</valid></checkVatResponse></Envelope>";
                else
                {
                    XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/", v = "urn:ec.europa.eu:taxud:vies:services:checkVat:types";
                    var body = new XDocument(new XElement(soap + "Envelope", new XElement(soap + "Body", new XElement(v + "checkVat", new XElement(v + "countryCode", country), new XElement(v + "vatNumber", vat)))));
                    using var req = new HttpRequestMessage(HttpMethod.Post, "https://ec.europa.eu/taxation_customs/vies/services/checkVatService")
                    {
                        Content = new StringContent(body.ToString(), Encoding.UTF8, "text/xml")
                    };
                    req.Headers.Add("SOAPAction", "\"\"");
                    using var response = await http.SendAsync(req);
                    xml = await response.Content.ReadAsStringAsync();
                }

                result = Parse(xml);
            }
            catch (Exception ex)when (ex is HttpRequestException or TaskCanceledException or XmlException or InvalidOperationException)
            {
                result = new("provider_unavailable", "The service did not return a usable answer. This does not mean the business is invalid.", "VIES", DateTimeOffset.UtcNow);
            }

            if (result.Status == "provider_unavailable")
            {
                if (++failures >= 3)
                {
                    openUntil = DateTimeOffset.UtcNow.AddSeconds(30);
                    failures = 0;
                }
            }
            else
            {
                failures = 0;
                if (result.Status is "valid" or "invalid")
                {
                    if (cache.Count >= 500)
                        cache.Clear();
                    cache[key] = result;
                }
            }

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    public static Evidence Parse(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100_000 });
        var doc = XDocument.Load(reader);
        var fault = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "faultstring")?.Value;
        if (fault is not null)
            return new(fault == "INVALID_INPUT" ? "invalid_input" : "provider_unavailable", fault == "INVALID_INPUT" ? "VIES rejected the identifier format." : "A VIES national service is unavailable. No validity conclusion can be made.", "European Commission VIES", DateTimeOffset.UtcNow);
        var valid = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "valid")?.Value;
        if (valid is not ("true" or "false" or "1" or "0"))
            throw new XmlException("Missing valid result.");
        return new(valid is "true" or "1" ? "valid" : "invalid", valid is "true" or "1" ? "VIES confirms VAT registration at the time of this check." : "VIES did not confirm VAT registration at the time of this check.", "European Commission VIES", DateTimeOffset.UtcNow);
    }
}
