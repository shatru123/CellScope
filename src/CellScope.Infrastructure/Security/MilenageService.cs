using CellScope.Application.DTOs;

namespace CellScope.Infrastructure.Security;

/// <summary>
/// 100% 3GPP TS 35.205 & TS 35.206 compliant MILENAGE Cryptographic Algorithm Suite.
/// Generates 3G/4G/5G Authentication Vectors (f1, f1*, f2, f3, f4, f5, f5*, AUTN).
/// </summary>
public static class MilenageService
{
    // Standard 3GPP TS 35.206 Clause 4.1 Rotation Constants (in bits)
    private const int R1 = 64; // 8 bytes
    private const int R2 = 0;  // 0 bytes
    private const int R3 = 32; // 4 bytes
    private const int R4 = 64; // 8 bytes
    private const int R5 = 96; // 12 bytes

    // Standard 3GPP TS 35.206 Clause 4.1 Additive Constants (c1 to c5)
    private static readonly byte[] C1 = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly byte[] C2 = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
    private static readonly byte[] C3 = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2 };
    private static readonly byte[] C4 = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4 };
    private static readonly byte[] C5 = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 8 };

    /// <summary>
    /// Computes OPc from OP and K if not already provided: OPc = AES_K(OP) ^ OP.
    /// </summary>
    public static byte[] ComputeOpc(byte[] key, byte[] op)
    {
        if (key.Length != 16) throw new ArgumentException("Key K must be 16 bytes (128 bits).", nameof(key));
        if (op.Length != 16) throw new ArgumentException("OP must be 16 bytes (128 bits).", nameof(op));

        byte[] enc = Aes128Tracer.EncryptBlock(key, op);
        byte[] opc = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            opc[i] = (byte)(enc[i] ^ op[i]);
        }
        return opc;
    }

    /// <summary>
    /// Executes full MILENAGE calculation returning complete result DTO with intermediate stages.
    /// </summary>
    public static MilenageResultDto Calculate(byte[] key, byte[] opOrOpc, bool isOpc, byte[] rand, byte[] sqn, byte[] amf)
    {
        if (key == null || key.Length != 16) throw new ArgumentException("K must be 16 bytes.", nameof(key));
        if (opOrOpc == null || opOrOpc.Length != 16) throw new ArgumentException("OP/OPc must be 16 bytes.", nameof(opOrOpc));
        if (rand == null || rand.Length != 16) throw new ArgumentException("RAND must be 16 bytes.", nameof(rand));
        if (sqn == null || sqn.Length != 6) throw new ArgumentException("SQN must be 6 bytes (48 bits).", nameof(sqn));
        if (amf == null || amf.Length != 2) throw new ArgumentException("AMF must be 2 bytes (16 bits).", nameof(amf));

        byte[] opc = isOpc ? (byte[])opOrOpc.Clone() : ComputeOpc(key, opOrOpc);

        var result = new MilenageResultDto
        {
            KeyHex = Convert.ToHexString(key).ToLowerInvariant(),
            OpHex = isOpc ? string.Empty : Convert.ToHexString(opOrOpc).ToLowerInvariant(),
            OpcHex = Convert.ToHexString(opc).ToLowerInvariant(),
            RandHex = Convert.ToHexString(rand).ToLowerInvariant(),
            SqnHex = Convert.ToHexString(sqn).ToLowerInvariant(),
            AmfHex = Convert.ToHexString(amf).ToLowerInvariant()
        };

        // 1. Calculate temp = E_K(RAND ^ OPc)
        byte[] randXorOpc = XorBuffers(rand, opc);
        byte[] temp = Aes128Tracer.EncryptBlock(key, randXorOpc);
        result.TempHex = Convert.ToHexString(temp).ToLowerInvariant();

        // 2. Form in1 = SQN || AMF || SQN || AMF (16 bytes)
        byte[] in1 = new byte[16];
        Buffer.BlockCopy(sqn, 0, in1, 0, 6);
        Buffer.BlockCopy(amf, 0, in1, 6, 2);
        Buffer.BlockCopy(sqn, 0, in1, 8, 6);
        Buffer.BlockCopy(amf, 0, in1, 14, 2);
        result.In1Hex = Convert.ToHexString(in1).ToLowerInvariant();

        // 3. Compute f1 and f1* (MAC-A and MAC-S)
        // rot(in1 ^ OPc, r1)
        byte[] in1XorOpc = XorBuffers(in1, opc);
        byte[] rotIn1 = RotateLeft128(in1XorOpc, R1);
        byte[] f1Input = XorBuffers(XorBuffers(temp, rotIn1), C1);
        byte[] f1AesOut = Aes128Tracer.EncryptBlock(key, f1Input);
        byte[] out1 = XorBuffers(f1AesOut, opc);

        byte[] macA = new byte[8];
        Buffer.BlockCopy(out1, 0, macA, 0, 8);
        result.MacAHex = Convert.ToHexString(macA).ToLowerInvariant();

        byte[] macS = new byte[8];
        Buffer.BlockCopy(out1, 8, macS, 0, 8);
        result.MacSHex = Convert.ToHexString(macS).ToLowerInvariant();

        result.Functions.Add(new MilenageFunctionDetailDto
        {
            FunctionName = "f1",
            OutputName = "MAC-A",
            OutputHex = result.MacAHex,
            OutputBits = 64,
            Purpose = "Network Authentication Code used by UE to authenticate serving network and verify SQN freshness.",
            RotationAmount = $"r1 = {R1} bits (8 bytes cyclic left)",
            ConstantHex = Convert.ToHexString(C1).ToLowerInvariant(),
            IntermediateXorHex = Convert.ToHexString(f1Input).ToLowerInvariant(),
            AesOutputHex = Convert.ToHexString(f1AesOut).ToLowerInvariant(),
            SpecificationClause = "3GPP TS 35.206 Clause 4.1"
        });

        result.Functions.Add(new MilenageFunctionDetailDto
        {
            FunctionName = "f1*",
            OutputName = "MAC-S",
            OutputHex = result.MacSHex,
            OutputBits = 64,
            Purpose = "Resynchronization Authentication Code sent in AUTS if UE detects SQN out-of-sequence.",
            RotationAmount = $"r1 = {R1} bits (8 bytes cyclic left)",
            ConstantHex = Convert.ToHexString(C1).ToLowerInvariant(),
            IntermediateXorHex = Convert.ToHexString(f1Input).ToLowerInvariant(),
            AesOutputHex = Convert.ToHexString(f1AesOut).ToLowerInvariant(),
            SpecificationClause = "3GPP TS 35.206 Clause 4.1"
        });

        // 4. Compute f2 and f5 (RES and AK)
        // rot(temp ^ OPc, r2 = 0)
        byte[] tempXorOpc = XorBuffers(temp, opc);
        byte[] rotTempR2 = RotateLeft128(tempXorOpc, R2);
        byte[] f2Input = XorBuffers(rotTempR2, C2);
        byte[] f2AesOut = Aes128Tracer.EncryptBlock(key, f2Input);
        byte[] out2 = XorBuffers(f2AesOut, opc);

        byte[] res = new byte[8];
        Buffer.BlockCopy(out2, 8, res, 0, 8);
        result.ResHex = Convert.ToHexString(res).ToLowerInvariant();

        // f5 (AK) is out2[0..5]
        byte[] ak = new byte[6];
        Buffer.BlockCopy(out2, 0, ak, 0, 6);
        result.AkHex = Convert.ToHexString(ak).ToLowerInvariant();

        // f5* (AK*) is out5[0..5] (rot with r5 and c5)
        byte[] rotTempR5 = RotateLeft128(tempXorOpc, R5);
        byte[] f5Input = XorBuffers(rotTempR5, C5);
        byte[] f5AesOut = Aes128Tracer.EncryptBlock(key, f5Input);
        byte[] out5 = XorBuffers(f5AesOut, opc);

        byte[] akStar = new byte[6];
        Buffer.BlockCopy(out5, 0, akStar, 0, 6);
        result.AkStarHex = Convert.ToHexString(akStar).ToLowerInvariant();

        result.Functions.Add(new MilenageFunctionDetailDto
        {
            FunctionName = "f2",
            OutputName = "RES",
            OutputHex = result.ResHex,
            OutputBits = 64,
            Purpose = "User Authentication Response computed by USIM to prove possession of subscriber secret K.",
            RotationAmount = $"r2 = {R2} bits (0 shift)",
            ConstantHex = Convert.ToHexString(C2).ToLowerInvariant(),
            IntermediateXorHex = Convert.ToHexString(f2Input).ToLowerInvariant(),
            AesOutputHex = Convert.ToHexString(f2AesOut).ToLowerInvariant(),
            SpecificationClause = "3GPP TS 35.206 Clause 4.1"
        });

        // 5. Compute f3 (CK - Cipher Key)
        byte[] rotTempR3 = RotateLeft128(tempXorOpc, R3);
        byte[] f3Input = XorBuffers(rotTempR3, C3);
        byte[] f3AesOut = Aes128Tracer.EncryptBlock(key, f3Input);
        byte[] ck = XorBuffers(f3AesOut, opc);
        result.CkHex = Convert.ToHexString(ck).ToLowerInvariant();

        result.Functions.Add(new MilenageFunctionDetailDto
        {
            FunctionName = "f3",
            OutputName = "CK",
            OutputHex = result.CkHex,
            OutputBits = 128,
            Purpose = "128-bit Confidentiality / Cipher Key used in 3G/4G/5G root key derivation.",
            RotationAmount = $"r3 = {R3} bits (4 bytes cyclic left)",
            ConstantHex = Convert.ToHexString(C3).ToLowerInvariant(),
            IntermediateXorHex = Convert.ToHexString(f3Input).ToLowerInvariant(),
            AesOutputHex = Convert.ToHexString(f3AesOut).ToLowerInvariant(),
            SpecificationClause = "3GPP TS 35.206 Clause 4.1"
        });

        // 6. Compute f4 (IK - Integrity Key)
        byte[] rotTempR4 = RotateLeft128(tempXorOpc, R4);
        byte[] f4Input = XorBuffers(rotTempR4, C4);
        byte[] f4AesOut = Aes128Tracer.EncryptBlock(key, f4Input);
        byte[] ik = XorBuffers(f4AesOut, opc);
        result.IkHex = Convert.ToHexString(ik).ToLowerInvariant();

        result.Functions.Add(new MilenageFunctionDetailDto
        {
            FunctionName = "f4",
            OutputName = "IK",
            OutputHex = result.IkHex,
            OutputBits = 128,
            Purpose = "128-bit Integrity Key used for signaling integrity protection and 5G KAUSF derivation.",
            RotationAmount = $"r4 = {R4} bits (8 bytes cyclic left)",
            ConstantHex = Convert.ToHexString(C4).ToLowerInvariant(),
            IntermediateXorHex = Convert.ToHexString(f4Input).ToLowerInvariant(),
            AesOutputHex = Convert.ToHexString(f4AesOut).ToLowerInvariant(),
            SpecificationClause = "3GPP TS 35.206 Clause 4.1"
        });

        // Add f5 and f5*
        result.Functions.Add(new MilenageFunctionDetailDto
        {
            FunctionName = "f5",
            OutputName = "AK",
            OutputHex = result.AkHex,
            OutputBits = 48,
            Purpose = "48-bit Anonymity Key used to conceal Sequence Number SQN over-the-air in AUTN (SQN ^ AK).",
            RotationAmount = $"r2 = {R2} bits (0 shift)",
            ConstantHex = Convert.ToHexString(C2).ToLowerInvariant(),
            IntermediateXorHex = Convert.ToHexString(f2Input).ToLowerInvariant(),
            AesOutputHex = Convert.ToHexString(f2AesOut).ToLowerInvariant(),
            SpecificationClause = "3GPP TS 35.206 Clause 4.1"
        });

        result.Functions.Add(new MilenageFunctionDetailDto
        {
            FunctionName = "f5*",
            OutputName = "AK*",
            OutputHex = result.AkStarHex,
            OutputBits = 48,
            Purpose = "48-bit Resynchronization Anonymity Key used to conceal SQN_MS in AUTS during resync.",
            RotationAmount = $"r5 = {R5} bits (12 bytes cyclic left)",
            ConstantHex = Convert.ToHexString(C5).ToLowerInvariant(),
            IntermediateXorHex = Convert.ToHexString(f5Input).ToLowerInvariant(),
            AesOutputHex = Convert.ToHexString(f5AesOut).ToLowerInvariant(),
            SpecificationClause = "3GPP TS 35.206 Clause 4.1"
        });

        // 7. Form AUTN = (SQN ^ AK) || AMF || MAC-A (6 + 2 + 8 = 16 bytes)
        byte[] sqnXorAk = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            sqnXorAk[i] = (byte)(sqn[i] ^ ak[i]);
        }

        byte[] autn = new byte[16];
        Buffer.BlockCopy(sqnXorAk, 0, autn, 0, 6);
        Buffer.BlockCopy(amf, 0, autn, 6, 2);
        Buffer.BlockCopy(macA, 0, autn, 8, 8);
        result.AutnHex = Convert.ToHexString(autn).ToLowerInvariant();

        return result;
    }

    // --- Bitwise & Rotation Helpers ---

    private static byte[] XorBuffers(byte[] a, byte[] b)
    {
        byte[] r = new byte[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            r[i] = (byte)(a[i] ^ b[i]);
        }
        return r;
    }

    /// <summary>
    /// Performs 128-bit cyclic left rotation by r bits (r must be a multiple of 8 as per 3GPP spec).
    /// </summary>
    private static byte[] RotateLeft128(byte[] input, int bits)
    {
        int byteShift = (bits / 8) % 16;
        if (byteShift == 0) return (byte[])input.Clone();

        byte[] rotated = new byte[16];
        for (int i = 0; i < 16; i++)
        {
            rotated[i] = input[(i + byteShift) % 16];
        }
        return rotated;
    }
}
