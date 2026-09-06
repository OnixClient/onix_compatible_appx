using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

AppxListJsonFromText appxListJsonFromText = new AppxListJsonFromText();
appxListJsonFromText.main(); // return value intentionally discarded: original process always exited 0

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
    }

    public string version { get; set; }
    public string package_version { get; set; }
    public string url { get; set; }
    public Version version_number { get; set; }
    public Version package_version_number { get; set; }
}

internal class ProfileData
{
    public string name { get; set; }
    public string file_name { get; set; }
    public string url { get; set; }
}

internal class AppxListJsonFromText
{
    public static string BASEPPATH = "./../../";

    public List<VersionEntry> GetVersionEntries(string path)
    {
        string[] array = File.ReadAllLines(path);
        List<VersionEntry> list = new List<VersionEntry>();
        for (int i = 0; i + 2 < array.Length; i += 3)
        {
            VersionEntry versionEntry = new VersionEntry();
            versionEntry.version = array[i];
            versionEntry.package_version = array[i + 1];
            versionEntry.url = array[i + 2];
            versionEntry.version_number = new VersionEntry.Version(versionEntry.version);
            versionEntry.package_version_number = new VersionEntry.Version(versionEntry.package_version);
            list.Add(versionEntry);
        }
        return list;
    }

    public void HandleTxt(string name)
    {
        try
        {
            string path = BASEPPATH + name + ".txt";
            List<VersionEntry> versionEntries = GetVersionEntries(path);
            string contents = JsonSerializer.Serialize(versionEntries);
            File.WriteAllText(Path.ChangeExtension(path, "json"), contents);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not get json for profile " + name);
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    public List<ProfileData> GetProfiles()
    {
        List<ProfileData> list = new List<ProfileData>();
        string[] array = File.ReadAllLines(BASEPPATH + "profiles.txt");
        for (int i = 0; i + 2 < array.Length; i += 3)
        {
            ProfileData profileData = new ProfileData();
            profileData.name = array[i];
            profileData.file_name = array[i + 1];
            profileData.url = array[i + 2].Replace(profileData.file_name + ".txt", profileData.file_name + ".json");
            list.Add(profileData);
        }
        return list;
    }

    public int main()
    {
        try
        {
            List<ProfileData> profiles = GetProfiles();
            try
            {
                File.WriteAllText(BASEPPATH + "profiles.json", JsonSerializer.Serialize(profiles));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not output profile list json!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
            foreach (ProfileData item in profiles)
            {
                HandleTxt(item.file_name);
            }
        }
        catch (Exception ex2)
        {
            Console.WriteLine("Could not get profile list!");
            Console.WriteLine(ex2.Message);
            Console.WriteLine(ex2.StackTrace);
            return 1;
        }
        Console.WriteLine("Done.");
        return 0;
    }
}
