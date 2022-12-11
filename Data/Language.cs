﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageReimaginer.Data
{
    /// <summary>
    /// Language showcasing:
    ///     - Fauxglish. A language designed to mimic the patterns of English.
    ///     - Elvish.
    ///     - Orcish.
    ///     - Singing. A silly language. Uses L, D, A, E, O, no double consonants, yet triple vowel potential.
    /// </summary>

    public class Language
    {
        /// <summary>
        /// The name of the language.
        /// </summary>
        public string META_Name { get; set; } = string.Empty;
        /// <summary>
        /// The description of the language.
        /// </summary>
        public string META_Description { get; set; } = string.Empty;
        /// <summary>
        /// The author of the language.
        /// </summary>
        public string META_Author { get; set; } = string.Empty;

        public LanguageOptions Options { get; set; } = new LanguageOptions();

        //Ordered by the simplest components to the most complex. (i.e, by density)
        public Alphabet Alphabet { get; private set; }
        public Pathways Pathways { get; private set; }
        public Structure Structure { get; private set; }
        public Lexemes Lexemes { get; private set; }
        public Flagging Flagging { get; private set; }
        public Punctuation Punctuation { get; private set; }

        public Language()
        {
            Alphabet = new Alphabet();
            Structure = new Structure();
            Lexemes = new Lexemes();
            Flagging = new Flagging();
            Punctuation = new Punctuation();
        }
    }
    public class LanguageOptions
    {
        public int Seed { get; set; } = 0;
        /// <summary>
        /// A global seed used to offset the seed generated for each word. Default is 0.
        /// Increasing or decreasing will change every word of the generated language.
        /// </summary>
        public int SeedOffset { get; set; } = 0;
        /// <summary>
        /// If true, the generator will attempt to match the output word's cade to the input word's case.
        /// Because a generated word may be longer or shorter, a true 1:1 match is impossible.
        /// 
        /// When the first letter is the only capitalized letter, the first letter of the output matches.
        /// When all of the letters are of the same case, the output will match.
        /// If, for some reason, the case of the input word is a mix of lower and uppercase, the output is randomized.
        /// 
        /// If false, all letters are lower case.
        /// </summary>
        public bool PreserveCase { get; set; } = true;
        /// <summary>
        /// Helps determine how the generator splits words for processing.
        /// Default is just ' ' (space).
        /// </summary>
        public char[] Delimiters { get; set; } = new char[] { ' ' };

        public enum LetterPathing { Exclusion, Inclusion, EndWord }
        /// <summary>
        /// Determines how the generator behaves if it encounters a letter path with no way forward, yet has more letters to generate.
        /// <para name="Exclusion">For language author's with a solid grip on their language. This will error if there isn't a next path.</para>
        /// <para name="Inclusion">With no valid path forward, the generator will default to selecting a letter by StartWeight that fits the sigma template.</para>
        /// <para name="EndWord">Tthe generator will simply end the word. While this is the safest option, too many empy pathways will result in a lot of short words.</para>
        /// </summary>
        public LetterPathing Pathing { get; set; } = LetterPathing.EndWord;

        /// <summary>
        /// All lowercase letters in your input language. This is for estimating syllable count, and defaults to english.
        /// </summary>
        public char[] InputLetters { get; set; } = "abcdefghijklmnopqrstuvwxyz".ToArray();
        /// <summary>
        /// Tells the generator how much lower the word's syllable count could be from the syllable estimate. Default is 0.6
        /// </summary>
        public double SigmaSkewMin { get; set; } = 0.6;
        /// <summary>
        /// Tells the generator how much higher the word's syllable count could be from the syllable estimate. Default is 1.2.
        /// </summary>
        public double SigmaSkewMax { get; set; } = 1.2;
    }
}