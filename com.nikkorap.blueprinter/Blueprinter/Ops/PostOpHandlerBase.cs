using System;
using System.Collections.Generic;

namespace Blueprinter.Ops
{
	public abstract class PostOpHandlerBase<TPayload> : OpHandlerCore
	{
		public sealed override void Execute(LoadedBundle bundle, string payloadJson)
		{
			bool flag = !this.ValidateBundle(bundle);
			if (!flag)
			{
				TPayload tpayload;
				bool flag2 = !this.TryDeserializePayload(payloadJson, out tpayload);
				if (!flag2)
				{
					bool flag3 = tpayload == null;
					if (flag3)
					{
						Plugin.Log.LogWarning("[" + this.opId + "] payload deserialized to null.");
					}
					else
					{
						this.Handle(bundle, tpayload);
					}
				}
			}
		}

		protected virtual bool ValidateBundle(LoadedBundle bundle)
		{
			bool flag = bundle != null;
			bool flag2;
			if (flag)
			{
				flag2 = true;
			}
			else
			{
				Plugin.Log.LogWarning("[" + this.opId + "] bundle is null.");
				flag2 = false;
			}
			return flag2;
		}

		protected virtual bool TryDeserializePayload(string payloadJson, out TPayload payload)
		{
			payload = default(TPayload);
			bool flag = string.IsNullOrEmpty(payloadJson);
			bool flag2;
			if (flag)
			{
				Plugin.Log.LogWarning("[" + this.opId + "] payloadJson is empty.");
				flag2 = false;
			}
			else
			{
				try
				{
					payload = JsonUtilities.Deserialize<TPayload>(payloadJson);
					flag2 = true;
				}
				catch (Exception ex)
				{
					Plugin.Log.LogError(string.Format("[{0}] failed to deserialize payload: {1}", this.opId, ex));
					flag2 = false;
				}
			}
			return flag2;
		}

		protected abstract void Handle(LoadedBundle bundle, TPayload payload);

		public static bool TryAdd<T>(ICollection<T> list, T item)
		{
			bool flag = list == null || item == null || list.Contains(item);
			bool flag2;
			if (flag)
			{
				flag2 = false;
			}
			else
			{
				list.Add(item);
				flag2 = true;
			}
			return flag2;
		}
	}
}
