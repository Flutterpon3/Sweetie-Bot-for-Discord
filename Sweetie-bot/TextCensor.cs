using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Sweetie_bot
{
    public class Censor
    {
        public static Dictionary<string, string> CensoredWords { get; private set; }
        public static string CensoredWordsString { get; private set; }
        public static List<int> CensoredWordsCharIndexes { get; private set; }
        public static Dictionary<string, Dictionary<string, string>> Dictionary { get; private set; }

        private static Dictionary<int, int> CensoredWordsIndexes { get; set; }
        private static string HardFilterKey { get; set; }

        public Censor(Dictionary<string, string> censoredWords, Dictionary<string, Dictionary<string, string>> dictWords)
        {
            if (censoredWords == null)
                throw new ArgumentNullException("censoredWords");

            if (dictWords == null)
                throw new ArgumentNullException("dictWords");

            CensoredWords = censoredWords;
            CensoredWordsCharIndexes = new List<int>();
            for (int i = 0; i < CensoredWords.Keys.Count; ++i)
            {
                string word = CensoredWords.Keys.ElementAt(i);
                CensoredWordsString += word + "?";
                CensoredWordsCharIndexes.AddRange(Enumerable.Repeat(i, word.Length + 1));
            }
            System.Diagnostics.Debug.WriteLine(CensoredWordsString.Length - CensoredWordsCharIndexes.Count);
            Dictionary = dictWords;
        }

        public bool CensorCheck(string CheckString)
        {
            foreach(string word in CheckString.Split(' ').ToArray())
            {
                if (CensoredWords.ContainsKey(word.ToLower())){
                    return true;
                }
            }

            return false;
        }


        public string PureCensor(string censoredText)
        {
            if (censoredText == null)
                throw new ArgumentNullException("censoredText");

            foreach (string censoredWord in CensoredWords.Values)
            {
                string regularExpression = ToRegexPatternBordered(censoredWord);
                censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            return censoredText;
        }

        public string CensorMessage(string message)
        {
            //float startTime = Time.realtimeSinceStartup;

            if (message == null)
                return message;

            string quickfilter = QuickFilter(message);
            string alteredMessage = RemoveNonAlphaNumeric(quickfilter);

            string filthdrain = alteredMessage;
            if (filthdrain.Length == 0) return quickfilter;
            filthdrain = Strip(quickfilter);

            string stripped = "";
            for (int i = 0; i < filthdrain.Length; ++i)
            {
                if (quickfilter[i] == '¢') stripped += message[i];
                else stripped += filthdrain[i];
            }

            filthdrain = RemoveNonAlphaNumeric(filthdrain);
            if (filthdrain.Length == 0) return quickfilter;
            string censoredMessage = Filter(RemoveNonAlphaNumericStar(stripped), alteredMessage, filthdrain);
            censoredMessage = RefillNonAlphaNumeric(censoredMessage, quickfilter);
            stripped = RefillApostrophes(stripped, quickfilter);
            censoredMessage = Unfilter(censoredMessage, stripped, message);

            //float endTime = Time.realtimeSinceStartup;
            //Console.WriteLine((endTime - startTime) * 1000 + " ms");

            return censoredMessage;
        }

        public string CensorMultiMessage(params string[] messages)
        {
            if (messages == null)
                throw new ArgumentNullException("text");

            string concatMessage = "";
            string alteredMessage = "";
            string filthdrain = "";
            foreach (string message in messages)
            {
                //string Tquickfilter = QuickFilter(message);

                //string Tfilthdrain = Tquickfilter;
                //if (Tfilthdrain.Length > 0) Tfilthdrain = Strip(Tquickfilter);

                //concatMessage += Tquickfilter + "\0";
                concatMessage += message + "\0";
                //filthdrain += Tfilthdrain + "\0";
            }

            concatMessage = QuickFilter(concatMessage);
            alteredMessage = RemoveNonAlphaNumeric(concatMessage);
            //filthdrain = RemoveNonAlphaNumeric(filthdrain);
            filthdrain = RemoveNonAlphaNumeric(Strip(concatMessage));
            //Console.WriteLine(concatMessage);

            string censoredMessage = Filter(concatMessage, alteredMessage, filthdrain);
            string[] censoredMessages = censoredMessage.Split("\0".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string[] filteredMessages = concatMessage.Split("\0".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //Console.WriteLine(filteredMessages.Length);
            //Console.WriteLine(censoredMessages.Length);

            censoredMessage = "";
            for (int i = 0; i < messages.Length; ++i)
                censoredMessage += Unfilter(RefillNonAlphaNumeric(censoredMessages[i], filteredMessages[i]), Strip(messages[i]), messages[i]) + " ";
            
            return censoredMessage;
        }

        public string Underline(string message, string censoredMessage)
        {
            string[] words = message.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            string[] filteredWords = censoredMessage.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            string underlined = "";
            for (int i = 0; i < words.Length; ++i)
            {
                if (filteredWords[i].Contains('¢') &&
                    !filteredWords[i].Equals(words[i]))
                {
                    words[i] = "__" + words[i] + "__";
                }
                underlined += words[i];
                if (i != words.Length - 1) underlined += " ";
            }
            return underlined;
        }

        public string BoldUnderline(string message, string censoredMessage)
        {
            string[] words = message.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            string[] filteredWords = censoredMessage.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            string underlined = "";
            for (int i = 0; i < words.Length; ++i)
            {
                if (filteredWords[i].Contains('¢') &&
                    !filteredWords[i].Equals(words[i]))
                    words[i] = "__**" + words[i] + "**__";
                underlined += words[i];
                if (i != words.Length - 1) underlined += " ";
            }
            return underlined;
        }

        public string FlutterCryFilter(string message, string censoredMessage)
        {
            string[] words = message.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            string[] filteredWords = censoredMessage.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            string tears = "";
            for (int i = 0; i < words.Length; ++i)
            {
                if (filteredWords[i].Contains('¢') &&
                    !filteredWords[i].Equals(words[i]))
                    words[i] = ":fluttercry:";
                tears += words[i];
                if (i != words.Length - 1) tears += " ";
            }
            return tears;
        }

        public string QuickFilter(string message)
        {
            string[] words = message.ToLower().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            string quickfilter = message;

            Dictionary<string, string> temp = new Dictionary<string, string>(words.Length);
            foreach (string word in words)
            {
                if (!temp.ContainsKey(word))
                {
                    temp.Add(word, word);
                    if (CensoredWords.ContainsKey(word))
                    {
                        string regularExpression = ToRegexPattern(word);

                        quickfilter = Regex.Replace(quickfilter, regularExpression, StarCensoredMatch,
                                              RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                }
            }
            return quickfilter;
        }

        public string Filter(string text, string alteredText, string key)
        {
            string censoredText = alteredText;
            
            if (key.Length > 2)
            {
                HardFilterKey = key;

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                Dictionary<string, string> sets = new Dictionary<string, string>();
                CensoredWordsIndexes = new Dictionary<int, int>();

                string alteredbadwords = CensoredWordsString;
                int cap = 3;
                //for (int i = 0; i < key.Length - cap; i += 3)
                for (int i = 0; i < Math.Max(key.Length - cap + 1, 1); ++i)
                {
                    string set = "";
                    /*
                    int limit = key.Length - i;
                    if (limit > cap) limit = cap;

                    int offset = 0;
                    if (limit < cap) offset = cap - limit;
                    */
                    for (int j = i; j < Math.Min(cap + i, key.Length); ++j)
                        set += key[j];
                    //set += key[j - offset];

                    if (!sets.ContainsKey(set))
                    {
                        sets.Add(set, set);
                        string regularExpression = ToRegexPattern(set);
                        alteredbadwords = Regex.Replace(alteredbadwords, regularExpression, StarCensoredMatchProfanityIndex,
                                                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                }

                //CensoredWordsIndexes = CensoredWordsIndexes.OrderBy(x => CensoredWords.Keys.ElementAt(x.Key).Length - x.Value).ToDictionary(x => x.Key, x => x.Value);

                for (int i = 0; i < CensoredWordsIndexes.Count; ++i)
                {
                    string censoredWord = CensoredWords.Keys.ElementAt(CensoredWordsIndexes.Keys.ElementAt(i));
                    if (censoredWord.Length > HardFilterKey.Length) continue;

                    if (HardFilterKey.IndexOf(censoredWord, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    string regularExpression = ToRegexPattern(censoredWord);

                    string old = censoredText;
                    censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (censoredText != old)
                        HardFilterKey = HardFilterKey.Replace(censoredWord, string.Empty);
                }

                watch.Stop();
                System.Diagnostics.Debug.WriteLine(watch.ElapsedMilliseconds);
            }
            
            string resultText = "";
            for (int i = 0; i < alteredText.Length; ++i)
            {
                if (text[i] == '¢' && censoredText[i] == '¢')
                    resultText += alteredText[i];
                else
                {
                    char c = censoredText[i];
                    if (char.IsUpper(alteredText[i]))
                        c = char.ToUpper(c);
                    resultText += c;
                }
            }

            return resultText;
        }

        string Unfilter(string censored, string unfilter, string original)
        {
            string unfiltered = "";
            for (int i = 0; i < original.Length; ++i)
            {
                if (unfilter[i] == '¢') unfiltered += original[i];
                else unfiltered += censored[i];
            }
            return unfiltered;
        }

        public string Strip(string text)
        {
            if (text != null)
            {
                string lText = text.ToLower();
                string cleanDictText = lText;
                string currentDict = "";
                int wordPlace = 0;
                Dictionary<string, Dictionary<string, string>> dicts = new Dictionary<string, Dictionary<string, string>>();
                string wordString = "";
                for (int i = 0; i < cleanDictText.Length; ++i)
                {
                    char lower = char.ToLower(cleanDictText[i]);
                    bool isalpha = isAlpha(lower) || lower == '\'';
                    if (wordPlace < 3)
                    {
                        if (isalpha)
                        {
                            currentDict = wordString + lower;
                            ++wordPlace;
                        }
                    }

                    if (wordPlace == 3 || !isalpha || i == cleanDictText.Length - 1)
                    {
                        if (!dicts.ContainsKey(currentDict) && currentDict.Length < 4 && currentDict.Length > 1)
                            dicts.Add(currentDict, new Dictionary<string, string>());
                        wordPlace = 0;
                    }

                    if (isalpha) wordString += cleanDictText[i];

                    if ((i == cleanDictText.Length - 1 && isalpha) || (wordString != "" && !isalpha))
                    {
                        if (wordString.Length > 1)
                        {
                            string sub = wordString.Substring(0, Math.Min(wordString.Length, 3));
                            if (!dicts[sub].ContainsKey(wordString))
                                dicts[sub].Add(wordString, wordString);
                        }
                        wordString = "";
                    }
                }
                
                if (dicts.Count > 0)
                {
                    for (int i = 0; i < dicts.Count; ++i)
                    {
                        KeyValuePair<string, Dictionary<string, string>> pair = dicts.ElementAt(i);
                        if (pair.Value.Count > 1)
                        {
                            Dictionary<string, string> temp = pair.Value.Keys.OrderByDescending(x => x.Length).ToDictionary(x => x, x => x);
                            dicts[pair.Key] = temp;
                        }
                    }
                    dicts = dicts.OrderByDescending(x => x.Value.Last().Key.Length).ToDictionary(x => x.Key, x => x.Value);

                    for (int i = 0; i < dicts.Count; ++i)
                    {
                        foreach (string word in dicts.Values.ElementAt(i).Keys)
                        {
                            if (Dictionary.ContainsKey(dicts.Keys.ElementAt(i)))
                            {
                                if (Dictionary[dicts.Keys.ElementAt(i)].ContainsKey(word))
                                {
                                    string regularExpression = ToRegexPattern(word);

                                    cleanDictText = Regex.Replace(cleanDictText, regularExpression, StarCensoredMatchRemoveApostrophe,
                                                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                                }
                            }
                        }
                    }
                }

                return cleanDictText;
            }
            else return text;
        }

        private static string StarCensoredMatch(Match m)
        {
            string word = m.Captures[0].Value;
            return new string('¢', word.Length);
        }

        private static string StarCensoredMatchProfanityIndex(Match m)
        {
            int index = CensoredWordsCharIndexes[m.Index];
            //string word = CensoredWords.Keys.ElementAt(index);

            //if (word.Length <= HardFilterKey.Length)
            //{
                if (!CensoredWordsIndexes.ContainsKey(index))
                    CensoredWordsIndexes.Add(index, 3);
                else
                    CensoredWordsIndexes[index] += 3;
            //}

            string match = m.Captures[0].Value;
            return new string('¢', match.Length);
            //return match;
        }

        private static string StarCensoredMatchRemoveApostrophe(Match m)
        {
            string word = m.Captures[0].Value;
            string result = "";
            for (int i = 0; i < word.Length; ++i)
                if (word[i] != '\'')
                    result += '¢';
            return result;
        }

        private static string EmoteFilter(Match m)
        {
            string word = m.Captures[0].Value;
            return ":fluttercry:";
        }

        private string ToRegexPattern(string wildcardSearch)
        {
            string regexPattern = Regex.Escape(wildcardSearch);

            regexPattern = regexPattern.Replace(@"\*", ".*?");
            regexPattern = regexPattern.Replace(@"\?", ".");

            if (regexPattern.StartsWith(".*?"))
            {
                regexPattern = regexPattern.Substring(3);
                regexPattern = @"(^\b)*?" + regexPattern + @"\s*";
            }

            return regexPattern;
        }

        private string ToRegexPatternBordered(string wildcardSearch)
        {
            string regexPattern = Regex.Escape(wildcardSearch);

            regexPattern = regexPattern.Replace(@"\*", ".*?");
            regexPattern = regexPattern.Replace(@"\?", ".");

            if (regexPattern.StartsWith(".*?"))
            {
                regexPattern = regexPattern.Substring(3);
                regexPattern = @"(^\b)*?" + regexPattern + @"\s*";
            }

            regexPattern = @"\b" + regexPattern + @"\b";

            return regexPattern;
        }

        public static string RemoveNonAlphaNumeric(string orig)
        {
            for (int i = orig.Length - 1; i >= 0; --i)
            {
                if (!isAlphaNumeric(orig[i]) && orig[i] != '\0')
                    orig = orig.Remove(i, 1);
            }

            return orig;
        }

        public static string RemoveNonAlphaNumericStar(string orig)
        {
            for (int i = orig.Length - 1; i >= 0; --i)
            {
                if (!isAlphaNumericStar(orig[i]) && orig[i] != '\0')
                    orig = orig.Remove(i, 1);
            }

            return orig;
        }


        public static string RemoveSpacing(string orig)
        {
            for (int i = orig.Length - 1; i >= 0; --i)
            {
                if (isSpacing(orig[i]) && orig[i] != '\0')
                    orig = orig.Remove(i, 1);
            }

            return orig;
        }

        public static string RefillNonAlphaNumericStar(string orig, string nonAlphaNumericStars)
        {
            for (int i = 0; i < nonAlphaNumericStars.Length; ++i)
            {
                if (!isAlphaNumericStar(nonAlphaNumericStars[i]))
                {
                    string part1 = orig.Substring(0, i);
                    string part2 = orig.Substring(i, orig.Length - i);

                    char refill = nonAlphaNumericStars[i];

                    orig = part1 + refill + part2;
                }
            }

            return orig;
        }

        public static string RefillNonAlphaNumeric(string orig, string nonAlphaNumerics)
        {
            for (int i = 0; i < nonAlphaNumerics.Length; ++i)
            {
                if (!isAlphaNumeric(nonAlphaNumerics[i]))
                {
                    string part1 = orig.Substring(0, i);
                    string part2 = orig.Substring(i, orig.Length - i);

                    char refill = nonAlphaNumerics[i];
                    /*
                    bool condition1 = false;
                    if (part1.Length != 0)
                        condition1 = !isAlphaNumeric(part1[part1.Length - 1]);

                    bool condition2 = false;
                    if (part2.Length != 0)
                        condition2 = !isAlphaNumeric(part2[0]);
                    
                    if (condition1 && condition2)
                        refill = '¢';
                    */

                    orig = part1 + refill + part2;
                }
            }

            return orig;
        }

        public static string RefillApostrophes(string orig, string apostrophes)
        {
            for (int i = 0; i < apostrophes.Length; ++i)
            {
                if (apostrophes[i] == '\'')
                {
                    string part1 = orig.Substring(0, i);
                    string part2 = orig.Substring(i, orig.Length - i);

                    char refill = apostrophes[i];

                    orig = part1 + refill + part2;
                }
            }

            return orig;
        }

        public static bool isSpacing(char c)
        {
            if (char.IsWhiteSpace(c) || c == '_' || c == '-')
                return true;
            return false;
        }

        public static bool isAlphaNumericSpacing(char c)
        {
            if (isSpacing(c))
                return true;

            if (isAlphaNumeric(c))
                return true;

            return false;
        }

        public static bool isAlphaNumericStar(char c)
        {
            if (c == '¢')
                return true;

            if (isAlphaNumeric(c))
                return true;
            return false;
        }

        public static bool isAlphaNumeric(char c)
        {
            if (isAlpha(c))
                return true;

            c = Char.ToLower(c);

            if ((int)c > 47 && (int)c < 58)
                return true;

            return false;
        }

        public static bool isAlpha(char c)
        {
            c = Char.ToLower(c);

            if ((int)c > 96 && (int)c < 123)
                return true;

            return false;
        }

        public static string ReplaceAt(string input, int index, char newChar)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            char[] chars = input.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }
    }

    public class CensorshipManager
    {
        public Censor censor;

        // Use this for initialization
        public void Initialize()
        {
            StreamReader badwords = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "badwords.txt");
            string str = badwords.ReadToEnd().ToLower();
            str = str.Replace(((char)13).ToString(), "");
            string[] filteredWords = str.Split('\n');

            Array.Sort(filteredWords, (x, y) => x.Length.CompareTo(y.Length));
            Array.Reverse(filteredWords);

            Dictionary<string, string> censoredWords = new Dictionary<string, string>();
            for (int i = 0; i < filteredWords.Length; ++i)
            {
                string filteredWord = filteredWords[i];
                if (filteredWord.Length < 2) continue;
                if (!censoredWords.ContainsKey(filteredWord))
                    censoredWords.Add(filteredWord, filteredWord);
            }

            /*
            StreamReader goodwords = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "whitelist.txt");
            string goodstr = goodwords.ReadToEnd().ToLower();
            string[] whitewords = goodstr.Split('\n');

            Array.Sort(whitewords, (x, y) => x.Length.CompareTo(y.Length));
            Array.Reverse(whitewords);

            Dictionary<string, string> whitelist = new Dictionary<string, string>();
            for (int i = 0; i < whitewords.Length; i++)
            {
                string cleanWord = whitewords[i];
                cleanWord = cleanWord.Replace(((char)13).ToString(), "");
                whitelist.Add(whitewords[i], whitewords[i]);
            }
            */

            StreamReader dict = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "cleanDict.txt");
            string dictstr = dict.ReadToEnd().ToLower();
            dictstr = dictstr.Replace(((char)13).ToString(), "");
            string[] dictionary = dictstr.Split('\n');

            Array.Sort(dictionary, (x, y) => x.Length.CompareTo(y.Length));
            Array.Reverse(dictionary);
            Array.Sort(dictionary);

            Dictionary<string, Dictionary<string, string>> dividedDict = new Dictionary<string, Dictionary<string, string>>();
            char previousFirstLetter = (char)0;
            char previousSecondLetter = (char)0;
            char previousThirdLetter = (char)0;
            string charDiv = "";
            for (int i = 0; i < dictionary.Length; ++i)
            {
                string word = dictionary[i];
                if (word.Length > 1)
                {
                    if (word[0] != previousFirstLetter || word[1] != previousSecondLetter || (word.Length > 2 && word[2] != previousThirdLetter))
                    {
                        if (charDiv != "")
                        {
                            List<string> temp = dividedDict[charDiv].Values.ToList();
                            temp.Sort((x, y) => x.Length.CompareTo(y.Length));
                            temp.Reverse();
                            dividedDict[charDiv] = temp.ToDictionary(x => x, x => x);
                        }
                        charDiv = "" + word[0] + word[1];
                        if (word.Length == 2 && !dividedDict.ContainsKey(charDiv))
                            dividedDict.Add(charDiv, new Dictionary<string, string>());

                        if (word.Length > 2) charDiv += word[2];

                        if (!dividedDict.ContainsKey(charDiv))
                            dividedDict.Add(charDiv, new Dictionary<string, string>());
                        
                        previousFirstLetter = word[0];
                        previousSecondLetter = word[1];
                        if (word.Length > 2)  previousThirdLetter = word[2];
                    }
                    dividedDict[charDiv].Add(word, word);
                }
            }
            
            censor = new Censor(censoredWords, dividedDict);
        }

        public void ClearDuplicates()
        {
            Dictionary<string, string> CensoredWords = Censor.CensoredWords;
            Dictionary<string, string> cleared = new Dictionary<string, string>();
            for (int i = 0; i < CensoredWords.Count; ++i)
            {
                //string word = Censor.RemoveSpacing(CensoredWords.ElementAt(i).Value);
                string word = CensoredWords.ElementAt(i).Value;
                if (!cleared.ContainsKey(word))
                {
                    cleared.Add(word, word);
                }
            }

            File.WriteAllLines(@"C:\Users\Public\TestFolder\badwords.txt", cleared.Values.ToArray());
        }

        public void WriteProfanityVariants()
        {
            /*
            Dictionary<string, string> CensoredWords = censor.CensoredWords;
            Dictionary<string, string> WhiteList = censor.WhiteList;

            string[] variantSuffixes = new string[] { "s", "es", "er", "ed", "ers", "ing" };
	        Dictionary<string, string> variants = new Dictionary<string, string>();

	        for (int i = 0; i < CensoredWords.Count; i++)
	        {
		        for (int j = 0; j < variantSuffixes.Length; j++)
		        {
			        string variant = CensoredWords.ElementAt(i).Value + variantSuffixes[j];
			        if (!CensoredWords.ContainsKey(variant) && !variants.ContainsKey(variant))
			        {
				        variants.Add(variant, variant);
			        }
		        }
	        }

	        for (int i = variants.Count - 1; i >= 0; i--)
	        {
                if (!WhiteList.ContainsKey(variants.ElementAt(i).Value))
                    variants.Remove(variants.ElementAt(i).Key);
	        }

	        File.WriteAllLines(@"C:\Users\Public\TestFolder\variants.txt", variants.Values.ToArray());
	        //*/
        }

        public void WriteWhiteList()
        {
            ///*
            StreamReader dict = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt");
            string dictstr = dict.ReadToEnd().ToLower();

            dictstr = dictstr.Replace(((char)13).ToString(), "");
            string[] actualDictionary = dictstr.Split('\n');
            dictstr = dictstr.Replace('\n'.ToString(), " ");

            dictstr = censor.PureCensor(dictstr);

            string[] dictionary = dictstr.Split(' ');
            List<string> whitelist = new List<string>();
            for (int i = 0; i < dictionary.Length; i++)
            {
                string word = dictionary[i];
                if (word.Contains("*"))
                {
                    if (!Censor.CensoredWords.ContainsKey(actualDictionary[i]))
                        whitelist.Add(actualDictionary[i]);
                }
            }

            File.WriteAllLines(@"C:\Users\Public\TestFolder\whitelist.txt", whitelist.ToArray());
            //*/
        }

        public void WriteCleanDictionary()
        {
            ///*
            Dictionary<string, string> CensoredWords = Censor.CensoredWords;
            
            StreamReader dict = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt");
            string dictstr = dict.ReadToEnd().ToLower();
            char[] splitStr = new char[] { (char)13, '\n' };
            string[] dictionary = dictstr.Split(splitStr);

            List<string> cleanDict = new List<string>();

            for (int i = 0; i < dictionary.Length; i++)
            {
                string actualWord = dictionary[i];

                if (actualWord.Length < 2)
                    continue;

                bool foundbad = CensoredWords.ContainsKey(actualWord);

                if (foundbad) continue;

                cleanDict.Add(actualWord);
            }
            string chars = "";
            for (int i = 0; i < cleanDict[0].Length; i++)
                chars += (int)cleanDict[0][i] + " ";
            //Console.WriteLine(chars);

            File.WriteAllLines(@"C:\Users\Public\TestFolder\cleanDict.txt", cleanDict.ToArray());
            //*/
        }
    }
}