using System.Text.RegularExpressions;
using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;

namespace CellScope.Infrastructure.Services;

public class PhoneNumberIntelligenceService : IPhoneNumberIntelligenceService
{
    private readonly IDemoDataService? _demoService;

    public PhoneNumberIntelligenceService(IDemoDataService? demoService = null)
    {
        _demoService = demoService;
    }

    public Task<PhoneNumberProfileDto> AnalyzePhoneNumberAsync(string rawNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
        {
            return Task.FromResult(new PhoneNumberProfileDto
            {
                InputNumber = rawNumber ?? string.Empty,
                IsValid = false,
                RiskLevel = "High",
                RiskScore = 95,
                RiskFactors = new List<string> { "Empty or null phone number string" }
            });
        }

        string trimmed = rawNumber.Trim();
        string digitsOnly = Regex.Replace(trimmed, @"[^\d+]", "");
        if (string.IsNullOrWhiteSpace(digitsOnly) || digitsOnly == "+")
        {
            return Task.FromResult(new PhoneNumberProfileDto
            {
                InputNumber = rawNumber,
                IsValid = false,
                LineType = "Unknown / Invalid",
                RiskLevel = "High",
                RiskScore = 95,
                RiskFactors = new List<string> { "No numeric digits found in input string" }
            });
        }

        // Global Normalizations for seamless input handling
        if (digitsOnly.StartsWith("00"))
        {
            digitsOnly = "+" + digitsOnly[2..];
        }
        else if (digitsOnly.StartsWith("91") && digitsOnly.Length == 12 && digitsOnly[2] >= '6' && digitsOnly[2] <= '9')
        {
            digitsOnly = "+" + digitsOnly;
        }
        else if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11 && digitsOnly[1] >= '6' && digitsOnly[1] <= '9')
        {
            digitsOnly = "+91" + digitsOnly[1..];
        }
        else if (digitsOnly.Length == 10 && digitsOnly[0] >= '6' && digitsOnly[0] <= '9' && !digitsOnly.StartsWith("+"))
        {
            digitsOnly = "+91" + digitsOnly;
        }
        else if (digitsOnly.StartsWith("1") && digitsOnly.Length == 11 && !digitsOnly.StartsWith("+"))
        {
            digitsOnly = "+" + digitsOnly;
        }
        else if (digitsOnly.StartsWith("44") && digitsOnly.Length == 12 && !digitsOnly.StartsWith("+"))
        {
            digitsOnly = "+" + digitsOnly;
        }

        var profile = new PhoneNumberProfileDto
        {
            InputNumber = trimmed,
            AnalyzedAt = DateTimeOffset.UtcNow
        };

        // Determine Country Dial Code & Base Routing
        if (digitsOnly.StartsWith("+91") || (digitsOnly.Length == 10 && (digitsOnly[0] >= '6' && digitsOnly[0] <= '9')) || (digitsOnly.StartsWith("0") && digitsOnly.Length == 11 && digitsOnly[1] >= '6') || (digitsOnly.Length is >= 4 and <= 9 && digitsOnly[0] >= '6' && digitsOnly[0] <= '9'))
        {
            AnalyzeIndiaNumber(digitsOnly, profile);
        }
        else if (digitsOnly.StartsWith("+1") || (digitsOnly.Length == 10 && !digitsOnly.StartsWith("+")))
        {
            AnalyzeNorthAmericaNumber(digitsOnly, profile);
        }
        else if (digitsOnly.StartsWith("+44") || (digitsOnly.StartsWith("07") && digitsOnly.Length == 11))
        {
            AnalyzeUnitedKingdomNumber(digitsOnly, profile);
        }
        else
        {
            AnalyzeGlobalInternationalNumber(digitsOnly, profile);
        }

        // Check live macro network attachment
        CheckNetworkAttachment(profile);

        // Generate Consensual Field Survey Link
        string cleanNum = Regex.Replace(profile.E164Number, @"[^\d]", "");
        profile.ConsensualTrackingUrl = $"/field-survey?dev={cleanNum}&session={Guid.NewGuid().ToString("N")[..8]}&consent=prompt";

