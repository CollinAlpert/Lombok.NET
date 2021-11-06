using System;

namespace Lombok.NET
{
    [AttributeUsage(AttributeTargets.Class)]
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

    [AttributeUsage(AttributeTargets.Class)]
    public class NoArgsConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class DecoratorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class WithAttribute : Attribute
    {
        public WithAttribute()
        {
        }
        public WithAttribute(MemberType memberType)
        {
        }
    }

    public enum MemberType
    {
        // default for this enum
        Field = 0,
        Property
    }

    [Flags]
    public enum AccessTypes
    {
        // default for this enum
        Private = 0,
        Protected,
        Internal,
        Public
    }
}