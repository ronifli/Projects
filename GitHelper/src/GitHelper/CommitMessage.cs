using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitHelper
{
    /// <summary>The fields that make up a structured commit message.</summary>
    public sealed class CommitFields
    {
        public CommitFields()
        {
            Subject = string.Empty;
            Body = string.Empty;
            TT = string.Empty;
            Customer = string.Empty;
            CommitType = string.Empty;
            Risk = string.Empty;
            Areas = new string[0];
        }

        public string Subject { get; set; }
        public string Body { get; set; }
        public string TT { get; set; }
        public string Customer { get; set; }
        public string CommitType { get; set; }
        public string Risk { get; set; }

        /// <summary>
        /// The areas this change touches - at least one, several allowed.
        /// Written as a comma-separated list.
        /// </summary>
        public string[] Areas { get; set; }

        public bool OtherVersions { get; set; }
    }

    /// <summary>
    /// Build and parse the message format. <see cref="Build"/> and
    /// <see cref="Parse"/> are a round trip - the "Last commit" button relies
    /// on it, so change both together.
    /// </summary>
    public static class CommitMessage
    {
        public const string TypePlaceholder = "<Commit Type>";
        public const string RiskPlaceholder = "<Risk Level>";
        public const string AreaPlaceholder = "<Area>";

        /// <summary>Keys recognised in the field block at the end of a message.</summary>
        private static readonly string[] Known =
            { "TT", "Customer", "Commit Type", "Risk", "Area", "Other Versions" };

        public static string Build(CommitFields f)
        {
            // The placeholders only ever reach the preview - the action buttons
            // stay disabled until Commit Type, Risk Level and Area are chosen.
            var type = string.IsNullOrEmpty(f.CommitType) ? TypePlaceholder : f.CommitType;
            var risk = string.IsNullOrEmpty(f.Risk) ? RiskPlaceholder : f.Risk;

            var areas = f.Areas == null
                ? new string[0]
                : f.Areas.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).ToArray();
            var area = areas.Length > 0 ? string.Join(", ", areas) : AreaPlaceholder;

            var lines = new List<string> { "Subject: " + f.Subject.Trim() };

            var body = f.Body.Trim();
            if (body.Length > 0) lines.Add("Body: " + body);

            lines.Add(string.Empty);

            // optional fields: omitted entirely when blank
            if (f.TT.Trim().Length > 0) lines.Add("TT: " + f.TT.Trim());
            if (f.Customer.Trim().Length > 0) lines.Add("Customer: " + f.Customer.Trim());

            // always present
            lines.Add("Commit Type: " + type);
            lines.Add("Risk: " + risk);
            lines.Add("Area: " + area);
            lines.Add("Other Versions: " + (f.OtherVersions ? "Yes" : "No"));

            return string.Join("\r\n", lines);
        }

        /// <summary>
        /// Was this message written by the dialog? True only when it carries
        /// both a <c>Subject:</c> line and a <c>Commit Type:</c> line, which is
        /// what <see cref="Build"/> always writes and free prose never does.
        ///
        /// <see cref="Parse"/> deliberately degrades an unstructured message to
        /// line 1 = Subject, so the parsed fields alone cannot tell the two
        /// apart - this can.
        /// </summary>
        public static bool IsStructured(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;

            var hasSubject = false;
            var hasType = false;

            foreach (var raw in message.Split(new[] { '\n' }, StringSplitOptions.None))
            {
                var line = raw.TrimEnd('\r').TrimStart();

                if (!hasSubject && line.StartsWith("Subject:", StringComparison.Ordinal))
                    hasSubject = line.Substring("Subject:".Length).Trim().Length > 0;
                else if (!hasType && line.StartsWith("Commit Type:", StringComparison.Ordinal))
                    hasType = line.Substring("Commit Type:".Length).Trim().Length > 0;

                if (hasSubject && hasType) return true;
            }
            return false;
        }

        /// <summary>
        /// Split a message written by this dialog back into its fields. The
        /// field block is read from the *bottom* upward and stops at the first
        /// line that isn't a known key - a body containing prose like "we
        /// lowered Risk: High to low" must not be mistaken for a field. A
        /// message that isn't in this format at all (an older commit) degrades
        /// to line 1 = Subject, remainder = Body, with no fields invented.
        /// </summary>
        public static CommitFields Parse(string text)
        {
            var result = new CommitFields();
            var found = new Dictionary<string, string>(StringComparer.Ordinal);

            var lines = new List<string>((text ?? string.Empty)
                .Split(new[] { '\n' }, StringSplitOptions.None)
                .Select(l => l.TrimEnd('\r')));
            TrimTrailingBlank(lines);

            var keyLine = new Regex(@"^\s*([A-Za-z][A-Za-z ]*?)\s*:\s*(.*)$");
            while (lines.Count > 0)
            {
                var m = keyLine.Match(lines[lines.Count - 1]);
                if (!m.Success || !Known.Contains(m.Groups[1].Value)) break;
                found[m.Groups[1].Value] = m.Groups[2].Value.Trim();
                lines.RemoveAt(lines.Count - 1);
            }

            TrimTrailingBlank(lines);

            if (lines.Count > 0)
            {
                var first = lines[0];
                var subj = Regex.Match(first, @"^\s*Subject:\s*(.*)$");
                // not our format - treat line 1 as the subject, the rest as body
                result.Subject = (subj.Success ? subj.Groups[1].Value : first).Trim();
                lines.RemoveAt(0);

                while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
                if (lines.Count > 0)
                {
                    var bodyFirst = Regex.Match(lines[0], @"^\s*Body:\s*(.*)$");
                    if (bodyFirst.Success) lines[0] = bodyFirst.Groups[1].Value;
                    result.Body = string.Join("\r\n", lines).Trim();
                }
            }

            result.TT = Field(found, "TT");
            result.Customer = Field(found, "Customer");
            result.CommitType = Field(found, "Commit Type");
            result.Risk = Field(found, "Risk");

            // 'Area: CDB, Orcal' back into its parts. The placeholder never
            // reaches a real commit, but an older message has no Area line at
            // all, which reads as none selected.
            var areaLine = Field(found, "Area");
            result.Areas = areaLine.Length == 0 || areaLine == AreaPlaceholder
                ? new string[0]
                : areaLine.Split(',')
                    .Select(a => a.Trim())
                    .Where(a => a.Length > 0)
                    .ToArray();

            result.OtherVersions = Field(found, "Other Versions") == "Yes";
            return result;
        }

        private static string Field(Dictionary<string, string> found, string key)
        {
            string value;
            return found.TryGetValue(key, out value) ? value : string.Empty;
        }

        private static void TrimTrailingBlank(List<string> lines)
        {
            while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
                lines.RemoveAt(lines.Count - 1);
        }

        /// <summary>
        /// git reads the message file as UTF-8; write it without a BOM, or the
        /// stray bytes show up at the start of the subject line.
        /// </summary>
        public static string WriteTempFile(string message)
        {
            var tmp = Path.GetTempFileName();
            File.WriteAllText(tmp, message, new UTF8Encoding(false));
            return tmp;
        }
    }
}
