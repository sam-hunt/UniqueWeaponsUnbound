using System.Collections.Generic;

namespace UniqueWeaponsUnbound
{
    // Pure, stateless label/defName tokenization used by the material-override
    // matcher and the trait cost rule keyword matcher. No dependency on the
    // material caches in the main file.
    public static partial class CostRuleHelpers
    {
        // Splits a trait label into a word set containing both the full
        // space-delimited words and the hyphen-delimited parts of any
        // hyphenated words. E.g. "crypto-coated rails" → {"crypto-coated",
        // "rails", "crypto", "coated"}.
        public static HashSet<string> SplitLabelWords(string label)
        {
            var words = new HashSet<string>();
            if (string.IsNullOrEmpty(label))
                return words;

            foreach (string word in label.ToLowerInvariant().Split(' '))
            {
                words.Add(word);
                if (word.Contains("-"))
                {
                    foreach (string part in word.Split('-'))
                    {
                        if (part.Length > 0)
                            words.Add(part);
                    }
                }
            }

            return words;
        }

        // Splits a defName into a lowercased word set so rules still match when
        // the trait label is fully localized. A leading underscore-delimited
        // segment is a mod-prefix acronym and is dropped, so "AArmoury_Oversized"
        // → {"oversized"} and no rule can match on the prefix itself. DefNames
        // without an underscore follow the vanilla convention and are kept whole.
        // The remainder splits on PascalCase boundaries and on any non-letter
        // character: "EMPBlaster" → {"emp", "blaster"}, "ChargeRifle2X" →
        // {"charge", "rifle", "x"}.
        public static HashSet<string> SplitDefNameWords(string defName)
        {
            var words = new HashSet<string>();
            if (string.IsNullOrEmpty(defName))
                return words;

            int prefixEnd = defName.IndexOf('_');
            string body = prefixEnd >= 0 ? defName.Substring(prefixEnd + 1) : defName;
            if (string.IsNullOrEmpty(body))
                return words;

            var token = new System.Text.StringBuilder(body.Length);

            for (int i = 0; i < body.Length; i++)
            {
                char c = body[i];

                if (!char.IsLetter(c))
                {
                    FlushToken(token, words);
                    continue;
                }

                // Start a new token at a lower→upper transition, and at the last
                // uppercase of an acronym run when a lowercase letter follows
                // (so "EMPBlaster" breaks between "EMP" and "Blaster").
                if (token.Length > 0 && char.IsUpper(c))
                {
                    char prev = token[token.Length - 1];
                    bool endsAcronymRun = char.IsUpper(prev)
                        && i + 1 < body.Length && char.IsLower(body[i + 1]);
                    if (!char.IsUpper(prev) || endsAcronymRun)
                        FlushToken(token, words);
                }

                token.Append(c);
            }

            FlushToken(token, words);
            return words;
        }

        private static void FlushToken(System.Text.StringBuilder token, HashSet<string> words)
        {
            if (token.Length == 0)
                return;

            words.Add(token.ToString().ToLowerInvariant());
            token.Length = 0;
        }
    }
}
