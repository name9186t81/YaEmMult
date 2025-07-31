using System;

namespace Networking
{
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ProcessorAttribute : Attribute
	{
		public enum ProcessorType
		{
			None = 0,
			Server = 1 << 0,
			Client = 1 << 1,

			Both = Server | Client
		}

		public PackageType ProcessedType;
		public ProcessorType Type;

		public ProcessorAttribute(PackageType processedType, ProcessorType type)
		{
			ProcessedType = processedType;
			Type = type;
		}
	}
}
