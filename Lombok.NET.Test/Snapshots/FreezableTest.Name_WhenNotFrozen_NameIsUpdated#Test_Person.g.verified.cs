﻿//HintName: Test_Person.g.cs
// <auto-generated/>
using Lombok.NET;

namespace Test;
#nullable enable
internal partial class Person
{
    public string Name
    {
        get => _name;
        set
        {
            if (IsFrozen)
            {
                throw new global::System.InvalidOperationException("'Person' is frozen and cannot be modified.");
            }

            _name = value;
        }
    }

    public bool IsFrozen { get; private set; }

    /// <summary>
    /// Freezes this 'Person' instance.
    /// </summary>
    /// <exception cref = "InvalidOperationException">When this instance has already been frozen.</exception>
    public void Freeze()
    {
        if (IsFrozen)
        {
            throw new InvalidOperationException("'Person' is already frozen.");
        }

        IsFrozen = true;
    }

    /// <summary>
    /// Tries to freeze this 'Person' instance.
    /// </summary>
    /// <returns>'true' when freezing was successful, 'false' when the instance was already frozen.</returns>
    public bool TryFreeze()
    {
        if (IsFrozen)
        {
            return false;
        }

        return IsFrozen = true;
    }

    /// <summary>
    /// Unfreezes this 'Person' instance.
    /// </summary>
    /// <exception cref = "InvalidOperationException">When this instance is not frozen.</exception>
    public void Unfreeze()
    {
        if (!IsFrozen)
        {
            throw new InvalidOperationException("'Person' is not frozen.");
        }

        IsFrozen = false;
    }

    /// <summary>
    /// Tries to unfreeze this 'Person' instance.
    /// </summary>
    /// <returns>'true' when unfreezing was successful, 'false' when the instance was not frozen.</returns>
    public bool TryUnfreeze()
    {
        if (!IsFrozen)
        {
            return false;
        }

        return !(IsFrozen = false);
    }
}