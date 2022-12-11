﻿using LanguageReimaginer.Data.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageReimaginer.Data
{
    /// <summary>
    /// A branching tree, telling the word generator the most favorable letter to choose, given the context of the previous letter(s) and the weight of the future options.
    /// For example, in English, the letters folowing 'p' could commonly be 'a', 'e', 'o', uncommonly 'u', 'i', and
    /// 
    /// NOTE! Consider moving back to the Letter class.
    /// Dictionary<Tuple<char, char>, double>, where the first char is the letter before (can be null/make certain), the second char is the next letter, and the double is the weight.
    /// 
    /// NOTE 2! This needs to take into account not just the letter before it, but the syllable position and word position!.
    /// No english word starts with "Ss", yet quite a lot end with "ss" (kiss, less, etc).
    /// </summary>
    public class Pathways
    {
        internal Alphabet Alphabet { get; set; }

        public Dictionary<char, List<Path>> Branches { get; set; } = new Dictionary<char, List<Path>>();

        public void AddPath(char current, char next, double weight)
        {
            Letter? output = Alphabet.Find(next);

            if (Branches.ContainsKey(current) == false)
                Branches.Add(current, new List<Path>());
            else
                Branches[current].Add(new Path(output, weight));
        }
        public void AddPaths(char current, params (char next, double weight)[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
                AddPath(current, paths[i].next, paths[i].weight);
        }

        public List<Path>? GetPaths(char c)
        {
            if (Branches.ContainsKey(c))
                return Branches[c];
            return null;
        }
    }
    public class Path
    {
        public Letter Letter { get; set; }
        public double Weight { get; set; }

        public Path(Letter letter, double weight)
        {
            Letter = letter;
            Weight = weight;
        }
    }
}