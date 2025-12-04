using System.Text.RegularExpressions;

namespace DeliverX.Infrastructure.Utilities;

public interface INameMatchHelper
{
    int CalculateSimilarity(string name1, string name2);
}

public class NameMatchHelper : INameMatchHelper
{
    /// <summary>
    /// Calculate similarity percentage between two names using Levenshtein distance
    /// Returns a score from 0 to 100
    /// </summary>
    public int CalculateSimilarity(string name1, string name2)
    {
        if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
            return 0;

        // Normalize names: uppercase, remove extra spaces, remove special characters
        var normalized1 = NormalizeName(name1);
        var normalized2 = NormalizeName(name2);

        if (normalized1 == normalized2)
            return 100;

        // Calculate Levenshtein distance
        int distance = LevenshteinDistance(normalized1, normalized2);
        int maxLength = Math.Max(normalized1.Length, normalized2.Length);

        if (maxLength == 0)
            return 100;

        // Convert distance to similarity percentage
        double similarity = (1.0 - (double)distance / maxLength) * 100;
        return (int)Math.Round(similarity);
    }

    private string NormalizeName(string name)
    {
        // Convert to uppercase
        var normalized = name.ToUpperInvariant();

        // Remove special characters, keep only letters and spaces
        normalized = Regex.Replace(normalized, @"[^A-Z\s]", "");

        // Remove extra spaces
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    private int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        int sourceLength = source.Length;
        int targetLength = target.Length;
        int[,] distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize first column and row
        for (int i = 0; i <= sourceLength; i++)
            distance[i, 0] = i;

        for (int j = 0; j <= targetLength; j++)
            distance[0, j] = j;

        // Calculate distances
        for (int i = 1; i <= sourceLength; i++)
        {
            for (int j = 1; j <= targetLength; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(
                        distance[i - 1, j] + 1,      // Deletion
                        distance[i, j - 1] + 1),     // Insertion
                    distance[i - 1, j - 1] + cost);  // Substitution
            }
        }

        return distance[sourceLength, targetLength];
    }
}
