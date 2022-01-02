using System;
using System.Linq;

namespace System.Text.RegularExpressions
{
    internal static class RegexUtils
    {
        internal const string RegexWildCardExp = "\\S+";

        internal const byte Q = 5;    // quantifier
        internal const byte S = 4;    // ordinary stoppper
        internal const byte Z = 3;    // ScanBlank stopper
        internal const byte X = 2;    // whitespace
        internal const byte E = 1;    // should be escaped

        internal static readonly byte[] _category = new byte[] {
            // 0 1 2 3 4 5 6 7 8 9 A B C D E F 0 1 2 3 4 5 6 7 8 9 A B C D E F 
               0,0,0,0,0,0,0,0,0,X,X,0,X,X,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            //   ! " # $ % & ' ( ) * + , - . / 0 1 2 3 4 5 6 7 8 9 : ; < = > ? 
               X,0,0,Z,S,0,0,0,S,S,Q,Q,0,0,S,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,
            // @ A B C D E F G H I J K L M N O P Q R S T U V W X Y Z [ \ ] ^ _
               0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,S,S,0,S,0,
            // ' a b c d e f g h i j k l m n o p q r s t u v w x y z { | } ~ 
               0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,S,0,0,0};

        internal static string EscapeExcluding(string input, params char[] exclude)
        {
            if (exclude is null)
                throw new ArgumentNullException("exclude");

            for (int i = 0; i < input.Length; i++)
            {
                if (IsMetachar(input[i]) && !exclude.Contains(input[i]))
                {
                    StringBuilder sb = new StringBuilder();
                    char ch = input[i];
                    int lastpos;

                    sb.Append(input, 0, i);
                    do
                    {
                        sb.Append('\\');
                        switch (ch)
                        {
                            case '\n':
                                ch = 'n';
                                break;
                            case '\r':
                                ch = 'r';
                                break;
                            case '\t':
                                ch = 't';
                                break;
                            case '\f':
                                ch = 'f';
                                break;
                        }
                        sb.Append(ch);
                        i++;
                        lastpos = i;

                        while (i < input.Length)
                        {
                            ch = input[i];
                            if (IsMetachar(ch) && !exclude.Contains(input[i]))
                                break;

                            i++;
                        }

                        sb.Append(input, lastpos, i - lastpos);

                    } while (i < input.Length);

                    return sb.ToString();
                }
            }

            return input;
        }

        internal static bool IsMetachar(char ch)
        {
            return (ch <= '|' && _category[ch] >= E);
        }

        internal static Regex CreateRegexPattern(string input, string wildCardStr)
        {
            var escapedStr = EscapeExcluding(input, wildCardStr.ToArray());
            var initial = "\\A" + escapedStr.Replace(wildCardStr, RegexWildCardExp) + "\\Z";
            var patternStr = Regex.Replace(initial, @"{(?<name>\w+):(?<constraint>\w+)?}", match => $"(?<{match.Groups["name"].Value}>{(match.Groups["constraint"].Success ? GetConstraint(match.Groups["constraint"].Value) : @"\S+")})");
            return new Regex(patternStr, RegexOptions.Compiled | RegexOptions.Singleline);

        }

        private static string GetConstraint(string key) =>
            key switch
            {
                "alpha" => @"\w+",
                "int" => @"-?\d+",
                "bool" => @"(true|false)",
                "datetime" => @"(\d{4}-\d{2}-\d{2} \d{1,2}:\d{2}(am|pm)|\d{4}-\d{2}-\d{2})",
                "float" or "decimal" => @"-?\d(.\d+)?",
                "guid" => @"[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}",
                _ => throw new InvalidOperationException($"{key} isn't a valid wild card constraint.");
            };
    }
}
