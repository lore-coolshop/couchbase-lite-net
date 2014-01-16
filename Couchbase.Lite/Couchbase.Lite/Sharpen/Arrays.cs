using System.Threading.Tasks;

namespace Sharpen
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	internal class Arrays
	{
		public static List<T> AsList<T> (params T[] array)
		{
			return array.ToList<T> ();
		}

		public static bool Equals<T> (T[] a1, T[] a2)
		{
			if (a1.Length != a2.Length) {
				return false;
			}
			for (int i = 0; i < a1.Length; i++) {
				if (!a1[i].Equals (a2[i])) {
					return false;
				}
			}
			return true;
		}

		public static void Fill<T> (T[] array, T val)
		{
			Fill<T> (array, 0, array.Length, val);
		}

		public static void Fill<T> (T[] array, int start, int end, T val)
		{
			for (int i = start; i < end; i++) {
				array[i] = val;
			}
		}

		public static void Sort (string[] array)
		{
			Array.Sort (array, (s1,s2) => string.CompareOrdinal (s1,s2));
		}

		public static void Sort<T> (T[] array)
		{
			Array.Sort<T> (array);
		}

		public static void Sort<T> (T[] array, IComparer<T> c)
		{
			Array.Sort<T> (array, c);
		}

		public static void Sort<T> (T[] array, int start, int count)
		{
			Array.Sort<T> (array, start, count);
		}

		public static void Sort<T> (T[] array, int start, int count, IComparer<T> c)
		{
			Array.Sort<T> (array, start, count, c);
		}

        public static int HashCode<T> (T[] array) {
            var hashCode = 1;
            foreach(T item in array) {
                hashCode = 31 * hashCode + ((item == null) ? 0 : item.GetHashCode());
            }
            return hashCode;
        }

        public static T[] CopyTo<T> (T[] original, Int32 length)
        {
            var copy = new T[length];
            Array.Copy(original, copy, length < original.Length ? length : original.Length);
            return copy;
        }

        public static T[] CopyTo<T> (T[] original, Int64 length)
        {
            var copy = new T[length];
            Array.Copy(original, copy, length < original.Length ? length : original.LongLength);
            return copy;
        }
	}
}
