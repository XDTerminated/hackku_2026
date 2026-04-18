using System.Text;
using System.Text.RegularExpressions;

namespace HackKU.Core
{
    // TTS-friendly string transforms. ElevenLabs (and most neural TTS) stumble on "$" and
    // on long comma-separated numbers. Convert "$1,240" into "one thousand two hundred
    // forty dollars" before speaking so the synthesis is clean.
    public static class SpeechUtils
    {
        static readonly Regex DollarRx =
            new Regex(@"\$\s*(\d{1,3}(?:,\d{3})+|\d+)(?:\.(\d+))?", RegexOptions.Compiled);

        // Also catch "400 dollars" / "400 bucks" phrasings.
        static readonly Regex SuffixedRx =
            new Regex(@"\b(\d{1,3}(?:,\d{3})+|\d{2,})(?=\s+(?:dollars?|bucks?)\b)", RegexOptions.Compiled);

        public static string SpeakifyMoney(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            // 1) "$N" or "$N.C" — rewrite to "N dollars" (drop cents, we don't care for speech).
            s = DollarRx.Replace(s, m =>
            {
                string whole = m.Groups[1].Value.Replace(",", "");
                if (!long.TryParse(whole, out long n)) return m.Value;
                return NumberToWords(n) + " dollars";
            });

            // 2) "1,234" standalone (with a dollars/bucks suffix) → spoken. Leaves "year 2028" etc alone.
            s = SuffixedRx.Replace(s, m =>
            {
                string whole = m.Groups[1].Value.Replace(",", "");
                if (!long.TryParse(whole, out long n)) return m.Value;
                return NumberToWords(n);
            });

            return s;
        }

        // Standard English number-to-words up to 999,999,999. Fine for the game's scale.
        public static string NumberToWords(long number)
        {
            if (number == 0) return "zero";
            if (number < 0) return "negative " + NumberToWords(-number);

            string[] ones = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
                              "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen",
                              "seventeen", "eighteen", "nineteen" };
            string[] tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

            var sb = new StringBuilder();

            long billions = number / 1000000000;
            number %= 1000000000;
            long millions = number / 1000000;
            number %= 1000000;
            long thousands = number / 1000;
            number %= 1000;
            long hundreds = number;

            void AppendTriplet(long n)
            {
                if (n == 0) return;
                long h = n / 100;
                long rest = n % 100;
                if (h > 0) { sb.Append(ones[h]).Append(" hundred"); if (rest > 0) sb.Append(' '); }
                if (rest >= 20)
                {
                    sb.Append(tens[rest / 10]);
                    if (rest % 10 > 0) sb.Append('-').Append(ones[rest % 10]);
                }
                else if (rest > 0)
                {
                    sb.Append(ones[rest]);
                }
            }

            if (billions > 0) { AppendTriplet(billions); sb.Append(" billion"); if (millions + thousands + hundreds > 0) sb.Append(' '); }
            if (millions > 0) { AppendTriplet(millions); sb.Append(" million"); if (thousands + hundreds > 0) sb.Append(' '); }
            if (thousands > 0) { AppendTriplet(thousands); sb.Append(" thousand"); if (hundreds > 0) sb.Append(' '); }
            if (hundreds > 0) AppendTriplet(hundreds);

            return sb.ToString();
        }
    }
}
