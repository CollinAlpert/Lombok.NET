using Xunit;

namespace Lombok.NET.Test;

public sealed class FreezableTest
{
	[Fact]
	public void Name_WhenNotFrozen_NameIsUpdated()
	{
		var person = new Person();
		person.Name = "Test";
		
		Assert.False(person.IsFrozen);
		Assert.Equal("Test", person.Name);
	}

	[Fact]
	public void Name_WhenFrozen_ThrowsAndDoesNotUpdate()
	{
		var person = new Person();
		person.Name = "Test";
		
		Assert.False(person.IsFrozen);
		person.Freeze();
		Assert.True(person.IsFrozen);
		Assert.Throws<InvalidOperationException>(() => person.Name = "Test2");
		Assert.Equal("Test", person.Name);
	}

	[Fact]
	public void Freeze_WhenNotFrozen_DoesNotThrow()
	{
		var person = new Person();
		Assert.False(person.IsFrozen);
		person.Freeze();
		Assert.True(person.IsFrozen);
	}

	[Fact]
	public void Freeze_WhenFrozen_Throws()
	{
		var person = new Person();
		Assert.False(person.IsFrozen);
		person.Freeze();
		Assert.True(person.IsFrozen);
		Assert.Throws<InvalidOperationException>(() => person.Freeze());
	}

	[Fact]
	public void Unfreeze_WhenNotFrozen_Throws()
	{
		var person = new Person();
		Assert.False(person.IsFrozen);
		Assert.Throws<InvalidOperationException>(() => person.Unfreeze());
	}

	[Fact]
	public void Unfreeze_WhenFrozen_DoesNotThrow()
	{
		var person = new Person();
		Assert.False(person.IsFrozen);
		person.Freeze();
		Assert.True(person.IsFrozen);
		person.Unfreeze();
		Assert.False(person.IsFrozen);
	}

	[Fact]
	public void TryFreeze_WhenFrozen_ReturnsFalse()
	{
		var person = new Person();
		Assert.False(person.IsFrozen);
		person.Freeze();
		Assert.True(person.IsFrozen);
		Assert.False(person.TryFreeze());
		Assert.True(person.IsFrozen);
	}

	[Fact]
	public void TryFreeze_WhenNotFrozen_ReturnsTrue()
	{
		var person = new Person();
		Assert.False(person.IsFrozen);
		Assert.True(person.TryFreeze());
		Assert.True(person.IsFrozen);
	}

	[Fact]
	public void TryUnfreeze_WhenFrozen_ReturnsTrue()
	{
		var person = new Person();
		Assert.False(person.IsFrozen);
		person.Freeze();
		Assert.True(person.IsFrozen);
		Assert.True(person.TryUnfreeze());
		Assert.False(person.IsFrozen);
	}

	[Fact]
	public void TryUnfreeze_WhenNotFrozen_ReturnsFalse()
	{
		var person = new Person();
		Assert.False(person.IsFrozen);
		Assert.False(person.TryUnfreeze());
		Assert.False(person.IsFrozen);
	}
}

[Freezable]
partial class Person
{
	[Freezable]
	private string _name;

	private int _age;
}