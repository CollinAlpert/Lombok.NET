using System;

namespace Lombok.NET;

/// <summary>
/// Tells Lombok.NET to generate an AllArgsConstructor for this type. 
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class AllArgsConstructorAttribute : Attribute
{
	/// <summary>
	/// Empty constructor. Private fields will included in the constructor.
	/// </summary>
	public AllArgsConstructorAttribute()
	{
	}

	/// <summary>
	/// Allows specifying which private members (fields or properties) will be included in the constructor.
	/// </summary>
	/// <param name="memberType">The member type to include.</param>
	public AllArgsConstructorAttribute(MemberType memberType)
	{
	}

	/// <summary>
	/// Allows specifying fields of which access type (public, protected etc.) will be included in the constructor.
	/// </summary>
	/// <param name="accessType">The access type of fields to include.</param>
	public AllArgsConstructorAttribute(AccessTypes accessType)
	{
	}

	/// <summary>
	/// Allows specifying members (fields or properties) of which access type (public, protected etc.) will be included in the constructor.
	/// </summary>
	/// <param name="memberType">The member type to include.</param>
	/// <param name="accessType">The access type of fields to include.</param>
	public AllArgsConstructorAttribute(MemberType memberType, AccessTypes accessType)
	{
	}
}

/// <summary>
/// Tells Lombok.NET to generate a RequiredArgsConstructor for this type. 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RequiredArgsConstructorAttribute : Attribute
{
	/// <summary>
	/// Empty constructor. Readonly private fields will included in the constructor.
	/// </summary>
	public RequiredArgsConstructorAttribute()
	{
	}

	/// <summary>
	/// Allows specifying which private members (fields or properties) will be included in the constructor.
	/// </summary>
	/// <param name="memberType">The member type to include.</param>
	public RequiredArgsConstructorAttribute(MemberType memberType)
	{
	}

	/// <summary>
	/// Allows specifying fields of which access type (public, protected etc.) will be included in the constructor.
	/// </summary>
	/// <param name="accessType">The access type of fields to include.</param>
	public RequiredArgsConstructorAttribute(AccessTypes accessType)
	{
	}

	/// <summary>
	/// Allows specifying members (fields or properties) of which access type (public, protected etc.) will be included in the constructor.
	/// </summary>
	/// <param name="memberType">The member type to include.</param>
	/// <param name="accessType">The access type of fields to include.</param>
	public RequiredArgsConstructorAttribute(MemberType memberType, AccessTypes accessType)
	{
	}
}

/// <summary>
/// Tells Lombok.NET to generate a ToString implementation for this type. For enums, a ToText method will be added.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
public sealed class ToStringAttribute : Attribute
{
	/// <summary>
	/// Empty constructor. Private fields will included in the ToString implementation.
	/// </summary>
	public ToStringAttribute()
	{
	}

	/// <summary>
	/// Allows specifying which private members (fields or properties) will be included in the ToString implementation.
	/// </summary>
	/// <param name="memberType">The member type to include.</param>
	public ToStringAttribute(MemberType memberType)
	{
	}

	/// <summary>
	/// Allows specifying fields of which access type (public, protected etc.) will be included in the ToString implementation.
	/// </summary>
	/// <param name="accessType">The access type of fields to include.</param>
	public ToStringAttribute(AccessTypes accessType)
	{
	}

	/// <summary>
	/// Allows specifying members (fields or properties) of which access type (public, protected etc.) will be included in the ToString implementation.
	/// </summary>
	/// <param name="memberType">The member type to include.</param>
	/// <param name="accessType">The access type of fields to include.</param>
	public ToStringAttribute(MemberType memberType, AccessTypes accessType)
	{
	}
}

/// <summary>
/// Tells Lombok.NET to generate an empty constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NoArgsConstructorAttribute : Attribute
{
}

/// <summary>
/// Tells Lombok.NET to generate a Decorator implementation for this type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class DecoratorAttribute : Attribute
{
}

/// <summary>
/// Tells Lombok.NET to make the type a singleton and expose an Instance property.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SingletonAttribute : Attribute
{
}

/// <summary>
/// Tells Lombok.NET to expose a <see cref="Lazy{T}"/> property for the class.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class LazyAttribute : Attribute
{
}

/// <summary>
/// Tells Lombok.NET to generate With builder methods for this type. 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class WithAttribute : Attribute
{
	/// <summary>
	/// Empty constructor. With methods will be generated for non-readonly fields.
	/// </summary>
	public WithAttribute()
	{
	}

	/// <summary>
	/// Allows specifying for which members (fields or properties) the With methods will be generated.
	/// </summary>
	/// <param name="memberType">The member type to include.</param>
	public WithAttribute(MemberType memberType)
	{
	}
}

/// <summary>
/// Tells Lombok.NET to generate a property for this field.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class PropertyAttribute : Attribute
{
	/// <summary>
	/// Empty constructor. Generates a property with the field as the backing field.
	/// </summary>
	public PropertyAttribute()
	{
	}

	/// <summary>
	/// Allows specifying which kind of change event should be raised when the property is set.
	/// </summary>
	/// <param name="propertyChangeType">The type of change event to raise when the property is set.</param>
	public PropertyAttribute(PropertyChangeType propertyChangeType)
	{
	}
}

/// <summary>
/// Tells Lombok.NET to generate a INotifyPropertyChanged implementation for this class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NotifyPropertyChangedAttribute : Attribute
{
}

/// <summary>
/// Tells Lombok.NET to generate a INotifyPropertyChanging implementation for this class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class NotifyPropertyChangingAttribute : Attribute
{
}

/// <summary>
/// Tells Lombok.NET to generate async overloads for method definitions (abstract or interface methods).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class AsyncOverloadsAttribute : Attribute
{
		
}

/// <summary>
/// Tells Lombok.NET to generate an async version for this method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AsyncAttribute : Attribute
{
		
}

/// <summary>
/// The kind of members which Lombok.NET supports.
/// </summary>
public enum MemberType
{
	/// <summary>
	/// A C# field.
	/// </summary>
	Field,
		
	/// <summary>
	/// A C# property.
	/// </summary>
	Property
}

/// <summary>
/// The kinds of accesses Lombok.NET supports.
/// </summary>
[Flags]
public enum AccessTypes
{
	/// <summary>
	/// Associated with the private keyword.
	/// </summary>
	Private,
		
	/// <summary>
	/// Associated with the protected keyword.
	/// </summary>
	Protected,
		
	/// <summary>
	/// Associated with the internal keyword.
	/// </summary>
	Internal,
		
	/// <summary>
	/// Associated with the public keyword.
	/// </summary>
	Public
}

/// <summary>
/// The types of change events which can be raised by Lombok.NET
/// </summary>
public enum PropertyChangeType
{
	/// <summary>
	/// After a property has changed.
	/// <see cref="System.ComponentModel.INotifyPropertyChanged"/>
	/// </summary>
	PropertyChanged,
		
	/// <summary>
	/// Before a property has changed.
	/// <see cref="System.ComponentModel.INotifyPropertyChanging"/>
	/// </summary>
	PropertyChanging,
		
	/// <summary>
	/// Property change handling as performed by the ReactiveUI library.
	/// </summary>
	ReactivePropertyChange
}