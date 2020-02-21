﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VKAppearanceToXAML {
    public enum LogType { Info, Warning, Error }

    public class Parser {
        private static string GetHeader() {
            DateTime dt = DateTime.Now;
            int y = dt.Year;
            string a = dt.Month > 9 ? $"{dt.Month}" : $"0{dt.Month}";
            string d = dt.Day > 9 ? $"{dt.Day}" : $"0{dt.Day}";
            return $"<!-- This resource dictionary file generated by VKAppearanceToXAML tool by Elorucov on {y}/{a}/{d}. -->";
        }

        private static int GetVersionBasedOnDate() {
            DateTime dt = DateTime.Now.ToUniversalTime();
            int y = dt.Year - 2000;
            string a = dt.Month > 9 ? $"{dt.Month}" : $"0{dt.Month}";
            string d = dt.Day > 9 ? $"{dt.Day}" : $"0{dt.Day}";
            string h = dt.Hour > 9 ? $"{dt.Hour}" : $"0{dt.Hour}";
            return Int32.Parse($"{y}{a}{d}{h}");
        }

        private static string JSONKeyToXAMLStyle(string key, string suffix = "") {
            string[] ss = key.Split('_');
            string res = "VK";
            foreach(string s in ss) {
                res += $"{s[0].ToString().ToUpper()}{s.Substring(1)}";
            }
            return res + suffix;
        }

        private static readonly string RepositoryUri = "https://raw.githubusercontent.com/VKCOM/Appearance/master/main.valette/";
        private static readonly string PaletteUri = RepositoryUri + "palette.json";
        private static readonly string PaletteMessagesUri = RepositoryUri + "palette_messages.json";
        private static readonly string SchemeUri = RepositoryUri + "scheme.json";
        private static readonly string SchemeMessagesUri = RepositoryUri + "scheme_messages.json";

        public static async Task<Dictionary<string, string>> DoItAsync(IProgress<Tuple<LogType, string>> progress) {
            Dictionary<string, string> XamlFiles = new Dictionary<string, string>();

            try {
                if (progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Getting palette..."));
                string palettejson = await GetFileFromWebAsync(PaletteUri);
                if(progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Getting messages palette..."));
                string mpalettejson = await GetFileFromWebAsync(PaletteMessagesUri);
                if (progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Getting schemes..."));
                string schemejson = await GetFileFromWebAsync(SchemeUri);
                if(progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Getting messages schemes..."));
                string mschemejson = await GetFileFromWebAsync(SchemeMessagesUri);

                // Palette
                if (progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Parsing palette..."));
                JObject pj = JObject.Parse(palettejson);
                await Task.Delay(5);
                string colorxaml = ParsePalette(progress, pj);
                XamlFiles.Add("VKPalette.xaml", colorxaml);

                // Palette for messages
                if(progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Parsing messages palette..."));
                JObject mpj = JObject.Parse(mpalettejson);
                await Task.Delay(5);
                string mcolorxaml = ParsePalette(progress, mpj);
                XamlFiles.Add("VKPaletteMessages.xaml", mcolorxaml);

                if (progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"XAML dictionary for palettess successfully generated."));
                await Task.Delay(5);

                // Schemes
                if (progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Parsing schemes..."));
                JObject sj = JObject.Parse(schemejson);
                JObject msj = JObject.Parse(mschemejson);
                await Task.Delay(5);
                XamlFiles.Add("VKScheme.xaml", ParseSchemes(false, progress, sj, "space_gray", "bright_light", "Milkshake"));
                await Task.Delay(5);
                XamlFiles.Add("VKSchemeMessages.xaml", ParseSchemes(true, progress, msj, "space_gray", "bright_light", "Milkshake"));
                return XamlFiles;
            } catch(Exception ex) {
                if (progress != null) progress.Report(new Tuple<LogType, string>(LogType.Error, $"Exception 0x{ex.HResult.ToString("x8")}: {ex.Message}"));
                return null;
            }
        }

        private static string ParsePalette(IProgress<Tuple<LogType, string>> progress, JObject palette) {
            if(progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Total colors: {palette.Values().Count()}. Generate XAML..."));
            string colorxaml = $"{GetHeader()}\n";
            colorxaml += $"<ResourceDictionary\n";
            colorxaml += $"    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n";
            colorxaml += $"    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n";
            colorxaml += $"    \n";
            colorxaml += $"    <x:String x:Key=\"VKUIPaletteVersion\">{GetVersionBasedOnDate()}</x:String>\n";
            colorxaml += $"    \n";

            foreach(var pkv in palette) {
                colorxaml += $"    <Color x:Key=\"{JSONKeyToXAMLStyle(pkv.Key, "Color")}\">{pkv.Value}</Color>\n";
            }

            colorxaml += $"</ResourceDictionary>\n";
            return colorxaml;
        }

        private static string ParseSchemes(bool isForMessage, IProgress<Tuple<LogType, string>> progress, JObject schemes, string darkname, string lightname, string finalname) {
            JObject ds = null;
            JObject ls = null;

            if (progress != null) progress.Report(new Tuple<LogType, string>(LogType.Info, $"Parsing scheme \"{darkname}\" and \"{lightname}\" for \"{finalname}\" (isForMessage: {isForMessage})..."));
            foreach (var skv in schemes) {
                if (skv.Key == darkname && skv.Value.Value<string>("appearance") == "dark") ds = skv.Value.Value<JObject>("colors");
                if (skv.Key == lightname && skv.Value.Value<string>("appearance") == "light") ls = skv.Value.Value<JObject>("colors");
            }

            string xaml = $"{GetHeader()}\n";
            if(!isForMessage) {
                xaml += $"<!-- Based on scheme.json -->\n";
            } else {
                xaml += $"<!-- Based on scheme_messages.json -->\n";
            }
            xaml += $"<ResourceDictionary\n";
            xaml += $"    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n";
            xaml += $"    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n";
            xaml += $"    \n";
            if (!isForMessage) {
                xaml += $"    <x:String x:Key=\"VKUISchemeName\">{finalname}</x:String>\n";
                xaml += $"    <x:String x:Key=\"VKUISchemeVersion\">{GetVersionBasedOnDate()}</x:String>\n";
            } else {
                xaml += $"    <x:String x:Key=\"VKUIMessagesSchemeName\">{finalname}</x:String>\n";
                xaml += $"    <x:String x:Key=\"VKUIMessagesSchemeVersion\">{GetVersionBasedOnDate()}</x:String>\n";
            }
            xaml += $"    \n";
            xaml += $"    <ResourceDictionary.ThemeDictionaries>\n";
            xaml += $"        <!-- Original scheme name: \"{darkname}\" -->\n";
            xaml += $"        <ResourceDictionary x:Key=\"Default\">\n";
            xaml += ParseScheme(ds);
            xaml += $"        </ResourceDictionary>\n";
            xaml += $"        \n";
            xaml += $"        <!-- Original scheme name: \"{lightname}\" -->\n";
            xaml += $"        <ResourceDictionary x:Key=\"Light\">\n";
            xaml += ParseScheme(ls);
            xaml += $"        </ResourceDictionary>\n";
            xaml += $"    </ResourceDictionary.ThemeDictionaries>\n";
            xaml += $"</ResourceDictionary>\n";
            return xaml;
        }

        private static string ParseScheme(JObject scheme) {
            string xaml = "";

            foreach(var sk in scheme) {
                xaml += $"            <SolidColorBrush x:Key=\"{JSONKeyToXAMLStyle(sk.Key, "Brush")}\" Color=\"{{StaticResource {JSONKeyToXAMLStyle(sk.Value.Value<string>("color_identifier"), "Color")}}}\" />\n";
            }

            return xaml;
        }

        private static async Task<string> GetFileFromWebAsync(string url) {
            using(HttpClient hc = new HttpClient()) {
                HttpResponseMessage resp = await hc.GetAsync(url);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync();
            }
        }
    }
}
