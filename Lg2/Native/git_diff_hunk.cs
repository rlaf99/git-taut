using System.Runtime.CompilerServices;

namespace Lg2.Native
{
    public partial struct git_diff_hunk
    {
        public int old_start;

        public int old_lines;

        public int new_start;

        public int new_lines;

        [NativeTypeName("size_t")]
        public nuint header_len;

        [NativeTypeName("char[128]")]
        public _header_e__FixedBuffer header;

        [InlineArray(128)]
        public partial struct _header_e__FixedBuffer
        {
            public sbyte e0;
        }
    }
}
