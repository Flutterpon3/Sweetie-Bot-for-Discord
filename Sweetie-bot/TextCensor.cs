using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Sweetie_bot
{
    public class Censor
    {
        public HashSet<string> CensoredWords { get; private set; }
        public Dictionary<string, HashSet<string>> WordDictionary { get; private set; }

        public Censor(HashSet<string> censoredWords, Dictionary<string, HashSet<string>> dictWords)
        {
            CensoredWords = censoredWords ?? throw new ArgumentNullException("censoredWords");

            WordDictionary = dictWords ?? throw new ArgumentNullException("dictWords");
        }

        public void DictRemove(string word, string substring)
        {
            WordDictionary[substring].Remove(word);
            if (WordDictionary[substring].Count == 0)
                WordDictionary.Remove(substring);
        }

        public bool CensorCheck(string CheckString)
        {
            foreach(string word in CheckString.Split(' ').ToArray())
            {
                if (CensoredWords.Contains(word.ToLower())){
                    return true;
                }
            }

            return false;
        }
        
        public string PureCensor(string censoredText)
        {
            if (censoredText == null)
                throw new ArgumentNullException("censoredText");

            foreach (string censoredWord in CensoredWords)
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

            HashSet<string> temp = new HashSet<string>();
            foreach (string word in words)
            {
                if (!temp.Contains(word))
                {
                    temp.Add(word);
                    if (CensoredWords.Contains(word))
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
                //CensoredWordsIndexes = CensoredWordsIndexes.OrderBy(x => CensoredWords.Keys.ElementAt(x.Key).Length - x.Value).ToDictionary(x => x.Key, x => x.Value);

                for (int i = 0; i < CensoredWords.Count; ++i)
                {
                    string censoredWord = CensoredWords.ToList()[i];
                    if (censoredWord.Length > key.Length) continue;

                    if (key.IndexOf(censoredWord, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    string regularExpression = ToRegexPattern(censoredWord);

                    string old = censoredText;
                    censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (censoredText != old)
                        key = key.Replace(censoredWord, string.Empty);
                }
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
                Dictionary<string, List<string>> dicts = new Dictionary<string, List<string>>();
                string wordString = "";
                for (int i = 0; i < cleanDictText.Length; ++i)
                {
                    char lower = char.ToLower(cleanDictText[i]);
                    bool isalpha = IsAlpha(lower) || lower == '\'';
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
                            dicts.Add(currentDict, new List<string>());
                        wordPlace = 0;
                    }

                    if (isalpha) wordString += cleanDictText[i];

                    if ((i == cleanDictText.Length - 1 && isalpha) || (wordString != "" && !isalpha))
                    {
                        if (wordString.Length > 1)
                        {
                            string sub = wordString.Substring(0, Math.Min(wordString.Length, 3));
                            if (!dicts[sub].Contains(wordString))
                                dicts[sub].Add(wordString);
                        }
                        wordString = "";
                    }
                }
                
                if (dicts.Count > 0)
                {
                    for (int i = 0; i < dicts.Count; ++i)
                    {
                        KeyValuePair<string, List<string>> pair = dicts.ElementAt(i);
                        if (pair.Value.Count > 1)
                        {
                            List<string> temp = pair.Value.OrderByDescending(x => x.Length).ToList();
                            dicts[pair.Key] = temp;
                        }
                    }
                    dicts = dicts.OrderByDescending(x => x.Value.Last().Length).ToDictionary(x => x.Key, x => x.Value);

                    foreach (KeyValuePair<string, List<string>> i in dicts)
                    {
                        foreach (string word in i.Value)
                        {
                            if (WordDictionary.ContainsKey(i.Key))
                            {
                                if (WordDictionary[i.Key].Contains(word))
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

        private string StarCensoredMatch(Match m)
        {
            string word = m.Captures[0].Value;
            return new string('¢', word.Length);
        }

        private string StarCensoredMatchRemoveApostrophe(Match m)
        {
            string word = m.Captures[0].Value;
            string result = "";
            for (int i = 0; i < word.Length; ++i)
                if (word[i] != '\'')
                    result += '¢';
            return result;
        }

        private string EmoteFilter(Match m)
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

        public string RemoveNonAlphaNumeric(string orig)
        {
            for (int i = orig.Length - 1; i >= 0; --i)
            {
                if (!IsAlphaNumeric(orig[i]) && orig[i] != '\0')
                    orig = orig.Remove(i, 1);
            }

            return orig;
        }

        public string RemoveNonAlphaNumericStar(string orig)
        {
            for (int i = orig.Length - 1; i >= 0; --i)
            {
                if (!IsAlphaNumericStar(orig[i]) && orig[i] != '\0')
                    orig = orig.Remove(i, 1);
            }

            return orig;
        }

        public string RemoveSpacing(string orig)
        {
            for (int i = orig.Length - 1; i >= 0; --i)
            {
                if (IsSpacing(orig[i]) && orig[i] != '\0')
                    orig = orig.Remove(i, 1);
            }

            return orig;
        }

        public string RefillNonAlphaNumericStar(string orig, string nonAlphaNumericStars)
        {
            for (int i = 0; i < nonAlphaNumericStars.Length; ++i)
            {
                if (!IsAlphaNumericStar(nonAlphaNumericStars[i]))
                {
                    string part1 = orig.Substring(0, i);
                    string part2 = orig.Substring(i, orig.Length - i);

                    char refill = nonAlphaNumericStars[i];

                    orig = part1 + refill + part2;
                }
            }

            return orig;
        }

        public string RefillNonAlphaNumeric(string orig, string nonAlphaNumerics)
        {
            for (int i = 0; i < nonAlphaNumerics.Length; ++i)
            {
                if (!IsAlphaNumeric(nonAlphaNumerics[i]))
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

        public string RefillApostrophes(string orig, string apostrophes)
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
        
        public bool IsAlpha(char c)
        {
            c = Char.ToLower(c);

            if ((int)c > 96 && (int)c < 123)
                return true;

            return false;
        }

        public bool IsAlphaNumeric(char c)
        {
            c = Char.ToLower(c);

            return ((IsAlpha(c)) || ((int)c > 47 && (int)c < 58));
        }

        public bool IsSpacing(char c)
        {
            return (char.IsWhiteSpace(c) || c == '_' || c == '-');
        }

        public bool IsAlphaNumericSpacing(char c)
        {
            return (IsSpacing(c) || IsAlphaNumeric(c));
        }

        public bool IsAlphaNumericStar(char c)
        {
            return (c == '¢' || IsAlphaNumeric(c));
        }

        public string ReplaceAt(string input, int index, char newChar)
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

        public string[] LoadDictFromFile(string dir)
        {
            StreamReader words = new StreamReader(dir);
            string str = words.ReadToEnd().ToLower();
            str = str.Replace(('\r').ToString(), "");
            string[] dirArray = str.Split('\n');

            return dirArray;
        }

        // Use this for initialization
        public void Initialize()
        {
            string filterDir = "./filtering/";

            if (!File.Exists(filterDir + "badWords.txt"))
            {
                File.Copy(filterDir + "default_badWords.txt", filterDir + "badWords.txt");
            }

            string[] filteredWords =  LoadDictFromFile(filterDir + "badWords.txt");

            HashSet<string> censoredWords = new HashSet<string>();
            foreach (string word in filteredWords)
            {
                if (word.Length < 2) continue;
                if (!censoredWords.Contains(word))
                    censoredWords.Add(word);
            }

            /*
            StreamReader goodwords = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "whitelist.txt");
            string goodstr = goodwords.ReadToEnd().ToLower();
            string[] whitewords = goodstr.Split('\n');

            Array.Sort(whitewords, (x, y) => x.Length.CompareTo(y.Length));
            Array.Reverse(whitewords);

            List<string> whitelist = new List<string>();
            for (int i = 0; i < whitewords.Length; i++)
            {
                string cleanWord = whitewords[i];
                cleanWord = cleanWord.Replace(((char)13).ToString(), "");
                whitelist.Add(whitewords[i], whitewords[i]);
            }
            */

            if (!File.Exists(filterDir + "cleanDict.txt"))
            {
                File.Copy(filterDir + "default_cleanDict.txt", filterDir + "cleanDict.txt");
            }
            
            string[] dictionary = LoadDictFromFile(filterDir + "cleanDict.txt");

            Dictionary<string, HashSet<string>> dividedDict = new Dictionary<string, HashSet<string>>();
            string prevSubst = "";
            string charDiv = "";
            foreach (string word in dictionary)
            {
                if (word.Length > 1)
                {
                    if (word.Substring(0, Math.Min(word.Length, 3)) != prevSubst)
                    {
                        charDiv = "" + word[0] + word[1];
                        if (word.Length == 2 && !dividedDict.ContainsKey(charDiv))
                            dividedDict.Add(charDiv, new HashSet<string>());

                        if (word.Length > 2) charDiv += word[2];

                        if (!dividedDict.ContainsKey(charDiv))
                            dividedDict.Add(charDiv, new HashSet<string>());

                        prevSubst = word.Substring(0, Math.Min(word.Length, 3));
                    }
                    dividedDict[charDiv].Add(word);
                }
            }
            
            censor = new Censor(censoredWords, dividedDict);
        }

        public void UpdateDictionary()
        {
            List<string> cleanDict = new List<string>();
            foreach (string key in censor.WordDictionary.Keys)
            {
                HashSet<string> subDict = censor.WordDictionary[key];
                cleanDict.AddRange(subDict);
            }
            File.WriteAllLines("filtering/cleanDict.txt", cleanDict.ToArray());
        }

        public void UpdateFilter()
        {
            string[] badwords = censor.CensoredWords.ToArray();
            File.WriteAllLines("filtering/badwords.txt", badwords);
        }

        public void ClearDuplicates()
        {
            HashSet<string> CensoredWords = censor.CensoredWords;
            HashSet<string> cleared = new HashSet<string>();
            for (int i = 0; i < CensoredWords.Count; ++i)
            {
                //string word = Censor.RemoveSpacing(CensoredWords.ElementAt(i).Value);
                string word = CensoredWords.ToList()[i];
                if (!cleared.Contains(word))
                {
                    cleared.Add(word);
                }
            }

            File.WriteAllLines(@"C:\Users\Public\TestFolder\badwords.txt", cleared.ToArray());
        }

        public void WriteProfanityVariants()
        {
            /*
            List<string> CensoredWords = censor.CensoredWords;
            List<string> WhiteList = censor.WhiteList;

            string[] variantSuffixes = new string[] { "s", "es", "er", "ed", "ers", "ing" };
	        List<string> variants = new List<string>();

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
            StreamReader dict = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "filtering/dictionary.txt");
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
                    if (!censor.CensoredWords.Contains(actualDictionary[i]))
                        whitelist.Add(actualDictionary[i]);
                }
            }

            File.WriteAllLines(@"C:\Users\Public\TestFolder\whitelist.txt", whitelist.ToArray());
            //*/
        }

        public void WriteCleanDictionary()
        {
            ///*
            HashSet<string> CensoredWords = censor.CensoredWords;
            
            StreamReader dict = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "filtering/dictionary.txt");
            string dictstr = dict.ReadToEnd().ToLower();
            char[] splitStr = new char[] { (char)13, '\n' };
            string[] dictionary = dictstr.Split(splitStr);

            List<string> cleanDict = new List<string>();

            for (int i = 0; i < dictionary.Length; i++)
            {
                string actualWord = dictionary[i];

                if (actualWord.Length < 2)
                    continue;

                bool foundbad = CensoredWords.Contains(actualWord);

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