using CellScope.Application.DTOs;

namespace CellScope.Infrastructure.Security;

/// <summary>
/// Educational step-by-step FIPS-197 compliant AES-128 execution engine and trace generator.
/// </summary>
public static class Aes128Tracer
{
    private static readonly byte[] SBox = new byte[256]
    {
        0x63, 0x7c, 0x77, 0x7b, 0xf2, 0x6b, 0x6f, 0xc5, 0x30, 0x01, 0x67, 0x2b, 0xfe, 0xd7, 0xab, 0x76,
        0xca, 0x82, 0xc9, 0x7d, 0xfa, 0x59, 0x47, 0xf0, 0xad, 0xd4, 0xa2, 0xaf, 0x9c, 0xa4, 0x72, 0xc0,
        0xb7, 0xfd, 0x93, 0x26, 0x36, 0x3f, 0xf7, 0xcc, 0x34, 0xa5, 0xe5, 0xf1, 0x71, 0xd8, 0x31, 0x15,
        0x04, 0xc7, 0x23, 0xc3, 0x18, 0x96, 0x05, 0x9a, 0x07, 0x12, 0x80, 0xe2, 0xeb, 0x27, 0xb2, 0x75,
        0x09, 0x83, 0x2c, 0x1a, 0x1b, 0x6e, 0x5a, 0xa0, 0x52, 0x3b, 0xd6, 0xb3, 0x29, 0xe3, 0x2f, 0x84,
        0x53, 0xd1, 0x00, 0xed, 0x20, 0xfc, 0xb1, 0x5b, 0x6a, 0xcb, 0xbe, 0x39, 0x4a, 0x4c, 0x58, 0xcf,
        0xd0, 0xef, 0xaa, 0xfb, 0x43, 0x4d, 0x33, 0x85, 0x45, 0xf9, 0x02, 0x7f, 0x50, 0x3c, 0x9f, 0xa8,
        0x51, 0xa3, 0x40, 0x8f, 0x92, 0x9d, 0x38, 0xf5, 0xbc, 0xb6, 0xda, 0x21, 0x10, 0xff, 0xf3, 0xd2,
        0xcd, 0x0c, 0x13, 0xec, 0x5f, 0x97, 0x44, 0x17, 0xc4, 0xa7, 0x7e, 0x3d, 0x64, 0x5d, 0x19, 0x73,
        0x60, 0x81, 0x4f, 0xdc, 0x22, 0x2a, 0x90, 0x88, 0x46, 0xee, 0xb8, 0x14, 0xde, 0x5e, 0x0b, 0xdb,
        0xe0, 0x32, 0x3a, 0x0a, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0xd3, 0xac, 0x62, 0x91, 0x95, 0xe4, 0x79,
        0xe7, 0xc8, 0x37, 0x6d, 0x8d, 0xd5, 0x4e, 0xa9, 0x6c, 0x56, 0xf4, 0xea, 0x65, 0x7a, 0xae, 0x08,
        0xba, 0x78, 0x25, 0x2e, 0x1c, 0xa6, 0xb4, 0xc6, 0xe8, 0xdd, 0x74, 0x1f, 0x4b, 0xbd, 0x8b, 0x8a,
        0x70, 0x3e, 0xb5, 0x66, 0x48, 0x03, 0xf6, 0x0e, 0x61, 0x35, 0x57, 0xb9, 0x86, 0xc1, 0x1d, 0x9e,
        0xe1, 0xf8, 0x98, 0x11, 0x69, 0xd9, 0x8e, 0x94, 0x9b, 0x1e, 0x87, 0xe9, 0xce, 0x55, 0x28, 0xdf,
        0x8c, 0xa1, 0x89, 0x0d, 0xbf, 0xe6, 0x42, 0x68, 0x41, 0x99, 0x2d, 0x0f, 0xb0, 0x54, 0xbb, 0x16
    };

    private static readonly byte[] Rcon = new byte[11]
    {
        0x00, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1B, 0x36
    };

