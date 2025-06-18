using System.Runtime.InteropServices;

namespace Lg2.Sharpy;

public unsafe interface INativeRelease<TNative>
    where TNative : unmanaged
{
    static abstract void NativeRelease(TNative* pNative);
}

public abstract unsafe class NativeSafePointer<TDerived, TNative> : SafeHandle
    where TNative : unmanaged
    where TDerived : INativeRelease<TNative>, new()
{
    public delegate void Release(TNative* pNative);

    internal NativeSafePointer(TNative* pNative)
        : base(default, true)
    {
        handle = (nint)pNative;
    }

    public override bool IsInvalid => handle == default;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid == false)
        {
            TDerived.NativeRelease((TNative*)handle);
            handle = default;
        }

        return true;
    }

    internal TNative* Ptr => (TNative*)handle;

    internal ref TNative Ref
    {
        get
        {
            EnsureValid();
            return ref (*Ptr);
        }
    }

    public void EnsureValid()
    {
        if (IsInvalid)
        {
            ThrowHelper.ThrowInvalidNullInstance<TDerived>();
        }
    }
}

public abstract unsafe class NativeOwnedRef<TOwner, TNative>
    where TOwner : class
    where TNative : unmanaged
{
    protected readonly WeakReference<TOwner> _ownerWeakRef;

    protected readonly TNative* _pNative;

    internal NativeOwnedRef(TOwner owner, TNative* pNative)
    {
        _ownerWeakRef = new WeakReference<TOwner>(owner);
        _pNative = pNative;
    }

    internal TNative* Ptr => _pNative;

    internal ref TNative Ref
    {
        get
        {
            EnsureValid();
            return ref (*Ptr);
        }
    }

    public void EnsureValid()
    {
        if (_ownerWeakRef.TryGetTarget(out _) == false)
        {
            ThrowHelper.ThrowInvalidNullInstance<TOwner>();
        }
    }

    internal TOwner EnsureOwner()
    {
        if (_ownerWeakRef.TryGetTarget(out var owner) == false)
        {
            ThrowHelper.ThrowInvalidNullInstance<TOwner>();
        }

        return owner!;
    }
}
