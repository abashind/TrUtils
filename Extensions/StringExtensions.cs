using System;
using System.Collections.Generic;
using F23.StringSimilarity;

namespace TrUtils.Extensions;

internal static class StringExtensions
{
    public static bool IsNullOrEmpty(this string str) => str is null or "";

    public static bool IsNotNullOrEmpty(this string str) => !str.IsNullOrEmpty();

    /// <summary>
    /// Cut the string after the phrase. If no phrase in the string - return the string as is.
    /// </summary>
    public static string CutAfterPhrase(this string s, string phrase)
    {
        var cutIndex = s.IndexOf(phrase, StringComparison.Ordinal);
        return cutIndex is not -1 ? s[..cutIndex] : s;
    }

    /// <summary>
    /// Cut the string before the phrase. If no phrase in the string - return the string as is.
    /// </summary>
    public static string CutBeforePhrase(this string s, string phrase)
    {
        var cutIndex = s.IndexOf(phrase, StringComparison.Ordinal);
        return cutIndex is not -1 ? s[(cutIndex + phrase.Length)..] : s;
    }

    public static bool ContainedIn(this string s, string container) =>
        container is not null && container.Contains(s);

    public static string ReplaceUtfForComment(this string input)
    {
        return input
            .Replace("&#039;", "'")
            .Replace("&quot;", "\"")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">");
    }

    /// <summary>
    /// Normalized Levenshtein similarity.
    /// </summary>
    /// <param name="source">Source string.</param>
    /// <param name="target">String to compare with.</param>
    /// <param name="percentage">How much similarity between the strings should be to considered them similar. 100% - strings should be same.</param>
    /// <returns></returns>
    public static bool IsSimilarTo(this string source, string target, int percentage)
    {
        var similarity = new NormalizedLevenshtein().Similarity(source, target);
        return similarity * 100 >= percentage;
    }

    public static string FindPattern(this IList<string> source)
    {
        //TODO Implement better pattern finder.
        return source[1];
    }
}