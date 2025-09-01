using System.Text.RegularExpressions;

namespace ProcessaTxtAsync
{
    public static class FileProcessor
    {
        private static readonly Regex WordRegex = new Regex(@"\b[\p{L}\p{N}']+\b",
                                                            RegexOptions.Compiled);

        public static async Task<(string FileName, int Lines, int Words)> ProcessFileAsync(string path)
        {
            int lineCount = 0;
            int wordCount = 0;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var sr = new StreamReader(fs);

            string? line;
            while ((line = await sr.ReadLineAsync()) is not null)
            {
                lineCount++;
                wordCount += WordRegex.Matches(line).Count;
            }

            return (Path.GetFileName(path), lineCount, wordCount);
        }
    }
}
