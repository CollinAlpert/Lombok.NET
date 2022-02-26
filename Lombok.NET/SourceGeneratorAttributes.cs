using System;

namespace Lombok.NET
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	[Partial]
	public class AllArgsConstructorAttribute : Attribute
	{
		public AllArgsConstructorAttribute()
		{
		}

		public AllArgsConstructorAttribute(MemberType memberType)
		{
		}

		public AllArgsConstructorAttribute(AccessTypes accessType)
		{
		}

		public AllArgsConstructorAttribute(MemberType memberType, AccessTypes accessType)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	[Partial]
	public class RequiredArgsConstructorAttribute : Attribute
	{
		public RequiredArgsConstructorAttribute()
		{
		}

		public RequiredArgsConstructorAttribute(MemberType memberType)
		{
		}

		public RequiredArgsConstructorAttribute(AccessTypes accessType)
		{
		}

		public RequiredArgsConstructorAttribute(MemberType memberType, AccessTypes accessType)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
	[Partial]
	public class ToStringAttribute : Attribute
	{
		public ToStringAttribute()
		{
		}

		public ToStringAttribute(MemberType memberType)
		{
		}

		public ToStringAttribute(AccessTypes accessType)
		{
		}

		public ToStringAttribute(MemberType memberType, AccessTypes accessType)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	[Partial]
	public class NoArgsConstructorAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class DecoratorAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class)]
	[Partial]
	public class SingletonAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class)]
	[Partial]
	public class WithAttribute : Attribute
	{
		public WithAttribute()
		{
		}

		public WithAttribute(MemberType memberType)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class PropertyAttribute : Attribute
	{
		public PropertyAttribute()
		{
		}

		public PropertyAttribute(PropertyChangeType propertyChangeType)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	[Partial]
	public class NotifyPropertyChangedAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class)]
	[Partial]
	public class NotifyPropertyChangingAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	[Partial]
	public class AsyncOverloadsAttribute : Attribute
	{
		
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class AsyncAttribute : Attribute
	{
		
	}

	public enum MemberType
	{
		Field,
		Property
	}

	[Flags]
	public enum AccessTypes
	{
		Private,
		Protected,
		Internal,
		Public
	}

	public enum PropertyChangeType
	{
		PropertyChanged,
		PropertyChanging,
		ReactivePropertyChange
	}

	internal class PartialAttribute : Attribute
	{
	}
}