    public static AesTraceResultDto Trace(byte[] key, byte[] plaintext)
    {
        if (key == null || key.Length != 16)
            throw new ArgumentException("AES-128 requires exactly a 16-byte (128-bit) key.", nameof(key));
        if (plaintext == null || plaintext.Length != 16)
            throw new ArgumentException("AES-128 requires exactly a 16-byte (128-bit) plaintext block.", nameof(plaintext));

        var result = new AesTraceResultDto
        {
            PlaintextHex = Convert.ToHexString(plaintext).ToLowerInvariant(),
            KeyHex = Convert.ToHexString(key).ToLowerInvariant()
        };

        // 1. Key Expansion (FIPS-197 Section 5.2)
        var roundKeys = ExpandKey(key);
        for (int r = 0; r <= 10; r++)
        {
            result.ExpandedRoundKeysHex.Add(StateToHex(roundKeys[r]));
        }

        // 2. Initialize State Matrix (column-major order)
        var state = BytesToState(plaintext);
        int stepIdx = 0;

        // Round 0: Initial AddRoundKey
        var round0Key = roundKeys[0];
        var preState = CloneState(state);
        state = AddRoundKey(state, round0Key);
        result.Steps.Add(new AesStepTraceDto
        {
            StepIndex = stepIdx++,
            RoundNumber = 0,
            StepName = "Round 0: Initial AddRoundKey",
            OperationType = "AddRoundKey",
            Description = "Bitwise XOR between initial 128-bit plaintext block and Round Key K_0 (w[0..3]).",
            SpecificationClause = "FIPS-197 Clause 5.1 / 3GPP TS 33.501",
            InputState = preState,
            OutputState = CloneState(state),
            RoundKey = round0Key,
            ChangedCells = GetDiffCells(preState, state)
        });

        // Rounds 1 to 9 (Standard 4 Transformations)
        for (int round = 1; round <= 9; round++)
        {
            // Step A: SubBytes
            var subInput = CloneState(state);
            state = SubBytes(state);
            result.Steps.Add(new AesStepTraceDto
            {
                StepIndex = stepIdx++,
                RoundNumber = round,
                StepName = $"Round {round}: SubBytes",
                OperationType = "SubBytes",
                Description = "Non-linear byte substitution using the Rijndael S-Box (multiplicative inverse in GF(2^8) + affine transform).",
                SpecificationClause = "FIPS-197 Clause 5.1.1",
                InputState = subInput,
                OutputState = CloneState(state),
                RoundKey = null,
                ChangedCells = GetDiffCells(subInput, state)
            });

            // Step B: ShiftRows
            var shiftInput = CloneState(state);
            state = ShiftRows(state);
            result.Steps.Add(new AesStepTraceDto
            {
                StepIndex = stepIdx++,
                RoundNumber = round,
                StepName = $"Round {round}: ShiftRows",
                OperationType = "ShiftRows",
                Description = "Cyclic byte transposition on rows (Row 0: shift 0, Row 1: shift 1, Row 2: shift 2, Row 3: shift 3).",
                SpecificationClause = "FIPS-197 Clause 5.1.2",
                InputState = shiftInput,
                OutputState = CloneState(state),
                RoundKey = null,
                ChangedCells = GetDiffCells(shiftInput, state)
            });

            // Step C: MixColumns
            var mixInput = CloneState(state);
            state = MixColumns(state);
            result.Steps.Add(new AesStepTraceDto
            {
                StepIndex = stepIdx++,
                RoundNumber = round,
                StepName = $"Round {round}: MixColumns",
                OperationType = "MixColumns",
                Description = "Linear diffusion mixing column bytes via matrix multiplication in GF(2^8) with polynomial {03}x^3 + {01}x^2 + {01}x + {02}.",
                SpecificationClause = "FIPS-197 Clause 5.1.3",
                InputState = mixInput,
                OutputState = CloneState(state),
                RoundKey = null,
                ChangedCells = GetDiffCells(mixInput, state)
            });

            // Step D: AddRoundKey
            var rKey = roundKeys[round];
            var addInput = CloneState(state);
            state = AddRoundKey(state, rKey);
            result.Steps.Add(new AesStepTraceDto
            {
                StepIndex = stepIdx++,
                RoundNumber = round,
                StepName = $"Round {round}: AddRoundKey",
                OperationType = "AddRoundKey",
                Description = $"Bitwise XOR of state columns with expanded 128-bit Round Key K_{round} (w[{4 * round}..{4 * round + 3}]).",
                SpecificationClause = "FIPS-197 Clause 5.1.4",
                InputState = addInput,
                OutputState = CloneState(state),
                RoundKey = rKey,
                ChangedCells = GetDiffCells(addInput, state)
            });
        }

        // Round 10: Final Round (SubBytes, ShiftRows, AddRoundKey - MixColumns omitted)
        {
            int round = 10;

            // Step A: SubBytes
            var subInput = CloneState(state);
            state = SubBytes(state);
            result.Steps.Add(new AesStepTraceDto
            {
                StepIndex = stepIdx++,
                RoundNumber = round,
                StepName = $"Round {round}: SubBytes (Final)",
                OperationType = "SubBytes",
                Description = "Final non-linear S-Box substitution.",
                SpecificationClause = "FIPS-197 Clause 5.1.1",
                InputState = subInput,
                OutputState = CloneState(state),
                RoundKey = null,
                ChangedCells = GetDiffCells(subInput, state)
            });

            // Step B: ShiftRows
            var shiftInput = CloneState(state);
            state = ShiftRows(state);
            result.Steps.Add(new AesStepTraceDto
            {
                StepIndex = stepIdx++,
                RoundNumber = round,
                StepName = $"Round {round}: ShiftRows (Final)",
                OperationType = "ShiftRows",
                Description = "Final row transposition.",
                SpecificationClause = "FIPS-197 Clause 5.1.2",
                InputState = shiftInput,
                OutputState = CloneState(state),
                RoundKey = null,
                ChangedCells = GetDiffCells(shiftInput, state)
            });

            // Step C: AddRoundKey (MixColumns skipped in final round)
            var rKey = roundKeys[10];
            var addInput = CloneState(state);
            state = AddRoundKey(state, rKey);
            result.Steps.Add(new AesStepTraceDto
            {
                StepIndex = stepIdx++,
                RoundNumber = round,
                StepName = $"Round {round}: AddRoundKey (Final Ciphertext)",
                OperationType = "AddRoundKey",
                Description = "Final AddRoundKey with K_10 yielding the 128-bit ciphertext block.",
                SpecificationClause = "FIPS-197 Clause 5.1.4",
                InputState = addInput,
                OutputState = CloneState(state),
                RoundKey = rKey,
                ChangedCells = GetDiffCells(addInput, state)
            });
        }

        result.CiphertextHex = StateToHex(state);
        return result;
    }

