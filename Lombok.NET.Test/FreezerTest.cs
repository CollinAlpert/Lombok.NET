namespace Lombok.NET.Test;

public sealed class FreezerTest
{
	[Fact]
	public void Name_WhenNotFrozen_NameIsUpdated()
	{
		var freezer = new Freezer();
		freezer.Name = "Test";
		
		Assert.False(freezer.IsFrozen);
		Assert.Equal("Test", freezer.Name);
	}

	[Fact]
	public void Name_WhenFrozen_ThrowsAndDoesNotUpdate()
	{
		var freezer = new Freezer();
		freezer.Name = "Test";
		
		Assert.False(freezer.IsFrozen);
		freezer.Freeze();
		Assert.True(freezer.IsFrozen);
		Assert.Throws<InvalidOperationException>(() => freezer.Name = "Test2");
		Assert.Equal("Test", freezer.Name);
	}

	[Fact]
	public void Freeze_WhenNotFrozen_DoesNotThrow()
	{
		var freezer = new Freezer();
		Assert.False(freezer.IsFrozen);
		freezer.Freeze();
		Assert.True(freezer.IsFrozen);
	}

	[Fact]
	public void Freeze_WhenFrozen_Throws()
	{
		var freezer = new Freezer();
		Assert.False(freezer.IsFrozen);
		freezer.Freeze();
		Assert.True(freezer.IsFrozen);
		Assert.Throws<InvalidOperationException>(() => freezer.Freeze());
	}

	[Fact]
	public void Unfreeze_WhenNotFrozen_Throws()
	{
		var freezer = new Freezer();
		Assert.False(freezer.IsFrozen);
		Assert.Throws<InvalidOperationException>(() => freezer.Unfreeze());
	}

	[Fact]
	public void Unfreeze_WhenFrozen_DoesNotThrow()
	{
		var freezer = new Freezer();
		Assert.False(freezer.IsFrozen);
		freezer.Freeze();
		Assert.True(freezer.IsFrozen);
		freezer.Unfreeze();
		Assert.False(freezer.IsFrozen);
	}

	[Fact]
	public void TryFreeze_WhenFrozen_ReturnsFalse()
	{
		var freezer = new Freezer();
		Assert.False(freezer.IsFrozen);
		freezer.Freeze();
		Assert.True(freezer.IsFrozen);
		Assert.False(freezer.TryFreeze());
		Assert.True(freezer.IsFrozen);
	}

	[Fact]
	public void TryFreeze_WhenNotFrozen_ReturnsTrue()
	{
		var freezer = new Freezer();
		Assert.False(freezer.IsFrozen);
		Assert.True(freezer.TryFreeze());
		Assert.True(freezer.IsFrozen);
	}

	[Fact]
	public void TryUnfreeze_WhenFrozen_ReturnsTrue()
	{
		var freezer = new Freezer();
		Assert.False(freezer.IsFrozen);
		freezer.Freeze();
		Assert.True(freezer.IsFrozen);
		Assert.True(freezer.TryUnfreeze());
		Assert.False(freezer.IsFrozen);
	}

	[Fact]
	public void TryUnfreeze_WhenNotFrozen_ReturnsFalse()
	{
		var freezer = new Freezer();
		Assert.False(freezer.IsFrozen);
		Assert.False(freezer.TryUnfreeze());
		Assert.False(freezer.IsFrozen);
	}
}

file sealed class Freezer
{
	private string _name = null!;

	public bool IsFrozen { get; private set; }

	public string Name
	{
		get => _name;
		set
		{
			if (IsFrozen)
			{
				throw new InvalidOperationException("Freezer is frozen and cannot be modified.");
			}

			_name = value;
		}
	}

	public void Freeze()
	{
		if (IsFrozen)
		{
			throw new InvalidOperationException("Freezer is already frozen.");
		}

		IsFrozen = true;
	}

	public bool TryFreeze()
	{
		if (IsFrozen)
		{
			return false;
		}

		return IsFrozen = true;
	}

	public void Unfreeze()
	{
		if (!IsFrozen)
		{
			throw new InvalidOperationException("Freezer is not frozen.");
		}

		IsFrozen = false;
	}

	public bool TryUnfreeze()
	{
		if (!IsFrozen)
		{
			return false;
		}

		return !(IsFrozen = false);
	}
}