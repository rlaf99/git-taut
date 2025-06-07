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
        public static extern int git_object_lookup(git_object** @object, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id, git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_object_lookup_prefix(git_object** object_out, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("size_t")] nuint len, git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_object_lookup_bypath(git_object** @out, [NativeTypeName("const git_object *")] git_object* treeish, [NativeTypeName("const char *")] sbyte* path, git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_object_id([NativeTypeName("const git_object *")] git_object* obj);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_object_short_id(git_buf* @out, [NativeTypeName("const git_object *")] git_object* obj);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_object_t git_object_type([NativeTypeName("const git_object *")] git_object* obj);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_repository* git_object_owner([NativeTypeName("const git_object *")] git_object* obj);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_object_free(git_object* @object);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_object_type2string(git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_object_t git_object_string2type([NativeTypeName("const char *")] sbyte* str);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_object_typeisloose(git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_object_peel(git_object** peeled, [NativeTypeName("const git_object *")] git_object* @object, git_object_t target_type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_object_dup(git_object** dest, git_object* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_object_rawcontent_is_valid(int* valid, [NativeTypeName("const char *")] sbyte* buf, [NativeTypeName("size_t")] nuint len, git_object_t object_type);

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
        public static extern int git_tree_lookup(git_tree** @out, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tree_lookup_prefix(git_tree** @out, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_tree_free(git_tree* tree);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_tree_id([NativeTypeName("const git_tree *")] git_tree* tree);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_repository* git_tree_owner([NativeTypeName("const git_tree *")] git_tree* tree);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_tree_entrycount([NativeTypeName("const git_tree *")] git_tree* tree);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_tree_entry *")]
        public static extern git_tree_entry* git_tree_entry_byname([NativeTypeName("const git_tree *")] git_tree* tree, [NativeTypeName("const char *")] sbyte* filename);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_tree_entry *")]
        public static extern git_tree_entry* git_tree_entry_byindex([NativeTypeName("const git_tree *")] git_tree* tree, [NativeTypeName("size_t")] nuint idx);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_tree_entry *")]
        public static extern git_tree_entry* git_tree_entry_byid([NativeTypeName("const git_tree *")] git_tree* tree, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tree_entry_bypath(git_tree_entry** @out, [NativeTypeName("const git_tree *")] git_tree* root, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tree_entry_dup(git_tree_entry** dest, [NativeTypeName("const git_tree_entry *")] git_tree_entry* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_tree_entry_free(git_tree_entry* entry);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_tree_entry_name([NativeTypeName("const git_tree_entry *")] git_tree_entry* entry);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_tree_entry_id([NativeTypeName("const git_tree_entry *")] git_tree_entry* entry);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_object_t git_tree_entry_type([NativeTypeName("const git_tree_entry *")] git_tree_entry* entry);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_filemode_t git_tree_entry_filemode([NativeTypeName("const git_tree_entry *")] git_tree_entry* entry);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_filemode_t git_tree_entry_filemode_raw([NativeTypeName("const git_tree_entry *")] git_tree_entry* entry);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tree_entry_cmp([NativeTypeName("const git_tree_entry *")] git_tree_entry* e1, [NativeTypeName("const git_tree_entry *")] git_tree_entry* e2);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tree_entry_to_object(git_object** object_out, git_repository* repo, [NativeTypeName("const git_tree_entry *")] git_tree_entry* entry);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_treebuilder_new(git_treebuilder** @out, git_repository* repo, [NativeTypeName("const git_tree *")] git_tree* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_treebuilder_clear(git_treebuilder* bld);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_treebuilder_entrycount(git_treebuilder* bld);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_treebuilder_free(git_treebuilder* bld);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_tree_entry *")]
        public static extern git_tree_entry* git_treebuilder_get(git_treebuilder* bld, [NativeTypeName("const char *")] sbyte* filename);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_treebuilder_insert([NativeTypeName("const git_tree_entry **")] git_tree_entry** @out, git_treebuilder* bld, [NativeTypeName("const char *")] sbyte* filename, [NativeTypeName("const git_oid *")] git_oid* id, git_filemode_t filemode);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_treebuilder_remove(git_treebuilder* bld, [NativeTypeName("const char *")] sbyte* filename);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_treebuilder_filter(git_treebuilder* bld, [NativeTypeName("git_treebuilder_filter_cb")] delegate* unmanaged[Cdecl]<git_tree_entry*, void*, int> filter, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_treebuilder_write(git_oid* id, git_treebuilder* bld);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tree_walk([NativeTypeName("const git_tree *")] git_tree* tree, git_treewalk_mode mode, [NativeTypeName("git_treewalk_cb")] delegate* unmanaged[Cdecl]<sbyte*, git_tree_entry*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tree_dup(git_tree** @out, git_tree* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tree_create_updated(git_oid* @out, git_repository* repo, git_tree* baseline, [NativeTypeName("size_t")] nuint nupdates, [NativeTypeName("const git_tree_update *")] git_tree_update* updates);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_strarray_dispose(git_strarray* array);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_lookup(git_reference** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_name_to_id(git_oid* @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_dwim(git_reference** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* shorthand);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_symbolic_create_matching(git_reference** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* target, int force, [NativeTypeName("const char *")] sbyte* current_value, [NativeTypeName("const char *")] sbyte* log_message);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_symbolic_create(git_reference** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* target, int force, [NativeTypeName("const char *")] sbyte* log_message);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_create(git_reference** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const git_oid *")] git_oid* id, int force, [NativeTypeName("const char *")] sbyte* log_message);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_create_matching(git_reference** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const git_oid *")] git_oid* id, int force, [NativeTypeName("const git_oid *")] git_oid* current_id, [NativeTypeName("const char *")] sbyte* log_message);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_reference_target([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_reference_target_peel([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_reference_symbolic_target([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_reference_t git_reference_type([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_reference_name([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_resolve(git_reference** @out, [NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_repository* git_reference_owner([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_symbolic_set_target(git_reference** @out, git_reference* @ref, [NativeTypeName("const char *")] sbyte* target, [NativeTypeName("const char *")] sbyte* log_message);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_set_target(git_reference** @out, git_reference* @ref, [NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("const char *")] sbyte* log_message);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_rename(git_reference** new_ref, git_reference* @ref, [NativeTypeName("const char *")] sbyte* new_name, int force, [NativeTypeName("const char *")] sbyte* log_message);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_delete(git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_remove(git_repository* repo, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_list(git_strarray* array, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_foreach(git_repository* repo, [NativeTypeName("git_reference_foreach_cb")] delegate* unmanaged[Cdecl]<git_reference*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_foreach_name(git_repository* repo, [NativeTypeName("git_reference_foreach_name_cb")] delegate* unmanaged[Cdecl]<sbyte*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_dup(git_reference** dest, git_reference* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_reference_free(git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_cmp([NativeTypeName("const git_reference *")] git_reference* ref1, [NativeTypeName("const git_reference *")] git_reference* ref2);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_iterator_new(git_reference_iterator** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_iterator_glob_new(git_reference_iterator** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* glob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_next(git_reference** @out, git_reference_iterator* iter);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_next_name([NativeTypeName("const char **")] sbyte** @out, git_reference_iterator* iter);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_reference_iterator_free(git_reference_iterator* iter);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_foreach_glob(git_repository* repo, [NativeTypeName("const char *")] sbyte* glob, [NativeTypeName("git_reference_foreach_name_cb")] delegate* unmanaged[Cdecl]<sbyte*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_has_log(git_repository* repo, [NativeTypeName("const char *")] sbyte* refname);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_ensure_log(git_repository* repo, [NativeTypeName("const char *")] sbyte* refname);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_is_branch([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_is_remote([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_is_tag([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_is_note([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_normalize_name([NativeTypeName("char *")] sbyte* buffer_out, [NativeTypeName("size_t")] nuint buffer_size, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("unsigned int")] uint flags);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_peel(git_object** @out, [NativeTypeName("const git_reference *")] git_reference* @ref, git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_reference_name_is_valid(int* valid, [NativeTypeName("const char *")] sbyte* refname);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_reference_shorthand([NativeTypeName("const git_reference *")] git_reference* @ref);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_options_init(git_diff_options* opts, [NativeTypeName("unsigned int")] uint version);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_find_options_init(git_diff_find_options* opts, [NativeTypeName("unsigned int")] uint version);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_diff_free(git_diff* diff);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_tree_to_tree(git_diff** diff, git_repository* repo, git_tree* old_tree, git_tree* new_tree, [NativeTypeName("const git_diff_options *")] git_diff_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_tree_to_index(git_diff** diff, git_repository* repo, git_tree* old_tree, git_index* index, [NativeTypeName("const git_diff_options *")] git_diff_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_index_to_workdir(git_diff** diff, git_repository* repo, git_index* index, [NativeTypeName("const git_diff_options *")] git_diff_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_tree_to_workdir(git_diff** diff, git_repository* repo, git_tree* old_tree, [NativeTypeName("const git_diff_options *")] git_diff_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_tree_to_workdir_with_index(git_diff** diff, git_repository* repo, git_tree* old_tree, [NativeTypeName("const git_diff_options *")] git_diff_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_index_to_index(git_diff** diff, git_repository* repo, git_index* old_index, git_index* new_index, [NativeTypeName("const git_diff_options *")] git_diff_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_merge(git_diff* onto, [NativeTypeName("const git_diff *")] git_diff* from);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_find_similar(git_diff* diff, [NativeTypeName("const git_diff_find_options *")] git_diff_find_options* options);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_diff_num_deltas([NativeTypeName("const git_diff *")] git_diff* diff);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_diff_num_deltas_of_type([NativeTypeName("const git_diff *")] git_diff* diff, git_delta_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_diff_delta *")]
        public static extern git_diff_delta* git_diff_get_delta([NativeTypeName("const git_diff *")] git_diff* diff, [NativeTypeName("size_t")] nuint idx);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_is_sorted_icase([NativeTypeName("const git_diff *")] git_diff* diff);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_foreach(git_diff* diff, [NativeTypeName("git_diff_file_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, float, void*, int> file_cb, [NativeTypeName("git_diff_binary_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_binary*, void*, int> binary_cb, [NativeTypeName("git_diff_hunk_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, void*, int> hunk_cb, [NativeTypeName("git_diff_line_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, git_diff_line*, void*, int> line_cb, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char")]
        public static extern sbyte git_diff_status_char(git_delta_t status);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_print(git_diff* diff, git_diff_format_t format, [NativeTypeName("git_diff_line_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, git_diff_line*, void*, int> print_cb, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_to_buf(git_buf* @out, git_diff* diff, git_diff_format_t format);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_blobs([NativeTypeName("const git_blob *")] git_blob* old_blob, [NativeTypeName("const char *")] sbyte* old_as_path, [NativeTypeName("const git_blob *")] git_blob* new_blob, [NativeTypeName("const char *")] sbyte* new_as_path, [NativeTypeName("const git_diff_options *")] git_diff_options* options, [NativeTypeName("git_diff_file_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, float, void*, int> file_cb, [NativeTypeName("git_diff_binary_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_binary*, void*, int> binary_cb, [NativeTypeName("git_diff_hunk_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, void*, int> hunk_cb, [NativeTypeName("git_diff_line_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, git_diff_line*, void*, int> line_cb, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_blob_to_buffer([NativeTypeName("const git_blob *")] git_blob* old_blob, [NativeTypeName("const char *")] sbyte* old_as_path, [NativeTypeName("const char *")] sbyte* buffer, [NativeTypeName("size_t")] nuint buffer_len, [NativeTypeName("const char *")] sbyte* buffer_as_path, [NativeTypeName("const git_diff_options *")] git_diff_options* options, [NativeTypeName("git_diff_file_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, float, void*, int> file_cb, [NativeTypeName("git_diff_binary_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_binary*, void*, int> binary_cb, [NativeTypeName("git_diff_hunk_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, void*, int> hunk_cb, [NativeTypeName("git_diff_line_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, git_diff_line*, void*, int> line_cb, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_buffers([NativeTypeName("const void *")] void* old_buffer, [NativeTypeName("size_t")] nuint old_len, [NativeTypeName("const char *")] sbyte* old_as_path, [NativeTypeName("const void *")] void* new_buffer, [NativeTypeName("size_t")] nuint new_len, [NativeTypeName("const char *")] sbyte* new_as_path, [NativeTypeName("const git_diff_options *")] git_diff_options* options, [NativeTypeName("git_diff_file_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, float, void*, int> file_cb, [NativeTypeName("git_diff_binary_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_binary*, void*, int> binary_cb, [NativeTypeName("git_diff_hunk_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, void*, int> hunk_cb, [NativeTypeName("git_diff_line_cb")] delegate* unmanaged[Cdecl]<git_diff_delta*, git_diff_hunk*, git_diff_line*, void*, int> line_cb, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_from_buffer(git_diff** @out, [NativeTypeName("const char *")] sbyte* content, [NativeTypeName("size_t")] nuint content_len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_get_stats(git_diff_stats** @out, git_diff* diff);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_diff_stats_files_changed([NativeTypeName("const git_diff_stats *")] git_diff_stats* stats);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_diff_stats_insertions([NativeTypeName("const git_diff_stats *")] git_diff_stats* stats);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_diff_stats_deletions([NativeTypeName("const git_diff_stats *")] git_diff_stats* stats);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_stats_to_buf(git_buf* @out, [NativeTypeName("const git_diff_stats *")] git_diff_stats* stats, git_diff_stats_format_t format, [NativeTypeName("size_t")] nuint width);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_diff_stats_free(git_diff_stats* stats);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_patchid_options_init(git_diff_patchid_options* opts, [NativeTypeName("unsigned int")] uint version);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_diff_patchid(git_oid* @out, git_diff* diff, git_diff_patchid_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_lookup(git_blob** blob, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_lookup_prefix(git_blob** blob, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_blob_free(git_blob* blob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_blob_id([NativeTypeName("const git_blob *")] git_blob* blob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_repository* git_blob_owner([NativeTypeName("const git_blob *")] git_blob* blob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* git_blob_rawcontent([NativeTypeName("const git_blob *")] git_blob* blob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("git_object_size_t")]
        public static extern ulong git_blob_rawsize([NativeTypeName("const git_blob *")] git_blob* blob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_filter_options_init(git_blob_filter_options* opts, [NativeTypeName("unsigned int")] uint version);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_filter(git_buf* @out, git_blob* blob, [NativeTypeName("const char *")] sbyte* as_path, git_blob_filter_options* opts);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_create_from_workdir(git_oid* id, git_repository* repo, [NativeTypeName("const char *")] sbyte* relative_path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_create_from_disk(git_oid* id, git_repository* repo, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_create_from_stream(git_writestream** @out, git_repository* repo, [NativeTypeName("const char *")] sbyte* hintpath);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_create_from_stream_commit(git_oid* @out, git_writestream* stream);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_create_from_buffer(git_oid* id, git_repository* repo, [NativeTypeName("const void *")] void* buffer, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_is_binary([NativeTypeName("const git_blob *")] git_blob* blob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_data_is_binary([NativeTypeName("const char *")] sbyte* data, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_blob_dup(git_blob** @out, git_blob* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_refspec_parse(git_refspec** refspec, [NativeTypeName("const char *")] sbyte* input, int is_fetch);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_refspec_free(git_refspec* refspec);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_refspec_src([NativeTypeName("const git_refspec *")] git_refspec* refspec);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_refspec_dst([NativeTypeName("const git_refspec *")] git_refspec* refspec);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_refspec_string([NativeTypeName("const git_refspec *")] git_refspec* refspec);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_refspec_force([NativeTypeName("const git_refspec *")] git_refspec* refspec);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_direction git_refspec_direction([NativeTypeName("const git_refspec *")] git_refspec* spec);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_refspec_src_matches([NativeTypeName("const git_refspec *")] git_refspec* refspec, [NativeTypeName("const char *")] sbyte* refname);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_refspec_dst_matches([NativeTypeName("const git_refspec *")] git_refspec* refspec, [NativeTypeName("const char *")] sbyte* refname);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_refspec_transform(git_buf* @out, [NativeTypeName("const git_refspec *")] git_refspec* spec, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_refspec_rtransform(git_buf* @out, [NativeTypeName("const git_refspec *")] git_refspec* spec, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_config_entry_free(git_config_entry* entry);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_find_global(git_buf* @out);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_find_xdg(git_buf* @out);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_find_system(git_buf* @out);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_find_programdata(git_buf* @out);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_open_default(git_config** @out);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_new(git_config** @out);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_add_file_ondisk(git_config* cfg, [NativeTypeName("const char *")] sbyte* path, git_config_level_t level, [NativeTypeName("const git_repository *")] git_repository* repo, int force);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_open_ondisk(git_config** @out, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_open_level(git_config** @out, [NativeTypeName("const git_config *")] git_config* parent, git_config_level_t level);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_open_global(git_config** @out, git_config* config);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_set_writeorder(git_config* cfg, git_config_level_t* levels, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_snapshot(git_config** @out, git_config* config);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_config_free(git_config* cfg);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_entry(git_config_entry** @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_int32([NativeTypeName("int32_t *")] int* @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_int64([NativeTypeName("int64_t *")] long* @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_bool(int* @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_path(git_buf* @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_string([NativeTypeName("const char **")] sbyte** @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_string_buf(git_buf* @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_multivar_foreach([NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* regexp, [NativeTypeName("git_config_foreach_cb")] delegate* unmanaged[Cdecl]<git_config_entry*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_multivar_iterator_new(git_config_iterator** @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* regexp);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_next(git_config_entry** entry, git_config_iterator* iter);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_config_iterator_free(git_config_iterator* iter);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_set_int32(git_config* cfg, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("int32_t")] int value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_set_int64(git_config* cfg, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("int64_t")] long value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_set_bool(git_config* cfg, [NativeTypeName("const char *")] sbyte* name, int value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_set_string(git_config* cfg, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_set_multivar(git_config* cfg, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* regexp, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_delete_entry(git_config* cfg, [NativeTypeName("const char *")] sbyte* name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_delete_multivar(git_config* cfg, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* regexp);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_foreach([NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("git_config_foreach_cb")] delegate* unmanaged[Cdecl]<git_config_entry*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_iterator_new(git_config_iterator** @out, [NativeTypeName("const git_config *")] git_config* cfg);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_iterator_glob_new(git_config_iterator** @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* regexp);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_foreach_match([NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* regexp, [NativeTypeName("git_config_foreach_cb")] delegate* unmanaged[Cdecl]<git_config_entry*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_get_mapped(int* @out, [NativeTypeName("const git_config *")] git_config* cfg, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const git_configmap *")] git_configmap* maps, [NativeTypeName("size_t")] nuint map_n);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_lookup_map_value(int* @out, [NativeTypeName("const git_configmap *")] git_configmap* maps, [NativeTypeName("size_t")] nuint map_n, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_parse_bool(int* @out, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_parse_int32([NativeTypeName("int32_t *")] int* @out, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_parse_int64([NativeTypeName("int64_t *")] long* @out, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_parse_path(git_buf* @out, [NativeTypeName("const char *")] sbyte* value);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_backend_foreach_match(git_config_backend* backend, [NativeTypeName("const char *")] sbyte* regexp, [NativeTypeName("git_config_foreach_cb")] delegate* unmanaged[Cdecl]<git_config_entry*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_config_lock(git_transaction** tx, git_config* cfg);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_error *")]
        public static extern git_error* git_error_last();

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_trace_set(git_trace_level_t level, [NativeTypeName("git_trace_cb")] delegate* unmanaged[Cdecl]<git_trace_level_t, sbyte*, void> cb);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_libgit2_init();

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_libgit2_shutdown();

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_new(git_odb** @out);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_open(git_odb** @out, [NativeTypeName("const char *")] sbyte* objects_dir);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_add_disk_alternate(git_odb* odb, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_odb_free(git_odb* db);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_read(git_odb_object** @out, git_odb* db, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_read_prefix(git_odb_object** @out, git_odb* db, [NativeTypeName("const git_oid *")] git_oid* short_id, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_read_header([NativeTypeName("size_t *")] nuint* len_out, git_object_t* type_out, git_odb* db, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_exists(git_odb* db, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_exists_ext(git_odb* db, [NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("unsigned int")] uint flags);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_exists_prefix(git_oid* @out, git_odb* db, [NativeTypeName("const git_oid *")] git_oid* short_id, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_expand_ids(git_odb* db, git_odb_expand_id* ids, [NativeTypeName("size_t")] nuint count);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_refresh([NativeTypeName("struct git_odb *")] git_odb* db);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_foreach(git_odb* db, [NativeTypeName("git_odb_foreach_cb")] delegate* unmanaged[Cdecl]<git_oid*, void*, int> cb, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_write(git_oid* @out, git_odb* odb, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint len, git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_open_wstream(git_odb_stream** @out, git_odb* db, [NativeTypeName("git_object_size_t")] ulong size, git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_stream_write(git_odb_stream* stream, [NativeTypeName("const char *")] sbyte* buffer, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_stream_finalize_write(git_oid* @out, git_odb_stream* stream);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_stream_read(git_odb_stream* stream, [NativeTypeName("char *")] sbyte* buffer, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_odb_stream_free(git_odb_stream* stream);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_open_rstream(git_odb_stream** @out, [NativeTypeName("size_t *")] nuint* len, git_object_t* type, git_odb* db, [NativeTypeName("const git_oid *")] git_oid* oid);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_write_multi_pack_index(git_odb* db);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_hash(git_oid* @out, [NativeTypeName("const void *")] void* data, [NativeTypeName("size_t")] nuint len, git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_hashfile(git_oid* @out, [NativeTypeName("const char *")] sbyte* path, git_object_t type);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_object_dup(git_odb_object** dest, git_odb_object* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_odb_object_free(git_odb_object* @object);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_odb_object_id(git_odb_object* @object);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const void *")]
        public static extern void* git_odb_object_data(git_odb_object* @object);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_odb_object_size(git_odb_object* @object);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_object_t git_odb_object_type(git_odb_object* @object);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_add_backend(git_odb* odb, git_odb_backend* backend, int priority);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_add_alternate(git_odb* odb, git_odb_backend* backend, int priority);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_odb_num_backends(git_odb* odb);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_get_backend(git_odb_backend** @out, git_odb* odb, [NativeTypeName("size_t")] nuint pos);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_set_commit_graph(git_odb* odb, git_commit_graph* cgraph);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_backend_pack(git_odb_backend** @out, [NativeTypeName("const char *")] sbyte* objects_dir);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_backend_one_pack(git_odb_backend** @out, [NativeTypeName("const char *")] sbyte* index_file);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_odb_backend_loose(git_odb_backend** @out, [NativeTypeName("const char *")] sbyte* objects_dir, int compression_level, int do_fsync, [NativeTypeName("unsigned int")] uint dir_mode, [NativeTypeName("unsigned int")] uint file_mode);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_pathspec_new(git_pathspec** @out, [NativeTypeName("const git_strarray *")] git_strarray* pathspec);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_pathspec_free(git_pathspec* ps);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_pathspec_matches_path([NativeTypeName("const git_pathspec *")] git_pathspec* ps, [NativeTypeName("uint32_t")] uint flags, [NativeTypeName("const char *")] sbyte* path);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_pathspec_match_workdir(git_pathspec_match_list** @out, git_repository* repo, [NativeTypeName("uint32_t")] uint flags, git_pathspec* ps);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_pathspec_match_index(git_pathspec_match_list** @out, git_index* index, [NativeTypeName("uint32_t")] uint flags, git_pathspec* ps);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_pathspec_match_tree(git_pathspec_match_list** @out, git_tree* tree, [NativeTypeName("uint32_t")] uint flags, git_pathspec* ps);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_pathspec_match_diff(git_pathspec_match_list** @out, git_diff* diff, [NativeTypeName("uint32_t")] uint flags, git_pathspec* ps);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_pathspec_match_list_free(git_pathspec_match_list* m);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_pathspec_match_list_entrycount([NativeTypeName("const git_pathspec_match_list *")] git_pathspec_match_list* m);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_pathspec_match_list_entry([NativeTypeName("const git_pathspec_match_list *")] git_pathspec_match_list* m, [NativeTypeName("size_t")] nuint pos);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_diff_delta *")]
        public static extern git_diff_delta* git_pathspec_match_list_diff_entry([NativeTypeName("const git_pathspec_match_list *")] git_pathspec_match_list* m, [NativeTypeName("size_t")] nuint pos);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint git_pathspec_match_list_failed_entrycount([NativeTypeName("const git_pathspec_match_list *")] git_pathspec_match_list* m);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_pathspec_match_list_failed_entry([NativeTypeName("const git_pathspec_match_list *")] git_pathspec_match_list* m, [NativeTypeName("size_t")] nuint pos);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_new(git_revwalk** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_reset(git_revwalk* walker);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_push(git_revwalk* walk, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_push_glob(git_revwalk* walk, [NativeTypeName("const char *")] sbyte* glob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_push_head(git_revwalk* walk);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_hide(git_revwalk* walk, [NativeTypeName("const git_oid *")] git_oid* commit_id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_hide_glob(git_revwalk* walk, [NativeTypeName("const char *")] sbyte* glob);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_hide_head(git_revwalk* walk);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_push_ref(git_revwalk* walk, [NativeTypeName("const char *")] sbyte* refname);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_hide_ref(git_revwalk* walk, [NativeTypeName("const char *")] sbyte* refname);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_next(git_oid* @out, git_revwalk* walk);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_sorting(git_revwalk* walk, [NativeTypeName("unsigned int")] uint sort_mode);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_push_range(git_revwalk* walk, [NativeTypeName("const char *")] sbyte* range);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_simplify_first_parent(git_revwalk* walk);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_revwalk_free(git_revwalk* walk);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_repository* git_revwalk_repository(git_revwalk* walk);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_revwalk_add_hide_cb(git_revwalk* walk, [NativeTypeName("git_revwalk_hide_cb")] delegate* unmanaged[Cdecl]<git_oid*, void*, int> hide_cb, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_signature_new(git_signature** @out, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* email, [NativeTypeName("git_time_t")] long time, int offset);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_signature_now(git_signature** @out, [NativeTypeName("const char *")] sbyte* name, [NativeTypeName("const char *")] sbyte* email);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_signature_default(git_signature** @out, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_signature_from_buffer(git_signature** @out, [NativeTypeName("const char *")] sbyte* buf);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_signature_dup(git_signature** dest, [NativeTypeName("const git_signature *")] git_signature* sig);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_signature_free(git_signature* sig);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_lookup(git_tag** @out, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_lookup_prefix(git_tag** @out, git_repository* repo, [NativeTypeName("const git_oid *")] git_oid* id, [NativeTypeName("size_t")] nuint len);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void git_tag_free(git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_tag_id([NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_repository* git_tag_owner([NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_target(git_object** target_out, [NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_oid *")]
        public static extern git_oid* git_tag_target_id([NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern git_object_t git_tag_target_type([NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_tag_name([NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const git_signature *")]
        public static extern git_signature* git_tag_tagger([NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern sbyte* git_tag_message([NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_create(git_oid* oid, git_repository* repo, [NativeTypeName("const char *")] sbyte* tag_name, [NativeTypeName("const git_object *")] git_object* target, [NativeTypeName("const git_signature *")] git_signature* tagger, [NativeTypeName("const char *")] sbyte* message, int force);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_annotation_create(git_oid* oid, git_repository* repo, [NativeTypeName("const char *")] sbyte* tag_name, [NativeTypeName("const git_object *")] git_object* target, [NativeTypeName("const git_signature *")] git_signature* tagger, [NativeTypeName("const char *")] sbyte* message);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_create_from_buffer(git_oid* oid, git_repository* repo, [NativeTypeName("const char *")] sbyte* buffer, int force);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_create_lightweight(git_oid* oid, git_repository* repo, [NativeTypeName("const char *")] sbyte* tag_name, [NativeTypeName("const git_object *")] git_object* target, int force);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_delete(git_repository* repo, [NativeTypeName("const char *")] sbyte* tag_name);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_list(git_strarray* tag_names, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_list_match(git_strarray* tag_names, [NativeTypeName("const char *")] sbyte* pattern, git_repository* repo);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_foreach(git_repository* repo, [NativeTypeName("git_tag_foreach_cb")] delegate* unmanaged[Cdecl]<sbyte*, git_oid*, void*, int> callback, void* payload);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_peel(git_object** tag_target_out, [NativeTypeName("const git_tag *")] git_tag* tag);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_dup(git_tag** @out, git_tag* source);

        [DllImport("git2-3f4182d", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int git_tag_name_is_valid(int* valid, [NativeTypeName("const char *")] sbyte* name);

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

        [NativeTypeName("#define GIT_OBJECT_SIZE_MAX UINT64_MAX")]
        public const ulong GIT_OBJECT_SIZE_MAX = 0xffffffffffffffffUL;

        [NativeTypeName("#define GIT_COMMIT_CREATE_OPTIONS_VERSION 1")]
        public const int GIT_COMMIT_CREATE_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_REPOSITORY_INIT_OPTIONS_VERSION 1")]
        public const int GIT_REPOSITORY_INIT_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_DIFF_OPTIONS_VERSION 1")]
        public const int GIT_DIFF_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_DIFF_HUNK_HEADER_SIZE 128")]
        public const int GIT_DIFF_HUNK_HEADER_SIZE = 128;

        [NativeTypeName("#define GIT_DIFF_FIND_OPTIONS_VERSION 1")]
        public const int GIT_DIFF_FIND_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_DIFF_PARSE_OPTIONS_VERSION 1")]
        public const int GIT_DIFF_PARSE_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_DIFF_PATCHID_OPTIONS_VERSION 1")]
        public const int GIT_DIFF_PATCHID_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_BLOB_FILTER_OPTIONS_VERSION 1")]
        public const int GIT_BLOB_FILTER_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_DEFAULT_PORT \"9418\"")]
        public static ReadOnlySpan<byte> GIT_DEFAULT_PORT => "9418"u8;

        [NativeTypeName("#define GIT_ODB_OPTIONS_VERSION 1")]
        public const int GIT_ODB_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_ODB_BACKEND_PACK_OPTIONS_VERSION 1")]
        public const int GIT_ODB_BACKEND_PACK_OPTIONS_VERSION = 1;

        [NativeTypeName("#define GIT_ODB_BACKEND_LOOSE_OPTIONS_VERSION 1")]
        public const int GIT_ODB_BACKEND_LOOSE_OPTIONS_VERSION = 1;

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
