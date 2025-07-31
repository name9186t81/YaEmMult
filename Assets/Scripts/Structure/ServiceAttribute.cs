using System;

namespace Core
{
	public sealed class ServiceAttribute : Attribute
	{
		public bool InitFromStart;

		public ServiceAttribute(bool initFromStart)
		{
			InitFromStart = initFromStart;
		}
	}
}
