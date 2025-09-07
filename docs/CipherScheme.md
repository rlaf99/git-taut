
## Cipher Scheme

*git-taut* uses AES CBC mode with 256-bit key, and the padding mode is PKCS7.

At first, a crude key is derived from user password using PBKDF2 with an iteration count of 64000.
If a user name is present in the credential, then the user name is used as salt for the crude key derivation.

Then, upon encrypting individual files or file names (including directory names), further iteration of PBKDF2 is applied to derive the cipher key for that target.
If it is a file, then 20 random bytes are generated, first 16 bytes of which is used as IV and the last 16 bytes of which is used as salt for the cipher key derivation.
If it is a name, then no random bytes are generated, instead the object hash (assuming 20 bytes long) assosciated with the name is used. 
Similarly, first 16 bytes of the hash is used as IV and the last 16 bytes of the hash is used as salt for the cipher key derivation.

All PBKDF2 processing uses SHA-256 as the hash algorithm.

Before encrypting a file, a content header is added to to the plain text, and consists of the following fields

- Scramble data (4 bytes)
  - Currently, the first byte of the scramble data is used to store the primary header flags.
  - The remaining bytes are randomly filled.
- Output length (4 bytes)
  - The length of the plain text (before encompression) is stored here in big-endian format.
- Extra payload (variable)
  - If present, the length is stored in a byte, so it must not exceed 255 bytes.
  - Used for delta-encoding, to store the git object id of the delta base.

An overall header is prepended to the encryption output, and consists of the following fields

- Tautened mark (4 bytes)
  - Used to identify the content, fixed to `[0x0, 0x9, 0x9, 0xa1]`.
- Reserved data (4 bytes)
  - Filled with zeroes by default.
- Random data (20 bytes)
  - First 16 bytes are used as the IV for encryption/decryption.
  - Last 16 bytes are used as salt for cipher key derivation.
