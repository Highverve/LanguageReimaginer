﻿# PLGL Changelog

### 2022-12-6:
    - Added weighted suffixes to each letter for diverse branching.
    - Moved 1.1 changes into a separate class: Pathways. May be subject to a name change later.
    - Created the Structure class for word formation (as "CV", "VC", "CVC", "CC", etc) by a weighted distribution. Good start, but more work to do.
    - Added the foundation of the Lexemes class, that answers the question: "How do we handle lexemes?"
### 2022-12-7:
    - Created the Punctuation class, for processing and manipulating punctuation marks.
    - Added SyllableInfo and WordInfo. These classes are used by the generator for additional context.
    - Added a few WordGenerator methods.
    - Added the Markings class.
### 2022-12-8:
    - Renamed WordGenerator to LanguageGenerator.
    - Changed the Syllable class to Sigma, and improved how the data is structured. It now uses an onset-nucleus-coda approach, adding another layer above the "CVC" structure.
    - Changed SyllableInfo to SigmaInfo, and it's structure to reflesh 1.10's changes.
    - Temporarily gutted Structure.cs while I plan it's code upgrade.
### 2022-12-9:
    - Designed an ideal scaffolding for letter pathing. It utilizes filters for word position, syllable position, and weighted distribution to pick the next letter.
### 2022-12-10:
    - Moved the more broad options in Language to an Options class for better clarity.
    - Renamed Markers to Flagging to clarify it's different from punctuation marks.
    - Selected affixes (in Lexemes) are now ordered from longest to shortest. This prevent in- stopping inter- from parsing (in·ter·cept).
    - Added "Skip" and "Possessive" booleans to WordInfo (though I should consider making Possessive flagging more modular).
    - Added text position targeting to Flags: BeforePrefix, AfterPrefix, BeforeSuffix, AfterSuffix. Useful for flags that add text.
### 2022-12-12:
    - Removed Skip and Possessive booleans from WordInfo, as I realized it was redundant.
    - Progress in the generating method(s) for language generation.
    - Defined SigmaWeight; a author-specified class that helps the generator decide which sigma to pick.
    - Added InputConsonants/Vowels to Language.Options. This is for estimating syllable count.
    - Added and tested a method to count syllables for any input word. According to my testing, it only has trouble with words that use y as a vowel.
    - Small changes and additional variables to WordInfo.
    - Added supporting methods to Structure.
    - Added the Affix class, and supporting methods to Lexemes.
    - Added a few classes inherited from Flagging: SkipGenerate, SkipLexemes, Add, and Remove.
### 2022-12-13:
    - Coded in the barebone framework for word generation. Currently, it strips a word from it's affixes, generates the seed from the word root, compiles the sigma template (no weighting yet), sets the letters (without pathways yet), and outputs the result. Good progress, with more to accomplish.
### 2022-12-15:
    - Added letter pathing support into the generator itself.
    - Added the Lexicon class, which supports custom words from the root to the inflection ("lexeme") level.
    - Added MemorizeWords boolean to Language.Options. If true, it will add newly generated words to the Lexicon's InflectedWords dictionary.
    - Added syllable count skewing to the language generation. Set SigmaSkewMin and SigmaSkewMax in Language.Options.
    - Tracked down a bug that prevented the generator from adding characters to the sigma's onset.
    - Fixed a bug that was causing an extra vowel to be added if the nucleus was the start of the word.
### 2022-12-16:
    - Merged Lexicon with Lexemes, and renamed Lexemes to Lexicon.
    - Added Language switching to LanguageGenerator.
    - Cleaned up the code in LanguageGenerator. Much nicer.
    - Minor cleanup in various classes.
    - Deleted SyllableGenerator as it's been made Redundant.
### 2022-12-17:
    - Deleted Main class (as it was empty), and moved the changelog to it's own markdown file.
    - Deleted RandomGenerator, moving it's components into LanguageGenerator.
    - Moved LanguageGenerator a level above the Operators folder, and deleted that folder.
    - Cleaned up the LanguageGenerator.Generate method even further. A few aspects of the method have been separated out into new methods for enhanced clarity.
    -