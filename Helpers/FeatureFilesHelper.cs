using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TrUtils.Extensions;

namespace TrUtils.Helpers;

public static class FeatureFilesHelper
{
    private static IEnumerable<int> SearchLines(IReadOnlyList<string> lines, string searchLine)
    {
        var positions = new List<int>();
        for (var i = lines.Count - 1; i >= 0; i--)
            if (Regex.IsMatch(lines[i], searchLine))
                positions.Add(i);

        return positions;
    }

    private static string GetSolutionDirectory(string solutionPattern, string dir = "")
    {
        while (true)
        {
            if (dir.IsNullOrEmpty())
                dir = Directory.GetCurrentDirectory();
            if (Directory.GetFiles(dir, solutionPattern).Any())
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent is null)
                throw new Exception($"Could not find solution by the '{solutionPattern}' pattern.");
            dir = parent.FullName;
        }
    }

    public static List<string> FindAllNotUtfFiles(string path = null)
    {
        path ??= GetSolutionDirectory("*.sln");
        var allFiles = Directory.GetFiles(path, "*.feature", SearchOption.AllDirectories);

        var result = new List<string>();
        foreach (var file in allFiles)
        {
            using var reader = new StreamReader(file, Encoding.ASCII, true);
            reader.Peek();
            var encoding = reader.CurrentEncoding;
            if(!encoding.Equals(Encoding.UTF8))
                result.Add(file);
        }

        return result;
    }

    public static void DoActionInFeatureFilesNearbyFoundLine(string lineToFind, Action<List<int>, List<string>, string> actionToDo, string operand, string path = null)
    {
        path ??= GetSolutionDirectory("*.sln");

        var allFiles = Directory.GetFiles(path, "*.feature", SearchOption.AllDirectories);

        var count = 0;

        foreach (var f in allFiles)
        {
            var lines = File.ReadAllLines(f).ToList();
            var positions = SearchLines(lines, lineToFind).ToList();

            if (!positions.Any()) continue;

            actionToDo(positions, lines, operand);

            File.WriteAllLines(f, lines);

            count += positions.Count;
        }

        Console.WriteLine($"'{count}' lines were found and processed with '{operand}'.");
    }

    public static void AddLineToFeatureFilesAboveLineToFind(string lineToFind, string addLine)
    {
        void ActionToDo(List<int> positions, List<string> lines, string operand)
        {
            foreach (var p in positions)
                lines.Insert(p, $"    {operand}");
        }
        DoActionInFeatureFilesNearbyFoundLine(lineToFind, ActionToDo, addLine);
    }

    public static void AddLineToFeatureFilesUnderLineToFind(string lineToFind, string addLine)
    {
        void ActionToDo(IEnumerable<int> positions, IList<string> lines, string operand)
        {
            foreach (var p in positions)
                lines.Insert(p + 1, $"    {operand}");
        }
        DoActionInFeatureFilesNearbyFoundLine(lineToFind, ActionToDo, addLine);
    }

    public static void AddTagToTest(string testName, string tag)
    {
        static void ActionToDo(IEnumerable<int> positions, IList<string> lines, string operand)
        {
            foreach (var p in positions)
            {
                if (lines[p - 1].Contains($@"{operand}"))
                    continue;
                lines[p - 1] += $" @{operand}";
            }
        }
        DoActionInFeatureFilesNearbyFoundLine(testName, ActionToDo, tag);
    }

    public static void AddTagToTests(IEnumerable<string> tests, string tag)
    {
        foreach (var test in tests)
        {
            AddTagToTest(test, tag);
            Console.WriteLine($"{tag} --> {test}");
        }
    }

    /// <summary>
    /// Return Dictionary(test, tags.)
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Dictionary<string, string> GetAllSpecflowScenariosAndTheirsTags(string path = null)
    {
        path ??= GetSolutionDirectory("*.sln");

        var allFiles = Directory.GetFiles(path, "*.feature", SearchOption.AllDirectories);
        const string searchLine1 = "Scenario:";
        const string searchLine2 = "Scenario Outline:";

        var result = new Dictionary<string, string>();

        foreach (var f in allFiles)
        {
            var lines = File.ReadAllLines(f).ToList();
            var positions = SearchLines(lines, searchLine1).ToList();
            positions.AddRange(SearchLines(lines, searchLine2).ToList());

            foreach (var p in positions)
            {
                var test = lines[p].Replace(searchLine1, "").Replace(searchLine2, "").Trim();
                if (result.ContainsKey(test))
                    continue;

                var tags = lines[p - 1];
                result.Add(test, tags);
            }
        }

        return result;
    }

    public static IEnumerable<string> CleanTestsFromIterators(IEnumerable<string> tests)
    {
        var testsToMark = new HashSet<string>();

        var specFlowTests = GetAllSpecflowScenariosAndTheirsTags().Keys;
        foreach (var test in tests)
        {
            var testTitle = test;
            while (true)
            {
                if (specFlowTests.Any(st => st.Contains(testTitle)))
                {
                    testsToMark.Add(testTitle);
                    break;
                }

                var cutIndex = testTitle.LastIndexOf('_');
                if (cutIndex is -1)
                    break;
                testTitle = testTitle.Remove(cutIndex);
            }
        }

        return testsToMark.Where(t => t.Length > 25);
    }
}