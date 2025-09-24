namespace Lg2.Native
{
    public unsafe partial struct git_status_entry
    {
        public git_status_t status;

        public git_diff_delta* head_to_index;

        public git_diff_delta* index_to_workdir;
    }
}
