﻿using LanguageReimaginer.Data;
using LanguageReimaginer.Data.Elements;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LanguageReimaginer.Operators
{
    /// <summary>
    /// Generator breakdown:
    /// 
    /// 1. Flagging. All valid flags are read, and changes are applied.
    /// 2. Lexemes. All valid affixes are grabbed and separated. The root word is retrieved.
    /// 3. Preparation. The generated word's foundation is prepared.
    ///     - The syllable count of the root word is estimated, by checking the boundaries of "VC" and "CV".
    ///       Example: in·ter·est·ing·ly -> VC·CVC·VC(C)·V(C)C·CV. Double consonants/vowels are stripped (ignored).
    ///     - The syllable structure is generated, according to the language author's specifications. "CCV·VC·VC·CV"
    ///     - The first letter is chosen, according to the first sigma's C/V, and the language's letter weight distribution.
    ///     - The letter pathways are generated by the language author's constraints.
    /// 4. The sigmas are merged together, with any punctuation in between preserved. Affixes are prepended and appended in order.
    /// 5. The word is complete! Repeat for the next word until the sentence is processed and returned.
    /// 
    ///     Lexemes in action: "interestingly"
    ///         |est| is handled entirely by the generator.
    ///         |inter|, |ing|, and |ly| are all affixes. They are handles by the lexeme rules.
    ///     
    ///     Flags in action: "Bateman's**xp" and "dog's**p"
    ///         |Bateman's| is skipped entirely. **x skips generation. **p skips contraction rules, however, **x also superceeds it.
    ///         |'s| is processed according to the language's possessive rules. |dog| is processed as normal.
    ///         
    ///     
    /// 
    /// </summary>
    public class LanguageGenerator
    {
        internal RandomGenerator RanGen { get; set; } = new RandomGenerator();
        //internal SyllableGenerator SyllableGen { get; set; }

        List<WordInfo> wordInfo = new List<WordInfo>();
        StringBuilder sentenceBuilder = new StringBuilder();

        private Language language;
        public Language Language
        {
            get { return language; }
            set
            {
                language = value;
                RanGen.Language = language;
            }
        }

        public LanguageGenerator() { }
        public LanguageGenerator(Language Language) { this.Language = Language; }

        public string Generate(string sentence, out List<WordInfo> info)
        {
            sentenceBuilder.Clear();
            wordInfo.Clear();

            //Pre: Split words by delimiter and add to WordInfo list.

            //Deconstructing a word.
            //1. Punctuation marks (1 of 2). Loop through WordInfo list, isolate all punctuation marks (e.g., comma, or quote/comma),
            //   add punctuation to WordInfo.
            //2. Flagging. Check for flag marcation, add flags to WordInfo, process flags.
            //3. Lexemes. Check for affixes, add prefixes and suffixes to WordInfo.

            //Constructing a new word.
            //4. Estimate sigma (syllable) count by checking the boundaries of "VC" and "CV".
            //5. Generate sigma structure; by length of actual sigma count * sigma weight. "CCV·VC·VC·CV"
            //6. The first letter of the word is chosen; by first sigma's C/V, then by start letter weight.
            //7. The next letters are chosen, according to the pathways set by the language author.

            //Combining together.
            //8. The sigmas of the root word are combined in order.
            //9. The new affixes are retrieved and placed in order.
            //10. The new punctuation marks are adding to the end of the word.


            //This should split a word by the delimiters, and then leave the delimiter at the end.
            string[] words = sentence.Split(Language.Options.Delimiters);//Regex.Split(sentence, "@\"(?<=[" + string.Join("", Language.Options.Delimiters) + "])\"");

            //Loop through split words and add to wordInfo list.
            foreach (string s in words)
            {
                WordInfo word = new WordInfo();
                word.WordActual = s;

                wordInfo.Add(word);
            }

            //Link adjacent words.
            for (int i = 0; i < wordInfo.Count; i++)
            {
                if (i != 0)
                    wordInfo[i].AdjacentLeft = wordInfo[i - 1];
                if (i != wordInfo.Count - 1)
                    wordInfo[i].AdjacentRight = wordInfo[i + 1];
            }

            foreach (WordInfo word in wordInfo)
            {
                bool skipGeneration = false;
                bool skipLexemes = false;

                //The word is checked for flags. If any, the flags are processed and stripped.
                if (Language.Flagging.ContainsFlag(word.WordActual))
                {
                    //Add any flags as char array to WordInfo.
                    int marcation = Language.Flagging.FlagIndex(word.WordActual);
                    string flags = word.WordActual.Substring(marcation, word.WordActual.Length - marcation); //May need + Flagging.Marcation.Length to startIndex.
                    string flagsFinal = string.Empty;

                    foreach (char c in flags)
                    {
                        if (Language.Flagging.Flags.ContainsKey(c))
                            flagsFinal += c;
                    }

                    word.Flags = flagsFinal.ToCharArray();
                }

                //Set generator-level flags.
                if (word.Flags?.Any() == true && word.Flags.Contains('X') == true) skipGeneration = true;
                if (word.Flags?.Any() == true && word.Flags.Contains('x') == true) skipLexemes = true;

                //Check inflection-level lexicon for matches. If there's a match, skip procedural generation and lexeme processing.
                if (Language.Lexicon.InflectedWords.ContainsKey(word.WordActual))
                    skipGeneration = skipLexemes = ProcessLexiconInflections(word);

                //The lexemes are processed, stripping the actual word down to its root.
                //The affixes are assembled and set in order.
                if (skipLexemes == false)
                {
                    ProcessLexemes(word);
                    AssembleLexemes(word);
                }
                else word.WordRoot = word.WordActual;

                //Check root-level lexicon for matches. If there's a match, skip procedural generation.
                if (Language.Lexicon.RootWords.ContainsKey(word.WordRoot))
                    skipGeneration = ProcessLexiconRoots(word);

                //Set random to the root word.
                RanGen.SetRandom(word.WordRoot);

                //TO-DO:
                //1. Check for punctuation marks. If the sentence contains any, then: isolate (Add before or after
                //2. Check for and process lexemes.

                if (skipGeneration == false)
                    ConstructWord(word);

                //Put the word together.
                word.WordFinal = word.WordPrefixes + word.WordGenerated + word.WordSuffixes;
                sentenceBuilder.Append(word.WordFinal + ' ');

                //"Memorizes" the word if the option has been set and the word isn't there.
                if (Language.Options.MemorizeWords == true && Language.Lexicon.InflectedWords.ContainsKey(word.WordActual) == false)
                    Language.Lexicon.InflectedWords.Add(word.WordActual, word.WordFinal);
            }

            //Return the result.
            info = wordInfo;
            return sentenceBuilder.ToString();
        }

        private bool ProcessLexiconInflections(WordInfo word)
        {
            word.WordGenerated = Language.Lexicon.InflectedWords[word.WordActual];
            return true;
        }
        private bool ProcessLexiconRoots(WordInfo word)
        {
            word.WordGenerated = Language.Lexicon.RootWords[word.WordRoot];
            return true;
        }

        private void ProcessLexemes(WordInfo word)
        {
            //Extract affixes.
            word.Prefixes = Language.Lexemes.GetPrefixes(word.WordActual).ToArray();
            word.Suffixes = Language.Lexemes.GetSuffixes(word.WordActual).ToArray();

            //Strip word to root.
            int prefixLength = word.Prefixes.Sum((a) => a.Key.Length);
            int suffixLength = word.Suffixes.Sum((a) => a.Key.Length);
            word.WordRoot = word.WordActual.Substring(prefixLength, word.WordActual.Length - suffixLength);
        }
        private void AssembleLexemes(WordInfo word)
        {
            foreach (Affix p in word.Prefixes)
                word.WordPrefixes += p.Value;
            foreach (Affix s in word.Suffixes)
                word.WordSuffixes += s.Value;
        }

        private void ConstructWord(WordInfo word)
        {
            //4. Estimate sigma (syllable) count by checking the boundaries of "VC" and "CV".
            //5. Generate sigma structure; by length of actual sigma count * sigma weight. "CCV·VC·VC·CV"
            SelectSigmaStructures(word);

            //6. The first letter of the word is chosen; by first sigma's C/V, then by start letter weight.
            //7. The next letters are chosen, according to the pathways set by the language author.
            PopulateWord(word);
        }

        private void SelectSigmaStructures(WordInfo word)
        {
            int sigmaCount = (int)((double)SigmaCount(word.WordRoot) * 
                    RanGen.NextDouble(Language.Options.SigmaSkewMin, Language.Options.SigmaSkewMax));

            //Generate sigma structure.
            for (int i = 0; i < sigmaCount; i++)
            {
                //5.1 Select sigma by sigma's weights and the language's sigma options.
                SigmaInfo info = new SigmaInfo();

                Sigma last = (word.SigmaInfo.LastOrDefault() != null) ? word.SigmaInfo.LastOrDefault().Sigma : null;
                Sigma sigma = SelectSigma(i, last);

                if (i == 0) info.Position = WordPosition.First;
                else if (i == sigmaCount - 1) info.Position = WordPosition.Last;
                else info.Position = WordPosition.Middle;

                info.Sigma = sigma;
                word.SigmaInfo.Add(info);
            }

            //Link adjacent sigma.
            for (int i = 0; i < word.SigmaInfo.Count; i++)
            {
                if (i != 0)
                    word.SigmaInfo[i].AdjacentLeft = word.SigmaInfo[i - 1];
                if (i != word.SigmaInfo.Count - 1)
                    word.SigmaInfo[i].AdjacentRight = word.SigmaInfo[i + 1];
            }
        }
        private Sigma SelectSigma(int sigmaPosition, Sigma lastSigma)
        {
            //Temporary
            return Language.Structure.SigmaTemplates[RanGen.Random.Next(0, Language.Structure.SigmaTemplates.Count)];
        }
        /// <summary>
        /// Roughly estimates a word's syllables. It transforms the word into c/v, and counts where a consonant shares a border with a vowel. Only misses where a consonant could also be a vowel (such as "y")).
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public int SigmaCount(string word)
        {
            string cv = string.Empty;

            foreach (char c in word)
            {
                if (Language.Options.InputConsonants.Contains(c)) cv += 'c';
                if (Language.Options.InputVowels.Contains(c)) cv += 'v';
            }

            int result = 0;
            while (cv.Length > 1)
            {

                //Check for consonant-vowel border.
                if ((cv[0] == 'c' && cv[1] == 'v') ||
                    (cv[0] == 'v' && cv[1] == 'c'))
                {
                    result++;
                    cv = cv.Remove(0, Math.Min(cv.Length, 2));
                }

                if (cv.Length <= 1)
                    break;

                //If double consonant or vowel, remove one.
                if ((cv[0] == 'c' && cv[1] == 'c') ||
                    (cv[0] == 'v' && cv[1] == 'v'))
                {
                    cv = cv.Remove(0, 1);
                }
            }

            if (word.Length > 0 && result == 0)
                result = 1;

            return result;
        }


        private void PopulateWord(WordInfo word)
        {
            //Select starting letter according to letter weights.
            if (word.SigmaInfo[0].First().Type == BlockType.Nucleus)
                word.WordGenerated += SelectFirstVowel();
            else
                word.WordGenerated += SelectFirstConsonant();

            int onsetOffset = 0, nucleusOffset = 0;
            if (word.SigmaInfo.First().First().Type == BlockType.Onset)
                onsetOffset = word.WordGenerated.Length;
            if (word.SigmaInfo.First().First().Type == BlockType.Nucleus)
                nucleusOffset = word.WordGenerated.Length;

            foreach (SigmaInfo s in word.SigmaInfo)
            {
                for (int i = 0; i < s.Sigma.Onset.Count - onsetOffset; i++)
                    s.Onset += SelectLetter(word.WordGenerated.LastOrDefault(), s.Position, SigmaPosition.Onset, false);
                word.WordGenerated += s.Onset;

                for (int i = 0; i < s.Sigma.Nucleus.Count - nucleusOffset; i++)
                    s.Nucleus += SelectLetter(word.WordGenerated.LastOrDefault(), s.Position, SigmaPosition.Nucleus, true);
                word.WordGenerated += s.Nucleus;

                for (int i = 0; i < s.Sigma.Coda.Count; i++)
                    s.Coda += SelectLetter(word.WordGenerated.LastOrDefault(), s.Position, SigmaPosition.Coda, false);
                word.WordGenerated += s.Coda;

                onsetOffset = 0;
                nucleusOffset = 0;
            }
        }

        private char SelectFirstVowel()
        {
            double weight = RanGen.Random.NextDouble() * Language.Alphabet.Vowels.Values.Sum(w => w.StartWeight);

            foreach (Vowel v in Language.Alphabet.Vowels.Values)
            {
                weight -= v.StartWeight;

                if (weight <= 0)
                    return v.Value;
            }

            return '_';
        }
        private char SelectFirstConsonant()
        {
            double weight = RanGen.Random.NextDouble() * Language.Alphabet.Consonants.Values.Sum(w => w.StartWeight);

            foreach (Consonant c in Language.Alphabet.Consonants.Values)
            {
                weight -= c.StartWeight;

                if (weight <= 0)
                    return c.Value;
            }

            return '_';
        }
        private char SelectLetter(char last, WordPosition wordPos, SigmaPosition sigmaPos, bool isVowel)
        {
            LetterPath[] potentials = Language.Structure.GetPotentialPaths(last, wordPos, sigmaPos);
            LetterPath chosen = potentials[0]; //Add failsafes for errors. See Language.Pathing for guidelines.

            List<(char, double)> filter = isVowel ?
                chosen.Next.Where<(char, double)>(x => Language.Alphabet.Vowels.ContainsKey(x.Item1)).ToList() :
                chosen.Next.Where<(char, double)>(x => Language.Alphabet.Consonants.ContainsKey(x.Item1)).ToList();

            double weight = RanGen.Random.NextDouble() * filter.Sum(w => w.Item2);

            foreach ((char, double) l in filter)
            {
                weight -= l.Item2;

                if (weight <= 0)
                    return l.Item1;
            }

            throw new Exception(string.Format("Letter pathing match not found: {0}, {1}, {2}, {3}", last, wordPos, sigmaPos));
        }
    }
}
