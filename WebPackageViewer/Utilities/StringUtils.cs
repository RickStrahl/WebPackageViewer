using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebPackageViewer.Utilities
{
    internal static class StringUtils
    {
        /// <summary>
        /// Extracts a string from between a pair of delimiters. Only the first 
        /// instance is found.
        /// </summary>
        /// <param name="source">Input String to work on</param>
        /// <param name="beginDelim">Beginning delimiter</param>
        /// <param name="endDelim">ending delimiter</param>
        /// <param name="caseSensitive">Determines whether the search for delimiters is case sensitive</param>        
        /// <param name="allowMissingEndDelimiter"></param>
        /// <param name="returnDelimiters"></param>
        /// <returns>Extracted string or string.Empty on no match</returns>
        public static string ExtractString(this string source,
            string beginDelim,
            string endDelim,
            bool caseSensitive = false,
            bool allowMissingEndDelimiter = false,
            bool returnDelimiters = false)
        {
            int at1, at2;

            if (string.IsNullOrEmpty(source))
                return string.Empty;

            if (caseSensitive)
            {
                at1 = source.IndexOf(beginDelim, StringComparison.CurrentCulture);
                if (at1 == -1)
                    return string.Empty;

                at2 = source.IndexOf(endDelim, at1 + beginDelim.Length, StringComparison.CurrentCulture);
            }
            else
            {
                //string Lower = source.ToLower();
                at1 = source.IndexOf(beginDelim, 0, source.Length, StringComparison.OrdinalIgnoreCase);
                if (at1 == -1)
                    return string.Empty;

                at2 = source.IndexOf(endDelim, at1 + beginDelim.Length, StringComparison.OrdinalIgnoreCase);
            }

            if (allowMissingEndDelimiter && at2 < 0)
            {
                if (!returnDelimiters)
                    return source.Substring(at1 + beginDelim.Length);

                return source.Substring(at1);
            }

            if (at1 > -1 && at2 > 1)
            {
                if (!returnDelimiters)
                    return source.Substring(at1 + beginDelim.Length, at2 - at1 - beginDelim.Length);

                return source.Substring(at1, at2 - at1 + endDelim.Length);
            }

            return string.Empty;
        }

        /// <summary>
        /// Generates a unique Id as a string of up to 16 characters.
        /// Based on a GUID and the size takes that subset of a the
        /// Guid's 16 bytes to create a string id.
        /// 
        /// String Id contains numbers and lower case alpha chars 36 total.
        /// 
        /// Sizes: 6 gives roughly 99.97% uniqueness. 
        ///        8 gives less than 1 in a million doubles.
        ///        16 will give near full GUID strength uniqueness
        /// </summary>
        /// <param name="stringSize">Number of characters to generate between 8 and 16</param>
        /// <param name="additionalCharacters">Any additional characters you allow in the string. 
        /// You can add upper case letters and symbols which are not included in the default
        /// which includes only digits and lower case letters.
        /// </param>
        /// <returns></returns>        
        public static string GenerateUniqueId(int stringSize = 8, string additionalCharacters = null)
        {
            string chars = "abcdefghijkmnopqrstuvwxyz1234567890" + (additionalCharacters ?? string.Empty);
            StringBuilder result = new StringBuilder(stringSize);
            int count = 0;


            foreach (byte b in Guid.NewGuid().ToByteArray())
            {
                result.Append(chars[b % (chars.Length)]);
                count++;
                if (count >= stringSize)
                    break;
            }
            return result.ToString();
        }

    }
}