        return Task.FromResult(profile);
    }

    private void CheckNetworkAttachment(PhoneNumberProfileDto profile)
    {
        if (_demoService == null) return;
        try
        {
            string cleanTarget = Regex.Replace(profile.E164Number, @"[^\d]", "");
            if (cleanTarget.Length < 10) return;
            string last10 = cleanTarget[^10..];

            var towers = _demoService.GetDemoTowers(18.5913, 73.7389);
            foreach (var tower in towers)
            {
                var devices = _demoService.GetDemoConnectedDevicesForTower(tower.CellId);
                var match = devices.FirstOrDefault(d => d.PhoneNumber != null && Regex.Replace(d.PhoneNumber, @"[^\d]", "").EndsWith(last10));
                if (match != null)
                {
                    profile.IsAttachedToNetwork = true;
                    profile.ServingTowerName = $"{tower.OperatorName} ({tower.Area})";
                    profile.ServingCellId = tower.CellId;
                    profile.ServingLatitude = tower.Latitude;
                    profile.ServingLongitude = tower.Longitude;
                    profile.ServingArea = $"{tower.Area}, {tower.City}";
                    profile.ServingTechnology = tower.RadioTechnology;
                    profile.ServingBand = match.Band;
                    profile.ServingSignalDbm = match.SignalStrengthDbm;
                    profile.MatchedDeviceName = $"{match.DeviceName} ({match.Model})";
                    break;
                }
            }
        }
        catch { }
    }

    private static void AnalyzeIndiaNumber(string rawDigits, PhoneNumberProfileDto profile)
    {
        profile.CountryName = "India";
        profile.CountryCode = "IN";
        profile.DialCode = "+91";
        profile.CountryFlag = "🇮🇳";
        profile.Timezone = "Asia/Kolkata (IST, UTC+5:30)";

        string pureDigits = rawDigits.Replace("+91", "").TrimStart('0');
        if (pureDigits.Length > 10)
        {
            pureDigits = pureDigits[^10..];
        }

        if (pureDigits.Length != 10)
        {
            if (pureDigits.Length is >= 4 and <= 9)
            {
                int p4Partial = pureDigits.Length >= 4 && int.TryParse(pureDigits[..4], out int p4Val) ? p4Val : 
                               (int.TryParse(pureDigits, out int pVal) ? pVal : 0);
                int p2Partial = pureDigits.Length >= 2 && int.TryParse(pureDigits[..2], out int p2Val) ? p2Val : 0;
                var (partialCircle, partialCarrier, partialMccMnc) = ResolveIndiaCircleAndCarrier(p4Partial, p2Partial, pureDigits);
                
                profile.E164Number = "+91 " + pureDigits + "...";
                profile.NationalNumber = pureDigits;
                profile.IsValid = true;
                profile.LineType = "Mobile Allocation Prefix (Series Search)";
                profile.TelecomCircle = partialCircle;
                profile.OriginalCarrier = partialCarrier;
                profile.MccMncHint = partialMccMnc;
                profile.RiskLevel = "Low";
                profile.RiskScore = 15;
                profile.RiskFactors.Add($"DoT Licensed Allocation Series Match ({pureDigits.Length} digits)");
                return;
            }

            profile.E164Number = "+91" + pureDigits;
            profile.NationalNumber = pureDigits;
            profile.IsValid = false;
            profile.RiskLevel = "High";
            profile.RiskScore = 80;
            profile.RiskFactors.Add($"Invalid Indian number length ({pureDigits.Length} digits instead of standard 10)");
            profile.TelecomCircle = "Unknown / Invalid Indian Number";
            profile.OriginalCarrier = "Unallocated Telecom Range";
            return;
        }

        profile.E164Number = $"+91 {pureDigits[..5]} {pureDigits[5..]}";
        profile.NationalNumber = $"0{pureDigits[..5]} {pureDigits[5..]}";
        profile.IsValid = true;

        int prefix4 = int.TryParse(pureDigits[..4], out int p4) ? p4 : 0;
        int prefix2 = int.TryParse(pureDigits[..2], out int p2) ? p2 : 0;
        char firstChar = pureDigits[0];

        // Check if Landline or Mobile
        if (firstChar >= '6' && firstChar <= '9')
        {
            profile.LineType = "Mobile (3GPP Cellular SIM)";
            profile.IsVoip = false;
        }
        else
        {
            profile.LineType = "Fixed Line / Landline";
            profile.IsVoip = false;
        }

        // Telecom Circle Mapping (TRAI/DoT Licensed Service Area)
        var (circle, carrierHint, mccMnc) = ResolveIndiaCircleAndCarrier(prefix4, prefix2, pureDigits);
        profile.TelecomCircle = circle;
        profile.OriginalCarrier = carrierHint;
        profile.MccMncHint = mccMnc;

        // Risk & Spoof Scoring
        EvaluateFraudAndSpoofRisk(profile, pureDigits);
    }

    private static (string Circle, string Carrier, string MccMnc) ResolveIndiaCircleAndCarrier(int prefix4, int prefix2, string pureDigits)
    {
        // 1. Precise Circle lookup based on classic 4-digit prefixes
        string circle;
        string mccMnc;

        if (prefix4 is >= 9820 and <= 9821 or 9819 or 9869 or 9870 or 9892 or 9769 or 9773 or 9833 or 9930 or 9967 or 9987)
        {
            circle = "Mumbai Metropolitan Region (MH)";
            mccMnc = "404-20";
        }
        else if (prefix4 is >= 9822 and <= 9823 or 9850 or 9860 or 9881 or 9890 or 9921 or 9922 or 9923 or 9960 or 9970 or 9975 or 9604 or 9762 or 9763 or 9764 or 9765)
        {
            circle = "Maharashtra & Goa (Pune, Nagpur, Nashik, Goa)";
            mccMnc = "404-45";
        }
        else if (prefix4 is >= 9810 and <= 9811 or 9818 or 9871 or 9873 or 9891 or 9910 or 9911 or 9953 or 9971 or 9999 or 9868)
        {
            circle = "Delhi NCR (Delhi, Gurugram, Faridabad, Noida)";
            mccMnc = "404-10";
        }
        else if (prefix4 is >= 9844 and <= 9845 or 9880 or 9886 or 9900 or 9901 or 9902 or 9945 or 9972 or 9980 or 9986)
        {
            circle = "Karnataka (Bengaluru, Mysuru, Hubballi)";
            mccMnc = "404-86";
        }
        else if (prefix4 is >= 9840 and <= 9841 or 9884 or 9940 or 9941 or 9962)
        {
            circle = "Chennai Metro (TN)";
            mccMnc = "404-84";
        }
        else if (prefix4 is >= 9842 and <= 9843 or 9894 or 9942 or 9943 or 9944 or 9952 or 9965 or 9976 or 9994)
        {
            circle = "Tamil Nadu (Coimbatore, Madurai, Tiruchirappalli)";
            mccMnc = "404-90";
        }
        else if (prefix4 is >= 9848 and <= 9849 or 9866 or 9885 or 9908 or 9912 or 9948 or 9949 or 9951 or 9959 or 9963 or 9966 or 9989)
        {
            circle = "Andhra Pradesh & Telangana (Hyderabad, Visakhapatnam)";
            mccMnc = "404-49";
        }
        else if (prefix4 is >= 9824 and <= 9825 or 9879 or 9898 or 9904 or 9909 or 9913 or 9924 or 9925 or 9974 or 9978 or 9979)
        {
            circle = "Gujarat (Ahmedabad, Surat, Vadodara, Rajkot)";
            mccMnc = "404-98";
        }
        else if (prefix4 is >= 9830 and <= 9831 or 9832 or 9836 or 9874 or 9903)
        {
            circle = "Kolkata Metro (WB)";
            mccMnc = "404-30";
        }
        else if (prefix4 is 9851 or 9883 or 9932 or 9933)
        {
            circle = "West Bengal (Siliguri, Asansol, Durgapur)";
            mccMnc = "404-80";
        }
        else if (prefix4 is >= 9846 and <= 9847 or 9895 or 9946 or 9947 or 9961 or 9995)
        {
            circle = "Kerala (Kochi, Thiruvananthapuram, Kozhikode)";
            mccMnc = "404-46";
        }
        else if (prefix4 is >= 9814 and <= 9815 or 9855 or 9872 or 9876 or 9878 or 9888 or 9914 or 9915 or 9988)
        {
            circle = "Punjab & Chandigarh (Ludhiana, Amritsar)";
            mccMnc = "404-14";
        }
        else if (prefix4 is >= 9812 and <= 9813 or 9896 or 9991 or 9992 or 9996)
        {
            circle = "Haryana (Rohtak, Panipat, Karnal)";
            mccMnc = "404-12";
        }
        else if (prefix4 is >= 9828 and <= 9829 or 9887 or 9928 or 9929 or 9950 or 9982 or 9983)
        {
            circle = "Rajasthan (Jaipur, Jodhpur, Udaipur, Kota)";
            mccMnc = "404-70";
        }
        else if (prefix4 is >= 9838 and <= 9839 or 9889 or 9918 or 9919 or 9935 or 9936 or 9956)
        {
            circle = "Uttar Pradesh (East) (Lucknow, Varanasi, Prayagraj)";
            mccMnc = "404-56";
        }
        else if (prefix4 is 9837 or 9897 or 9917 or 9927 or 9997)
        {
            circle = "Uttar Pradesh (West) & Uttarakhand (Agra, Meerut, Dehradun)";
            mccMnc = "404-58";
        }
        else if (prefix4 is 9835 or 9852 or 9905 or 9931 or 9934 or 9939 or 9955)
        {
            circle = "Bihar & Jharkhand (Patna, Ranchi, Jamshedpur)";
            mccMnc = "404-44";
        }
        else if (prefix4 is >= 9826 and <= 9827 or 9893 or 9907 or 9926 or 9977 or 9981 or 9993)
        {
            circle = "Madhya Pradesh & Chhattisgarh (Bhopal, Indore, Raipur)";
            mccMnc = "404-75";
        }
        else
        {
            // Regional grouping for high-number newer series
            circle = pureDigits[0] switch
            {
                '9' => "Western / Northern India Telecom Grid",
                '8' => "Southern / Central India Telecom Grid",
                '7' => "National Unified 4G/5G Access (Pan-India)",
                '6' => "National Reliance Jio / Digital LTE Grid",
                _ => "Pan-India Telecom Circle"
            };
            mccMnc = "404-45";
        }

        // 2. Carrier identification
        string carrier;
        if (pureDigits.StartsWith("6") || pureDigits.StartsWith("70") || pureDigits.StartsWith("79") || pureDigits.StartsWith("800") || pureDigits.StartsWith("808") || pureDigits.StartsWith("89") || pureDigits.StartsWith("829") || pureDigits.StartsWith("879"))
        {
            carrier = "Reliance Jio Infocomm (5G NR / VoLTE)";
            mccMnc = "405-861";
        }
        else if (pureDigits.StartsWith("98") || pureDigits.StartsWith("99") || pureDigits.StartsWith("96") || pureDigits.StartsWith("97") || pureDigits.StartsWith("81") || pureDigits.StartsWith("84") || pureDigits.StartsWith("76"))
        {
            carrier = "Bharti Airtel Limited (5G Plus / 4G LTE)";
            mccMnc = "404-45";
        }
        else if (pureDigits.StartsWith("88") || pureDigits.StartsWith("87") || pureDigits.StartsWith("86") || pureDigits.StartsWith("77") || pureDigits.StartsWith("78") || pureDigits.StartsWith("91") || pureDigits.StartsWith("90"))
        {
            carrier = "Vodafone Idea (Vi GIGAnet 4G)";
            mccMnc = "404-20";
        }
        else if (pureDigits.StartsWith("94") || pureDigits.StartsWith("93") || pureDigits.StartsWith("75"))
        {
            carrier = "Bharat Sanchar Nigam Limited (BSNL Mobile)";
            mccMnc = "404-34";
        }
        else
        {
            carrier = "Major Telecom Provider (Airtel / Jio / Vi)";
        }

        return (circle, carrier, mccMnc);
    }

    private static void AnalyzeNorthAmericaNumber(string rawDigits, PhoneNumberProfileDto profile)
    {
        profile.CountryName = "United States / Canada";
        profile.CountryCode = "US";
        profile.DialCode = "+1";
        profile.CountryFlag = "🇺🇸";

        string pure = rawDigits.Replace("+1", "").TrimStart('1', '0');
        if (pure.Length > 10) pure = pure[^10..];

        if (pure.Length != 10)
        {
            profile.E164Number = "+1" + pure;
            profile.NationalNumber = pure;
            profile.IsValid = false;
            profile.RiskLevel = "High";
            profile.RiskScore = 75;
            profile.RiskFactors.Add($"Invalid NANP number length ({pure.Length} digits instead of 10)");
            profile.TelecomCircle = "NANP Invalid Area";
            profile.OriginalCarrier = "Unallocated Carrier";
            return;
        }

        string areaCode = pure[..3];
        profile.E164Number = $"+1 ({areaCode}) {pure.Substring(3, 3)}-{pure.Substring(6, 4)}";
        profile.NationalNumber = $"({areaCode}) {pure.Substring(3, 3)}-{pure.Substring(6, 4)}";
        profile.IsValid = true;

        // Toll-Free detection
        if (areaCode is "800" or "888" or "877" or "866" or "855" or "844" or "833")
        {
            profile.LineType = "Toll-Free Enterprise Line";
            profile.TelecomCircle = "North America Toll-Free";
            profile.OriginalCarrier = "Inbound Toll-Free Routing";
            profile.IsVoip = false;
            profile.RiskScore = 15;
            profile.RiskLevel = "Low";
            profile.Timezone = "Continental US / Canada";
            return;
        }

        // NANP Area Code Mapping
        var (region, state, tz) = areaCode switch
        {
            "415" or "628" => ("San Francisco & Marin County", "California", "America/Los_Angeles (PST, UTC-8)"),
            "408" or "669" => ("San Jose / Silicon Valley", "California", "America/Los_Angeles (PST, UTC-8)"),
            "510" or "341" => ("Oakland & East Bay", "California", "America/Los_Angeles (PST, UTC-8)"),
            "650" => ("San Mateo / Palo Alto / Peninsula", "California", "America/Los_Angeles (PST, UTC-8)"),
            "213" or "310" or "323" or "424" => ("Los Angeles Metropolitan", "California", "America/Los_Angeles (PST, UTC-8)"),
            "212" or "646" or "917" or "332" => ("New York City (Manhattan)", "New York", "America/New_York (EST, UTC-5)"),
            "718" or "347" or "929" => ("New York City (Outer Boroughs)", "New York", "America/New_York (EST, UTC-5)"),
            "312" or "773" or "872" => ("Chicago Metropolitan Area", "Illinois", "America/Chicago (CST, UTC-6)"),
            "206" => ("Seattle Metropolitan Area", "Washington", "America/Los_Angeles (PST, UTC-8)"),
            "512" or "737" => ("Austin Metropolitan Area", "Texas", "America/Chicago (CST, UTC-6)"),
            "214" or "972" or "469" => ("Dallas-Fort Worth", "Texas", "America/Chicago (CST, UTC-6)"),
            "305" or "786" => ("Miami & Florida Keys", "Florida", "America/New_York (EST, UTC-5)"),
            "617" or "857" => ("Boston Metropolitan", "Massachusetts", "America/New_York (EST, UTC-5)"),
            "202" => ("Washington D.C. Capital District", "District of Columbia", "America/New_York (EST, UTC-5)"),
            _ => ($"Area Code {areaCode} Regional Rate Center", "United States", "US Continental")
        };

        profile.TelecomCircle = $"{region}, {state}";
        profile.Timezone = tz;
        profile.LineType = "Mobile / Wireless (Cellular SIM)";
        profile.OriginalCarrier = "Major US Carrier (Verizon / AT&T / T-Mobile)";
        profile.MccMncHint = "310-410";

        EvaluateFraudAndSpoofRisk(profile, pure);
    }

    private static void AnalyzeUnitedKingdomNumber(string rawDigits, PhoneNumberProfileDto profile)
    {
        profile.CountryName = "United Kingdom";
        profile.CountryCode = "GB";
        profile.DialCode = "+44";
        profile.CountryFlag = "🇬🇧";
        profile.Timezone = "Europe/London (GMT, UTC+0)";

        string pure = rawDigits.Replace("+44", "").TrimStart('0');
        profile.E164Number = "+44 " + pure;
        profile.NationalNumber = "0" + pure;
        profile.IsValid = pure.Length >= 9 && pure.Length <= 11;

        if (pure.StartsWith("7"))
        {
            profile.LineType = "Mobile (3GPP Cellular SIM)";
            profile.TelecomCircle = "United Kingdom Nationwide Cellular";
            profile.OriginalCarrier = "Major UK Carrier (EE / O2 / Vodafone / Three)";
            profile.MccMncHint = "234-30";
        }
        else if (pure.StartsWith("20"))
        {
            profile.LineType = "Fixed Landline";
            profile.TelecomCircle = "Greater London Telecommunications Area";
            profile.OriginalCarrier = "British Telecom (BT) Fixed Network";
        }
        else
        {
            profile.LineType = "Geographic Landline";
            profile.TelecomCircle = "UK Regional Telecom Center";
            profile.OriginalCarrier = "BT / Virgin Media";
        }

        EvaluateFraudAndSpoofRisk(profile, pure);
    }

    private static void AnalyzeGlobalInternationalNumber(string rawDigits, PhoneNumberProfileDto profile)
    {
        profile.LineType = "International Telecommunications Line";
        profile.E164Number = rawDigits.StartsWith("+") ? rawDigits : "+" + rawDigits;
        profile.NationalNumber = rawDigits;

        if (rawDigits.StartsWith("+33"))
        {
            profile.CountryName = "France";
            profile.CountryCode = "FR";
            profile.DialCode = "+33";
            profile.CountryFlag = "🇫🇷";
            profile.TelecomCircle = "France Telecom Grid";
            profile.OriginalCarrier = "Orange / SFR / Bouygues";
            profile.Timezone = "Europe/Paris (CET, UTC+1)";
        }
        else if (rawDigits.StartsWith("+49"))
        {
            profile.CountryName = "Germany";
            profile.CountryCode = "DE";
            profile.DialCode = "+49";
            profile.CountryFlag = "🇩🇪";
            profile.TelecomCircle = "Germany Bundesnetzagentur Grid";
            profile.OriginalCarrier = "Deutsche Telekom / Vodafone Germany";
            profile.Timezone = "Europe/Berlin (CET, UTC+1)";
        }
        else if (rawDigits.StartsWith("+81"))
        {
            profile.CountryName = "Japan";
            profile.CountryCode = "JP";
            profile.DialCode = "+81";
            profile.CountryFlag = "🇯🇵";
            profile.TelecomCircle = "Japan MIC Telecom Region";
            profile.OriginalCarrier = "NTT Docomo / SoftBank / KDDI";
            profile.Timezone = "Asia/Tokyo (JST, UTC+9)";
        }
        else if (rawDigits.StartsWith("+971"))
        {
            profile.CountryName = "United Arab Emirates";
            profile.CountryCode = "AE";
            profile.DialCode = "+971";
            profile.CountryFlag = "🇦🇪";
            profile.TelecomCircle = "Dubai & Abu Dhabi Telecommunications Area";
            profile.OriginalCarrier = "e& (Etisalat) / du";
            profile.Timezone = "Asia/Dubai (GST, UTC+4)";
        }
        else if (rawDigits.StartsWith("+65"))
        {
            profile.CountryName = "Singapore";
            profile.CountryCode = "SG";
            profile.DialCode = "+65";
            profile.CountryFlag = "🇸🇬";
            profile.TelecomCircle = "Singapore IMDA City-State Telecom";
            profile.OriginalCarrier = "Singtel / StarHub / M1";
            profile.Timezone = "Asia/Singapore (SGT, UTC+8)";
        }
        else if (rawDigits.StartsWith("+61"))
        {
            profile.CountryName = "Australia";
            profile.CountryCode = "AU";
            profile.DialCode = "+61";
            profile.CountryFlag = "🇦🇺";
            profile.TelecomCircle = "Australia ACMA National Grid";
            profile.OriginalCarrier = "Telstra / Optus";
            profile.Timezone = "Australia/Sydney (AEST, UTC+10)";
        }
        else
        {
            profile.CountryName = "International Destination";
            profile.CountryCode = "INT";
            profile.CountryFlag = "🌐";
            profile.TelecomCircle = "Global International Telecom Routing";
            profile.OriginalCarrier = "International Carrier Gateway";
            profile.Timezone = "UTC";
        }

        profile.IsValid = rawDigits.Length >= 8;
        EvaluateFraudAndSpoofRisk(profile, rawDigits);
    }

    private static void EvaluateFraudAndSpoofRisk(PhoneNumberProfileDto profile, string digits)
    {
        profile.RiskFactors.Clear();
        int score = 10; // baseline low risk for valid numbers

        // Check for repetitive/trivial dummy digits
        if (Regex.IsMatch(digits, @"^(\d)\1{6,}$"))
        {
            score += 70;
            profile.RiskFactors.Add("Highly repetitive sequential digits (likely dummy or spoofed number)");
        }
        if (digits.Contains("1234567") || digits.Contains("9876543"))
        {
            score += 50;
            profile.RiskFactors.Add("Sequential sequential test pattern detected");
        }

        // Virtual VoIP check
        if (profile.LineType.Contains("VoIP") || profile.IsVoip)
        {
            score += 35;
            profile.RiskFactors.Add("Virtual VoIP Cloud number allocation (higher spoofing and robocall incidence)");
        }

        if (profile.RiskFactors.Count == 0)
        {
            profile.RiskFactors.Add("Valid standard telecom numbering format");
            profile.RiskFactors.Add("Legitimate licensed carrier allocation");
            profile.RiskFactors.Add("No automated robocall spoof signatures detected");
        }

        profile.RiskScore = Math.Clamp(score, 5, 95);
        profile.RiskLevel = profile.RiskScore switch
        {
            >= 65 => "High",
            >= 35 => "Medium",
            _ => "Low"
        };
    }
}
