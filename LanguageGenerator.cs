﻿using PLGL.Construct;
using PLGL.Construct.Elements;
using PLGL.Deconstruct;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PLGL
{
    /// <summary>
    /// Allows deconstructed blocks to be merged, or whatever else. "Let's" gets split into three blocks, when it should be one.
    /// </summary>
    /// <param name="current"></param>
    /// <param name="adjacentLeft"></param>
    /// <param name="adjacentRight"></param>
    public delegate void OnDeconstruct(LanguageGenerator lg, CharacterBlock current, CharacterBlock adjacentLeft, CharacterBlock adjacentRight);
    /// <summary>
    /// Processes author-defined filters, determining how each character block is processed.
    /// </summary>
    /// <param name="lg"></param>
    /// <param name="word"></param>
    public delegate void OnConstruct(LanguageGenerator lg, WordInfo word);
    /// <summary>
    /// Allows manipulation of the generated word's letters.
    /// </summary>
    /// <param name="lg"></param>
    /// <param name="word"></param>
    /// <param name="letter"></param>
    public delegate void OnGenerate(LanguageGenerator lg, WordInfo word, LetterInfo current, LetterInfo adjacentLeft, LetterInfo adjacentRight);

    //For modifying affixes with relative context. Called nearly at the end of the process, just before the affixes are assembled.
    //E.g, adding a vowel for smooth vocal transitions.
    public delegate void OnPrefix(WordInfo word, Affix current, Affix adjacentLeft, Affix adjacentRight);
    public delegate void OnSuffix(WordInfo word, Affix current, Affix adjacentLeft, Affix adjacentRight);

    public class LanguageGenerator
    {
        public Deconstructor Deconstruct { get; private set; } = new Deconstructor();

        StringBuilder sentenceBuilder = new StringBuilder();
        List<CharacterBlock> blocks = new List<CharacterBlock>();
        List<WordInfo> wordInfo = new List<WordInfo>();

        public void CONSTRUCT_Hide(WordInfo word, string filter)
        {
            if (word.Filter.Name.ToUpper() == filter && word.IsProcessed == false)
            {
                word.WordFinal = string.Empty;
                word.Prefixes = new Affix[0];
                word.Suffixes = new Affix[0];
                word.IsProcessed = true;
            }
        }
        /// <summary>
        /// Applies to delimiters, undefined, etc.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="filter"></param>
        public void CONSTRUCT_KeepAsIs(WordInfo word, string filter)
        {
            if (word.Filter.Name.ToUpper() == filter && word.IsProcessed == false)
            {
                word.WordFinal = word.WordActual;
                word.IsProcessed = true;
            }
        }
        public void CONSTRUCT_Within(WordInfo word, string filter, int index, int subtract)
        {
            if (word.Filter.Name.ToUpper() == filter && word.IsProcessed == false)
            {
                word.WordFinal = word.WordActual.Substring(index, word.WordActual.Length - subtract);
                word.IsProcessed = true;
            }
        }
        /// <summary>
        /// Almost exclusively applied to letter filters.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="filter"></param>
        public void CONSTRUCT_Generate(WordInfo word, string filter)
        {
            if (word.Filter.Name.ToUpper() == filter.ToUpper() && word.IsProcessed == false)
            {
                Lexemes(word);
                SetRandom(word.WordRoot);

                if (string.IsNullOrEmpty(word.WordGenerated))
                    GenerateWord(word);

                word.WordFinal = word.WordPrefixes + word.WordGenerated + word.WordSuffixes;
                word.IsProcessed = true;

                LexiconMemorize(word);

                SetCase(word);
            }
        }

        public void DECONSTRUCT_MergeBlocks(CharacterBlock current, CharacterBlock left, CharacterBlock right,
            string currentFilter, string leftFilter, string rightFilter, string currentText, string newFilter)
        {
            if (left != null && right != null)
            {
                if (current.Filter.Name.ToUpper() == currentFilter &&
                    left.Filter.Name.ToUpper() == leftFilter &&
                    right.Filter.Name.ToUpper() == rightFilter)
                {
                    if (current.Text == currentText)
                    {
                        current.Text = left.Text + current.Text + right.Text;
                        current.Filter = Deconstruct.GetFilter(newFilter);

                        left.IsAlive = false;
                        right.IsAlive = false;

                        LinkLeftBlock(current);
                        LinkRightBlock(current);
                    }
                }
            }
        }
        public void DECONSTRUCT_MergeBlocks(CharacterBlock current, CharacterBlock left, CharacterBlock right,
            string currentFilter, string leftFilter, string rightFilter, string newFilter)
        {
            if (left != null && right != null)
            {
                if (current.Filter.Name.ToUpper() == currentFilter &&
                    left.Filter.Name.ToUpper() == leftFilter &&
                    right.Filter.Name.ToUpper() == rightFilter)
                {
                    current.Text = left.Text + current.Text + right.Text;
                    current.Filter = Deconstruct.GetFilter(newFilter);

                    left.IsAlive = false;
                    right.IsAlive = false;

                    LinkLeftBlock(current);
                    LinkRightBlock(current);
                }
            }
        }
        public void DECONSTRUCT_ContainWithin(CharacterBlock current, CharacterBlock left, CharacterBlock right,
            string openFilter, string closeFilter, string newFilter)
        {
            string buildBlock = newFilter + "_Build";

            //Process opening filter. The filter "eats" right adjacent blocks until it encounters it's filter again.
            if (current.Filter.Name.ToUpper() == openFilter || current.Filter.Name.ToUpper() == buildBlock.ToUpper())
            {
                //It's not the end; therefore, absorb the block.
                if (right != null && right.Filter.Name.ToUpper() != closeFilter)
                {
                    right.Text = current.Text + right.Text;
                    right.Filter = new CharacterFilter() { Characters = Deconstruct.Undefined.Characters, Name = buildBlock };

                    current.IsAlive = false;

                    LinkLeftBlock(right);
                }
            }
            //Process end filter.
            if (current.Filter.Name.ToUpper() == closeFilter)
            {
                //It's the wrapperFilter, and the left block is built on. Therefore, this means it's the end of the block.
                if (left != null && left.Filter.Name.ToUpper() == buildBlock.ToUpper())
                {
                    current.Text = left.Text + current.Text;
                    current.Filter = Deconstruct.GetFilter(newFilter);

                    left.IsAlive = false;

                    LinkLeftBlock(current);
                }
            }
        }

        /// <summary>
        /// Sets how the generator count's a root's syllables. Default is EnglishSigmaCount (C/V border checking).
        /// </summary>
        public Func<string, int> SigmaCount { get; set; }
        public Action<WordInfo> Capitalize { get; set; } = (w) => { w.WordFinal = char.ToUpper(w.WordFinal[0]) + w.WordFinal.Substring(1); };
        public Action<WordInfo> Uppercase { get; set; } = (w) => { w.WordFinal = w.WordFinal.ToUpper(); };
        public Action<WordInfo> RandomCase { get; set; } = (w) =>
        {
            Random random = new Random();

            string result = string.Empty;
            for (int i = 0; i < w.WordFinal.Length; i++)
            {
                if (random.Next(100) > 50)
                    result += char.ToUpper(w.WordFinal[i]);
                else
                    result += w.WordFinal[i];
            }
            w.WordFinal = result;
        };

        public void LoopThroughLetters(WordInfo word, Action<char, int> action)
        {
            char[] array = word.WordFinal.ToCharArray();
            for (int i = 0; i < array.Length; i++)
            {
            }
        }

        #region Language management
        private Language language;
        public Language Language
        {
            get { return language; }
            set
            {
                language = value;
                Deconstruct.Language = Language;
            }
        }
        public Dictionary<string, Language> Languages { get; private set; } = new Dictionary<string, Language>();

        public void AddLanguage(string key, Language language)
        {
            if (Languages.ContainsKey(key) == false)
                Languages.Add(key, language);
        }
        public void AddLanguage(Language language) { AddLanguage(language.META_Nickname, language); }
        public void SelectLanguage(string nickname)
        {
            if (Languages.ContainsKey(nickname))
                Language = Languages[nickname];
        }
        public void SelectLanguage(Language language) { Language = language; }
        #endregion

        #region Random management
        public int Seed { get; private set; }
        public Random Random { get; set; } = new Random();

        /// <summary>
        /// The foundation of the generator. Initializes a new Random using the root word as its seed.
        /// This needs to be set in the letters filter in Language.Construction.ConstructFilter, before the call to generate a new word.
        /// This should be set to the root word.
        /// </summary>
        /// <param name="word"></param>
        public void SetRandom(string word)
        {
            Seed = WordSeed(word.ToUpper());
            Random = new Random(Seed);
        }
        private int WordSeed(string word)
        {
            using var a = System.Security.Cryptography.SHA1.Create();
            return BitConverter.ToInt32(a.ComputeHash(Encoding.UTF8.GetBytes(word))) + Language.Options.SeedOffset;
        }
        private double NextDouble(double minimum, double maximum) { return Random.NextDouble() * (maximum - minimum) + minimum; }
        #endregion

        public LanguageGenerator() { SigmaCount = (s) => EnglishSigmaCount(s); }
        public LanguageGenerator(Language Language) : base() { this.Language = Language; }

        #region Generation — Main method and overloads
        /// <summary>
        /// Generates a new sentence from the input sentence according to the language's constraints. This overload returns the word list for debugging purposes.
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public List<WordInfo> GenerateRaw(string sentence)
        {
            //Clear class-level variables for the next sentence.
            blocks.Clear();
            wordInfo.Clear();

            blocks = Deconstruct.Deconstruct(sentence);
            for (int i = 0; i < blocks.Count; i++)
            {
                CharacterBlock current = blocks[i];

                if (i > 0) LinkLeftBlock(current);
                if (i < blocks.Count - 1) LinkRightBlock(current);

                Language.Deconstruct(this, current, current.Left, current.Right);
            }

            blocks.RemoveAll((b) => b.IsAlive == false);

            AddWordInfo(blocks);
            LinkWords();

            //Loop through every word, applying the filters
            foreach (WordInfo word in wordInfo)
                Language.Construct(this, word);

            return wordInfo;
        }
        /// <summary>
        /// Calls GenerateRaw(), compiling and returning the string.
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
        public string GenerateClean(string sentence)
        {
            sentenceBuilder.Clear();

            List<WordInfo> result = GenerateRaw(sentence);

            for (int i = 0; i < wordInfo.Count; i++)
                sentenceBuilder.Append(wordInfo[i].WordFinal);

            return sentenceBuilder.ToString();
        }
        /// <summary>
        /// Switches to the specified language and generates a new sentence.
        /// </summary>
        /// <param name="language"></param>
        /// <param name="sentence"></param>
        /// <returns></returns>
        public string GenerateClean(Language language, string sentence) { SelectLanguage(language); return GenerateClean(sentence); }
        /// <summary>
        /// Switches to the specified language and generates a new sentence.
        /// </summary>
        /// <param name="language"></param>
        /// <param name="sentence"></param>
        /// <returns></returns>
        public string GenerateClean(string language, string sentence) { SelectLanguage(language); return GenerateClean(sentence); }
        /// <summary>
        /// Does the same as GenerateClean, while also returning the list of WordInfo.
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public string GenerateDebug(string sentence, out List<WordInfo> info)
        {
            sentenceBuilder.Clear();

            List<WordInfo> result = GenerateRaw(sentence);
            info = result;

            for (int i = 0; i < wordInfo.Count; i++)
                sentenceBuilder.Append(wordInfo[i].WordFinal);

            return sentenceBuilder.ToString();
        }
        #endregion

        #region Other methods
        /// <summary>
        /// Loops through each word, and adds it to wordInfo.
        /// </summary>
        /// <param name="words"></param>
        private void AddWordInfo(List<CharacterBlock> blocks)
        {
            foreach (CharacterBlock b in blocks)
            {
                WordInfo word = new WordInfo();
                word.WordActual = b.Text;
                word.Filter = b.Filter;

                wordInfo.Add(word);
            }
        }
        /// <summary>
        /// Links adjacent words for additional generation context.
        /// </summary>
        private void LinkWords()
        {
            for (int i = 0; i < wordInfo.Count; i++)
            {
                if (i != 0)
                    wordInfo[i].AdjacentLeft = wordInfo[i - 1];
                if (i != wordInfo.Count - 1)
                    wordInfo[i].AdjacentRight = wordInfo[i + 1];
            }
        }
        private void LinkLeftBlock(CharacterBlock block)
        {
            int index = blocks.IndexOf(block);

            for (int i = index - 1; i > 0; i--)
            {
                if (blocks[i].IsAlive == true)
                {
                    block.Left = blocks[i];
                    break;
                }
            }
        }
        private void LinkRightBlock(CharacterBlock block)
        {
            int index = blocks.IndexOf(block);

            for (int i = index + 1; i < blocks.Count; i++)
            {
                if (blocks[i].IsAlive == true)
                {
                    block.Right = blocks[i];
                    break;
                }
            }
        }

        public void SetCase(WordInfo word)
        {
            if (Language.Options.AllowAutomaticCasing == true)
            {
                bool firstLetter = false;
                int upper = 0;
                for (int i = 0; i < word.WordActual.Length; i++)
                {
                    if (char.IsUpper(word.WordActual[i]))
                    {
                        if (i == 0)
                            firstLetter = true;
                        upper++;
                    }
                }

                if (firstLetter == true && upper == 1)
                    word.Case = WordInfo.CaseType.Capitalize;
                if (word.WordActual.Length > 1)
                {
                    if (upper == word.WordActual.Length)
                        word.Case = WordInfo.CaseType.Uppercase;
                    if (upper > 1 && upper < word.WordActual.Length && Language.Options.AllowRandomCase == true)
                        word.Case = WordInfo.CaseType.RandomCase;
                }

                switch (word.Case)
                {
                    case WordInfo.CaseType.Capitalize: Capitalize(word); break;
                    case WordInfo.CaseType.Uppercase: Uppercase(word); break;
                    case WordInfo.CaseType.RandomCase: RandomCase(word); break;
                }
            }
        }
        #endregion

        #region Lexicon (and inflections) — Affixes, root extraction, custom words
        private void ProcessLexiconInflections(WordInfo word)
        {
            if (Language.Lexicon.Inflections.ContainsKey(word.WordActual.ToLower()))
                word.WordGenerated = Language.Lexicon.Inflections[word.WordActual.ToLower()];
        }
        private void ProcessLexiconRoots(WordInfo word)
        {
            if (Language.Lexicon.Roots.ContainsKey(word.WordRoot.ToLower()))
                word.WordGenerated = Language.Lexicon.Roots[word.WordRoot.ToLower()];
        }
        
        /// <summary>
        /// The lexemes are processed, stripping the actual word down to its root.
        /// </summary>
        /// <param name="word"></param>
        private void ProcessLexemes(WordInfo word)
        {
            //Extract affixes.
            word.Prefixes = Language.Lexicon.GetPrefixes(word.WordActual).ToArray();
            word.Suffixes = Language.Lexicon.GetSuffixes(word.WordActual).ToArray();

            //Strip word to root.
            int prefixLength = word.Prefixes.Sum((a) => a.Key.Length);
            int suffixLength = word.Suffixes.Sum((a) => a.Key.Length);
            word.WordRoot = word.WordActual.Substring(prefixLength, word.WordActual.Length - suffixLength);
        }
        /// <summary>
        /// The affixes are assembled and set in order.
        /// </summary>
        /// <param name="word"></param>
        private void AssembleLexemes(WordInfo word)
        {
            foreach (Affix p in word.Prefixes)
                word.WordPrefixes += p.Value;
            foreach (Affix s in word.Suffixes)
                word.WordSuffixes += s.Value;
        }

        /// <summary>
        /// "Memorizes" the word if the option has been set and the word isn't in Lexicon.Inflections.
        /// </summary>
        /// <param name="word"></param>
        public void LexiconMemorize(WordInfo word)
        {
            if (Language.Options.MemorizeWords == true && Language.Lexicon.Inflections.ContainsKey(word.WordActual) == false)
                Language.Lexicon.Inflections.Add(word.WordActual, word.WordFinal);
        }

        /// <summary>
        /// Add this to an if statement that surrounds word generation.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public bool LexiconContains(WordInfo word)
        {
            return Language.Lexicon.Inflections.ContainsKey(word.WordActual) &&
                    Language.Lexicon.Roots.ContainsKey(word.WordRoot);
        }
        public void Lexemes(WordInfo word)
        {
            ProcessLexiconInflections(word);
            ProcessLexemes(word);
            AssembleLexemes(word);
            ProcessLexiconRoots(word);
        }
        #endregion

        #region Word construction — Sigma structuring and word population
        /// <summary>
        /// Estimate sigma count and generate sigma structure.
        /// </summary>
        /// <param name="word"></param>
        private void SelectSigmaStructures(WordInfo word)
        {
            int sigmaCount = Math.Max((int)(SigmaCount(word.WordRoot) *
                    NextDouble(Language.Options.SigmaSkewMin, Language.Options.SigmaSkewMax)), 1);

            //Generate sigma structure.
            for (int i = 0; i < sigmaCount; i++)
            {
                //5.1 Select sigma by sigma's weights and the language's sigma options.
                SigmaInfo info = new SigmaInfo();

                Sigma last = word.SigmaInfo.LastOrDefault() != null ? word.SigmaInfo.LastOrDefault().Sigma : null;
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
            double weight = 0;

            //if (lastSigma.Coda != null || lastSigma.Coda.Count > 0)


            weight = Random.NextDouble() * Language.Structure.Templates.Sum(s => s.Weight.SelectionWeight);

            foreach (Sigma s in Language.Structure.Templates)
            {
                weight -= s.Weight.SelectionWeight;

                if (weight <= 0)
                    return s;
            }

            return null;
        }
        /// <summary>
        /// Roughly estimates a word's syllables. It transforms the word into c/v, and counts where a consonant shares a border with a vowel. Only misses where a consonant could also be a vowel (such as "y")).
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public int EnglishSigmaCount(string word)
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
                if (cv[0] == 'c' && cv[1] == 'v' ||
                    cv[0] == 'v' && cv[1] == 'c')
                {
                    result++;
                    cv = cv.Remove(0, Math.Min(cv.Length, 2));
                }

                if (cv.Length <= 1)
                    break;

                //If double consonant or vowel, remove one.
                if (cv[0] == 'c' && cv[1] == 'c' ||
                    cv[0] == 'v' && cv[1] == 'v')
                {
                    cv = cv.Remove(0, 1);
                }
            }

            if (word.Length > 0 && result == 0)
                result = 1;

            return result;
        }
        /// <summary>
        /// Counts each valid character found in Language.Options.InputConsonants and InputVowels. More ideal for languages where each letter may be a syllable.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public int CharacterSigmaCount(string word)
        {
            int result = 0;

            foreach (char c in word)
            {
                if (Language.Options.InputConsonants.Contains(c)) result++;
                if (Language.Options.InputVowels.Contains(c)) result++;
            }

            return result;
        }

        /// <summary>
        /// Populates the sigma structures according to the language's letter pathing.
        /// </summary>
        /// <param name="word"></param>
        private void PopulateWord(WordInfo word)
        {
            //Select starting letter according to letter weights.
            if (word.SigmaInfo[0].First().Type == SigmaType.Nucleus)
                word.GeneratedLetters.Add(new LetterInfo(SelectFirstVowel()));
            else
                word.GeneratedLetters.Add(new LetterInfo(SelectFirstConsonant()));

            int onsetOffset = 0, nucleusOffset = 0;
            if (word.SigmaInfo.First().First().Type == SigmaType.Onset)
                onsetOffset = word.GeneratedLetters.Count;
            if (word.SigmaInfo.First().First().Type == SigmaType.Nucleus)
                nucleusOffset = word.GeneratedLetters.Count;

            foreach (SigmaInfo s in word.SigmaInfo)
            {
                for (int i = 0; i < s.Sigma.Onset.Count - onsetOffset; i++)
                    word.GeneratedLetters.Add(SelectLetter(word.GeneratedLetters.Last().Letter.Key, s.Position, SigmaPosition.Onset, false));
                word.WordGenerated += s.Onset;

                for (int i = 0; i < s.Sigma.Nucleus.Count - nucleusOffset; i++)
                    word.GeneratedLetters.Add(SelectLetter(word.GeneratedLetters.Last().Letter.Key, s.Position, SigmaPosition.Nucleus, true));
                word.WordGenerated += s.Nucleus;

                for (int i = 0; i < s.Sigma.Coda.Count; i++)
                    word.GeneratedLetters.Add(SelectLetter(word.GeneratedLetters.Last().Letter.Key, s.Position, SigmaPosition.Coda, false));
                word.WordGenerated += s.Coda;

                onsetOffset = 0;
                nucleusOffset = 0;
            }

            for (int i = 0; i < word.GeneratedLetters.Count; i++)
            {
                if (i != 0) word.GeneratedLetters[i].AdjacentLeft = word.GeneratedLetters[i - 1];
                if (i < word.GeneratedLetters.Count - 1) word.GeneratedLetters[i].AdjacentRight = word.GeneratedLetters[i + 1];
            }

            if (Language.Generate != null)
            {
                for (int i = 0; i < word.GeneratedLetters.Count; i++)
                {
                    int currentCount = word.GeneratedLetters.Count;

                    if (word.GeneratedLetters[i].IsProcessed == false)
                    {
                        word.GeneratedLetters[i].IsProcessed = true;

                        Language.Generate(this, word,
                            word.GeneratedLetters[i],
                            word.GeneratedLetters[i].AdjacentLeft,
                            word.GeneratedLetters[i].AdjacentRight);

                        LinkLeftLetter(word, word.GeneratedLetters[i]);
                        LinkRightLetter(word, word.GeneratedLetters[i]);
                    }

                    //If there is a difference (e.g, a letter was added), return to 0.
                    if (currentCount != word.GeneratedLetters.Count)
                        i = 0;
                }
            }

            word.GeneratedLetters.RemoveAll((l) => l.IsAlive == false);

            foreach (LetterInfo l in word.GeneratedLetters)
                word.WordGenerated += l.Letter.Case.lower;
        }
        private Letter SelectFirstVowel()
        {
            double weight = Random.NextDouble() * Language.Alphabet.Vowels.Values.Sum(w => w.StartWeight);

            foreach (Vowel v in Language.Alphabet.Vowels.Values)
            {
                weight -= v.StartWeight;

                if (weight <= 0)
                    return v;
            }

            return null;
        }
        private Letter SelectFirstConsonant()
        {
            double weight = Random.NextDouble() * Language.Alphabet.Consonants.Values.Sum(w => w.StartWeight);

            foreach (Consonant c in Language.Alphabet.Consonants.Values)
            {
                weight -= c.StartWeight;

                if (weight <= 0)
                    return c;
            }

            return null;
        }
        private LetterInfo SelectLetter(char last, WordPosition wordPos, SigmaPosition sigmaPos, bool isVowel)
        {
            LetterPath[] potentials = Language.Structure.GetPotentialPaths(last, wordPos, sigmaPos);
            LetterPath chosen = potentials[0]; //Add failsafes for errors. See Language.Pathing for guidelines.

            List<(char, double)> filter = isVowel ?
                chosen.Next.Where(x => Language.Alphabet.Vowels.ContainsKey(x.Item1)).ToList() :
                chosen.Next.Where(x => Language.Alphabet.Consonants.ContainsKey(x.Item1)).ToList();

            double weight = Random.NextDouble() * filter.Sum(w => w.Item2);

            foreach ((char, double) l in filter)
            {
                weight -= l.Item2;

                if (weight <= 0)
                    return new LetterInfo(Language.Alphabet.Find(l.Item1));
            }

            throw new Exception(string.Format("Letter pathing match not found: {0}, {1}, {2}, {3}", last, wordPos, sigmaPos));
        }

        public void GenerateWord(WordInfo word)
        {
            SelectSigmaStructures(word);
            PopulateWord(word);
        }

        public void GENERATE_ReplaceLetter(WordInfo word, LetterInfo current, LetterInfo target, char letterKey)
        {
            int index = word.GeneratedLetters.IndexOf(current);
            target.Letter = Language.Alphabet.Find(letterKey);
        }
        public void GENERATE_InsertLetter(WordInfo word, LetterInfo current, char letterKey, int indexOffset)
        {
            int index = word.GeneratedLetters.IndexOf(current);
            Letter letter = Language.Alphabet.Find(letterKey);
            word.GeneratedLetters.Insert(index + indexOffset, new LetterInfo(letter));
        }

        private void LinkLeftLetter(WordInfo word, LetterInfo letter)
        {
            int index = word.GeneratedLetters.IndexOf(letter);

            for (int i = index - 1; i > 0; i--)
            {
                if (word.GeneratedLetters[i].IsAlive == true)
                {
                    letter.AdjacentLeft = word.GeneratedLetters[i];
                    break;
                }
            }
        }
        private void LinkRightLetter(WordInfo word, LetterInfo letter)
        {
            int index = word.GeneratedLetters.IndexOf(letter);

            for (int i = index + 1; i < word.GeneratedLetters.Count; i++)
            {
                if (word.GeneratedLetters[i].IsAlive == true)
                {
                    letter.AdjacentRight = word.GeneratedLetters[i];
                    break;
                }
            }
        }
        #endregion
    }
}
