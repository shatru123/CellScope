using System.Security.Cryptography;
using System.Text;
using CellScope.Application.DTOs;

namespace CellScope.Infrastructure.Security;

/// <summary>
/// 3GPP TS 33.501 compliant 5G-AKA Authentication and Hierarchical Key Derivation Service.
/// Implements TS 33.501 Annex A (Derivations) and Annex B (KDF / HMAC-SHA-256).
/// </summary>
public static class FiveGAkaService
{
    /// <summary>
    /// Executes full 5G-AKA authentication vector generation and key hierarchy derivation.
    /// </summary>
    public static FiveGAkaResultDto Calculate(
        byte[] key,
        byte[] opOrOpc,
        bool isOpc,
        byte[] rand,
        byte[] sqn,
        byte[] amf,
        string servingNetworkName,
        string supi)
    {
        // 1. Run MILENAGE core
        var milenage = MilenageService.Calculate(key, opOrOpc, isOpc, rand, sqn, amf);

        byte[] ck = Convert.FromHexString(milenage.CkHex);
        byte[] ik = Convert.FromHexString(milenage.IkHex);
        byte[] res = Convert.FromHexString(milenage.ResHex);
        byte[] autn = Convert.FromHexString(milenage.AutnHex);

        // Combined Key = CK || IK (32 bytes)
        byte[] ckIk = new byte[32];
        Buffer.BlockCopy(ck, 0, ckIk, 0, 16);
        Buffer.BlockCopy(ik, 0, ckIk, 16, 16);

        byte[] snNameBytes = Encoding.UTF8.GetBytes(servingNetworkName);
        byte[] supiBytes = Encoding.UTF8.GetBytes(supi);

        // (SQN ^ AK) extracted from first 6 bytes of AUTN
        byte[] sqnXorAk = new byte[6];
        Buffer.BlockCopy(autn, 0, sqnXorAk, 0, 6);

        var result = new FiveGAkaResultDto
        {
            KeyHex = Convert.ToHexString(key).ToLowerInvariant(),
            OpcHex = milenage.OpcHex,
            RandHex = Convert.ToHexString(rand).ToLowerInvariant(),
            SqnHex = Convert.ToHexString(sqn).ToLowerInvariant(),
            AmfHex = Convert.ToHexString(amf).ToLowerInvariant(),
            ServingNetworkName = servingNetworkName,
            Supi = supi,
            Milenage = milenage
        };

        // 2. Compute RES* and XRES* (TS 33.501 Annex A.4)
        // FC = 0x6B, P0 = SN Name, P1 = RAND, P2 = RES
        var resStarKdf = ComputeKdfInternal(ckIk, 0x6B, new List<(byte[] P, string Label)>
        {
            (snNameBytes, "Serving Network Name"),
            (rand, "RAND"),
            (res, "RES / XRES")
        });
        // (X)RES* is 128 bits (16 bytes)
        byte[] xresStarFull = Convert.FromHexString(resStarKdf.DerivedKeyHex);
        byte[] xresStar = new byte[16];
        Buffer.BlockCopy(xresStarFull, 16, xresStar, 0, 16); // Least significant 128 bits
        result.XresStarHex = Convert.ToHexString(xresStar).ToLowerInvariant();
        result.ResStarHex = result.XresStarHex; // In honest USIM execution RES* == XRES*

        // 3. Compute HXRES* = SHA-256(RAND || XRES*)[16..31] (TS 33.501 Clause 6.1.3.2)
        byte[] randXresStar = new byte[32];
        Buffer.BlockCopy(rand, 0, randXresStar, 0, 16);
        Buffer.BlockCopy(xresStar, 0, randXresStar, 16, 16);
        byte[] hxresFull = SHA256.HashData(randXresStar);
        byte[] hxresStar = new byte[16];
        Buffer.BlockCopy(hxresFull, 16, hxresStar, 0, 16);
        result.HxresStarHex = Convert.ToHexString(hxresStar).ToLowerInvariant();

        // 4. Derive K_AUSF (TS 33.501 Annex A.2)
        // FC = 0x6A, Key = CK || IK, P0 = SN Name, P1 = SQN ^ AK
        var kausfKdf = ComputeKdfInternal(ckIk, 0x6A, new List<(byte[] P, string Label)>
        {
            (snNameBytes, "Serving Network Name"),
            (sqnXorAk, "SQN ^ AK")
        });
        result.KausfHex = kausfKdf.DerivedKeyHex;
        byte[] kausf = Convert.FromHexString(result.KausfHex);

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_AUSF",
            KeyHex = result.KausfHex,
            KeyBitLength = 256,
            ParentKeyName = "CK || IK",
            FunctionCode = "FC = 0x6A",
            Purpose = "Authentication Server Function Master Key. Anchors 5G security in the home network (AUSF).",
            Location = "AUSF / UDM (Home Network)",
            DerivationInputS = kausfKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.2"
        });

        // 5. Derive K_SEAF (TS 33.501 Annex A.6)
        // FC = 0x6C, Key = K_AUSF, P0 = SN Name
        var kseafKdf = ComputeKdfInternal(kausf, 0x6C, new List<(byte[] P, string Label)>
        {
            (snNameBytes, "Serving Network Name")
        });
        result.KseafHex = kseafKdf.DerivedKeyHex;
        byte[] kseaf = Convert.FromHexString(result.KseafHex);

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_SEAF",
            KeyHex = result.KseafHex,
            KeyBitLength = 256,
            ParentKeyName = "K_AUSF",
            FunctionCode = "FC = 0x6C",
            Purpose = "Security Anchor Function Key. Transferred from AUSF to SEAF/AMF in the serving network.",
            Location = "SEAF / AMF (Serving Network)",
            DerivationInputS = kseafKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.6"
        });

        // 6. Derive K_AMF (TS 33.501 Annex A.7)
        // FC = 0x6D, Key = K_SEAF, P0 = SUPI, P1 = ABBA (0x0000)
        byte[] abba = new byte[2] { 0x00, 0x00 };
        var kamfKdf = ComputeKdfInternal(kseaf, 0x6D, new List<(byte[] P, string Label)>
        {
            (supiBytes, "SUPI (IMSI)"),
            (abba, "ABBA Parameter (0x0000)")
        });
        result.KamfHex = kamfKdf.DerivedKeyHex;
        byte[] kamf = Convert.FromHexString(result.KamfHex);

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_AMF",
            KeyHex = result.KamfHex,
            KeyBitLength = 256,
            ParentKeyName = "K_SEAF",
            FunctionCode = "FC = 0x6D",
            Purpose = "Access and Mobility Management Function Master Key. Derives all NAS signaling and gNB keys.",
            Location = "AMF & UE (ME / Non-Access Stratum)",
            DerivationInputS = kamfKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.7"
        });

        // 7. Derive K_NASenc & K_NASint (TS 33.501 Annex A.8)
        // FC = 0x69, Key = K_AMF, P0 = Type (0x01 NAS-enc, 0x02 NAS-int), P1 = AlgId (0x02 128-AES)
        byte[] p0NasEnc = new byte[1] { 0x01 };
        byte[] p0NasInt = new byte[1] { 0x02 };
        byte[] p1AlgAes = new byte[1] { 0x02 }; // 128-NEA2 / 128-NIA2

        var knasEncKdf = ComputeKdfInternal(kamf, 0x69, new List<(byte[] P, string Label)>
        {
            (p0NasEnc, "Algorithm Type Distinguisher (0x01 NAS-enc)"),
            (p1AlgAes, "Algorithm Identity (0x02 128-NEA2 AES)")
        });
        result.KnasEncHex = knasEncKdf.TruncatedKeyHex;

        var knasIntKdf = ComputeKdfInternal(kamf, 0x69, new List<(byte[] P, string Label)>
        {
            (p0NasInt, "Algorithm Type Distinguisher (0x02 NAS-int)"),
            (p1AlgAes, "Algorithm Identity (0x02 128-NIA2 AES)")
        });
        result.KnasIntHex = knasIntKdf.TruncatedKeyHex;

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_NASenc",
            KeyHex = result.KnasEncHex,
            KeyBitLength = 128,
            ParentKeyName = "K_AMF",
            FunctionCode = "FC = 0x69",
            Purpose = "NAS Encryption Key used for encrypting NAS signaling messages between UE and AMF.",
            Location = "AMF & UE (NAS Protocol Layer)",
            DerivationInputS = knasEncKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.8"
        });

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_NASint",
            KeyHex = result.KnasIntHex,
            KeyBitLength = 128,
            ParentKeyName = "K_AMF",
            FunctionCode = "FC = 0x69",
            Purpose = "NAS Integrity Key used for cryptographically authenticating NAS signaling messages.",
            Location = "AMF & UE (NAS Protocol Layer)",
            DerivationInputS = knasIntKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.8"
        });

        // 8. Derive K_gNB (TS 33.501 Annex A.9)
        // FC = 0x6E, Key = K_AMF, P0 = Uplink NAS COUNT (0x00000001), P1 = Access Type (0x01 3GPP)
        byte[] ulNasCount = new byte[4] { 0x00, 0x00, 0x00, 0x01 };
        byte[] accessType = new byte[1] { 0x01 }; // 3GPP Access

        var kgnbKdf = ComputeKdfInternal(kamf, 0x6E, new List<(byte[] P, string Label)>
        {
            (ulNasCount, "Uplink NAS COUNT (0x00000001)"),
            (accessType, "Access Type Distinguisher (0x01 3GPP)")
        });
        result.KgnbHex = kgnbKdf.DerivedKeyHex;
        byte[] kgnb = Convert.FromHexString(result.KgnbHex);

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_gNB",
            KeyHex = result.KgnbHex,
            KeyBitLength = 256,
            ParentKeyName = "K_AMF",
            FunctionCode = "FC = 0x6E",
            Purpose = "gNodeB Base Station Master Key. Provided to base station during RRC connection establishment.",
            Location = "gNodeB (Base Station) & UE (AS Layer)",
            DerivationInputS = kgnbKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.9"
        });

        // 9. Derive AS Keys (K_RRCenc, K_RRCint, K_UPenc, K_UPint) (TS 33.501 Annex A.8)
        byte[] p0RrcEnc = new byte[1] { 0x03 };
        byte[] p0RrcInt = new byte[1] { 0x04 };
        byte[] p0UpEnc = new byte[1] { 0x05 };
        byte[] p0UpInt = new byte[1] { 0x06 };

        var krrcEncKdf = ComputeKdfInternal(kgnb, 0x69, new List<(byte[] P, string Label)>
        {
            (p0RrcEnc, "Distinguisher (0x03 RRC-enc)"),
            (p1AlgAes, "Algorithm Identity (0x02 128-AES)")
        });
        result.KrrcEncHex = krrcEncKdf.TruncatedKeyHex;

        var krrcIntKdf = ComputeKdfInternal(kgnb, 0x69, new List<(byte[] P, string Label)>
        {
            (p0RrcInt, "Distinguisher (0x04 RRC-int)"),
            (p1AlgAes, "Algorithm Identity (0x02 128-AES)")
        });
        result.KrrcIntHex = krrcIntKdf.TruncatedKeyHex;

        var kupEncKdf = ComputeKdfInternal(kgnb, 0x69, new List<(byte[] P, string Label)>
        {
            (p0UpEnc, "Distinguisher (0x05 UP-enc)"),
            (p1AlgAes, "Algorithm Identity (0x02 128-AES)")
        });
        result.KupEncHex = kupEncKdf.TruncatedKeyHex;

        var kupIntKdf = ComputeKdfInternal(kgnb, 0x69, new List<(byte[] P, string Label)>
        {
            (p0UpInt, "Distinguisher (0x06 UP-int)"),
            (p1AlgAes, "Algorithm Identity (0x02 128-AES)")
        });
        result.KupIntHex = kupIntKdf.TruncatedKeyHex;

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_RRCenc",
            KeyHex = result.KrrcEncHex,
            KeyBitLength = 128,
            ParentKeyName = "K_gNB",
            FunctionCode = "FC = 0x69",
            Purpose = "Radio Resource Control (RRC) Encryption Key protecting air-interface control plane messages.",
            Location = "gNodeB & UE (RRC Layer)",
            DerivationInputS = krrcEncKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.8"
        });

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_RRCint",
            KeyHex = result.KrrcIntHex,
            KeyBitLength = 128,
            ParentKeyName = "K_gNB",
            FunctionCode = "FC = 0x69",
            Purpose = "RRC Integrity Key preventing unauthorized tampering with cellular control plane signaling.",
            Location = "gNodeB & UE (RRC Layer)",
            DerivationInputS = krrcIntKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.8"
        });

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_UPenc",
            KeyHex = result.KupEncHex,
            KeyBitLength = 128,
            ParentKeyName = "K_gNB",
            FunctionCode = "FC = 0x69",
            Purpose = "User Plane Encryption Key protecting user data traffic (TCP/UDP payloads) over-the-air.",
            Location = "gNodeB (PDCP) & UE",
            DerivationInputS = kupEncKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.8"
        });

        result.KeyNodes.Add(new KeyHierarchyNodeDto
        {
            KeyName = "K_UPint",
            KeyHex = result.KupIntHex,
            KeyBitLength = 128,
            ParentKeyName = "K_gNB",
            FunctionCode = "FC = 0x69",
            Purpose = "User Plane Integrity Key ensuring user data packets cannot be forged or injected.",
            Location = "gNodeB (PDCP) & UE",
            DerivationInputS = kupIntKdf.StringSHex,
            SpecificationClause = "3GPP TS 33.501 Annex A.8"
        });

        return result;
    }

    /// <summary>
    /// Computes 3GPP KDF (HMAC-SHA-256) per TS 33.220 Annex B:
    /// S = FC || P0 || L0 || P1 || L1 ... || Pn || Ln
    /// </summary>
    public static KdfCalculationDto ComputeKdf(byte[] key, byte fc, List<(byte[] Param, string Label)> parameters)
    {
        return ComputeKdfInternal(key, fc, parameters);
    }

    private static KdfCalculationDto ComputeKdfInternal(byte[] key, byte fc, List<(byte[] Param, string Label)> parameters)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(fc);

        var paramDtos = new List<KdfParameterDto>();
        for (int i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];
            byte[] val = p.Param ?? Array.Empty<byte>();
            ushort len = (ushort)val.Length;

            // Write P_i
            ms.Write(val, 0, val.Length);

            // Write L_i (2 bytes, big-endian)
            byte[] lenBytes = new byte[2]
            {
                (byte)((len >> 8) & 0xFF),
                (byte)(len & 0xFF)
            };
            ms.Write(lenBytes, 0, 2);

            string ascii = string.Empty;
            try
            {
                if (val.All(b => b >= 32 && b <= 126))
                    ascii = Encoding.UTF8.GetString(val);
            }
            catch { }

            paramDtos.Add(new KdfParameterDto
            {
                Index = i,
                Label = p.Label,
                ValueHex = Convert.ToHexString(val).ToLowerInvariant(),
                ValueAscii = ascii,
                LengthBytes = val.Length
            });
        }

        byte[] stringS = ms.ToArray();

        using var hmac = new HMACSHA256(key);
        byte[] derived = hmac.ComputeHash(stringS);

        byte[] truncated128 = new byte[16];
        Buffer.BlockCopy(derived, 16, truncated128, 0, 16); // Least significant 128 bits

        return new KdfCalculationDto
        {
            InputKeyHex = Convert.ToHexString(key).ToLowerInvariant(),
            FunctionCode = fc,
            Parameters = paramDtos,
            StringSHex = Convert.ToHexString(stringS).ToLowerInvariant(),
            DerivedKeyHex = Convert.ToHexString(derived).ToLowerInvariant(),
            TruncatedKeyHex = Convert.ToHexString(truncated128).ToLowerInvariant()
        };
    }
}
