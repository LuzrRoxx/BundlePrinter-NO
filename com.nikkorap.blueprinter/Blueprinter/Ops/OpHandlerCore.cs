using System;

namespace Blueprinter.Ops
{
	public abstract class OpHandlerCore
	{
		public abstract string opId { get; }

		public abstract void Execute(LoadedBundle bundle, string payloadJson);
	}
}
