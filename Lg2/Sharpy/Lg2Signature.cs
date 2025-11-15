using System.Runtime.InteropServices;
using Lg2.Native;
using static Lg2.Native.LibGit2Exports;

namespace Lg2.Sharpy;

public unsafe ref struct Lg2SignaturePlainRef
{
    internal readonly git_signature* Ptr;

    internal ref git_signature Ref
    {
        get
        {
            EnsureValid();
            return ref (*Ptr);
        }
    }

    internal Lg2SignaturePlainRef(git_signature* pSig)
    {
        Ptr = pSig;
    }

    public void EnsureValid()
    {
        if (Ptr is null)
        {
            throw new InvalidOperationException($"Invalid {nameof(Lg2SignaturePlainRef)}");
        }
    }
}

public unsafe class Lg2SignatureOwnedRef<TOwner> : NativeOwnedRef<TOwner, git_signature>
    where TOwner : class
{
    internal Lg2SignatureOwnedRef(TOwner owner, git_signature* pNative)
        : base(owner, pNative) { }

    public static implicit operator Lg2SignaturePlainRef(Lg2SignatureOwnedRef<TOwner> ownedRef) =>
        new(ownedRef.Ptr);
}
