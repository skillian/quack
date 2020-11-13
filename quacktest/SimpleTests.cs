using NUnit.Framework;

using Quack;

namespace Quack.Test
{
	public class Tests
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void Test1()
		{
			var myObject = new MyObject
			{
				ID = 123,
				Name = "Hello World"
			};

			var myInterface = myObject.As<IMyInterface>();

			Assert.AreEqual(myObject.ID, myInterface.ID);
			Assert.AreEqual(myObject.Name, myInterface.Name);

			var myInterface2 = myObject.As<IMyInterface2>();

			Assert.AreEqual(myObject.ID, myInterface2.ID);
			Assert.AreEqual(myObject.Name, myInterface2.Name);

			Assert.Pass();
        }
    }

	public interface IMyInterface
	{
		int ID { get; }
		string Name { get; }
	}

	public class MyObject
	{
		public int ID { get; set; }
		public string Name { get; set; }
	}

	public interface IMyInterface2
	{
		int ID { get; }
		string Name { get; }
	}
}