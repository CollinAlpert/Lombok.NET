using Lombok.NET.MethodGenerators;

namespace Lombok.NET.Test;

public sealed class SerializationTest
{
    [Fact]
    public Task TestFields()
    {
        const string source = """
                              using Lombok.NET;
                              
                              namespace Test;
                              
                              [Serialization]
                              partial class Person : BasePerson
                              {
                                  private short s;
                                  private int i;
                                  private long l;
                                  private ushort us;
                                  private uint ui;
                              }
                              
                              class BasePerson : BaseBasePerson
                              {
                                  private ulong ul;
                                  private byte b;
                                  private sbyte sb;
                                  private float f;
                              }
                              
                              class BaseBasePerson
                              {
                                  private double d;
                                  private decimal dec;
                                  private string str;
                                  private char c;
                                  private bool boolean;
                              }
                              """;

        return TestHelper.Verify<SerializationGenerator>(source);
    }
    
    [Fact]
    public Task TestProperties()
    {
        const string source = """
                              using Lombok.NET;
                              
                              namespace Test;
                              
                              [Serialization(MemberType = MemberType.Property)]
                              partial class Person : BasePerson
                              {
                                  public short S { get; set; }
                                  public int I { get; set; }
                                  public long L { get; set; }
                                  public ushort Us { get; set; }
                                  public uint Ui { get; set; }
                              }
                              
                              class BasePerson : BaseBasePerson
                              {
                                  public ulong Ul { get; set; }
                                  public byte B { get; set; }
                                  public sbyte Sb { get; set; }
                                  public float F { get; set; }
                              }
                              
                              class BaseBasePerson
                              {
                                  public double D { get; set; }
                                  public decimal Dec { get; set; }
                                  public string Str { get; set; }
                                  public char C { get; set; }
                                  public bool Boolean { get; set; }
                              }
                              """;

        return TestHelper.Verify<SerializationGenerator>(source);
    }

    [Fact]
    public Task TestAccessibilityModifiers()
    {
        const string source = """
                              using Lombok.NET;
                              
                              namespace Test;
                              
                              [Serialization]
                              public partial class Person
                              {
                                  private string? name;
                                  private object value;
                                  private object? value2;
                              }
                              """;

        return TestHelper.Verify<SerializationGenerator>(source);
    }

    [Fact]
    public Task TestEmptyClass()
    {
        const string source = """
                              using Lombok.NET;
                              
                              namespace Test;
                              
                              [Serialization]
                              partial class Person;
                              """;

        return TestHelper.Verify<SerializationGenerator>(source, true);
    }

    [Fact]
    public Task TestNullableTypes()
    {
        const string source = """
                              using Lombok.NET;
                              
                              namespace Test;
                              
                              [Serialization]
                              partial class Person
                              {
                                  private int? value;
                              }
                              """;

        return TestHelper.Verify<SerializationGenerator>(source, true);
    }

    [Fact]
    public Task TestPropertiesWithoutSpecifyingMemberType()
    {
        const string source = """
                              using Lombok.NET;
                              
                              namespace Test;
                              
                              [Serialization]
                              partial class Person
                              {
                                  public string Name { get; set; }
                              }
                              """;

        return TestHelper.Verify<SerializationGenerator>(source, true);
    }
}