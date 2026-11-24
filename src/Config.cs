// Quina Barra! · versió 1.0 · el temps del teu poble a la barra de tasques, en català
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Configuració, registre, municipis de l'Idescat i tema de Windows.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace TempsBarra
{
    public class Municipi { public string Id; public string Nom; }

    // { tema, llengua, llocs: [ {nom, latitud, longitud, codiMeteocat}, ... ] }. El primer lloc és el principal.
    public class Config
    {
        public Dictionary<string, object> Arrel;
        public List<Dictionary<string, object>> Llocs;
        public string Tema
        {
            get { return J.S(J.G(Arrel, "tema")); }
            set { Arrel["tema"] = value; }
        }
        // Codi de la llengua triada a mà ("gl", "eu"...); null = la del Windows
        public string Llengua
        {
            get { return J.S(J.G(Arrel, "llengua")); }
            set { if (value == null) Arrel.Remove("llengua"); else Arrel["llengua"] = value; }
        }
    }

    static partial class G
    {
        public static readonly string Carpeta = Path.Combine(
            Environment.GetEnvironmentVariable("APPDATA") ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuinaBarra");
        public static readonly string FitxerConf = Path.Combine(Carpeta, "config.json");
        public static readonly string FitxerMuns = Path.Combine(Carpeta, "municipis-idescat.json");
        public const string Versio = "1.0";
        public const string UrlAutor = "https://peppayo.com";
        public const string UrlCodi = "https://github.com/PepPepet";
        public const int Minuts = 15;
        public static readonly string[] Models = { "meteofrance_seamless", "icon_seamless", "ecmwf_ifs025" };
        public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        public static readonly CultureInfo Ca = CultureInfo.GetCultureInfo("ca-ES");
        static readonly UTF8Encoding Utf8SenseBom = new UTF8Encoding(false);

        // Mode de prova (/prova): no escriu la configuració ni el registre
        public static bool Prova;

        public static Config Conf;
        public static int Vist;

        // ---------- Utilitats ----------
        public static string TreuAccents(string s)
        {
            string n = (s ?? "").Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (char ch in n)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // "Hospitalet de Llobregat, l'" i "l'Hospitalet de Llobregat" donen la mateixa clau
        public static string Clau(string nom)
        {
            string s = TreuAccents(nom).ToLowerInvariant().Trim();
            Match m = Regex.Match(s, "^(.*),\\s*(l'|el|la|els|les)$", RegexOptions.IgnoreCase);
            if (m.Success) s = m.Groups[1].Value;
            s = Regex.Replace(s, "^(l'|el |la |els |les )", "", RegexOptions.IgnoreCase);
            return Regex.Replace(s, "[^a-z0-9]", "", RegexOptions.IgnoreCase);
        }

        // Registre petit d'arrencades i errors (es manté per sota de 100 línies)
        public static void Registre(string text)
        {
            if (Prova) return;
            try
            {
                string f = Path.Combine(Carpeta, "registre.log");
                List<string> linies = new List<string>();
                if (File.Exists(f))
                {
                    string[] totes = File.ReadAllLines(f, Encoding.UTF8);
                    linies.AddRange(totes.Skip(Math.Max(0, totes.Length - 99)));
                }
                linies.Add(DateTime.Now.ToString("dd/MM/yyyy · HH:mm:ss", Inv) + "  " + text);
                File.WriteAllLines(f, linies.ToArray(), Utf8SenseBom);
            }
            catch { }
        }

        // ---------- Configuració ----------
        // Escriptura segura: primer a un fitxer temporal, després es canvia; l'anterior queda com a config.bak
        public static void DesaConfig(Config c)
        {
            if (Prova) return;
            c.Arrel["llocs"] = c.Llocs;
            string json = J.Escriu(c.Arrel);
            string tmp = FitxerConf + ".tmp";
            string bak = Path.Combine(Carpeta, "config.bak");
            File.WriteAllText(tmp, json, Utf8SenseBom);
            if (File.Exists(FitxerConf)) File.Replace(tmp, FitxerConf, bak); else File.Move(tmp, FitxerConf);
        }

        // Torna a provar uns quants cops: en arrencar el Windows el fitxer pot estar ocupat un moment
        static object LlegeixConfigFitxer(string fitxer)
        {
            for (int i = 1; i <= 8; i++)
            {
                try
                {
                    string t = File.ReadAllText(fitxer, Encoding.UTF8);
                    if (t.Trim().Length == 0) return null;
                    return J.Parse(t);
                }
                catch (Exception ex)
                {
                    Registre("No s'ha pogut llegir " + Path.GetFileName(fitxer) + " (intent " + i + "): " + ex.Message);
                    Thread.Sleep(750);
                }
            }
            return null;
        }

        public static Config LlegeixConfig()
        {
            string bak = Path.Combine(Carpeta, "config.bak");
            string font = FitxerConf;
            if (!File.Exists(FitxerConf))
            {
                // Si no hi és però queda la còpia de seguretat, es recupera
                if (File.Exists(bak))
                {
                    Registre("Falta config.json: es recupera de config.bak");
                    if (Prova) font = bak; else File.Copy(bak, FitxerConf);
                }
                else return null;
            }
            Dictionary<string, object> c = LlegeixConfigFitxer(font) as Dictionary<string, object>;
            if (c == null || !(J.Truthy(J.G(c, "llocs")) || J.Truthy(J.G(c, "nom"))))
            {
                if (File.Exists(bak))
                {
                    Registre("config.json il·legible o buit: es prova config.bak");
                    c = LlegeixConfigFitxer(bak) as Dictionary<string, object>;
                }
            }
            if (c == null) return null;
            if (!J.Truthy(J.G(c, "llocs")) && J.Truthy(J.G(c, "nom")))
            {
                // Configuració antiga (un sol lloc): es passa al format nou
                Dictionary<string, object> lloc = new Dictionary<string, object>();
                lloc["nom"] = J.G(c, "nom");
                lloc["latitud"] = J.G(c, "latitud");
                lloc["longitud"] = J.G(c, "longitud");
                lloc["codiMeteocat"] = J.G(c, "codiMeteocat");
                Dictionary<string, object> nou = new Dictionary<string, object>();
                nou["tema"] = J.G(c, "tema");
                nou["llocs"] = new object[] { lloc };
                c = nou;
                Config migrat = AConfig(c);
                DesaConfig(migrat);
            }
            if (!J.Truthy(J.G(c, "llocs"))) return null;
            Config r = AConfig(c);
            if (r.Llocs.Count == 0) return null;
            if (!c.ContainsKey("tema")) c["tema"] = null;
            return r;
        }

        static Config AConfig(Dictionary<string, object> c)
        {
            Config r = new Config();
            r.Arrel = c;
            r.Llocs = new List<Dictionary<string, object>>();
            foreach (object x in J.A(J.G(c, "llocs")))
            {
                Dictionary<string, object> d = x as Dictionary<string, object>;
                if (d != null) r.Llocs.Add(d);
            }
            return r;
        }

        public static Config NovaConfig(Dictionary<string, object> primer)
        {
            Config c = new Config();
            c.Arrel = new Dictionary<string, object>();
            c.Arrel["tema"] = null;
            c.Llocs = new List<Dictionary<string, object>>();
            c.Llocs.Add(primer);
            c.Arrel["llocs"] = c.Llocs;
            return c;
        }

        // Accés als camps d'un lloc
        public static string Nom(Dictionary<string, object> l) { return J.S(J.G(l, "nom")) ?? ""; }
        public static double Lat(Dictionary<string, object> l) { return J.Dz(J.G(l, "latitud")); }
        public static double Lon(Dictionary<string, object> l) { return J.Dz(J.G(l, "longitud")); }
        public static string Codi(Dictionary<string, object> l)
        {
            string s = J.S(J.G(l, "codiMeteocat"));
            return string.IsNullOrEmpty(s) ? null : s;
        }

        public static Dictionary<string, object> LlocVist()
        {
            int n = Conf.Llocs.Count;
            if (Vist >= n) Vist = 0;
            return Conf.Llocs[Vist];
        }

        public static string ClauLloc(Dictionary<string, object> l)
        {
            return Lat(l).ToString("0.####", Inv) + ";" + Lon(l).ToString("0.####", Inv);
        }

        // ---------- Municipis oficials de Catalunya (Idescat) ----------
        // Nom oficial i codi de 6 xifres que fa servir meteo.cat. Serveix per corregir noms mal escrits del cercador
        // (p. ex. GeoNames diu «Copóns» i el posa dins d'Argençola).
        static Dictionary<string, Municipi> munsIdx;

        public static Dictionary<string, Municipi> IndexMunicipis()
        {
            if (munsIdx != null) return munsIdx;
            try
            {
                if (!File.Exists(FitxerMuns) || (DateTime.Now - File.GetLastWriteTime(FitxerMuns)).TotalDays > 90)
                {
                    string txt;
                    using (WebClient wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        txt = wc.DownloadString("https://api.idescat.cat/emex/v1/nodes.json?tipus=mun&lang=ca");
                    }
                    File.WriteAllText(FitxerMuns, txt, Utf8SenseBom);
                }
                Dictionary<string, Municipi> idx = new Dictionary<string, Municipi>();
                object arrel = J.Parse(File.ReadAllText(FitxerMuns, Encoding.UTF8));
                List<object> vs = new List<object>();
                foreach (object f in J.A(J.G(arrel, "fitxes")))
                    foreach (object v in J.A(J.G(f, "v"))) vs.Add(v);
                foreach (object m in vs)
                {
                    // «Hospitalet de Llobregat, l'» es mostra com «l'Hospitalet de Llobregat»
                    string content = J.S(J.G(m, "content")) ?? "";
                    string nom = content;
                    Match a = Regex.Match(nom, "^(.*),\\s*(l')$", RegexOptions.IgnoreCase);
                    if (a.Success) nom = "l'" + a.Groups[1].Value;
                    else
                    {
                        Match b = Regex.Match(nom, "^(.*),\\s*(el|la|els|les)$", RegexOptions.IgnoreCase);
                        if (b.Success) nom = b.Groups[2].Value + " " + b.Groups[1].Value;
                    }
                    Municipi mu = new Municipi();
                    mu.Id = J.S(J.G(m, "id"));
                    mu.Nom = nom;
                    idx[Clau(content)] = mu;
                }
                munsIdx = idx;
            }
            catch { munsIdx = new Dictionary<string, Municipi>(); }
            return munsIdx;
        }

        public static Municipi MunicipiOficial(string nom)
        {
            if (string.IsNullOrEmpty(nom)) return null;
            Municipi m;
            if (IndexMunicipis().TryGetValue(Clau(nom), out m)) return m;
            return null;
        }

        // ---------- Tema de Windows ----------
        public static object ValorRegistre(string clau, string valor)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(clau))
                {
                    if (k == null) return null;
                    return k.GetValue(valor);
                }
            }
            catch { return null; }
        }

        public static bool TemaClar(string valor)
        {
            object v = ValorRegistre("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", valor);
            return v is int && (int)v == 1;
        }

        public static bool ModeClar()
        {
            string m = Conf != null ? Conf.Tema : null;
            if (m == "clar") return true;
            if (m == "fosc") return false;
            return TemaClar("AppsUseLightTheme");
        }
    }
}
