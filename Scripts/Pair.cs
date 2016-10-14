#if UNITY_EDITOR || RUNTIME_CSG

namespace Sabresaurus.SabreCSG
{
	public struct Pair<T, U>
	{
		T first;
		U second;

		public T First 
		{
			get 
			{
				return first;
			}
		}

		public U Second 
		{
			get 
			{
				return second;
			}
		}

		public Pair (T first, U second)
		{
			this.first = first;
			this.second = second;
		}
	}

}
#endif