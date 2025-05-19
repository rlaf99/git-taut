using System;
using System.Runtime.InteropServices;

namespace Lg2.Native
{
    public static unsafe partial class LibGit2Exports
    {
        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_libgit2_version(int* major, int* minor, int* rev);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_libgit2_prerelease();

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_libgit2_features();

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_libgit2_opts(int option, __arglist);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_buf_dispose(git_buf* buffer);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_fromstr(git_oid* @out, [NativeTypeName("const char *")] sbyte* str);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_fromstrp(git_oid* @out, [NativeTypeName("const char *")] sbyte* str);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_fromstrn(git_oid* @out, [NativeTypeName("const char *")] sbyte* str, [NativeTypeName("size_t")] nuint length);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_fromraw(git_oid* @out, [NativeTypeName("const unsigned char *")] byte* raw);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_fmt([NativeTypeName("char *")] sbyte* @out, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_nfmt([NativeTypeName("char *")] sbyte* @out, [NativeTypeName("size_t")] nuint n, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_pathfmt([NativeTypeName("char *")] sbyte* @out, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* git_oid_tostr_s([NativeTypeName("const git_oid *")] git_oid* oid);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern sbyte* git_oid_tostr([NativeTypeName("char *")] sbyte* @out, [NativeTypeName("size_t")] nuint n, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_cpy(git_oid* @out, [NativeTypeName("const git_oid *")] git_oid* src);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_cmp([NativeTypeName("const git_oid *")] git_oid* a, [NativeTypeName("const git_oid *")] git_oid* b);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_equal([NativeTypeName("const git_oid *")] git_oid* a, [NativeTypeName("const git_oid *")] git_oid* b);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_ncmp([NativeTypeName("const git_oid *")] git_oid* a, [NativeTypeName("const git_oid *")] git_oid* b, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_streq([NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("const char *")] sbyte* str);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_strcmp([NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("const char *")] sbyte* str);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_is_zero([NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_oid_shorten* git_oid_shorten_new([NativeTypeName("size_t")] nuint min_length);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_oid_shorten_add(git_oid_shorten* os, [NativeTypeName("const char *")] sbyte* text_id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_oid_shorten_free(git_oid_shorten* os);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_lookup(git_commit** commit, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_lookup_prefix(git_commit** commit, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_commit_free(git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_commit_id([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_repository* git_commit_owner([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_commit_message_encoding([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_commit_message([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_commit_message_raw([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_commit_summary(git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_commit_body(git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("git_time_t")]
        public static extern long git_commit_time([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_time_offset([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_signature *")]
        public static extern git_signature* git_commit_committer([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_signature *")]
        public static extern git_signature* git_commit_author([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_committer_with_mailmap(git_signature** @out, [NativeTypeName("const git_commit *")] git_commit* commit, [NativeTypeName("const git_mailmap *")] git_mailmap* mailmap);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_author_with_mailmap(git_signature** @out, [NativeTypeName("const git_commit *")] git_commit* commit, [NativeTypeName("const git_mailmap *")] git_mailmap* mailmap);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_commit_raw_header([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_tree(git_tree** tree_out, [NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_commit_tree_id([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint git_commit_parentcount([NativeTypeName("const git_commit *")] git_commit* commit);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_parent(git_commit** @out, [NativeTypeName("const git_commit *")] git_commit* commit, [NativeTypeName("unsigned int")] uint n);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_commit_parent_id([NativeTypeName("const git_commit *")] git_commit* commit, [NativeTypeName("unsigned int")] uint n);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_nth_gen_ancestor(git_commit** ancestor, [NativeTypeName("const git_commit *")] git_commit* commit, [NativeTypeName("unsigned int")] uint n);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_header_field(git_buf* @out, [NativeTypeName("const git_commit *")] git_commit* commit, [NativeTypeName("const char *")] sbyte* field);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_extract_signature(git_buf* signature, git_buf* signed_data, git_repository* repo, git_oid* commit_id, [NativeTypeName("const char *")] sbyte* field);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_create(git_oid* id, git_repository* repo, [NativeTypeName("const char *")] sbyte* update_ref, [NativeTypeName("const git_signature *")] git_signature* author, [NativeTypeName("const git_signature *")] git_signature* committer, [NativeTypeName("const char *")] sbyte* message_encoding, [NativeTypeName("const char *")] sbyte* message, [NativeTypeName("const git_tree *")] git_tree* tree, [NativeTypeName("size_t")] nuint parent_count, [NativeTypeName("const git_commit *[]")] git_commit** parents);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_create_v(git_oid* id, git_repository* repo, [NativeTypeName("const char *")] sbyte* update_ref, [NativeTypeName("const git_signature *")] git_signature* author, [NativeTypeName("const git_signature *")] git_signature* committer, [NativeTypeName("const char *")] sbyte* message_encoding, [NativeTypeName("const char *")] sbyte* message, [NativeTypeName("const git_tree *")] git_tree* tree, [NativeTypeName("size_t")] nuint parent_count, __arglist);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_create_from_stage(git_oid* id, git_repository* repo, [NativeTypeName("const char *")] sbyte* message, [NativeTypeName("const git_commit_create_options *")] git_commit_create_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_amend(git_oid* id, [NativeTypeName("const git_commit *")] git_commit* commit_to_amend, [NativeTypeName("const char *")] sbyte* update_ref, [NativeTypeName("const git_signature *")] git_signature* author, [NativeTypeName("const git_signature *")] git_signature* committer, [NativeTypeName("const char *")] sbyte* message_encoding, [NativeTypeName("const char *")] sbyte* message, [NativeTypeName("const git_tree *")] git_tree* tree);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_create_buffer(git_buf* @out, git_repository* repo, [NativeTypeName("const git_signature *")] git_signature* author, [NativeTypeName("const git_signature *")] git_signature* committer, [NativeTypeName("const char *")] sbyte* message_encoding, [NativeTypeName("const char *")] sbyte* message, [NativeTypeName("const git_tree *")] git_tree* tree, [NativeTypeName("size_t")] nuint parent_count, [NativeTypeName("const git_commit *[]")] git_commit** parents);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_create_with_signature(git_oid* @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* commit_content, [NativeTypeName("const char *")] sbyte* signature, [NativeTypeName("const char *")] sbyte* signature_field);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_commit_dup(git_commit** @out, git_commit* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_commitarray_dispose(git_commitarray* array);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_open(git_repository** @out, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_open_from_worktree(git_repository** @out, git_worktree* wt);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_wrap_odb(git_repository** @out, git_odb* odb);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_discover(git_buf* @out, [NativeTypeName("const char *")] sbyte* start_path, int across_fs, [NativeTypeName("const char *")] sbyte* ceiling_dirs);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_open_ext(git_repository** @out, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("unsigned int")] uint flags, [NativeTypeName("const char *")] sbyte* ceiling_dirs);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_open_bare(git_repository** @out, [NativeTypeName("const char *")] sbyte* bare_path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_repository_free(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_init(git_repository** @out, [NativeTypeName("const char *")] sbyte* path, [NativeTypeName("unsigned int")] uint is_bare);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_init_options_init(git_repository_init_options* opts, [NativeTypeName("unsigned int")] uint version);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_init_ext(git_repository** @out, [NativeTypeName("const char *")] sbyte* repo_path, git_repository_init_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_head(git_reference** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_head_for_worktree(git_reference** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_head_detached(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_head_detached_for_worktree(git_repository* repo, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_head_unborn(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_is_empty(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_item_path(git_buf* @out, [NativeTypeName("const git_repository *")] git_repository* repo, git_repository_item_t item);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_repository_path([NativeTypeName("const git_repository *")] git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_repository_workdir([NativeTypeName("const git_repository *")] git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_repository_commondir([NativeTypeName("const git_repository *")] git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_set_workdir(git_repository* repo, [NativeTypeName("const char *")] sbyte* workdir, int update_gitlink);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_is_bare([NativeTypeName("const git_repository *")] git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_is_worktree([NativeTypeName("const git_repository *")] git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_config(git_config** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_config_snapshot(git_config** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_odb(git_odb** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_refdb(git_refdb** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_index(git_index** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_message(git_buf* @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_message_remove(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_state_cleanup(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_fetchhead_foreach(git_repository* repo, [NativeTypeName("git_repository_fetchhead_foreach_cb")] delegate* unmanaged[Cdecl]<sbyte*, sbyte*, git_oid*, uint, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_mergehead_foreach(git_repository* repo, [NativeTypeName("git_repository_mergehead_foreach_cb")] delegate* unmanaged[Cdecl]<git_oid*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_hashfile(git_oid* @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* path, git_object_t type, [NativeTypeName("const char *")] sbyte* as_path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_set_head(git_repository* repo, [NativeTypeName("const char *")] sbyte* refname);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_set_head_detached(git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* committish);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_set_head_detached_from_annotated(git_repository* repo, [NativeTypeName("const git_annotated_commit *")] git_annotated_commit* committish);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_detach_head(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_state(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_set_namespace(git_repository* repo, [NativeTypeName("const char *")] sbyte* nmspace);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_repository_get_namespace(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_is_shallow(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_ident([NativeTypeName("const char **")] sbyte** name, [NativeTypeName("const char **")] sbyte** email, [NativeTypeName("const git_repository *")] git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_set_ident(git_repository* repo, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* email);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_oid_t git_repository_oid_type(git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_repository_commit_parents(git_commitarray* commits, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_libgit2_init();

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_libgit2_shutdown();

        [NativeTypeName("#define GIT_WIN32 1")]
        public const int GIT_WIN32 = 1;

        [NativeTypeName("#define GIT_PATH_LIST_SEPARATOR ';'")]
        public const int GIT_PATH_LIST_SEPARATOR = (sbyte)(';');

        [NativeTypeName("#define GIT_PATH_MAX 4096")]
        public const int GIT_PATH_MAX = 4096;

        [NativeTypeName("#define GIT_OID_SHA1_SIZE 20")]
        public const int GIT_OID_SHA1_SIZE = 20;

        [NativeTypeName("#define GIT_OID_SHA1_HEXSIZE (GIT_OID_SHA1_SIZE * 2)")]
        public const int GIT_OID_SHA1_HEXSIZE = (20 * 2);

        [NativeTypeName("#define GIT_OID_SHA1_HEXZERO \"0000000000000000000000000000000000000000\"")]
        public static ReadOnlySpan<byte> GIT_OID_SHA1_HEXZERO => "0000000000000000000000000000000000000000"u8;

        [NativeTypeName("#define GIT_OID_MAX_SIZE GIT_OID_SHA1_SIZE")]
        public const int GIT_OID_MAX_SIZE = 20;

        [NativeTypeName("#define GIT_OID_MAX_HEXSIZE GIT_OID_SHA1_HEXSIZE")]
        public const int GIT_OID_MAX_HEXSIZE = (20 * 2);

        [NativeTypeName("#define GIT_OID_MINPREFIXLEN 4")]
        public const int GIT_OID_MINPREFIXLEN = 4;

        [NativeTypeName("#define GIT_COMMIT_CREATE_OPTIONS_VERSION 1")]
        public const int GIT_COMMIT_CREATE_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_REPOSITORY_INIT_OPTIONS_VERSION 1")]
        public const int GIT_REPOSITORY_INIT_OPTIONS_VERSION = 1;

        [NativeTypeName("#define LIBGIT2_VERSION \"1.8.4\"")]
        public static ReadOnlySpan<byte> LIBGIT2_VERSION => "1.8.4"u8;

        [NativeTypeName("#define LIBGIT2_VER_MAJOR 1")]
        public const int LIBGIT2_VER_MAJOR = 1;

        [NativeTypeName("#define LIBGIT2_VER_MINOR 8")]
        public const int LIBGIT2_VER_MINOR = 8;

        [NativeTypeName("#define LIBGIT2_VER_REVISION 4")]
        public const int LIBGIT2_VER_REVISION = 4;

        [NativeTypeName("#define LIBGIT2_VER_PATCH 0")]
        public const int LIBGIT2_VER_PATCH = 0;

        [NativeTypeName("#define LIBGIT2_VER_PRERELEASE NULL")]
        public static readonly void* LIBGIT2_VER_PRERELEASE = null;

        [NativeTypeName("#define LIBGIT2_SOVERSION \"1.8\"")]
        public static ReadOnlySpan<byte> LIBGIT2_SOVERSION => "1.8"u8;
    }
}
