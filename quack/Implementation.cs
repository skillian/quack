using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Quack
{
	public interface IValue
	{
		object Value { get; }
	}

	public interface IValue<T> : IValue
	{
		new T Value { get; }
	}

	public abstract class Implementation
	{
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<ValueTuple<Type, Type>, ValueTuple<Type, Func<object, object>>> implementationTypes
			= new System.Collections.Concurrent.ConcurrentDictionary<ValueTuple<Type, Type>, ValueTuple<Type, Func<object, object>>>();

		public static TInterface Implement<TInterface>(object value)
		{
			var key = ValueTuple.Create(value.GetType(), typeof(TInterface));

			if (implementationTypes.TryGetValue(key, out var data))
				return (TInterface)data.Item2(value);

			var implementationType = typeof(Implementation<,>).MakeGenericType(key.Item1, key.Item2);

			data = (ValueTuple<Type, Func<object, object>>)implementationType
				.GetProperty(
					"InstanceData",
					BindingFlags.Public |
					BindingFlags.Static)
				.GetValue(default);

			data = implementationTypes.GetOrAdd(key, data);

			return (TInterface)data.Item2(value);
		}
	}

	public abstract class Implementation<TValue> : Implementation, IValue<TValue>
	{
		public TValue Value { get; }

		object IValue.Value => Value;

		protected Implementation(TValue value)
		{
			Value = value;
		}

		public TInterface Implement<TInterface>()
			=> Implementation<TValue, TInterface>.CreateInstance(Value);

		internal static readonly System.Reflection.PropertyInfo valuePropertyInfo
			= typeof(Implementation<TValue>).GetProperty(nameof(Implementation<TValue>.Value));

		internal static readonly System.Reflection.ConstructorInfo constructorInfo
			= typeof(Implementation<TValue>).GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				default,
				new Type[] { typeof(TValue) },
				Array.Empty<ParameterModifier>());
	}

	static class Implementation<TValue, TInterface>
	{
		public static ValueTuple<
			Type,
			Func<object, object>
		> InstanceData => new ValueTuple<
			Type,
			Func<object, object>
		>(
			Type,
			o => CreateInstance((TValue)o)
		);

		public static Type Type { get; }

		public static Func<TValue, TInterface> CreateInstance { get; }

		static Implementation()
		{
			if (!typeof(TInterface).IsInterface)
				throw new Exception($"{typeof(TInterface)} is not an interface type");

			var name = String.Concat(typeof(TValue).Name, ".", typeof(TInterface).Name);

			var assemblyBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
				new System.Reflection.AssemblyName(name), AssemblyBuilderAccess.RunAndCollect);

			var moduleBuilder = assemblyBuilder.DefineDynamicModule(name);

			var typeBuilder = moduleBuilder.DefineType(
				String.Concat(name, ".", "Implementation"),
				System.Reflection.TypeAttributes.Public |
				System.Reflection.TypeAttributes.AutoLayout |
				System.Reflection.TypeAttributes.AnsiClass |
				System.Reflection.TypeAttributes.BeforeFieldInit,
				typeof(Implementation<TValue>),
				new Type[] { typeof(TInterface) });

			var methodBuilders = typeof(TInterface).GetMethods()
				.Select(mi => ValueTuple.Create(
					mi,
					typeof(TValue).GetMethod(
						mi.Name, mi.GetParameters()
							.Select(p => p.ParameterType)
							.ToArray())))
				.Select(t => {
					var mb = CreatePassThruMethod(typeBuilder, t.Item2);

					typeBuilder.DefineMethodOverride(mb, t.Item1);

					return ValueTuple.Create(t.Item1, t.Item2, mb);
				})
				.ToDictionary(t => t.Item1, t => ValueTuple.Create(t.Item2, t.Item3));

			foreach (var propertyInfo in typeof(TInterface).GetProperties())
			{
				var propertyBuilder = typeBuilder.DefineProperty(
					propertyInfo.Name,
					propertyInfo.Attributes,
					propertyInfo.PropertyType,
					propertyInfo.GetIndexParameters()
						.Select(p => p.ParameterType)
						.ToArray());

				if (propertyInfo.GetMethod != default)
					propertyBuilder.SetGetMethod(methodBuilders[propertyInfo.GetMethod].Item2);

				if (propertyInfo.SetMethod != default)
					propertyBuilder.SetSetMethod(methodBuilders[propertyInfo.SetMethod].Item2);
			}

			var constructorInfo = Implementation<TValue>.constructorInfo;

			var constructorBuilder = typeBuilder.DefineConstructor(
				MethodAttributes.Public |
				MethodAttributes.HideBySig |
				MethodAttributes.SpecialName |
				MethodAttributes.RTSpecialName,
				constructorInfo.CallingConvention,
				constructorInfo.GetParameters().Select(p => p.ParameterType).ToArray());

			var ilGenerator = constructorBuilder.GetILGenerator();

			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldarg_1);
			ilGenerator.Emit(OpCodes.Call, constructorInfo);
			ilGenerator.Emit(OpCodes.Nop);
			ilGenerator.Emit(OpCodes.Nop);
			ilGenerator.Emit(OpCodes.Ret);

			const string CREATE_INSTANCE = "CreateInstance";

			var instanceBuilder = typeBuilder.DefineMethod(
				CREATE_INSTANCE,
				MethodAttributes.Public |
				MethodAttributes.HideBySig |
				MethodAttributes.Static,
				typeof(TInterface),
				new Type[] { typeof(TValue) });

			ilGenerator = instanceBuilder.GetILGenerator();

			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Newobj, constructorBuilder);
			ilGenerator.Emit(OpCodes.Ret);

			var entryPointBuilder = typeBuilder.DefineMethod(
				"EntryPoint",
				MethodAttributes.Public |
				MethodAttributes.Static,
				typeof(void),
				Array.Empty<Type>());

			entryPointBuilder.GetILGenerator().Emit(OpCodes.Ret);

			//assemblyBuilder.SetEntryPoint(entryPointBuilder);

			Type = typeBuilder.CreateType();

			CreateInstance = (Func<TValue, TInterface>)Type.GetMethod(
				CREATE_INSTANCE,
				BindingFlags.Public |
				BindingFlags.Static).CreateDelegate(typeof(Func<TValue, TInterface>));

			//assemblyBuilder.Save(name + ".dll");
		}

		static MethodBuilder CreatePassThruMethod(TypeBuilder typeBuilder, MethodInfo methodInfo)
		{
			var parameters = methodInfo.GetParameters();

			var methodBuilder = typeBuilder.DefineMethod(
				methodInfo.Name,
				(methodInfo.Attributes & (~MethodAttributes.Abstract)) | MethodAttributes.Virtual,
				methodInfo.ReturnType,
				parameters
					.Select(p => p.ParameterType)
					.ToArray());

			var ilGenerator = methodBuilder.GetILGenerator();

			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Call, Implementation<TValue>.valuePropertyInfo.GetMethod);

			if (parameters.Length >= 1)
				ilGenerator.Emit(OpCodes.Ldarg_1);

			if (parameters.Length >= 2)
				ilGenerator.Emit(OpCodes.Ldarg_2);

			if (parameters.Length >= 3)
				ilGenerator.Emit(OpCodes.Ldarg_3);

			if (parameters.Length > 3)
				foreach (var (i, parameter) in parameters.Skip(3).Enumerate(start: 3))
					ilGenerator.Emit(OpCodes.Ldarg_S, checked((byte)i));

			ilGenerator.Emit(OpCodes.Callvirt, methodInfo);
			ilGenerator.Emit(OpCodes.Ret);

			return methodBuilder;
		}
	}
}