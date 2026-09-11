using System.Xml;

static void Check(bool value, string message)
{
    if (!value)
        throw new Exception(message);
}

Check(Iban.Valid("DE89370400440532013000"), "valid German IBAN");
Check(Iban.Valid("GB82WEST12345698765432"), "valid British IBAN");
Check(!Iban.Valid("DE89370400440532013001"), "bad checksum");
Check(!Iban.Valid("ZZ89370400440532013000"), "unsupported country");
Check(!Iban.Valid("DE89"), "short IBAN");
Check(Validator.Parse("<r><valid>false</valid></r>").Status == "invalid", "negative provider result");
Check(Validator.Parse("<r><faultstring>MS_UNAVAILABLE</faultstring></r>").Status == "provider_unavailable", "outage is not invalid");
Check(Validator.Parse("<r><faultstring>INVALID_INPUT</faultstring></r>").Status == "invalid_input", "provider input error");
try
{
    Validator.Parse("<!DOCTYPE r [<!ENTITY x SYSTEM 'file:///etc/passwd'>]><r><valid>&x;</valid></r>");
    throw new Exception("XXE allowed");
}
catch (XmlException)
{
}

try
{
    Validator.Parse("<r/>");
    throw new Exception("missing result accepted");
}
catch (XmlException)
{
}

Console.WriteLine("10 identifier and SOAP checks passed.");
