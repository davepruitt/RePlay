using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RePlay_Common
{
    public static class ListExtensionMethods
    {
        /// <summary>
        /// Limits a list to a specified number of elements.
        /// </summary>
        public static void LimitTo<T> (this List<T> original_list, int num_elements, bool take_from_back = false)
        {
            if (original_list.Count > 0 && num_elements > 0 && original_list.Count > num_elements)
            {
                int num_to_remove = original_list.Count - num_elements;

                if (take_from_back)
                {
                    original_list.RemoveRange(0, num_to_remove);
                }
                else
                {
                    int start_index = original_list.Count - num_to_remove;
                    original_list.RemoveRange(start_index, num_to_remove);
                }
            }
        }

        /// <summary>
        /// Shuffles a list so elements are in random order
        /// </summary>
        /// <param name="original_list">The original, unshuffled list</param>
        /// <returns>A shuffled list with elements in random order</returns>
        public static List<T> ShuffleList<T>(this List<T> original_list)
        {
            //Copy the original list.
            List<T> result = original_list.ToList();

            var random = RandomNumberStatic.RandomNumbers;

            int n = result.Count;
            while (n > 1)
            {
                n--;
                int i = random.Next(n + 1);
                T temp = result[i];
                result[i] = result[n];
                result[n] = temp;
            }

            return result;
        }

        /// <summary>
        /// Calculates the mode of a list
        /// </summary>
        public static T Mode<T>(this List<T> original_list)
        {
            var groups = original_list.GroupBy(v => v);
            int max_count = groups.Max(g => g.Count());
            T mode = groups.First(g => g.Count() == max_count).Key;
            return mode;
        }

        /// <summary>
        /// Calculates the index of the minimum element in a list of integers
        /// </summary>
        public static int IndexOfMin(this IList<int> self)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (self.Count == 0)
            {
                throw new ArgumentException("List is empty.", "self");
            }

            int min = self[0];
            int minIndex = 0;

            for (int i = 1; i < self.Count; ++i)
            {
                if (self[i] < min)
                {
                    min = self[i];
                    minIndex = i;
                }
            }

            return minIndex;
        }

        /// <summary>
        /// Calculates the index of the minimum element in a list of floats
        /// </summary>
        public static int IndexOfMin(this IList<float> self)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (self.Count == 0)
            {
                throw new ArgumentException("List is empty.", "self");
            }

            float min = self[0];
            int minIndex = 0;

            for (int i = 1; i < self.Count; ++i)
            {
                if (self[i] < min)
                {
                    min = self[i];
                    minIndex = i;
                }
            }

            return minIndex;
        }
    }
}
