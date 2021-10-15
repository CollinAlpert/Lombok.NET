using System;

namespace AttributeSourceGenerators
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
        public AllArgsConstructorAttribute(AccessType accessType)
        {
        }
        public AllArgsConstructorAttribute(MemberType memberType, AccessType accessType)
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
        public RequiredArgsConstructorAttribute(AccessType accessType)
        {
        }
        public RequiredArgsConstructorAttribute(MemberType memberType, AccessType accessType)
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

    public enum MemberType
    {
        Property,
        Field
    }

    public enum AccessType
    {
        Private,
        Protected,
        Public,
        PrivateAndProtected,
        PrivateAndPublic,
        ProtectedAndPublic,
        PrivateProtectedAndPublic
    }
}