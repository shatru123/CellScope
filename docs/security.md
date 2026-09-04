# Security Architecture

CellScope follows security best practices:

---

1. **Device Pairing Handshake**:
   - Randomly generated 8-character pairing codes (e.g. `ABCD-1234`) using `RandomNumberGenerator`.
   - Temporary expiration and cryptographic token issuance.
2. **Password Security**:
   - Industry-standard PBKDF2 hashing with SHA-256, 100,000 iterations, and cryptographic 128-bit random salt.
3. **No Hard-Coded Credentials**:
   - Secrets and connection strings are configured strictly through environment variables.
4. **CORS & Input Validation**:
   - Safe input sanitization, coordinate bounds validation, and CORS policies.
