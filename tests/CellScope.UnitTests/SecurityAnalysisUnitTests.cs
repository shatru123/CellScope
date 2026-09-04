using CellScope.Infrastructure.Security;
using CellScope.Infrastructure.Services;
using Xunit;

namespace CellScope.UnitTests;

public class SecurityAnalysisUnitTests
{
    [Fact]
    public void Aes128Tracer_MatchesFips197AppendixB_TestVector()
    {
        // NIST FIPS-197 Appendix B Test Vector
        byte[] plaintext = Convert.FromHexString("3243f6a8885a308d313198a2e0370734");
        byte[] key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
        string expectedCiphertext = "3925841d02dc09fbdc118597196a0b32";

        var trace = Aes128Tracer.Trace(key, plaintext);

        Assert.NotNull(trace);
        Assert.Equal(expectedCiphertext, trace.CiphertextHex, ignoreCase: true);
        Assert.Equal(11, trace.ExpandedRoundKeysHex.Count);
        Assert.Equal(40, trace.Steps.Count); // Round 0 (1 step) + Rounds 1-9 (9*4=36 steps) + Round 10 (3 steps)
        Assert.All(trace.Steps, s => Assert.NotNull(s.OutputState));
    }

    [Fact]
    public void Aes128Tracer_MatchesFips197AppendixC_TestVector()
    {
        // NIST FIPS-197 Appendix C.1 Test Vector
        byte[] plaintext = Convert.FromHexString("00112233445566778899aabbccddeeff");
        byte[] key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
        string expectedCiphertext = "69c4e0d86a7b0430d8cdb78070b4c55a";

        var trace = Aes128Tracer.Trace(key, plaintext);

        Assert.NotNull(trace);
        Assert.Equal(expectedCiphertext, trace.CiphertextHex, ignoreCase: true);
    }

    [Fact]
    public void MilenageService_MatchesTs35208Set1_TestVector()
    {
        // 3GPP TS 35.208 Clause 4.1 (Set 1)
        byte[] k = Convert.FromHexString("465b5ce8b199b49faa5f0a2ee238a6bc");
        byte[] op = Convert.FromHexString("cdc202d5123e20f62b6d676ac72cb318");
        byte[] rand = Convert.FromHexString("23553cbe9637a89d218ae64dae47bf35");
        byte[] sqn = Convert.FromHexString("ff9bb4d0b607");
        byte[] amf = Convert.FromHexString("b9b9");

        var result = MilenageService.Calculate(k, op, isOpc: false, rand, sqn, amf);

        Assert.Equal("cd63cb71954a9f4e48a5994e37a02baf", result.OpcHex, ignoreCase: true);
        Assert.Equal("4a9ffac354dfafb3", result.MacAHex, ignoreCase: true);
        Assert.Equal("a54211d5e3ba50bf", result.ResHex, ignoreCase: true);
        Assert.Equal("b40ba9a3c58b2a05bbf0d987b21bf8cb", result.CkHex, ignoreCase: true);
        Assert.Equal("f769bcd751044604127672711c6d3441", result.IkHex, ignoreCase: true);
        Assert.Equal("aa689c648370", result.AkHex, ignoreCase: true);
        Assert.Equal("55f328b43577b9b94a9ffac354dfafb3", result.AutnHex, ignoreCase: true);
    }

    [Fact]
    public void MilenageService_MatchesTs35208Set2_TestVector()
    {
        // 3GPP TS 35.208 Clause 4.2 (Set 2)
        byte[] k = Convert.FromHexString("fec86ba6eb707ed08905757b1bb44b8f");
        byte[] op = Convert.FromHexString("dbc59adcb6f9a0ef735477b7fadf8374");
        byte[] rand = Convert.FromHexString("9f7c8d021accf4db213ccff0c7f71a6a");
        byte[] sqn = Convert.FromHexString("9d0277595ffc");
        byte[] amf = Convert.FromHexString("725c");

        var result = MilenageService.Calculate(k, op, isOpc: false, rand, sqn, amf);

        Assert.Equal("1006020f0a478bf6b699f15c062e42b3", result.OpcHex, ignoreCase: true);
        Assert.Equal("9cabc3e99baf7281", result.MacAHex, ignoreCase: true);
        Assert.Equal("8011c48c0c214ed2", result.ResHex, ignoreCase: true);
        Assert.Equal("5dbdbb2954e8f3cde665b046179a5098", result.CkHex, ignoreCase: true);
        Assert.Equal("59a92d3b476a0443487055cf88b2307b", result.IkHex, ignoreCase: true);
        Assert.Equal("33484dc2136b", result.AkHex, ignoreCase: true);
    }

    [Fact]
    public void FiveGAkaService_DerivesCompleteHierarchy_Successfully()
    {
        byte[] k = Convert.FromHexString("465b5ce8b199b49faa5f0a2ee238a6bc");
        byte[] opc = Convert.FromHexString("cd63cb71954a9f4e48a5994e37a02baf");
        byte[] rand = Convert.FromHexString("23553cbe9637a89d218ae64dae47bf35");
        byte[] sqn = Convert.FromHexString("ff9bb4d0b607");
        byte[] amf = Convert.FromHexString("b9b9");
        string snName = "5G:mnc410.mcc310.3gppnetwork.org";
        string supi = "imsi-310410123456789";

        var aka = FiveGAkaService.Calculate(k, opc, isOpc: true, rand, sqn, amf, snName, supi);

        Assert.NotNull(aka);
        Assert.True(aka.AuthenticationSuccess);
        Assert.NotEmpty(aka.XresStarHex);
        Assert.NotEmpty(aka.HxresStarHex);
        Assert.NotEmpty(aka.KausfHex);
        Assert.NotEmpty(aka.KseafHex);
        Assert.NotEmpty(aka.KamfHex);
        Assert.NotEmpty(aka.KnasEncHex);
        Assert.NotEmpty(aka.KnasIntHex);
        Assert.NotEmpty(aka.KgnbHex);
        Assert.NotEmpty(aka.KrrcEncHex);
        Assert.NotEmpty(aka.KrrcIntHex);
        Assert.NotEmpty(aka.KupEncHex);
        Assert.NotEmpty(aka.KupIntHex);
        Assert.Equal(10, aka.KeyNodes.Count);
    }

    [Fact]
    public void SecurityAnalysisService_RunsAllTestVectors_WithAllPassing()
    {
        var service = new SecurityAnalysisService();
        var vectors = service.RunAllTestVectorVerifications();

        Assert.NotEmpty(vectors);
        Assert.All(vectors, v =>
        {
            Assert.True(v.IsPass, $"Test vector failed: {v.Title}");
        });
    }
}
