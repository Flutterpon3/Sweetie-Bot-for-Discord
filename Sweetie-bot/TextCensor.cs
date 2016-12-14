using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Sweetie_bot
{
    public class Censor
    {
        public Dictionary<string, string> CensoredWords { get; private set; }
        public string CensoredWordsString { get; private set; }
        public List<Dictionary<string, string>> Dictionary { get; private set; }

        public Censor(Dictionary<string, string> censoredWords, List<Dictionary<string, string>> dictWords)
        {
            if (censoredWords == null)
                throw new ArgumentNullException("censoredWords");

            if (dictWords == null)
                throw new ArgumentNullException("dictWords");

            CensoredWords = censoredWords;
            foreach (string word in CensoredWords.Keys)
                CensoredWordsString += word + "?";
            Dictionary = dictWords;
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
                throw new ArgumentNullException("text");

            string quickfilter = QuickFilter(message);
            string alteredMessage = RemoveNonAlphaNumeric(quickfilter);

            string filthdrain = alteredMessage;
            if (filthdrain.Length == 0) return quickfilter;
            filthdrain = Strip(quickfilter);

            string stripped = "";
            for (int i = 0; i < filthdrain.Length; ++i)
            {
                if (quickfilter[i] == '*') stripped += message[i];
                else stripped += filthdrain[i];
            }

            filthdrain = RemoveNonAlphaNumeric(filthdrain);
            if (filthdrain.Length == 0) return quickfilter;
            string censoredMessage = Filter(RemoveNonAlphaNumericStar(stripped), alteredMessage, filthdrain);
            censoredMessage = RefillNonAlphaNumeric(censoredMessage, quickfilter);
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
                if (!filteredWords[i].Equals(words[i]))
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
                if (!filteredWords[i].Equals(words[i]))
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
                if (!filteredWords[i].Equals(words[i]))
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

            foreach (string word in words)
            {
                string readword = word;
                if (CensoredWords.ContainsKey(readword))
                {
                    string regularExpression = ToRegexPattern(readword);

                    quickfilter = Regex.Replace(quickfilter, regularExpression, StarCensoredMatch,
                                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
            }
            return quickfilter;
        }

        public string Filter(string text, string alteredText, string key)
        {
            string censoredText = text;

            string alteredbadwords = CensoredWordsString;
            int cap = 3;
            for (int i = 0; i < key.Length - 2; i += 3)
            {
                string set = "";
                int limit = key.Length - i;
                if (limit > cap) limit = cap;

                int offset = 0;
                if (limit < cap) offset = cap - limit;

                for (int j = 0; j < cap; ++j)
                {
                    //System.Diagnostics.Debug.WriteLine(j + i - offset);
                    //System.Diagnostics.Debug.WriteLine(key.Length - 1);
                    set += key[j + i - offset];
                    
                }

               
                   

                string regularExpression = ToRegexPattern(set);
                alteredbadwords = Regex.Replace(alteredbadwords, regularExpression, StarCensoredMatch,
                                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            string[] limitedBadWords = alteredbadwords.Split("?".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < CensoredWords.Count; ++i)
            {
                if (limitedBadWords[i].Contains('*'))
                {
                    string censoredWord = CensoredWords.Keys.ElementAt(i);
                    if (!(censoredText.IndexOf(censoredWord, StringComparison.OrdinalIgnoreCase) >= 0)) continue;

                    string regularExpression = ToRegexPattern(censoredWord);
                    censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                                                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    
                }
            }

            string resultText = "";
            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '*' && censoredText[i] == '*')
                    resultText += alteredText[i];
                else resultText += char.IsUpper(alteredText[i]) ? alteredText[i] : censoredText[i];
            }
            return resultText;
        }

        string Unfilter(string censored, string unfilter, string original)
        {
            string unfiltered = "";
            for (int i = 0; i < original.Length; ++i)
            {
                if (unfilter[i] == '*') unfiltered += original[i];
                else unfiltered += censored[i];
            }
            return unfiltered;
        }

        public string Strip(string text)
        {
            string lText = text.ToLower();
            string cleanDictText = lText;
            int currentDictIndex = 0;
            int wordPlace = 0;
            List<int> dictIndexes = new List<int>();
            for (int i = 0; i < cleanDictText.Length; ++i)
            {
                bool isalpha = isAlpha(cleanDictText[i]);
                if (wordPlace < 2)
                {
                    if (isalpha)
                    {
                        for (int j = currentDictIndex; j < Dictionary.Count; ++j)
                        {
                            if (Dictionary[j].ElementAt(0).Value[wordPlace] == cleanDictText[i])
                            {
                                currentDictIndex = j;
                                break;
                            }

                            if (j == Dictionary.Count - 1)
                                currentDictIndex = -1;
                        }
                        ++wordPlace;
                    }
                }

                if (wordPlace == 2 || !isalpha)
                {
                    if (currentDictIndex != -1)
                    {
                        dictIndexes.Add(currentDictIndex);
                    }
                    currentDictIndex = 0;
                    wordPlace = 0;
                }
            }

            for (int i = 0; i < dictIndexes.Count; ++i)
            {
                string[] words3 = lText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words3)
                {
                    string readword = word;
                    bool contains = Dictionary[dictIndexes[i]].ContainsKey(readword);
                    if (!contains)
                    {
                        readword = RemoveNonAlphaNumeric(word);
                        contains = Dictionary[dictIndexes[i]].ContainsKey(readword);
                    }
                    if (contains)
                    {
                        string regularExpression = ToRegexPattern(readword);

                        cleanDictText = Regex.Replace(cleanDictText, regularExpression, StarCensoredMatch,
                                                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    }
                }
            }
            return cleanDictText;
        }



        private static string StarCensoredMatch(Match m)
        {
            string word = m.Captures[0].Value;
            return new string('*', word.Length);
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
                        refill = '*';
                    */

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
            if (c == '*')
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
            System.Diagnostics.Debug.WriteLine(AppDomain.CurrentDomain.BaseDirectory + "badwords.txt");
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

            List<Dictionary<string, string>> dividedDict = new List<Dictionary<string, string>>();
            char previousFirstLetter = (char)0;
            char previousSecondLetter = (char)0;
            int div = -1;
            for (int i = 0; i < dictionary.Length; ++i)
            {
                string word = dictionary[i];
                if (word.Length > 1)
                {
                    if (word[0] != previousFirstLetter || word[1] != previousSecondLetter)
                    {
                        if (div != -1)
                        {
                            List<string> temp = dividedDict[div].Values.ToList();
                            temp.Sort((x, y) => x.Length.CompareTo(y.Length));
                            temp.Reverse();
                            dividedDict[div] = temp.ToDictionary(x => x, x => x);
                        }
                        dividedDict.Add(new Dictionary<string, string>());
                        ++div;

                        previousFirstLetter = word[0];
                        previousSecondLetter = word[1];
                    }
                    dividedDict[div].Add(word, word);
                }
            }


            censor = new Censor(censoredWords, dividedDict);
        }

        public void ClearDuplicates()
        {
            Dictionary<string, string> CensoredWords = censor.CensoredWords;
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
                    if (!censor.CensoredWords.ContainsKey(actualDictionary[i]))
                        whitelist.Add(actualDictionary[i]);
                }
            }

            File.WriteAllLines(@"C:\Users\Public\TestFolder\whitelist.txt", whitelist.ToArray());
            //*/
        }

        public void WriteCleanDictionary()
        {
            ///*
            Dictionary<string, string> CensoredWords = censor.CensoredWords;
            
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