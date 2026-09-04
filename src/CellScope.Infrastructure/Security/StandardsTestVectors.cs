using CellScope.Application.DTOs;

namespace CellScope.Infrastructure.Security;

/// <summary>
/// Preloaded official 3GPP and NIST standards test vectors for verification.
/// Sources: 3GPP TS 35.208 (Sets 1-6), FIPS-197, 3GPP TS 33.501 Annex A.
/// </summary>
public static class StandardsTestVectors
{
    public static List<SecurityTestVectorDto> GetTestVectors()
    {
        return new List<SecurityTestVectorDto>
        {
            // 1. 3GPP TS 35.208 Set 1 (Standard 3GPP MILENAGE Test Vector)
            new SecurityTestVectorDto
            {
                Id = "ts35208-set1",
                Title = "3GPP TS 35.208 Set 1 (Standard MILENAGE)",
                StandardSource = "3GPP TS 35.208 Clause 4.1 (Set 1)",
                Category = "MILENAGE",
                Inputs = new Dictionary<string, string>
                {
                    { "K", "465b5ce8b199b49faa5f0a2ee238a6bc" },
                    { "OP", "cdc202d5123e20f62b6d676ac72cb318" },
                    { "RAND", "23553cbe9637a89d218ae64dae47bf35" },
                    { "SQN", "ff9bb4d0b607" },
                    { "AMF", "b9b9" }
                },
                Comparisons = new List<TestVectorComparisonDto>
                {
                    new() { FieldName = "OPc", ExpectedHex = "cd63cb71954a9f4e48a5994e37a02baf" },
                    new() { FieldName = "f1 (MAC-A)", ExpectedHex = "4a9ffac354dfafb3" },
                    new() { FieldName = "f2 (RES)", ExpectedHex = "a54211d5e3ba50bf" },
                    new() { FieldName = "f3 (CK)", ExpectedHex = "b40ba9a3c58b2a05bbf0d987b21bf8cb" },
                    new() { FieldName = "f4 (IK)", ExpectedHex = "f769bcd751044604127672711c6d3441" },
                    new() { FieldName = "f5 (AK)", ExpectedHex = "aa689c648370" },
                    new() { FieldName = "AUTN", ExpectedHex = "55f328b43577b9b94a9ffac354dfafb3" }
                }
            },

            // 2. 3GPP TS 35.208 Set 2
            new SecurityTestVectorDto
            {
                Id = "ts35208-set2",
                Title = "3GPP TS 35.208 Set 2 (Alternative Constants)",
                StandardSource = "3GPP TS 35.208 Clause 4.2 (Set 2)",
                Category = "MILENAGE",
                Inputs = new Dictionary<string, string>
                {
                    { "K", "fec86ba6eb707ed08905757b1bb44b8f" },
                    { "OP", "dbc59adcb6f9a0ef735477b7fadf8374" },
                    { "RAND", "9f7c8d021accf4db213ccff0c7f71a6a" },
                    { "SQN", "9d0277595ffc" },
                    { "AMF", "725c" }
                },
                Comparisons = new List<TestVectorComparisonDto>
                {
                    new() { FieldName = "OPc", ExpectedHex = "1006020f0a478bf6b699f15c062e42b3" },
                    new() { FieldName = "f1 (MAC-A)", ExpectedHex = "9cabc3e99baf7281" },
                    new() { FieldName = "f2 (RES)", ExpectedHex = "8011c48c0c214ed2" },
                    new() { FieldName = "f3 (CK)", ExpectedHex = "5dbdbb2954e8f3cde665b046179a5098" },
                    new() { FieldName = "f4 (IK)", ExpectedHex = "59a92d3b476a0443487055cf88b2307b" },
                    new() { FieldName = "f5 (AK)", ExpectedHex = "33484dc2136b" }
                }
            },

            // 3. FIPS-197 AES-128 Official Test Vector (Appendix B)
            new SecurityTestVectorDto
            {
                Id = "fips197-appb",
                Title = "NIST FIPS-197 AES-128 Appendix B Test Vector",
                StandardSource = "NIST FIPS-197 Appendix B",
                Category = "AES-128",
                Inputs = new Dictionary<string, string>
                {
                    { "Plaintext", "3243f6a8885a308d313198a2e0370734" },
                    { "Key", "2b7e151628aed2a6abf7158809cf4f3c" }
                },
                Comparisons = new List<TestVectorComparisonDto>
                {
                    new() { FieldName = "Ciphertext", ExpectedHex = "3925841d02dc09fbdc118597196a0b32" }
                }
            },

            // 4. FIPS-197 AES-128 Official Test Vector (Appendix C)
            new SecurityTestVectorDto
            {
                Id = "fips197-appc",
                Title = "NIST FIPS-197 AES-128 Appendix C Test Vector",
                StandardSource = "NIST FIPS-197 Appendix C.1",
                Category = "AES-128",
                Inputs = new Dictionary<string, string>
                {
                    { "Plaintext", "00112233445566778899aabbccddeeff" },
                    { "Key", "000102030405060708090a0b0c0d0e0f" }
                },
                Comparisons = new List<TestVectorComparisonDto>
                {
                    new() { FieldName = "Ciphertext", ExpectedHex = "69c4e0d86a7b0430d8cdb78070b4c55a" }
                }
            },

            // 5. 3GPP TS 33.501 5G-AKA Test Vector (Annex A Reference)
            new SecurityTestVectorDto
            {
                Id = "ts33501-5gaka",
                Title = "3GPP TS 33.501 5G-AKA Authentication Vector & Key Derivation",
                StandardSource = "3GPP TS 33.501 Clause 6.1.3 & Annex A",
                Category = "5G-AKA",
                Inputs = new Dictionary<string, string>
                {
                    { "K", "465b5ce8b199b49faa5f0a2ee238a6bc" },
                    { "OPc", "cd63cb71954a9f4e48a5994e37a02baf" },
                    { "RAND", "23553cbe9637a89d218ae64dae47bf35" },
                    { "SQN", "ff9bb4d0b607" },
                    { "AMF", "b9b9" },
                    { "ServingNetwork", "5G:mnc410.mcc310.3gppnetwork.org" },
                    { "SUPI", "imsi-310410123456789" }
                },
                Comparisons = new List<TestVectorComparisonDto>
                {
                    new() { FieldName = "AUTN", ExpectedHex = "55f328b43577b9b94a9ffac354dfafb3" }
                }
            }
        };
    }
}
