using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MiniJSON
{
	public static class Json
	{
		public static object Deserialize(string json)
		{
			bool flag = json == null;
			object obj;
			if (flag)
			{
				obj = null;
			}
			else
			{
				obj = Json.Parser.Parse(json);
			}
			return obj;
		}

		public static string Serialize(object obj)
		{
			return Json.Serializer.Serialize(obj);
		}

		private sealed class Parser : IDisposable
		{
			public static bool IsWordBreak(char c)
			{
				return char.IsWhiteSpace(c) || "{}[],:\"".IndexOf(c) != -1;
			}

			private Parser(string jsonString)
			{
				this.json = new StringReader(jsonString);
			}

			public static object Parse(string jsonString)
			{
				object obj;
				using (Json.Parser parser = new Json.Parser(jsonString))
				{
					obj = parser.ParseValue();
				}
				return obj;
			}

			public void Dispose()
			{
				this.json.Dispose();
				this.json = null;
			}

			private Dictionary<string, object> ParseObject()
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				this.json.Read();
				for (;;)
				{
					Json.Parser.TOKEN nextToken = this.NextToken;
					Json.Parser.TOKEN token = nextToken;
					if (token == Json.Parser.TOKEN.NONE)
					{
						break;
					}
					if (token == Json.Parser.TOKEN.CURLY_CLOSE)
					{
						goto IL_0037;
					}
					if (token != Json.Parser.TOKEN.COMMA)
					{
						string text = this.ParseString();
						bool flag = text == null;
						if (flag)
						{
							goto Block_4;
						}
						bool flag2 = this.NextToken != Json.Parser.TOKEN.COLON;
						if (flag2)
						{
							goto Block_5;
						}
						this.json.Read();
						dictionary[text] = this.ParseValue();
					}
				}
				return null;
				IL_0037:
				return dictionary;
				Block_4:
				return null;
				Block_5:
				return null;
			}

			private List<object> ParseArray()
			{
				List<object> list = new List<object>();
				this.json.Read();
				bool flag = true;
				while (flag)
				{
					Json.Parser.TOKEN nextToken = this.NextToken;
					Json.Parser.TOKEN token = nextToken;
					Json.Parser.TOKEN token2 = token;
					if (token2 == Json.Parser.TOKEN.NONE)
					{
						return null;
					}
					if (token2 != Json.Parser.TOKEN.SQUARED_CLOSE)
					{
						if (token2 != Json.Parser.TOKEN.COMMA)
						{
							object obj = this.ParseByToken(nextToken);
							list.Add(obj);
						}
					}
					else
					{
						flag = false;
					}
				}
				return list;
			}

			private object ParseValue()
			{
				Json.Parser.TOKEN nextToken = this.NextToken;
				return this.ParseByToken(nextToken);
			}

			private object ParseByToken(Json.Parser.TOKEN token)
			{
				switch (token)
				{
				case Json.Parser.TOKEN.CURLY_OPEN:
					return this.ParseObject();
				case Json.Parser.TOKEN.SQUARED_OPEN:
					return this.ParseArray();
				case Json.Parser.TOKEN.STRING:
					return this.ParseString();
				case Json.Parser.TOKEN.NUMBER:
					return this.ParseNumber();
				case Json.Parser.TOKEN.TRUE:
					return true;
				case Json.Parser.TOKEN.FALSE:
					return false;
				case Json.Parser.TOKEN.NULL:
					return null;
				}
				return null;
			}

			private string ParseString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				this.json.Read();
				bool flag = true;
				while (flag)
				{
					bool flag2 = this.json.Peek() == -1;
					if (flag2)
					{
						break;
					}
					char c = this.NextChar;
					char c2 = c;
					char c3 = c2;
					if (c3 != '"')
					{
						if (c3 != '\\')
						{
							stringBuilder.Append(c);
						}
						else
						{
							bool flag3 = this.json.Peek() == -1;
							if (flag3)
							{
								flag = false;
							}
							else
							{
								c = this.NextChar;
								char c4 = c;
								char c5 = c4;
								if (c5 <= '\\')
								{
									if (c5 == '"' || c5 == '/' || c5 == '\\')
									{
										stringBuilder.Append(c);
									}
								}
								else if (c5 <= 'f')
								{
									if (c5 != 'b')
									{
										if (c5 == 'f')
										{
											stringBuilder.Append('\f');
										}
									}
									else
									{
										stringBuilder.Append('\b');
									}
								}
								else if (c5 != 'n')
								{
									switch (c5)
									{
									case 'r':
										stringBuilder.Append('\r');
										break;
									case 't':
										stringBuilder.Append('\t');
										break;
									case 'u':
									{
										char[] array = new char[4];
										for (int i = 0; i < 4; i++)
										{
											array[i] = this.NextChar;
										}
										stringBuilder.Append((char)Convert.ToInt32(new string(array), 16));
										break;
									}
									}
								}
								else
								{
									stringBuilder.Append('\n');
								}
							}
						}
					}
					else
					{
						flag = false;
					}
				}
				return stringBuilder.ToString();
			}

			private object ParseNumber()
			{
				string nextWord = this.NextWord;
				bool flag = nextWord.IndexOf('.') == -1;
				object obj;
				if (flag)
				{
					long num;
					long.TryParse(nextWord, out num);
					obj = num;
				}
				else
				{
					double num2;
					double.TryParse(nextWord, out num2);
					obj = num2;
				}
				return obj;
			}

			private void EatWhitespace()
			{
				while (char.IsWhiteSpace(this.PeekChar))
				{
					this.json.Read();
					bool flag = this.json.Peek() == -1;
					if (flag)
					{
						break;
					}
				}
			}

			private char PeekChar
			{
				get
				{
					return Convert.ToChar(this.json.Peek());
				}
			}

			private char NextChar
			{
				get
				{
					return Convert.ToChar(this.json.Read());
				}
			}

			private string NextWord
			{
				get
				{
					StringBuilder stringBuilder = new StringBuilder();
					while (!Json.Parser.IsWordBreak(this.PeekChar))
					{
						stringBuilder.Append(this.NextChar);
						bool flag = this.json.Peek() == -1;
						if (flag)
						{
							break;
						}
					}
					return stringBuilder.ToString();
				}
			}

			private Json.Parser.TOKEN NextToken
			{
				get
				{
					this.EatWhitespace();
					bool flag = this.json.Peek() == -1;
					Json.Parser.TOKEN token;
					if (flag)
					{
						token = Json.Parser.TOKEN.NONE;
					}
					else
					{
						char peekChar = this.PeekChar;
						char c = peekChar;
						if (c <= '[')
						{
							switch (c)
							{
							case '"':
								return Json.Parser.TOKEN.STRING;
							case '#':
							case '$':
							case '%':
							case '&':
							case '\'':
							case '(':
							case ')':
							case '*':
							case '+':
							case '.':
							case '/':
								break;
							case ',':
								this.json.Read();
								return Json.Parser.TOKEN.COMMA;
							case '-':
							case '0':
							case '1':
							case '2':
							case '3':
							case '4':
							case '5':
							case '6':
							case '7':
							case '8':
							case '9':
								return Json.Parser.TOKEN.NUMBER;
							case ':':
								return Json.Parser.TOKEN.COLON;
							default:
								if (c == '[')
								{
									return Json.Parser.TOKEN.SQUARED_OPEN;
								}
								break;
							}
						}
						else
						{
							if (c == ']')
							{
								this.json.Read();
								return Json.Parser.TOKEN.SQUARED_CLOSE;
							}
							if (c == '{')
							{
								return Json.Parser.TOKEN.CURLY_OPEN;
							}
							if (c == '}')
							{
								this.json.Read();
								return Json.Parser.TOKEN.CURLY_CLOSE;
							}
						}
						string nextWord = this.NextWord;
						string text = nextWord;
						if (!(text == "false"))
						{
							if (!(text == "true"))
							{
								if (!(text == "null"))
								{
									token = Json.Parser.TOKEN.NONE;
								}
								else
								{
									token = Json.Parser.TOKEN.NULL;
								}
							}
							else
							{
								token = Json.Parser.TOKEN.TRUE;
							}
						}
						else
						{
							token = Json.Parser.TOKEN.FALSE;
						}
					}
					return token;
				}
			}

			private const string WORD_BREAK = "{}[],:\"";

			private StringReader json;

			private enum TOKEN
			{
				NONE,
				CURLY_OPEN,
				CURLY_CLOSE,
				SQUARED_OPEN,
				SQUARED_CLOSE,
				COLON,
				COMMA,
				STRING,
				NUMBER,
				TRUE,
				FALSE,
				NULL
			}
		}

		private sealed class Serializer
		{
			private Serializer()
			{
				this.builder = new StringBuilder();
			}

			public static string Serialize(object obj)
			{
				Json.Serializer serializer = new Json.Serializer();
				serializer.SerializeValue(obj);
				return serializer.builder.ToString();
			}

			private void SerializeValue(object value)
			{
				bool flag = value == null;
				if (flag)
				{
					this.builder.Append("null");
				}
				else
				{
					string text;
					bool flag2 = (text = value as string) != null;
					if (flag2)
					{
						this.SerializeString(text);
					}
					else
					{
						bool flag3 = value is bool;
						if (flag3)
						{
							this.builder.Append(((bool)value) ? "true" : "false");
						}
						else
						{
							IList list;
							bool flag4 = (list = value as IList) != null;
							if (flag4)
							{
								this.SerializeArray(list);
							}
							else
							{
								IDictionary dictionary;
								bool flag5 = (dictionary = value as IDictionary) != null;
								if (flag5)
								{
									this.SerializeObject(dictionary);
								}
								else
								{
									bool flag6 = value is char;
									if (flag6)
									{
										this.SerializeString(new string((char)value, 1));
									}
									else
									{
										this.SerializeOther(value);
									}
								}
							}
						}
					}
				}
			}

			private void SerializeObject(IDictionary obj)
			{
				bool flag = true;
				this.builder.Append('{');
				foreach (object obj2 in obj.Keys)
				{
					bool flag2 = !flag;
					if (flag2)
					{
						this.builder.Append(',');
					}
					this.SerializeString(obj2.ToString());
					this.builder.Append(':');
					this.SerializeValue(obj[obj2]);
					flag = false;
				}
				this.builder.Append('}');
			}

			private void SerializeArray(IList anArray)
			{
				this.builder.Append('[');
				bool flag = true;
				foreach (object obj in anArray)
				{
					bool flag2 = !flag;
					if (flag2)
					{
						this.builder.Append(',');
					}
					this.SerializeValue(obj);
					flag = false;
				}
				this.builder.Append(']');
			}

			private void SerializeString(string str)
			{
				this.builder.Append('"');
				char[] array = str.ToCharArray();
				char[] array2 = array;
				int i = 0;
				while (i < array2.Length)
				{
					char c = array2[i];
					char c2 = c;
					char c3 = c2;
					switch (c3)
					{
					case '\b':
						this.builder.Append("\\b");
						break;
					case '\t':
						this.builder.Append("\\t");
						break;
					case '\n':
						this.builder.Append("\\n");
						break;
					case '\v':
						goto IL_00F6;
					case '\f':
						this.builder.Append("\\f");
						break;
					case '\r':
						this.builder.Append("\\r");
						break;
					default:
						if (c3 != '"')
						{
							if (c3 != '\\')
							{
								goto IL_00F6;
							}
							this.builder.Append("\\\\");
						}
						else
						{
							this.builder.Append("\\\"");
						}
						break;
					}
					IL_0154:
					i++;
					continue;
					IL_00F6:
					int num = Convert.ToInt32(c);
					bool flag = num >= 32 && num <= 126;
					if (flag)
					{
						this.builder.Append(c);
					}
					else
					{
						this.builder.Append("\\u");
						this.builder.Append(num.ToString("x4"));
					}
					goto IL_0154;
				}
				this.builder.Append('"');
			}

			private void SerializeOther(object value)
			{
				bool flag = value is float;
				if (flag)
				{
					this.builder.Append(((float)value).ToString("R"));
				}
				else
				{
					bool flag2 = value is int || value is uint || value is long || value is sbyte || value is byte || value is short || value is ushort || value is ulong;
					if (flag2)
					{
						this.builder.Append(value);
					}
					else
					{
						bool flag3 = value is double || value is decimal;
						if (flag3)
						{
							this.builder.Append(Convert.ToDouble(value).ToString("R"));
						}
						else
						{
							this.SerializeString(value.ToString());
						}
					}
				}
			}

			private StringBuilder builder;
		}
	}
}
