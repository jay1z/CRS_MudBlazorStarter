using System;
using System.Linq;
using System.Reflection;

namespace CRS.Services {
    public static class HtmlSanitizerHelper {
        private static readonly object? _sanitizerInstance;
        private static readonly MethodInfo? _sanitizeMethod;

        static HtmlSanitizerHelper() {
            try {
                // Try to find a type named HtmlSanitizer in loaded assemblies
                var asmTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                });

                var sanitizerType = asmTypes.FirstOrDefault(t => string.Equals(t.Name, "HtmlSanitizer", StringComparison.OrdinalIgnoreCase)
                && (t.Namespace?.StartsWith("Ganss", StringComparison.OrdinalIgnoreCase) ?? false));

                // If not found, try loading common assembly names
                if (sanitizerType == null) {
                    var candidates = new[] { "Ganss.XSS", "Ganss.Xss", "Ganss.XSS.Core" };
                    foreach (var name in candidates) {
                        try {
                            var asm = Assembly.Load(new AssemblyName(name));
                            sanitizerType = asm.GetTypes().FirstOrDefault(t => string.Equals(t.Name, "HtmlSanitizer", StringComparison.OrdinalIgnoreCase));
                            if (sanitizerType != null) break;
                        } catch { }
                    }
                }

                if (sanitizerType != null) {
                    _sanitizerInstance = Activator.CreateInstance(sanitizerType)!;

                    // Try to configure common properties if present (AllowedTags, AllowedAttributes, AllowedSchemes)
                    try {
                        var allowedTagsProp = sanitizerType.GetProperty("AllowedTags");
                        var allowedAttrsProp = sanitizerType.GetProperty("AllowedAttributes");
                        var allowedSchemesProp = sanitizerType.GetProperty("AllowedSchemes");

                        if (allowedTagsProp != null) {
                            var collection = allowedTagsProp.GetValue(_sanitizerInstance) as System.Collections.IList;
                            if (collection != null && !collection.Contains("img")) collection.Add("img");
                        }
                        if (allowedAttrsProp != null) {
                            var collection = allowedAttrsProp.GetValue(_sanitizerInstance) as System.Collections.IList;
                            if (collection != null) {
                                if (!collection.Contains("src")) collection.Add("src");
                                if (!collection.Contains("alt")) collection.Add("alt");
                                if (!collection.Contains("title")) collection.Add("title");
                                if (!collection.Contains("class")) collection.Add("class");
                                if (!collection.Contains("href")) collection.Add("href");
                                if (!collection.Contains("rel")) collection.Add("rel");
                            }
                        }
                        if (allowedSchemesProp != null) {
                            var collection = allowedSchemesProp.GetValue(_sanitizerInstance) as System.Collections.IList;
                            if (collection != null && !collection.Contains("data")) collection.Add("data");
                        }
                    } catch { /* ignore configuration errors */ }

                    // Get Sanitize(string) method
                    _sanitizeMethod = sanitizerType.GetMethod("Sanitize", new[] { typeof(string) });
                }
            } catch {
                _sanitizerInstance = null;
                _sanitizeMethod = null;
            }
        }

        public static string Sanitize(string? html) {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            if (_sanitizerInstance != null && _sanitizeMethod != null) {
                try {
                    var result = _sanitizeMethod.Invoke(_sanitizerInstance, new object[] { html });
                    return result?.ToString() ?? string.Empty;
                } catch {
                    // fall through to fallback
                }
            }

            // Fallback: conservative regex-based sanitizer
            try {
                var s = html;
                s = System.Text.RegularExpressions.Regex.Replace(s, "<script\\b[^<]*(?:(?!</script>)<[^<]*)*</script>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                s = System.Text.RegularExpressions.Regex.Replace(s, "on[\\w]+\\s*=\\s*\"[^\"]*\"|on[\\w]+\\s*=\\s*'[^']*'|on[\\w]+\\s*=\\s*[^\\s>]+", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                s = System.Text.RegularExpressions.Regex.Replace(s, "(href|src)\\s*=\\s*(\\\"|')?\\s*javascript:[^\\\"' >]+(\\\"|')?", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return s;
            } catch {
                return string.Empty;
            }
        }
    }
}
