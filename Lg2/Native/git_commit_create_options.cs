namespace Lg2.Native
{
    public unsafe partial struct git_commit_create_options
    {
        [NativeTypeName("unsigned int")]
        public uint version;

        [NativeBitfield("allow_empty_commit", offset: 0, length: 1)]
        public uint _bitfield;

        [NativeTypeName("unsigned int : 1")]
        public uint allow_empty_commit
        {
            readonly get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("const git_signature *")]
        public git_signature* author;

        [NativeTypeName("const git_signature *")]
        public git_signature* committer;

        [NativeTypeName("const char *")]
        public sbyte* message_encoding;
    }
}
