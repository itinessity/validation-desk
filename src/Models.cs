using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Concurrent;

record Check(string? Country, string? Vat, string? Iban);
record Evidence(string Status, string Detail, string Source, DateTimeOffset CheckedAt, bool Cached = false);
record Audit(DateTimeOffset CreatedAt, string Country, string VatMasked, string IbanMasked, Evidence Vat, Evidence Iban);
