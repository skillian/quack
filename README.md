# quack
Interface "duck typing" that generates wrapper classes to make a class implement an interface if it has all the necessary properties and methods of that interface.  I got the idea from Go's implicit interface satisfaction by defining methods with the same names and function signatures.

## Example

```csharp
class MyObject
{
    public int ID { get; set; }
    public string Name { get; set; }
}

interface MyInterface
{
    int ID { get; }
    string Name { get; }
}

class Program
{
    public static void Main()
    {
        var myObject = new MyObject { ID = 123, Name = "Test" };

        var myInterface = myObject.As<IMyInterface>();

        if (myInterface.ID != 123 || myInterface.Name != "Test")
            throw new Exception("That was supposed to work...");
    }
}
```

## Why?

Some APIs don't create or implement all the interfaces they should.  I use one API almost every day where there are a bunch of objects that have `AddKeyword` functions but they don't share any common base class or interfaces, so if I'm writing code that doesn't care about _what_ I'm adding keywords to, I have to either:

- Write custom overloads that take the objects I want

- Accept a `KeywordAdder` delegate.

- Create boilerplate wrapper classes that pass-thru calls to different underlying implementations.

The delegate was preferable initially, but then I needed to start adding "keyword records" with an `AddKeywordRecord` function, so then I needed to accept a `KeywordAdder` and a `KeywordRecordAdder` delegate.  I instead started using an abstract base class with `AddKeyword` and `AddKeywordRecord` abstract methods and then extensions of that class for each `AddKeyword` + `AddKeywordRecord` object.

The purpose of this library is to automatically generate the boilerplate pass-thru classes.
