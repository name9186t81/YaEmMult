using System;

namespace Core
{
	public interface IService
	{
		event Action<Type> RemoveCallback;
	}
}
