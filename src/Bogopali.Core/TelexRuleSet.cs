using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bogopali.Core;

/// <summary>
/// An immutable set of Telex Rules, each mapping a typed sequence to the Pāli text it produces.
/// Sequences match case-insensitively; outputs are stored in Unicode NFC.
/// </summary>
public sealed class TelexRuleSet
{
    private readonly Dictionary<string, string> _rules;

    /// <summary>Creates a rule set from sequence and output pairs.</summary>
    /// <param name="rules">The rules. Sequences are lowercased and both sides are normalized to NFC.</param>
    /// <exception cref="ArgumentException">A sequence or output is empty, or two sequences differ only by case.</exception>
    public TelexRuleSet(IEnumerable<KeyValuePair<string, string>> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        _rules = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string sequence, string output) in rules)
        {
            if (string.IsNullOrEmpty(sequence))
            {
                throw new ArgumentException("A Telex Rule sequence cannot be empty.", nameof(rules));
            }

            if (string.IsNullOrEmpty(output))
            {
                throw new ArgumentException($"The Telex Rule '{sequence}' has no output.", nameof(rules));
            }

            string key = Normalize(sequence).ToLowerInvariant();
            if (!_rules.TryAdd(key, Normalize(output)))
            {
                throw new ArgumentException($"The Telex Rule '{sequence}' is defined twice.", nameof(rules));
            }

            MaxSequenceLength = Math.Max(MaxSequenceLength, key.Length);
        }
    }

    /// <summary>
    /// The default Telex Rules from ADR 0003, including the Diacritic Cycle steps for <c>n</c> and <c>m</c>.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, string>> DefaultRules { get; } =
    [
        new("af", "ā"),
        new("aa", "ā"),
        new("if", "ī"),
        new("ii", "ī"),
        new("uf", "ū"),
        new("uu", "ū"),
        new("tf", "ṭ"),
        new("df", "ḍ"),
        new("lf", "ḷ"),
        new("mf", "ṃ"),
        new("ṃf", "ṁ"),
        new("ṁf", "m"),
        new("nf", "ṇ"),
        new("ṇf", "ñ"),
        new("ñf", "ṅ"),
        new("ṅf", "n"),
        new("nj", "ñ"),
        new("nq", "ṅ"),
    ];

    /// <summary>The rule set built from <see cref="DefaultRules"/>.</summary>
    public static TelexRuleSet Default { get; } = new(DefaultRules);

    /// <summary>The length of the longest sequence, in UTF-16 code units.</summary>
    public int MaxSequenceLength { get; }

    /// <summary>The number of rules.</summary>
    public int Count => _rules.Count;

    /// <summary>Looks up the output for an exact, lowercase, NFC sequence.</summary>
    /// <param name="sequence">The sequence to look up.</param>
    /// <param name="output">The output when found.</param>
    /// <returns><see langword="true"/> when a rule exists for <paramref name="sequence"/>.</returns>
    public bool TryGetOutput(string sequence, [MaybeNullWhen(false)] out string output) =>
        _rules.TryGetValue(sequence, out output);

    private static string Normalize(string text) => text.Normalize(NormalizationForm.FormC);
}
