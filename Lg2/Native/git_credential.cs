namespace Lg2.Native
{
    public unsafe partial struct git_credential
    {
        public git_credential_t credtype;

        [NativeTypeName("void (*)(git_credential *) __attribute__((cdecl))")]
        public delegate* unmanaged[Cdecl]<git_credential*, void> free;
    }
}
