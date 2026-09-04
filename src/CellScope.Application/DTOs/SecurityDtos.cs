namespace CellScope.Application.DTOs;

/// <summary>
/// Represents an individual step during AES-128 encryption (e.g. SubBytes, ShiftRows, MixColumns, AddRoundKey).
/// </summary>
public class AesStepTraceDto
{
    public int StepIndex { get; set; }
    public int RoundNumber { get; set; } // 0 to 10
    public string StepName { get; set; } = string.Empty; // e.g. "Round 1: SubBytes"
    public string OperationType { get; set; } = string.Empty; // "AddRoundKey", "SubBytes", "ShiftRows", "MixColumns"
    public string Description { get; set; } = string.Empty;
    public string SpecificationClause { get; set; } = "FIPS-197 Clause 5.1 / 3GPP TS 33.501";

    // 4x4 State Matrices (column-major order as per AES standard)
    public byte[][] InputState { get; set; } = new byte[4][];
    public byte[][] OutputState { get; set; } = new byte[4][];
    public byte[][]? RoundKey { get; set; } // 4x4 round key matrix if applicable

    public List<(int Row, int Col)> ChangedCells { get; set; } = new();

    public string InputHex => StateToHex(InputState);
    public string OutputHex => StateToHex(OutputState);
    public string RoundKeyHex => RoundKey != null ? StateToHex(RoundKey) : string.Empty;

