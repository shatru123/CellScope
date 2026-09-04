using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Infrastructure.Security;

namespace CellScope.Infrastructure.Services;

public class SecurityAnalysisService : ISecurityAnalysisService
{
    public AesTraceResultDto TraceAes128(byte[] key, byte[] plaintext)
    {
        return Aes128Tracer.Trace(key, plaintext);
    }

    public MilenageResultDto ComputeMilenage(byte[] key, byte[] opOrOpc, bool isOpc, byte[] rand, byte[] sqn, byte[] amf)
    {
        return MilenageService.Calculate(key, opOrOpc, isOpc, rand, sqn, amf);
    }

    public FiveGAkaResultDto Compute5GAka(
        byte[] key,
        byte[] opOrOpc,
        bool isOpc,
        byte[] rand,
        byte[] sqn,
        byte[] amf,
        string servingNetworkName,
        string supi)
    {
        return FiveGAkaService.Calculate(key, opOrOpc, isOpc, rand, sqn, amf, servingNetworkName, supi);
    }

    public KdfCalculationDto ComputeKdf(byte[] key, byte fc, List<(byte[] Param, string Label)> parameters)
    {
        return FiveGAkaService.ComputeKdf(key, fc, parameters);
    }

    public IReadOnlyList<SecurityTestVectorDto> GetPredefinedTestVectors()
    {
        return StandardsTestVectors.GetTestVectors();
    }

    public IReadOnlyList<SecurityTestVectorDto> RunAllTestVectorVerifications()
    {
        var vectors = StandardsTestVectors.GetTestVectors();

        foreach (var v in vectors)
        {
            if (v.Category == "AES-128")
            {
                byte[] pt = Convert.FromHexString(v.Inputs["Plaintext"]);
                byte[] k = Convert.FromHexString(v.Inputs["Key"]);
                var trace = Aes128Tracer.Trace(k, pt);

                foreach (var cmp in v.Comparisons)
                {
                    if (cmp.FieldName == "Ciphertext")
                    {
                        cmp.CalculatedHex = trace.CiphertextHex;
                    }
                }
            }
            else if (v.Category == "MILENAGE")
            {
                byte[] k = Convert.FromHexString(v.Inputs["K"]);
                bool isOpc = v.Inputs.ContainsKey("OPc");
                byte[] opOrOpc = Convert.FromHexString(isOpc ? v.Inputs["OPc"] : v.Inputs["OP"]);
                byte[] rand = Convert.FromHexString(v.Inputs["RAND"]);
                byte[] sqn = Convert.FromHexString(v.Inputs["SQN"]);
                byte[] amf = Convert.FromHexString(v.Inputs["AMF"]);

                var mil = MilenageService.Calculate(k, opOrOpc, isOpc, rand, sqn, amf);

                foreach (var cmp in v.Comparisons)
                {
                    if (cmp.FieldName == "OPc") cmp.CalculatedHex = mil.OpcHex;
                    else if (cmp.FieldName.Contains("MAC-A")) cmp.CalculatedHex = mil.MacAHex;
                    else if (cmp.FieldName.Contains("MAC-S")) cmp.CalculatedHex = mil.MacSHex;
                    else if (cmp.FieldName.Contains("RES")) cmp.CalculatedHex = mil.ResHex;
                    else if (cmp.FieldName.Contains("CK")) cmp.CalculatedHex = mil.CkHex;
                    else if (cmp.FieldName.Contains("IK")) cmp.CalculatedHex = mil.IkHex;
                    else if (cmp.FieldName.Contains("AK") && !cmp.FieldName.Contains("AK*")) cmp.CalculatedHex = mil.AkHex;
                    else if (cmp.FieldName.Contains("AUTN")) cmp.CalculatedHex = mil.AutnHex;
                }
            }
            else if (v.Category == "5G-AKA")
            {
                byte[] k = Convert.FromHexString(v.Inputs["K"]);
                bool isOpc = v.Inputs.ContainsKey("OPc");
                byte[] opOrOpc = Convert.FromHexString(isOpc ? v.Inputs["OPc"] : v.Inputs["OP"]);
                byte[] rand = Convert.FromHexString(v.Inputs["RAND"]);
                byte[] sqn = Convert.FromHexString(v.Inputs["SQN"]);
                byte[] amf = Convert.FromHexString(v.Inputs["AMF"]);
                string snName = v.Inputs["ServingNetwork"];
                string supi = v.Inputs["SUPI"];

                var aka = FiveGAkaService.Calculate(k, opOrOpc, isOpc, rand, sqn, amf, snName, supi);

                foreach (var cmp in v.Comparisons)
                {
                    if (cmp.FieldName.Contains("AUTN")) cmp.CalculatedHex = aka.AutnHex;
                    else if (cmp.FieldName.Contains("XRES*")) cmp.CalculatedHex = aka.XresStarHex;
                    else if (cmp.FieldName.Contains("K_AUSF")) cmp.CalculatedHex = aka.KausfHex;
                    else if (cmp.FieldName.Contains("K_SEAF")) cmp.CalculatedHex = aka.KseafHex;
                    else if (cmp.FieldName.Contains("K_AMF")) cmp.CalculatedHex = aka.KamfHex;
                }
            }
        }

        return vectors;
    }

