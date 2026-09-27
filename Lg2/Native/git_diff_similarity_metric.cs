namespace Lg2.Native
{
    public unsafe partial struct git_diff_similarity_metric
    {
        [NativeTypeName("int (*)(void **, const git_diff_file *, const char *, void *)")]
        public delegate* unmanaged[Cdecl]<void**, git_diff_file*, sbyte*, void*, int> file_signature;

        [NativeTypeName("int (*)(void **, const git_diff_file *, const char *, size_t, void *)")]
        public delegate* unmanaged[Cdecl]<void**, git_diff_file*, sbyte*, nuint, void*, int> buffer_signature;

        [NativeTypeName("void (*)(void *, void *)")]
        public delegate* unmanaged[Cdecl]<void*, void*, void> free_signature;

        [NativeTypeName("int (*)(int *, void *, void *, void *)")]
        public delegate* unmanaged[Cdecl]<int*, void*, void*, void*, int> similarity;

        public void* payload;
    }
}