    /// <summary>
    /// Executes standard 1-block AES-128 encryption directly for high-throughput crypto calls (e.g. in MILENAGE).
    /// </summary>
    public static byte[] EncryptBlock(byte[] key, byte[] input)
    {
        var roundKeys = ExpandKey(key);
        var state = BytesToState(input);

        state = AddRoundKey(state, roundKeys[0]);

        for (int r = 1; r <= 9; r++)
        {
            state = SubBytes(state);
            state = ShiftRows(state);
            state = MixColumns(state);
            state = AddRoundKey(state, roundKeys[r]);
        }

        state = SubBytes(state);
        state = ShiftRows(state);
        state = AddRoundKey(state, roundKeys[10]);

        return StateToBytes(state);
    }

    // --- Core AES Transformations ---

    private static byte[][] SubBytes(byte[][] s)
    {
        var next = new byte[4][];
        for (int r = 0; r < 4; r++)
        {
            next[r] = new byte[4];
            for (int c = 0; c < 4; c++)
            {
                next[r][c] = SBox[s[r][c]];
            }
        }
        return next;
    }

    private static byte[][] ShiftRows(byte[][] s)
    {
        var next = new byte[4][];
        for (int r = 0; r < 4; r++)
            next[r] = new byte[4];

        // Row 0: shift 0
        next[0][0] = s[0][0]; next[0][1] = s[0][1]; next[0][2] = s[0][2]; next[0][3] = s[0][3];
        // Row 1: shift 1
        next[1][0] = s[1][1]; next[1][1] = s[1][2]; next[1][2] = s[1][3]; next[1][3] = s[1][0];
        // Row 2: shift 2
        next[2][0] = s[2][2]; next[2][1] = s[2][3]; next[2][2] = s[2][0]; next[2][3] = s[2][1];
        // Row 3: shift 3
        next[3][0] = s[3][3]; next[3][1] = s[3][0]; next[3][2] = s[3][1]; next[3][3] = s[3][2];

        return next;
    }

