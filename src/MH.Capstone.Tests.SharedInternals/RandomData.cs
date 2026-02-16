using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MH.Capstone.Tests.SharedInternals
{
    public static class RandomData
    {
        public static int GetRandomIntInRange(int min, int max) => 
            Random.Shared.Next(min, max + 1); // max + 1 to include max in the range

        public static string GetRandomStringOfLength(int length)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, 0, nameof(length));

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-#$%!.,/?=+_*&^()`~\"\\\' ";

            // This uses LINQ to generate a string of the specified length by randomly selecting characters from the chars string
            // Thanks Copilot and Resharper for the help with this one!
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        public static decimal GetRandomDecimalInRange(decimal min, decimal max)
        {
            double range = (double)(max - min);
            double sample = Random.Shared.NextDouble();
            return (decimal)(sample * range + (double)min); // Scale to range and shift to start at min
        }

        public static IEnumerable<decimal> GetEnumerableOfDecimalsInRangeOfAmount(int amount, decimal min, decimal max)
        {
            for (int i = 0; i < amount; i++)
            {
                yield return GetRandomDecimalInRange(min, max);
            }
        }
    }
}
