namespace Lg2.Native
{
    [NativeTypeName("int")]
    public enum git_checkout_notify_t : uint
    {
        GIT_CHECKOUT_NOTIFY_NONE = 0,
        GIT_CHECKOUT_NOTIFY_CONFLICT = (1U << 0),
        GIT_CHECKOUT_NOTIFY_DIRTY = (1U << 1),
        GIT_CHECKOUT_NOTIFY_UPDATED = (1U << 2),
        GIT_CHECKOUT_NOTIFY_UNTRACKED = (1U << 3),
        GIT_CHECKOUT_NOTIFY_IGNORED = (1U << 4),
        GIT_CHECKOUT_NOTIFY_ALL = 0x0FFFFU,
    }
}
