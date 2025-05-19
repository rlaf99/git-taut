using System.Runtime.CompilerServices;

namespace Lg2.Native
{
    public partial struct git_oid
    {
        [NativeTypeName("unsigned char[20]")]
        public _id_e__FixedBuffer id;

        [InlineArray(20)]
        public partial struct _id_e__FixedBuffer
        {
            public byte e0;
        }
    }
}
