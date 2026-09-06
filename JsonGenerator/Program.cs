using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

AppxListJsonFromText appxListJsonFromText = new AppxListJsonFromText();
appxListJsonFromText.main();

internal class VersionEntry
{
    public class Version
    {
        public int major { get; set; }

        public int minor { get; set; }

        public int patch { get; set; }

        public int build { get; set; }

        public Version(string str)
        {
            string[] array = str.Split(".");
            if (array.Length == 3 || array.Length == 4)
            {
                major = int.Parse(array[0]);
                minor = int.Parse(array[1]);
                patch = int.Parse(array[2]);
                if (array.Length == 4)
                {
                    build = int.Parse(array[3]);
                }
                else
                {
                    build = 0;
                }
            }
        }

        public bool AtLeast(Version other)
        {
            if (major != other.major) return major > other.major;
            if (minor != other.minor) return minor > other.minor;
            if (patch != other.patch) return patch > other.patch;
            return build >= other.build;
        }
    }

    public string version { get; set; }

    public string package_version { get; set; }

    public string url { get; set; }

    public Version version_number { get; set; }

    public Version package_version_number { get; set; }
}

internal class DownloadUrl
{
    public string url { get; set; }
    public string type { get; set; }
}

internal class Download
{
    public string type { get; set; }
    public object url { get; set; }
}

internal class VersionEntryV2
{
    public string version { get; set; }
    public string package_version { get; set; }
    public bool sdk { get; set; }
    public string install_type { get; set; }
    public Download url { get; set; }
    public Download mirror_url { get; set; }
    public Download xdelta_url { get; set; }
}

internal class ProfileData
{
    public string name { get; set; }

    public string file_name { get; set; }

    public string url { get; set; }
}

internal class VersionData
{
    public string version { get; set; }
    public string package_version { get; set; }
    public bool sdk { get; set; }
    public string url { get; set; }
    public string url_type { get; set; }
    public List<string> mirror_urls { get; set; }
    public List<DownloadUrl> mirror_typed { get; set; }
    public string mirror_type { get; set; }
    public string xdelta_url { get; set; }
    public string xdelta_url_type { get; set; }
    public string install_type { get; set; }
    public string download_type { get; set; }
}

internal class YamlModel
{
    public string raw_base { get; set; }
    public List<VersionData> versions { get; set; } = new List<VersionData>();
}

internal class YamlException : Exception
{
    public YamlException(string message) : base(message) { }
}

internal static class VersionsYaml
{
    private static readonly string[] VersionKeys = { "version", "package_version", "sdk", "url", "url_type",
        "mirror_urls", "mirror_type", "xdelta_url", "xdelta_url_type", "install_type" };

    private static string InferUrlType(string url)
    {
        if (url == null) return null;
        string file = url.Substring(url.LastIndexOf('/') + 1);
        return Regex.IsMatch(file, @"\.zip(\.\d+)?$", RegexOptions.IgnoreCase) ? "zip" : "direct";
    }

