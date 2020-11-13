using NUnit.Framework;

using Quack;
using System;
using System.Collections.Generic;

namespace Quack.Test
{
	public class Tests
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void TestProperties()
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
        }

		[Test]
		public void TestMethods()
		{
			var myObject = new MyObject();

			var myMethods = myObject.As<IMyMethodInterface>();

			var stored = "Hello, world!";

			myObject[0, 1] = stored;

			var retrieved = myMethods[0, 1];

			Assert.AreEqual(stored, retrieved);

			Assert.AreEqual(myObject.GetInt(), myMethods.GetInt());

			Assert.AreEqual(myObject.GetString(0.1m), myMethods.GetString(0.1m));
		}
    }

	public interface IMyInterface
	{
		int ID { get; }
		string Name { get; }
	}

	public interface IMyMethodInterface
	{
		dynamic this[int x, int y] { get; }

		int GetInt();

		string GetString(decimal x);
	}

	public class MyObject
	{
		public int ID { get; set; }
		public string Name { get; set; }

		private static readonly Dictionary<ValueTuple<int, int>, object> dictionary = new Dictionary<ValueTuple<int, int>, object>();

		public dynamic this[int x, int y]
		{
			get => dictionary[ValueTuple.Create(x, y)];
			set => dictionary[ValueTuple.Create(x, y)] = value;
		}

		public int GetInt() => dictionary.Count;

		public string GetString(decimal x) => this[(int)x, (int)(x * 10)];
	}

	public interface IMyInterface2
	{
		int ID { get; }
		string Name { get; }
	}
}