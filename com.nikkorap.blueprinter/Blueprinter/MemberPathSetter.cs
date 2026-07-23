using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace Blueprinter
{
	public static class MemberPathSetter
	{
		public static void Apply(object target, string memberPath, object asset)
		{
			bool flag = target == null;
			if (flag)
			{
				throw new ArgumentNullException("target");
			}
			bool flag2 = string.IsNullOrEmpty(memberPath);
			if (flag2)
			{
				throw new ArgumentException("Empty memberPath", "memberPath");
			}
			List<MemberPathSetter.Segment> list = MemberPathSetter.ParseMemberPath(memberPath);
			bool flag3 = list.Count == 0;
			if (flag3)
			{
				throw new ArgumentException("Invalid memberPath", "memberPath");
			}
			object obj = target;
			for (int i = 0; i < list.Count - 1; i++)
			{
				obj = MemberPathSetter.GetSegmentValue(obj, list[i]);
			}
			MemberPathSetter.SetMemberPathValue(obj, list[list.Count - 1], asset);
		}

		private static List<MemberPathSetter.Segment> ParseMemberPath(string path)
		{
			List<MemberPathSetter.Segment> list = new List<MemberPathSetter.Segment>();
			StringBuilder stringBuilder = new StringBuilder();
			int i = 0;
			while (i < path.Length)
			{
				char c = path[i];
				bool flag = c == '.';
				if (flag)
				{
					bool flag2 = stringBuilder.Length > 0;
					if (flag2)
					{
						list.Add(new MemberPathSetter.Segment
						{
							Name = stringBuilder.ToString()
						});
						stringBuilder.Length = 0;
					}
					i++;
				}
				else
				{
					bool flag3 = c == '[';
					if (flag3)
					{
						bool flag4 = stringBuilder.Length > 0;
						if (flag4)
						{
							list.Add(new MemberPathSetter.Segment
							{
								Name = stringBuilder.ToString()
							});
							stringBuilder.Length = 0;
						}
						i++;
						int num = i;
						while (i < path.Length && path[i] != ']')
						{
							i++;
						}
						string text = path.Substring(num, i - num);
						int num2 = int.Parse(text);
						list.Add(new MemberPathSetter.Segment
						{
							Index = new int?(num2)
						});
						bool flag5 = i < path.Length && path[i] == ']';
						if (flag5)
						{
							i++;
						}
					}
					else
					{
						stringBuilder.Append(c);
						i++;
					}
				}
			}
			bool flag6 = stringBuilder.Length > 0;
			if (flag6)
			{
				list.Add(new MemberPathSetter.Segment
				{
					Name = stringBuilder.ToString()
				});
			}
			return list;
		}

		private static void SetMemberPathValue(object obj, MemberPathSetter.Segment lastSeg, object value)
		{
			bool flag = obj == null;
			if (flag)
			{
				throw new NullReferenceException("Null while setting member path value.");
			}
			bool isIndex = lastSeg.IsIndex;
			if (isIndex)
			{
				IList list = obj as IList;
				bool flag2 = list != null;
				if (!flag2)
				{
					throw new InvalidOperationException("Object of type " + obj.GetType().FullName + " is not indexable.");
				}
				int value2 = lastSeg.Index.Value;
				bool flag3 = value2 < 0;
				if (flag3)
				{
					throw new IndexOutOfRangeException(string.Format("Index {0} out of range for list of size {1}.", value2, list.Count));
				}
				bool flag4 = value2 >= list.Count;
				if (flag4)
				{
					bool flag5 = list.IsFixedSize || list.IsReadOnly;
					if (flag5)
					{
						throw new IndexOutOfRangeException(string.Format("Index {0} out of range for fixed-size list of size {1}.", value2, list.Count));
					}
					MemberPathSetter.EnsureListSize(list, value2);
				}
				list[value2] = value;
			}
			else
			{
				string name = lastSeg.Name;
				Traverse traverse = Traverse.Create(obj);
				Traverse traverse2 = traverse.Property(name, null);
				bool flag6 = traverse2.PropertyExists();
				if (flag6)
				{
					traverse2.SetValue(value);
				}
				else
				{
					Traverse traverse3 = traverse.Field(name);
					bool flag7 = traverse3.FieldExists();
					if (!flag7)
					{
						throw new MissingMemberException(obj.GetType().FullName, name);
					}
					traverse3.SetValue(value);
				}
			}
		}

		private static void EnsureListSize(IList list, int idx)
		{
			Type listElementType = MemberPathSetter.GetListElementType(list);
			while (list.Count <= idx)
			{
				list.Add(MemberPathSetter.CreateDefault(listElementType));
			}
		}

		private static Type GetListElementType(IList list)
		{
			Type type = list.GetType();
			bool isArray = type.IsArray;
			Type type2;
			if (isArray)
			{
				type2 = type.GetElementType();
			}
			else
			{
				bool isGenericType = type.IsGenericType;
				if (isGenericType)
				{
					Type[] genericArguments = type.GetGenericArguments();
					bool flag = genericArguments.Length == 1;
					if (flag)
					{
						return genericArguments[0];
					}
				}
				foreach (Type type3 in type.GetInterfaces())
				{
					bool flag2 = type3.IsGenericType && type3.GetGenericTypeDefinition() == typeof(IList<>);
					if (flag2)
					{
						return type3.GetGenericArguments()[0];
					}
				}
				type2 = null;
			}
			return type2;
		}

		private static object CreateDefault(Type t)
		{
			bool flag = t == null || !t.IsValueType;
			object obj;
			if (flag)
			{
				obj = null;
			}
			else
			{
				obj = Activator.CreateInstance(t);
			}
			return obj;
		}

		private static object GetSegmentValue(object obj, MemberPathSetter.Segment seg)
		{
			bool flag = obj == null;
			if (flag)
			{
				throw new NullReferenceException("Null while evaluating member path segment.");
			}
			bool isIndex = seg.IsIndex;
			object obj2;
			if (isIndex)
			{
				IList list = obj as IList;
				bool flag2 = list != null;
				if (!flag2)
				{
					throw new InvalidOperationException("Object of type " + obj.GetType().FullName + " is not indexable.");
				}
				int value = seg.Index.Value;
				bool flag3 = value < 0 || value >= list.Count;
				if (flag3)
				{
					throw new IndexOutOfRangeException(string.Format("Index {0} out of range for list of size {1}.", value, list.Count));
				}
				obj2 = list[value];
			}
			else
			{
				Traverse traverse = Traverse.Create(obj);
				Traverse traverse2 = traverse.Property(seg.Name, null);
				bool flag4 = traverse2.PropertyExists();
				if (flag4)
				{
					obj2 = traverse2.GetValue();
				}
				else
				{
					Traverse traverse3 = traverse.Field(seg.Name);
					bool flag5 = traverse3.FieldExists();
					if (!flag5)
					{
						throw new MissingMemberException(obj.GetType().FullName, seg.Name);
					}
					obj2 = traverse3.GetValue();
				}
			}
			return obj2;
		}

		private struct Segment
		{
			public bool IsIndex
			{
				get
				{
					return this.Index != null;
				}
			}

			public string Name;

			public int? Index;
		}
	}
}