    public static YamlModel Load(string path)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length > 0 && lines[0].StartsWith('﻿'))
            lines[0] = lines[0].Substring(1);

        YamlModel model = new YamlModel();
        string section = null;
        VersionData curVersion = null;
        bool inMirrors = false;
        bool inMirrorMap = false;
        DownloadUrl curMirror = null;

        for (int i = 0; i < lines.Length; i++)
        {
            int n = i + 1;
            string raw = lines[i];
            if (raw.Contains("\t"))
                throw new YamlException($"line {n}: tabs are not allowed, use spaces for indentation");
            if (raw.TrimEnd().Length == 0)
                continue;
            string line = raw.Trim();
            if (line.StartsWith("#"))
                continue;
            int hash = FindCommentStart(line);
            if (hash >= 0)
                line = line.Substring(0, hash).Trim().TrimEnd();
            if (line.Length == 0)
                continue;

            int indent = raw.Length - raw.TrimStart().Length;

            if (indent == 0)
            {
                Match m = Regex.Match(line, @"^([A-Za-z_][A-Za-z0-9_]*):(.*)$");
                if (!m.Success)
                    throw new YamlException($"line {n}: expected 'key:' at top level, got: {line}");
                string key = m.Groups[1].Value;
                string val = m.Groups[2].Value.Trim();
                if (key != "raw_base" && key != "versions")
                    throw new YamlException($"line {n}: unknown top-level key '{key}'");
                if (key == "raw_base")
                {
                    if (val.Length == 0)
                        throw new YamlException($"line {n}: raw_base needs a value");
                    model.raw_base = Unquote(val);
                    continue;
                }
                if (val.Length > 0)
                    throw new YamlException($"line {n}: top-level key '{key}' must be a section header");
                section = key;
                curVersion = null; inMirrors = false; inMirrorMap = false;
                continue;
            }

            if (section == null)
                throw new YamlException($"line {n}: content before any top-level section: {line}");

            if (inMirrors && indent >= 6)
            {
                if (section != "versions" || curVersion == null)
                    throw new YamlException($"line {n}: mirror list entry outside a version entry");
                if (indent >= 8)
                {
                    if (!inMirrorMap || curMirror == null)
                        throw new YamlException($"line {n}: unexpected indentation in mirror_urls");
                    Match mk = Regex.Match(line, @"^([A-Za-z_][A-Za-z0-9_]*): (.*)$");
                    if (!mk.Success)
                        throw new YamlException($"line {n}: expected 'key: value' in mirror item, got: {line}");
                    string k = mk.Groups[1].Value;
                    string v = Unquote(mk.Groups[2].Value.Trim());
                    if (k == "url") curMirror.url = v;
                    else if (k == "type") curMirror.type = v;
                    else throw new YamlException($"line {n}: unknown mirror key '{k}'");
                    if (k == "url")
                    {
                        int idx = curVersion.mirror_typed.IndexOf(curMirror);
                        if (idx >= 0) curVersion.mirror_urls[idx] = v;
                    }
                    continue;
                }
                if (line.StartsWith("- "))
                {
                    string rest = line.Substring(2).Trim();
                    Match mk = Regex.Match(rest, @"^([A-Za-z_][A-Za-z0-9_]*): (.*)$");
                    if (mk.Success)
                    {
                        curMirror = new DownloadUrl();
                        inMirrorMap = true;
                        string k = mk.Groups[1].Value;
                        string v = Unquote(mk.Groups[2].Value.Trim());
                        if (k == "url") curMirror.url = v;
                        else if (k == "type") curMirror.type = v;
                        else throw new YamlException($"line {n}: unknown mirror key '{k}'");
                        curVersion.mirror_urls.Add(curMirror.url);
                        curVersion.mirror_typed.Add(curMirror);
                        continue;
                    }
                    curMirror = null;
                    inMirrorMap = false;
                    curVersion.mirror_urls.Add(Unquote(rest));
                    curVersion.mirror_typed.Add(null);
                    continue;
                }
                throw new YamlException($"line {n}: expected '- <url>' in mirror_urls, got: {line}");
            }
            else if (inMirrors && indent < 6)
            {
                inMirrors = false; inMirrorMap = false;
            }

            if (indent == 2 && line.StartsWith("- "))
            {
                curVersion = new VersionData();
                curVersion.mirror_urls = new List<string>();
                curVersion.mirror_typed = new List<DownloadUrl>();
                model.versions.Add(curVersion);
                inMirrors = false; inMirrorMap = false;
                line = line.Substring(2).Trim();
            }
            else if (indent == 2 && !line.StartsWith("- "))
            {
                throw new YamlException($"line {n}: expected a '- ' list item at this indentation, got: {line}");
            }
            else if (indent != 4 && indent != 2)
            {
                throw new YamlException($"line {n}: unexpected indentation (expected 2 or 4 spaces), got: {line}");
            }

            Match kv = Regex.Match(line, @"^([A-Za-z_][A-Za-z0-9_]*):(.*)$");
            if (!kv.Success)
                throw new YamlException($"line {n}: expected 'key: value', got: {line}");
            string key2 = kv.Groups[1].Value;
            string value = Unquote(kv.Groups[2].Value.Trim());

            if (curVersion == null)
                throw new YamlException($"line {n}: version key before '- ' item start");
            if (!Contains(VersionKeys, key2))
                throw new YamlException($"line {n}: unknown version key '{key2}'");
            if (key2 == "version") curVersion.version = value;
            else if (key2 == "package_version") curVersion.package_version = value;
            else if (key2 == "sdk")
            {
                if (value != "true" && value != "false")
                    throw new YamlException($"line {n}: sdk must be true or false");
                curVersion.sdk = value == "true";
            }
            else if (key2 == "url") curVersion.url = value;
            else if (key2 == "url_type") curVersion.url_type = value;
            else if (key2 == "mirror_urls")
            {
                if (value.Length != 0)
                    throw new YamlException($"line {n}: mirror_urls must be a list (no value on the key line)");
                inMirrors = true;
            }
            else if (key2 == "mirror_type") curVersion.mirror_type = value;
            else if (key2 == "xdelta_url") curVersion.xdelta_url = value;
            else if (key2 == "xdelta_url_type") curVersion.xdelta_url_type = value;
            else if (key2 == "install_type") curVersion.install_type = value;
            else if (key2 == "download_type") curVersion.download_type = value;
        }

        Validate(model);
        return model;
    }

    private static void Validate(YamlModel model)
    {
        if (model.raw_base == null || model.raw_base.Length == 0)
            throw new YamlException("missing required top-level key 'raw_base'");
        var seenVersions = new HashSet<string>();
        foreach (VersionData v in model.versions)
        {
            if (string.IsNullOrEmpty(v.version) || string.IsNullOrEmpty(v.package_version) || string.IsNullOrEmpty(v.url))
                throw new YamlException($"version entry missing version, package_version or url (near '{v.version}')");
            if (!seenVersions.Add(v.version))
                throw new YamlException($"duplicate version '{v.version}'");
            if (v.install_type != null && v.install_type != "uwp" && v.install_type != "msixvc")
                throw new YamlException($"version {v.version}: invalid install_type '{v.install_type}'");
            if (v.download_type != null && v.download_type != "direct" && v.download_type != "zip")
                throw new YamlException($"version {v.version}: invalid download_type '{v.download_type}'");
            if (v.url_type != null && v.url_type != "direct" && v.url_type != "zip")
                throw new YamlException($"version {v.version}: invalid url_type '{v.url_type}'");
            if (v.mirror_type != null && v.mirror_type != "direct" && v.mirror_type != "zip")
                throw new YamlException($"version {v.version}: invalid mirror_type '{v.mirror_type}'");
        }
    }

    private static bool Contains(string[] keys, string key)
    {
        foreach (string k in keys)
            if (k == key) return true;
        return false;
    }

    private static int FindCommentStart(string line)
    {
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (c == '#' && !inSingle && !inDouble && (i == 0 || line[i - 1] == ' '))
                return i;
        }
        return -1;
    }

    private static string Unquote(string s)
    {
        if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
            return s.Substring(1, s.Length - 2);
        if (s.Length >= 2 && s.StartsWith("'") && s.EndsWith("'"))
            return s.Substring(1, s.Length - 2);
        return s;
    }
}

