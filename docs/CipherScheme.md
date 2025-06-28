
## Cipher Scheme

*git-taut* uses AES CBC mode with 256-bit key, and the padding mode is PKCS7.

The cipher key is derived for each target from a keybase using PKBDF2 with an iteration count of 64007.
The PKBDF2 function takes 12-byte random data specific for that target as salt.
The keybase is a SHA-256 hash of the password provided by user.

Before encryption, a content header is added to to the plain text, and consists of the following fields

- Scramble data (4 bytes)
  - Currently, the first byte of the scramble data is used to store the primary header flags.
  - The remaining bytes are randomly filled.
- Output length (4 bytes)
  - The length of the plain text (before encompression) is stored here in big-endian format.
- Extra payload (variable)
  - If present, the length is stored in a byte and must not exceed 255.
  - Used for delta-encoding, to store the git object id of the delta base.

An overall header is added to the encryption output, and consists of the following fields

- Tautened mark (4 bytes)
  - Used to identify the content, fixed to `[0x0, 0x9, 0x9, 0xa1]`.
- Reserved data (4 bytes)
  - Filled with zeroes by default.
- Initialization Vector (16 bytes)
  - This is the IV used for encryption/decryption.
  - First 12 bytes are randomly generated.
  - Remaining 4 bytes are derived from the keybase using HKDF using the preceding 12 bytes as salt.