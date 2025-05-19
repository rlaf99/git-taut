namespace Lg2.Native
{
    public unsafe partial struct git_commitarray
    {
        [NativeTypeName("git_commit *const *")]
        public git_commit** commits;

        [NativeTypeName("size_t")]
        public nuint count;
    }
}