    public IReadOnlyList<AuthFlowMessageDto> GetAuthenticationFlowMessages()
    {
        return new List<AuthFlowMessageDto>
        {
            new()
            {
                StepNumber = 1,
                Sender = "UE (Mobile Device)",
                Receiver = "AMF / SEAF (Serving Network)",
                MessageName = "Registration Request (SUCI / 5G-GUTI)",
                Summary = "The UE initiates 5G registration by transmitting its Subscription Concealed Identifier (SUCI) encrypted with the Home Network Public Key (ECIES curve profile).",
                KeyParameters = new List<string> { "SUCI (Encrypted MSIN)", "Serving Network Name (SNN)", "5GMM Capability", "Last Visited TAI" },
                SecuritySignificance = "Prevents IMSI-catcher tracking over-the-air using asymmetric curve25519/secp256r1 concealment.",
                SpecificationReference = "3GPP TS 33.501 Clause 6.1.2 & TS 24.501 Clause 8.2.6"
            },
            new()
            {
                StepNumber = 2,
                Sender = "AMF / SEAF (Serving Network)",
                Receiver = "AUSF (Home Network Core)",
                MessageName = "Nausf_UEAuthentication_Authenticate Request",
                Summary = "SEAF forwards the authentication initiation request with the SUCI and its verified Serving Network Name (e.g. \"5G:mnc410.mcc310.3gppnetwork.org\").",
                KeyParameters = new List<string> { "SUCI", "Serving Network Name", "Trace Reference" },
                SecuritySignificance = "Binds the authentication request cryptographically to the serving operator identity.",
                SpecificationReference = "3GPP TS 33.501 Clause 6.1.3.1"
            },
            new()
            {
                StepNumber = 3,
                Sender = "AUSF (Home Network Core)",
                Receiver = "UDM / ARPF (Subscriber Database)",
                MessageName = "Nudm_UEAuthentication_Get Request",
                Summary = "AUSF requests an Authentication Vector from the subscriber's home UDM/ARPF for the specified SUCI.",
                KeyParameters = new List<string> { "SUCI", "Serving Network Name" },
                SecuritySignificance = "ARPF decrypts SUCI to obtain the true SUPI (IMSI) using the home operator private key.",
                SpecificationReference = "3GPP TS 33.501 Clause 6.1.3.2"
            },
            new()
            {
                StepNumber = 4,
                Sender = "UDM / ARPF",
                Receiver = "AUSF",
                MessageName = "Nudm_UEAuthentication_Get Response (5G HE AV + SUPI)",
                Summary = "ARPF executes MILENAGE with subscriber secret K, generates RAND, AUTN, derives XRES* and K_AUSF, and returns the 5G Home Environment Authentication Vector.",
                KeyParameters = new List<string> { "RAND (128-bit)", "AUTN (128-bit)", "XRES* (128-bit)", "K_AUSF (256-bit)", "SUPI" },
                SecuritySignificance = "The master secret K never leaves the secure ARPF/HSM hardware boundary.",
                SpecificationReference = "3GPP TS 33.501 Clause 6.1.3.2 & Annex A.2"
            },
            new()
            {
                StepNumber = 5,
                Sender = "AUSF (Home Network Core)",
                Receiver = "AMF / SEAF (Serving Network)",
                MessageName = "Nausf_UEAuthentication_Authenticate Response (5G SEAF AV)",
                Summary = "AUSF derives K_SEAF from K_AUSF and computes HXRES* = SHA-256(RAND || XRES*). It stores XRES* and sends (RAND, AUTN, HXRES*) to SEAF.",
                KeyParameters = new List<string> { "RAND (128-bit)", "AUTN (128-bit)", "HXRES* (128-bit hash)", "K_SEAF (256-bit derived anchor)" },
                SecuritySignificance = "SEAF receives only the hash HXRES*, ensuring the serving network cannot forge a response without UE participation.",
                SpecificationReference = "3GPP TS 33.501 Clause 6.1.3.2 & Annex A.6"
            },
            new()
            {
                StepNumber = 6,
                Sender = "AMF / SEAF",
                Receiver = "UE (Mobile Device / USIM)",
                MessageName = "NAS Authentication Request",
                Summary = "AMF sends the challenge RAND, network authentication token AUTN, and ngKSI key set identifier to the UE.",
                KeyParameters = new List<string> { "RAND (128-bit challenge)", "AUTN (SQN ^ AK || AMF || MAC-A)", "ngKSI" },
                SecuritySignificance = "USIM verifies MAC-A and checks that SQN is within the freshness window (preventing replay attacks).",
                SpecificationReference = "3GPP TS 24.501 Clause 8.2.1 & TS 33.501 Clause 6.1.3.2"
            },
            new()
            {
                StepNumber = 7,
                Sender = "UE (USIM / ME)",
                Receiver = "AMF / SEAF",
                MessageName = "NAS Authentication Response (RES*)",
                Summary = "USIM runs MILENAGE to calculate RES, CK, IK, AK, and the Mobile Equipment derives RES*, K_AUSF, K_SEAF, and K_AMF, transmitting RES* back to SEAF.",
                KeyParameters = new List<string> { "RES* (128-bit response)" },
                SecuritySignificance = "Proves possession of subscriber credential K without exposing K over the radio link.",
                SpecificationReference = "3GPP TS 33.501 Annex A.4 & TS 24.501 Clause 8.2.2"
            },
            new()
            {
                StepNumber = 8,
                Sender = "AMF / SEAF",
                Receiver = "AUSF",
                MessageName = "Nausf_UEAuthentication_Authenticate Request (RES* Confirmation)",
                Summary = "SEAF calculates HRES* = SHA-256(RAND || RES*). If HRES* matches HXRES*, it forwards RES* to AUSF for final verification against XRES*.",
                KeyParameters = new List<string> { "RES*" },
                SecuritySignificance = "Two-tier verification: First at SEAF (fast filter) and final at AUSF (authoritative).",
                SpecificationReference = "3GPP TS 33.501 Clause 6.1.3.2"
            },
            new()
            {
                StepNumber = 9,
                Sender = "AUSF",
                Receiver = "AMF / SEAF",
                MessageName = "Nausf_UEAuthentication_Authenticate Response (Auth Result: SUCCESS)",
                Summary = "AUSF confirms RES* == XRES*, records successful authentication for the SUPI, and confirms K_SEAF to the serving network.",
                KeyParameters = new List<string> { "AuthResult: SUCCESS", "SUPI", "K_SEAF" },
                SecuritySignificance = "Confirms mutual authentication between the subscriber and home network.",
                SpecificationReference = "3GPP TS 33.501 Clause 6.1.3.2"
            },
            new()
            {
                StepNumber = 10,
                Sender = "AMF",
                Receiver = "UE",
                MessageName = "NAS Security Mode Command",
                Summary = "AMF selects ciphering (e.g. 128-NEA2 AES) and integrity algorithms (e.g. 128-NIA2 AES), derives K_NASenc and K_NASint from K_AMF, and sends the command integrity-protected.",
                KeyParameters = new List<string> { "Selected NAS Encryption Alg (5G-EA2)", "Selected NAS Integrity Alg (5G-IA2)", "ngKSI", "Replayed UE Security Capabilities", "MAC-I" },
                SecuritySignificance = "Integrity protection activates immediately; protects against bidding-down attacks.",
                SpecificationReference = "3GPP TS 33.501 Clause 6.7.2 & TS 24.501 Clause 8.2.25"
            },
            new()
            {
                StepNumber = 11,
                Sender = "UE",
                Receiver = "AMF",
                MessageName = "NAS Security Mode Complete",
                Summary = "UE verifies NAS-MAC, derives K_NASenc and K_NASint, and responds with an encrypted and integrity-protected completion message.",
                KeyParameters = new List<string> { "IMEISV (Encrypted)", "NAS-MAC" },
                SecuritySignificance = "From this point onward, all NAS signaling between UE and AMF is encrypted and integrity protected.",
                SpecificationReference = "3GPP TS 33.501 Clause 6.7.2 & TS 24.501 Clause 8.2.26"
            }
        };
    }
}
