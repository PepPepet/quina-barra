// Quina Barra! · el temps del teu poble a la barra de tasques
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Textos de la interfície en diverses llengües.
//
// Cada llengua és un fitxer JSON pla dins de src\textos (ca.json, gl.json...), al format de jquery.i18n
// que fa servir translatewiki.net: {"@metadata": {...}, "clau": "text"}. El català (ca.json) és la base:
// si a una llengua li falta un text, surt en català. qqq.json és la documentació de cada text per als traductors.
// Dins dels textos: $1, $2... són valors que hi posa el programa, i {{PLURAL:$1|forma singular|forma plural}}
// tria la forma segons el número. Als XAML, @T:clau@ es canvia pel text de la llengua triada.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace TempsBarra
{
    static partial class G
    {
        const string PrefixTextos = "TempsBarra.textos.";
        public const string LlenguaBase = "ca";

        // Llengua en ús (codi del fitxer: "ca", "gl", "eu"...)
        public static string Llengua = LlenguaBase;
        static Dictionary<string, string> textos;
        public static readonly SortedSet<string> TextsQueFalten = new SortedSet<string>();   // per a la prova
        static Dictionary<string, string> textosBase;

        // Codis de les llengües que porta l'exe (sense qqq), amb el català primer
        public static List<string> LlenguesDisponibles()
        {
            List<string> r = new List<string>();
            foreach (string n in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (!n.StartsWith(PrefixTextos, StringComparison.Ordinal) || !n.EndsWith(".json", StringComparison.Ordinal)) continue;
                string codi = n.Substring(PrefixTextos.Length, n.Length - PrefixTextos.Length - 5);
                if (codi != "qqq") r.Add(codi);
            }
            r.Sort(StringComparer.Ordinal);
            if (r.Remove(LlenguaBase)) r.Insert(0, LlenguaBase);
            return r;
        }

        static Dictionary<string, string> LlegeixTextos(string codi)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(PrefixTextos + codi + ".json"))
                {
                    if (s == null) return d;
                    using (StreamReader r = new StreamReader(s, Encoding.UTF8, true))
                    {
                        IDictionary<string, object> o = J.Parse(r.ReadToEnd()) as IDictionary<string, object>;
                        if (o != null)
                            foreach (KeyValuePair<string, object> kv in o)
                            {
                                string v = kv.Value as string;
                                if (v != null && !kv.Key.StartsWith("@", StringComparison.Ordinal)) d[kv.Key] = v;
                            }
                    }
                }
            }
            catch { }
            return d;
        }

        // Tria la llengua: la desada a la configuració; si no n'hi ha, la del Windows; si no la tenim, el català
        public static void TriaLlengua(string desada)
        {
            List<string> disp = LlenguesDisponibles();
            string codi = null;
            if (!string.IsNullOrEmpty(desada) && disp.Contains(desada)) codi = desada;
            if (codi == null) codi = LlenguaDelWindows(disp);
            CarregaLlengua(codi ?? LlenguaBase);
        }

        public static void CarregaLlengua(string codi)
        {
            if (textosBase == null) textosBase = LlegeixTextos(LlenguaBase);
            Llengua = codi;
            textos = codi == LlenguaBase ? textosBase : LlegeixTextos(codi);
        }

        // ca-ES-valencia → ca-valencia; eu-ES → eu; gl-ES → gl. Null si no tenim la llengua del Windows.
        public static string LlenguaDelWindows(List<string> disp)
        {
            CultureInfo c = CultureInfo.CurrentUICulture;
            string nom = (c.Name ?? "").ToLowerInvariant();
            if (nom == "ca-es-valencia" && disp.Contains("ca-valencia")) return "ca-valencia";
            string dues = c.TwoLetterISOLanguageName;
            if (disp.Contains(dues)) return dues;
            string tres = c.ThreeLetterISOLanguageName;   // p. ex. ast
            if (disp.Contains(tres)) return tres;
            return null;
        }

        // Codi de llengua per al cercador de llocs (Open-Meteo/GeoNames): les variants fan servir la llengua mare
        public static string LlenguaCercador()
        {
            if (Llengua.StartsWith("ca", StringComparison.Ordinal)) return "ca";
            if (Llengua.StartsWith("oc", StringComparison.Ordinal)) return "oc";
            return Llengua;
        }

        // Català, valencià, alguerès i aranès: els llocs de Catalunya surten primer al cercador
        public static bool LlenguaCatalana()
        {
            return Llengua.StartsWith("ca", StringComparison.Ordinal) || Llengua == "oc-aranes";
        }

        // Catalunya al cercador: admin1_id 3336901 de GeoNames (el nom canvia segons la llengua)
        public static bool EsCatalunya(Dictionary<string, object> x)
        {
            return x != null && J.S(J.G(x, "admin1_id")) == "3336901";
        }

        // Nom de la llengua en la mateixa llengua (per al selector)
        public static string NomLlengua(string codi)
        {
            string n;
            if (LlegeixTextos(codi).TryGetValue("nom-llengua", out n) && !string.IsNullOrEmpty(n)) return n;
            return codi;
        }

        public static bool TeText(string clau)
        {
            if (textos == null) CarregaLlengua(Llengua);
            return textos.ContainsKey(clau) || textosBase.ContainsKey(clau);
        }

        // Text d'una clau en la llengua en ús, amb els valors $1, $2... i els plurals resolts
        public static string T(string clau, params object[] valors)
        {
            if (textos == null) CarregaLlengua(Llengua);
            string t;
            if (!textos.TryGetValue(clau, out t) || string.IsNullOrEmpty(t))
                if (!textosBase.TryGetValue(clau, out t)) { t = "⟨" + clau + "⟩"; TextsQueFalten.Add(clau); }
            return Omple(t, valors);
        }

        static readonly Regex rePlural = new Regex(@"\{\{PLURAL:\s*\$(\d+)\s*\|([^{}]*)\}\}", RegexOptions.IgnoreCase);
        static readonly Regex reValor = new Regex(@"\$(\d+)");

        static string Omple(string t, object[] valors)
        {
            if (t.IndexOf("{{", StringComparison.Ordinal) >= 0)
                t = rePlural.Replace(t, m =>
                {
                    int i = int.Parse(m.Groups[1].Value) - 1;
                    string[] formes = m.Groups[2].Value.Split('|');
                    double n = 0;
                    if (valors != null && i >= 0 && i < valors.Length && valors[i] != null)
                        double.TryParse(Convert.ToString(valors[i], CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out n);
                    int f = FormaPlural(n);
                    return formes[Math.Min(f, formes.Length - 1)];
                });
            if (valors != null && valors.Length > 0 && t.IndexOf('$') >= 0)
                t = reValor.Replace(t, m =>
                {
                    int i = int.Parse(m.Groups[1].Value) - 1;
                    return i >= 0 && i < valors.Length ? Convert.ToString(valors[i], CultureInfo.CurrentCulture) : m.Value;
                });
            return t;
        }

        // Índex de la forma de plural segons la llengua (regles de CLDR).
        // Les llengües d'aquí (i l'anglès, l'alemany, l'italià...) tenen dues formes: 1 i la resta.
        static int FormaPlural(double n)
        {
            bool enter = n == Math.Floor(n);
            switch (Llengua)
            {
                case "fr": case "pt": return (n >= 0 && n < 2) ? 0 : 1;   // 0 i 1 en singular
                default: return (enter && n == 1) ? 0 : 1;
            }
        }

        // Canvia els @T:clau@ d'un XAML pel text (escapat per a XML)
        static readonly Regex reXamlT = new Regex(@"@T:([A-Za-z0-9.\-]+)@");
        public static string TradueixXaml(string xaml)
        {
            if (xaml.IndexOf("@T:", StringComparison.Ordinal) < 0) return xaml;
            return reXamlT.Replace(xaml, m => SecurityElement.Escape(T(m.Groups[1].Value)));
        }
    }
}
