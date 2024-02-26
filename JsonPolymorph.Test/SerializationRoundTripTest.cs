using Newtonsoft.Json;

namespace JsonPolymorph.Test;

[JsonHierarchyBase]
public abstract partial record Container();
public sealed record ContainerForBaseType(BaseType Value) : Container;

[JsonHierarchyBase]
public abstract partial record BaseType(string Foo);
public sealed record DerivedType(string Foo, string Bar) : BaseType(Foo);
public sealed record AnotherDerivedType(string Foo, string Baz) : BaseType(Foo);

[TestClass]
public class SerializationRoundTripTest
{
    [TestMethod]
    public void RoundTripSerializationPreservesTypeAndProperties()
    {
        var derived = new DerivedType("foo", "bar");
        var anotherDerived = new AnotherDerivedType("foo", "baz");

        var containerForDerived = new ContainerForBaseType(derived);
        var containerForAnotherDerived = new ContainerForBaseType(anotherDerived);

        var containerForDerivedJson = JsonConvert.SerializeObject(containerForDerived,
            Formatting.Indented,
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        var containerForAnotherDerivedJson = JsonConvert.SerializeObject(containerForAnotherDerived);

        var deserializedDerived = JsonConvert.DeserializeObject<Container>(containerForDerivedJson);
        var deserializedAnotherDerived = JsonConvert.DeserializeObject<Container>(containerForAnotherDerivedJson);

        Assert.IsInstanceOfType(deserializedDerived, typeof(ContainerForBaseType));
        Assert.IsInstanceOfType(deserializedAnotherDerived, typeof(ContainerForBaseType));

        Assert.IsInstanceOfType<DerivedType>(((ContainerForBaseType)deserializedDerived).Value);
        Assert.IsInstanceOfType<AnotherDerivedType>(((ContainerForBaseType)deserializedAnotherDerived).Value);

        var deserializedDerivedValue = (DerivedType)((ContainerForBaseType)deserializedDerived).Value;
        var deserializedAnotherDerivedValue = (AnotherDerivedType)((ContainerForBaseType)deserializedAnotherDerived).Value;

        Assert.AreEqual(derived.Foo, deserializedDerivedValue.Foo);
        Assert.AreEqual(derived.Bar, deserializedDerivedValue.Bar);

        Assert.AreEqual(anotherDerived.Foo, deserializedAnotherDerivedValue.Foo);
        Assert.AreEqual(anotherDerived.Baz, deserializedAnotherDerivedValue.Baz);
    }
}