internal class AppxListJsonFromText
{
    public static string BASEPPATH = "./../../";

    private static readonly VersionEntry.Version MsixvcThreshold = new VersionEntry.Version("1.21.120.0");

    public int main()
    {
        YamlModel model = null;
        try
        {
            model = VersionsYaml.Load(BASEPPATH + "versions.yaml");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not parse versions.yaml!");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            return 0;
        }

        if (model.versions.Count == 0)
        {
            Console.WriteLine("versions.yaml contained no versions; no files written.");
            return 0;
        }

        try
        {
            List<string> profileLines = new List<string>
            {
                "All Versions", "versions", model.raw_base + "/versions.txt",
                "SDK Versions", "sdkversions", model.raw_base + "/sdkversions.txt",
                "All Versions (v2)", "versionsv2", model.raw_base + "/versionsv2.json",
            };
            List<ProfileData> profilesJson = new List<ProfileData>
            {
                new ProfileData { name = "All Versions", file_name = "versions", url = model.raw_base + "/versions.json" },
                new ProfileData { name = "SDK Versions", file_name = "sdkversions", url = model.raw_base + "/sdkversions.json" },
                new ProfileData { name = "All Versions (v2)", file_name = "versionsv2", url = model.raw_base + "/versionsv2.json" },
            };
            File.WriteAllText(BASEPPATH + "profiles.txt", string.Join("\r\n", profileLines), new UTF8Encoding(false));
            File.WriteAllText(BASEPPATH + "profiles.json", JsonSerializer.Serialize(profilesJson));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not output profile list!");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            return 0;
        }

        try
        {
            List<VersionEntry> entries = new List<VersionEntry>();
            foreach (VersionData v in model.versions)
            {
                entries.Add(new VersionEntry
                {
                    version = v.version,
                    package_version = v.package_version,
                    url = v.url,
                    version_number = new VersionEntry.Version(v.version),
                    package_version_number = new VersionEntry.Version(v.package_version),
                });
            }
            StringBuilder txt = new StringBuilder();
            foreach (VersionEntry e in entries)
            {
                txt.Append(e.version); txt.Append("\r\n");
                txt.Append(e.package_version); txt.Append("\r\n");
                txt.Append(e.url); txt.Append("\r\n");
            }
            File.WriteAllText(BASEPPATH + "versions.txt", txt.ToString(), new UTF8Encoding(false));
            File.WriteAllText(BASEPPATH + "versions.json", JsonSerializer.Serialize(entries));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not get json for profile versions");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        try
        {
            List<VersionEntry> entries = new List<VersionEntry>();
            foreach (VersionData v in model.versions)
            {
                if (!v.sdk) continue;
                entries.Add(new VersionEntry
                {
                    version = v.version,
                    package_version = v.package_version,
                    url = v.url,
                    version_number = new VersionEntry.Version(v.version),
                    package_version_number = new VersionEntry.Version(v.package_version),
                });
            }
            StringBuilder txt = new StringBuilder();
            foreach (VersionEntry e in entries)
            {
                txt.Append(e.version); txt.Append("\r\n");
                txt.Append(e.package_version); txt.Append("\r\n");
                txt.Append(e.url); txt.Append("\r\n");
            }
            File.WriteAllText(BASEPPATH + "sdkversions.txt", txt.ToString(), new UTF8Encoding(false));
            File.WriteAllText(BASEPPATH + "sdkversions.json", JsonSerializer.Serialize(entries));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not get json for profile sdkversions");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        try
        {
            List<VersionEntryV2> entries = new List<VersionEntryV2>();
            foreach (VersionData v in model.versions)
            {
                entries.Add(ToV2(v));
            }
            File.WriteAllText(BASEPPATH + "versionsv2.json", JsonSerializer.Serialize(entries));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not get json for profile versionsv2");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("Done.");
        return 0;
    }

    private static VersionEntryV2 ToV2(VersionData v)
    {
        VersionEntry.Version num = new VersionEntry.Version(v.version);

        string installType = v.install_type ?? (num.AtLeast(MsixvcThreshold) ? "msixvc" : "uwp");

        Download url = new Download
        {
            type = v.url_type ?? InferUrlType(v.url),
            url = v.url,
        };

        Download mirror = null;
        if (v.mirror_urls != null && v.mirror_urls.Count > 0)
        {
            string type = v.mirror_type;
            if (type == null)
            {
                type = "zip";
                foreach (DownloadUrl typed in v.mirror_typed)
                {
                    if (typed != null && typed.type != null) { type = typed.type; break; }
                }
            }
            mirror = new Download { type = type, url = v.mirror_urls.Count == 1 ? (object)v.mirror_urls[0] : v.mirror_urls };
        }

        Download xdelta = null;
        if (v.xdelta_url != null)
        {
            xdelta = new Download { type = v.xdelta_url_type ?? InferUrlType(v.xdelta_url), url = v.xdelta_url };
        }

        return new VersionEntryV2
        {
            version = v.version,
            package_version = v.package_version,
            sdk = v.sdk,
            install_type = installType,
            url = url,
            mirror_url = mirror,
            xdelta_url = xdelta,
        };
    }

    private static string InferUrlType(string url)
    {
        if (url == null) return null;
        string file = url.Substring(url.LastIndexOf('/') + 1);
        return Regex.IsMatch(file, @"\.zip(\.\d+)?$", RegexOptions.IgnoreCase) ? "zip" : "direct";
    }
}