    private static string StateToHex(byte[][] state)
    {
        if (state == null || state.Length != 4) return string.Empty;
        var bytes = new byte[16];
        int idx = 0;
        for (int c = 0; c < 4; c++)
        {
            for (int r = 0; r < 4; r++)
            {
                bytes[idx++] = state[r][c];
            }
        }
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>
/// Full execution trace of AES-128 encryption.
/// </summary>
public class AesTraceResultDto
{
    public string PlaintextHex { get; set; } = string.Empty;
    public string KeyHex { get; set; } = string.Empty;
    public string CiphertextHex { get; set; } = string.Empty;
    public List<string> ExpandedRoundKeysHex { get; set; } = new(); // 11 round keys (K0 to K10)
    public List<AesStepTraceDto> Steps { get; set; } = new();
    public int TotalSteps => Steps.Count;
}

/// <summary>
/// Results and intermediate stages of the 3GPP MILENAGE algorithm (TS 35.206).
/// </summary>
public class MilenageResultDto
{
    // Inputs
    public string KeyHex { get; set; } = string.Empty; // 128-bit K
    public string OpHex { get; set; } = string.Empty;  // 128-bit OP (optional)
    public string OpcHex { get; set; } = string.Empty; // 128-bit OPc
    public string RandHex { get; set; } = string.Empty; // 128-bit RAND
    public string SqnHex { get; set; } = string.Empty;  // 48-bit Sequence Number SQN
    public string AmfHex { get; set; } = string.Empty;  // 16-bit Authentication Management Field AMF

    // Intermediate states
    public string TempHex { get; set; } = string.Empty; // E_K(RAND xor OPc)
    public string In1Hex { get; set; } = string.Empty;  // SQN || AMF || SQN || AMF

    // Outputs of MILENAGE functions f1 - f5*
    public string MacAHex { get; set; } = string.Empty; // 64-bit Network Authentication Code (f1)
    public string MacSHex { get; set; } = string.Empty; // 64-bit Resync Auth Code (f1*)
    public string ResHex { get; set; } = string.Empty;  // 64-bit/128-bit User Response (f2)
    public string CkHex { get; set; } = string.Empty;   // 128-bit Cipher Key (f3)
    public string IkHex { get; set; } = string.Empty;   // 128-bit Integrity Key (f4)
    public string AkHex { get; set; } = string.Empty;   // 48-bit Anonymity Key (f5)
    public string AkStarHex { get; set; } = string.Empty; // 48-bit Resync Anonymity Key (f5*)
    public string AutnHex { get; set; } = string.Empty; // 128-bit AUTN = (SQN xor AK) || AMF || MAC-A

    public List<MilenageFunctionDetailDto> Functions { get; set; } = new();
}

/// <summary>
/// Detailed breakdown of an individual MILENAGE function (e.g. f1, f2, f3, f4, f5).
/// </summary>
public class MilenageFunctionDetailDto
{
    public string FunctionName { get; set; } = string.Empty; // "f1", "f1*", "f2", "f3", "f4", "f5", "f5*"
    public string OutputName { get; set; } = string.Empty;   // "MAC-A", "MAC-S", "RES", "CK", "IK", "AK", "AK*"
    public string OutputHex { get; set; } = string.Empty;
    public int OutputBits { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string RotationAmount { get; set; } = string.Empty; // e.g. "r1 = 64 bits"
    public string ConstantHex { get; set; } = string.Empty;    // e.g. "c1 = 00000000000000000000000000000000"
    public string IntermediateXorHex { get; set; } = string.Empty;
    public string AesOutputHex { get; set; } = string.Empty;
    public string SpecificationClause { get; set; } = "3GPP TS 35.206 Clause 4.1";
}

/// <summary>
/// Full 5G-AKA Authentication Vector & Hierarchical Key Derivation Result (TS 33.501).
/// </summary>
public class FiveGAkaResultDto
{
    // Primary Inputs
    public string KeyHex { get; set; } = string.Empty; // 128-bit K
    public string OpcHex { get; set; } = string.Empty; // 128-bit OPc
    public string RandHex { get; set; } = string.Empty; // 128-bit RAND
    public string SqnHex { get; set; } = string.Empty;  // 48-bit SQN
    public string AmfHex { get; set; } = string.Empty;  // 16-bit AMF
    public string ServingNetworkName { get; set; } = "5G:mnc410.mcc310.3gppnetwork.org";
    public string Supi { get; set; } = "imsi-310410123456789";

    // MILENAGE Core Outputs
    public MilenageResultDto Milenage { get; set; } = new();

    // 5G-AKA Authentication Outputs
    public string AutnHex => Milenage.AutnHex;
    public string XresStarHex { get; set; } = string.Empty; // 128-bit XRES* computed by UDM/ARPF
    public string ResStarHex { get; set; } = string.Empty;  // 128-bit RES* computed by UE
    public string HxresStarHex { get; set; } = string.Empty; // 128-bit HXRES* computed by AUSF for SEAF
    public bool AuthenticationSuccess => string.Equals(XresStarHex, ResStarHex, StringComparison.OrdinalIgnoreCase);

    // 5G Key Hierarchy (TS 33.501 Annex A)
    public string KausfHex { get; set; } = string.Empty; // 256-bit K_AUSF (derived from CK || IK)
    public string KseafHex { get; set; } = string.Empty; // 256-bit K_SEAF (derived from K_AUSF)
    public string KamfHex { get; set; } = string.Empty;  // 256-bit K_AMF (derived from K_SEAF)

    // NAS Security Keys
    public string KnasEncHex { get; set; } = string.Empty; // 128-bit / 256-bit K_NASenc
    public string KnasIntHex { get; set; } = string.Empty; // 128-bit / 256-bit K_NASint

    // Access Stratum (gNB) Keys
    public string KgnbHex { get; set; } = string.Empty;    // 256-bit K_gNB
    public string KrrcEncHex { get; set; } = string.Empty; // 128-bit K_RRCenc
    public string KrrcIntHex { get; set; } = string.Empty; // 128-bit K_RRCint
    public string KupEncHex { get; set; } = string.Empty;  // 128-bit K_UPenc
    public string KupIntHex { get; set; } = string.Empty;  // 128-bit K_UPint

    public List<KeyHierarchyNodeDto> KeyNodes { get; set; } = new();
}

/// <summary>
/// Individual Node in the 5G Key Hierarchy Tree.
/// </summary>
public class KeyHierarchyNodeDto
{
    public string KeyName { get; set; } = string.Empty;
    public string KeyHex { get; set; } = string.Empty;
    public int KeyBitLength { get; set; }
    public string ParentKeyName { get; set; } = string.Empty;
    public string FunctionCode { get; set; } = string.Empty; // e.g. "FC = 0x6A"
    public string Purpose { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // "USIM / UDM", "AUSF", "SEAF / AMF", "gNodeB (RAN)"
    public string DerivationInputS { get; set; } = string.Empty; // String S concatenation
    public string KdfAlgorithm { get; set; } = "HMAC-SHA-256 (TS 33.220 / TS 33.501 Annex B)";
    public string SpecificationClause { get; set; } = string.Empty;
}

/// <summary>
/// Detailed result of a 5G KDF (Key Derivation Function) calculation.
/// </summary>
public class KdfCalculationDto
{
    public string InputKeyHex { get; set; } = string.Empty;
    public byte FunctionCode { get; set; }
    public string FunctionCodeHex => $"0x{FunctionCode:X2}";
    public List<KdfParameterDto> Parameters { get; set; } = new();
    public string StringSHex { get; set; } = string.Empty; // FC || P0 || L0 || P1 || L1 ...
    public string DerivedKeyHex { get; set; } = string.Empty; // Full 256-bit HMAC-SHA-256 output
    public string TruncatedKeyHex { get; set; } = string.Empty; // First 128 bits if applicable
    public string SpecificationClause { get; set; } = "3GPP TS 33.501 Annex B.2 / TS 33.220 Annex B";
}

public class KdfParameterDto
{
    public int Index { get; set; }
    public string Label { get; set; } = string.Empty; // e.g. "Serving Network Name"
    public string ValueHex { get; set; } = string.Empty;
    public string ValueAscii { get; set; } = string.Empty;
    public int LengthBytes { get; set; }
    public string LengthHex => $"0x{LengthBytes:X4}";
}

/// <summary>
/// Test Vector entry with Expected vs Calculated verification.
/// </summary>
public class SecurityTestVectorDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StandardSource { get; set; } = string.Empty; // "3GPP TS 35.208 Set 1", "FIPS-197 Appendix B"
    public string Category { get; set; } = "MILENAGE"; // "AES-128", "MILENAGE", "5G-AKA", "KDF"
    public Dictionary<string, string> Inputs { get; set; } = new();
    public List<TestVectorComparisonDto> Comparisons { get; set; } = new();
    public bool IsPass => Comparisons.All(c => c.IsPass);
}

public class TestVectorComparisonDto
{
    public string FieldName { get; set; } = string.Empty; // e.g. "MAC-A", "CK", "AUTN", "Ciphertext"
    public string ExpectedHex { get; set; } = string.Empty;
    public string CalculatedHex { get; set; } = string.Empty;
    public bool IsPass => string.Equals(ExpectedHex.Trim(), CalculatedHex.Trim(), StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Sequence message in the 5G-AKA Authentication Flow.
/// </summary>
public class AuthFlowMessageDto
{
    public int StepNumber { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public string MessageName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyParameters { get; set; } = new();
    public string SecuritySignificance { get; set; } = string.Empty;
    public string SpecificationReference { get; set; } = "3GPP TS 33.501 Clause 6.1.3";
}