    private static byte[][] MixColumns(byte[][] s)
    {
        var next = new byte[4][];
        for (int r = 0; r < 4; r++) next[r] = new byte[4];

        for (int c = 0; c < 4; c++)
        {
            byte a0 = s[0][c], a1 = s[1][c], a2 = s[2][c], a3 = s[3][c];
            next[0][c] = (byte)(Gmul(0x02, a0) ^ Gmul(0x03, a1) ^ a2 ^ a3);
            next[1][c] = (byte)(a0 ^ Gmul(0x02, a1) ^ Gmul(0x03, a2) ^ a3);
            next[2][c] = (byte)(a0 ^ a1 ^ Gmul(0x02, a2) ^ Gmul(0x03, a3));
            next[3][c] = (byte)(Gmul(0x03, a0) ^ a1 ^ a2 ^ Gmul(0x02, a3));
        }

        return next;
    }

    private static byte Gmul(byte a, byte b)
    {
        byte p = 0;
        for (int counter = 0; counter < 8; counter++)
        {
            if ((b & 1) != 0) p ^= a;
            bool hiBitSet = (a & 0x80) != 0;
            a <<= 1;
            if (hiBitSet) a ^= 0x1b; // Irreducible polynomial x^8 + x^4 + x^3 + x + 1
            b >>= 1;
        }
        return p;
    }

    private static byte[][] AddRoundKey(byte[][] s, byte[][] k)
    {
        var next = new byte[4][];
        for (int r = 0; r < 4; r++)
        {
            next[r] = new byte[4];
            for (int c = 0; c < 4; c++)
            {
                next[r][c] = (byte)(s[r][c] ^ k[r][c]);
            }
        }
        return next;
    }

    // --- Key Expansion ---

    private static byte[][][] ExpandKey(byte[] key)
    {
        // 44 words (each word is 4 bytes)
        var w = new byte[44][];
        for (int i = 0; i < 4; i++)
        {
            w[i] = new byte[4]
            {
                key[4 * i],
                key[4 * i + 1],
                key[4 * i + 2],
                key[4 * i + 3]
            };
        }

        for (int i = 4; i < 44; i++)
        {
            var temp = (byte[])w[i - 1].Clone();
            if (i % 4 == 0)
            {
                // RotWord
                byte t = temp[0];
                temp[0] = temp[1];
                temp[1] = temp[2];
                temp[2] = temp[3];
                temp[3] = t;

                // SubWord
                temp[0] = SBox[temp[0]];
                temp[1] = SBox[temp[1]];
                temp[2] = SBox[temp[2]];
                temp[3] = SBox[temp[3]];

                // XOR Rcon
                temp[0] ^= Rcon[i / 4];
            }

            w[i] = new byte[4]
            {
                (byte)(w[i - 4][0] ^ temp[0]),
                (byte)(w[i - 4][1] ^ temp[1]),
                (byte)(w[i - 4][2] ^ temp[2]),
                (byte)(w[i - 4][3] ^ temp[3])
            };
        }

        // Group into 11 4x4 round keys (column-major)
        var roundKeys = new byte[11][][];
        for (int r = 0; r <= 10; r++)
        {
            roundKeys[r] = new byte[4][];
            for (int row = 0; row < 4; row++)
            {
                roundKeys[r][row] = new byte[4];
                for (int col = 0; col < 4; col++)
                {
                    roundKeys[r][row][col] = w[4 * r + col][row];
                }
            }
        }

        return roundKeys;
    }

    // --- Helper Utilities ---

    public static byte[][] BytesToState(byte[] bytes)
    {
        var s = new byte[4][];
        for (int r = 0; r < 4; r++)
        {
            s[r] = new byte[4];
            for (int c = 0; c < 4; c++)
            {
                s[r][c] = bytes[r + 4 * c];
            }
        }
        return s;
    }

    public static byte[] StateToBytes(byte[][] state)
    {
        var bytes = new byte[16];
        int idx = 0;
        for (int c = 0; c < 4; c++)
        {
            for (int r = 0; r < 4; r++)
            {
                bytes[idx++] = state[r][c];
            }
        }
        return bytes;
    }

    public static string StateToHex(byte[][] state)
    {
        return Convert.ToHexString(StateToBytes(state)).ToLowerInvariant();
    }

    private static byte[][] CloneState(byte[][] state)
    {
        var clone = new byte[4][];
        for (int r = 0; r < 4; r++)
        {
            clone[r] = (byte[])state[r].Clone();
        }
        return clone;
    }

    private static List<(int Row, int Col)> GetDiffCells(byte[][] before, byte[][] after)
    {
        var diff = new List<(int Row, int Col)>();
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if (before[r][c] != after[r][c])
                {
                    diff.Add((r, c));
                }
            }
        }
        return diff;
    }
}
