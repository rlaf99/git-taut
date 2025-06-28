using System.Runtime.CompilerServices;

namespace Lg2.Native
{
    public unsafe partial struct git_cert_hostkey
    {
        public git_cert parent;

        public git_cert_ssh_t type;

        [NativeTypeName("unsigned char[16]")]
        public _hash_md5_e__FixedBuffer hash_md5;

        [NativeTypeName("unsigned char[20]")]
        public _hash_sha1_e__FixedBuffer hash_sha1;

        [NativeTypeName("unsigned char[32]")]
        public _hash_sha256_e__FixedBuffer hash_sha256;

        public git_cert_ssh_raw_type_t raw_type;

        [NativeTypeName("const char *")]
        public sbyte* hostkey;

        [NativeTypeName("size_t")]
        public nuint hostkey_len;

        [InlineArray(16)]
        public partial struct _hash_md5_e__FixedBuffer
        {
            public byte e0;
        }

        [InlineArray(20)]
        public partial struct _hash_sha1_e__FixedBuffer
        {
            public byte e0;
        }

        [InlineArray(32)]
        public partial struct _hash_sha256_e__FixedBuffer
        {
            public byte e0;
        }
    }
}
