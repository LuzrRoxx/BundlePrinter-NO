using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using MiniJSON;

namespace Blueprinter
{
	internal static class JsonUtilities
	{
		public static T Deserialize<T>(string json)
		{
			object obj = Json.Deserialize(json);
			return (T)((object)JsonUtilities.ConvertToType(obj, typeof(T)));
		}

		private static object ConvertToType(object value, Type targetType)
		{
			bool flag = value == null;
			object obj;
			if (flag)
			{
				obj = (targetType.IsValueType ? Activator.CreateInstance(targetType) : null);
			}
			else
			{
				bool flag2 = targetType.IsAssignableFrom(value.GetType());
				if (flag2)
				{
					obj = value;
				}
				else
				{
					bool isEnum = targetType.IsEnum;
					if (isEnum)
					{
						string text = value as string;
						bool flag3 = text != null && Enum.IsDefined(targetType, text);
						if (flag3)
						{
							obj = Enum.Parse(targetType, text, true);
						}
						else
						{
							obj = Enum.ToObject(targetType, Convert.ChangeType(value, Enum.GetUnderlyingType(targetType), CultureInfo.InvariantCulture));
						}
					}
					else
					{
						bool flag4 = targetType == typeof(string);
						if (flag4)
						{
							obj = Convert.ToString(value, CultureInfo.InvariantCulture);
						}
						else
						{
							bool flag5 = JsonUtilities.IsNumericType(targetType) || targetType == typeof(bool);
							if (flag5)
							{
								obj = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
							}
							else
							{
								IList list = null;
								bool flag6;
								if (targetType.IsArray)
								{
									list = value as IList;
									flag6 = list != null;
								}
								else
								{
									flag6 = false;
								}
								bool flag7 = flag6;
								if (flag7)
								{
									obj = JsonUtilities.ConvertToArray(list, targetType.GetElementType());
								}
								else
								{
									IList list2 = null;
									bool flag8;
									if (JsonUtilities.IsGenericList(targetType))
									{
										list2 = value as IList;
										flag8 = list2 != null;
									}
									else
									{
										flag8 = false;
									}
									bool flag9 = flag8;
									if (flag9)
									{
										obj = JsonUtilities.ConvertToList(list2, targetType);
									}
									else
									{
										IDictionary dictionary = null;
										bool flag10;
										if (JsonUtilities.IsDictionary(targetType))
										{
											dictionary = value as IDictionary;
											flag10 = dictionary != null;
										}
										else
										{
											flag10 = false;
										}
										bool flag11 = flag10;
										if (flag11)
										{
											obj = JsonUtilities.ConvertToDictionary(dictionary, targetType);
										}
										else
										{
											IDictionary<string, object> dictionary2 = value as IDictionary<string, object>;
											bool flag12 = dictionary2 != null;
											if (flag12)
											{
												obj = JsonUtilities.ConvertToObject(dictionary2, targetType);
											}
											else
											{
												obj = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return obj;
		}

		private static object ConvertToObject(IDictionary<string, object> dict, Type targetType)
		{
			object obj = Activator.CreateInstance(targetType);
			FieldInfo[] fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public);
			foreach (FieldInfo fieldInfo in fields)
			{
				object obj2;
				bool flag = !dict.TryGetValue(fieldInfo.Name, out obj2);
				if (!flag)
				{
					object obj3 = JsonUtilities.ConvertToType(obj2, fieldInfo.FieldType);
					fieldInfo.SetValue(obj, obj3);
				}
			}
			return obj;
		}

		private static Array ConvertToArray(IList listValue, Type elementType)
		{
			Array array = Array.CreateInstance(elementType, listValue.Count);
			for (int i = 0; i < listValue.Count; i++)
			{
				array.SetValue(JsonUtilities.ConvertToType(listValue[i], elementType), i);
			}
			return array;
		}

		private static object ConvertToList(IList sourceList, Type targetType)
		{
			Type type = targetType.GetGenericArguments().FirstOrDefault<Type>() ?? typeof(object);
			IList list = (IList)Activator.CreateInstance(targetType);
			foreach (object obj in sourceList)
			{
				list.Add(JsonUtilities.ConvertToType(obj, type));
			}
			return list;
		}

		private static object ConvertToDictionary(IDictionary sourceDict, Type targetType)
		{
			Type[] genericArguments = targetType.GetGenericArguments();
			Type type = ((genericArguments.Length != 0) ? genericArguments[0] : typeof(object));
			Type type2 = ((genericArguments.Length > 1) ? genericArguments[1] : typeof(object));
			IDictionary dictionary = (IDictionary)Activator.CreateInstance(targetType);
			foreach (object obj in sourceDict)
			{
				DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
				object obj2 = JsonUtilities.ConvertToType(dictionaryEntry.Key, type);
				object obj3 = JsonUtilities.ConvertToType(dictionaryEntry.Value, type2);
				dictionary[obj2] = obj3;
			}
			return dictionary;
		}

		private static bool IsNumericType(Type type)
		{
			TypeCode typeCode = Type.GetTypeCode(type);
			TypeCode typeCode2 = typeCode;
			return typeCode2 - TypeCode.SByte <= 10;
		}

		private static bool IsGenericList(Type type)
		{
			return type.IsGenericType && typeof(IList).IsAssignableFrom(type);
		}

		private static bool IsDictionary(Type type)
		{
			return type.IsGenericType && typeof(IDictionary).IsAssignableFrom(type);
		}
	}
}
