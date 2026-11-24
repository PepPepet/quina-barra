﻿// Quina Barra! · el temps del teu poble a la barra de tasques
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Textos de la interfície en diverses llengües.
//
// Cada llengua és un fitxer JSON pla dins de src\textos (en.json, ca.json, gl.json...), al format de jquery.i18n
// que fa servir translatewiki.net: {"@metadata": {...}, "clau": "text"}. Els codis són els de MediaWiki
// (minúscules: ca, ca-valencia, pt-br, zh-hans, sr-ec...). L'anglès (en.json) és la base: si a una llengua li falta
// un text, es busca a la llengua mare (ca-valencia → ca, oc-aranes → oc, pt-br → pt) i, al final, en anglès.
// qqq.json és la documentació de cada text per als traductors.
// Dins dels textos: $1, $2... són valors que hi posa el programa, i {{PLURAL:$1|forma|forma...}} tria la forma
// segons el número i les regles de plural de la llengua (vegeu Plurals.cs). Als XAML, @T:clau@ es canvia pel text.
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
        public const string LlenguaBase = "en";

        // Llengua en ús (codi del fitxer: "en", "ca", "gl"...)
        public static string Llengua = LlenguaBase;
        public static bool Dreta;                 // la llengua s'escriu de dreta a esquerra
        static List<string> cadenaCodis;          // de la llengua triada fins a l'anglès
        static List<Dictionary<string, string>> cadena;
        public static readonly SortedSet<string> TextsQueFalten = new SortedSet<string>();   // ni en anglès (per a la prova)
        public static readonly SortedSet<string> TextsSenseTraduir = new SortedSet<string>(); // surten en una llengua de reserva

        // Llengües mare que no es dedueixen traient l'últim tros del codi
        static readonly Dictionary<string, string> Mares = new Dictionary<string, string> {
            { "ca-valencia", "ca" }, { "ca-alguer", "ca" }, { "oc-aranes", "oc" } };

        // Codis de les llengües que porta l'exe (sense qqq), per ordre alfabètic del codi
        static List<string> disponibles;
        public static List<string> LlenguesDisponibles()
        {
            if (disponibles == null)
            {
                List<string> r = new List<string>();
                foreach (string n in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    if (!n.StartsWith(PrefixTextos, StringComparison.Ordinal) || !n.EndsWith(".json", StringComparison.Ordinal)) continue;
                    string codi = n.Substring(PrefixTextos.Length, n.Length - PrefixTextos.Length - 5);
                    if (codi != "qqq") r.Add(codi);
                }
                r.Sort(StringComparer.Ordinal);
                disponibles = r;
            }
            return new List<string>(disponibles);
        }

        static readonly Dictionary<string, Dictionary<string, string>> llegits = new Dictionary<string, Dictionary<string, string>>();
        static Dictionary<string, string> LlegeixTextos(string codi)
        {
            Dictionary<string, string> d;
            if (llegits.TryGetValue(codi, out d)) return d;
            d = new Dictionary<string, string>();
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(PrefixTextos + codi + ".json"))
                {
                    if (s != null)
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
            llegits[codi] = d;
            return d;
        }

        // De quines llengües es treuen els textos, per ordre: la triada, les seves mares i l'anglès
        public static List<string> Cadena(string codi, ICollection<string> disp)
        {
            List<string> r = new List<string>();
            string c = codi;
            while (!string.IsNullOrEmpty(c) && !r.Contains(c))
            {
                if (disp.Contains(c)) r.Add(c);
                string m;
                if (Mares.TryGetValue(c, out m)) { c = m; continue; }
                int k = c.LastIndexOf('-');
                c = k > 0 ? c.Substring(0, k) : null;
            }
            if (!r.Contains(LlenguaBase)) r.Add(LlenguaBase);
            return r;
        }

        // Llengua si el Windows és en una llengua que no tenim: l'anglès, o el català per a qui ja tenia el giny abans de la 1.2
        public static string LlenguaReserva = LlenguaBase;

        // Tria la llengua: la desada a la configuració; si no n'hi ha, la del Windows; si no la tenim, la de reserva
        public static void TriaLlengua(string desada)
        {
            List<string> disp = LlenguesDisponibles();
            string codi = null;
            if (!string.IsNullOrEmpty(desada) && disp.Contains(desada)) codi = desada;
            if (codi == null) codi = LlenguaDelWindows(disp);
            if (codi == null && disp.Contains(LlenguaReserva)) codi = LlenguaReserva;
            CarregaLlengua(codi ?? LlenguaBase);
        }

        public static void CarregaLlengua(string codi)
        {
            Llengua = codi;
            cadenaCodis = Cadena(codi, LlenguesDisponibles());
            cadena = cadenaCodis.Select(LlegeixTextos).ToList();
            Dreta = EsDeDretaAEsquerra(codi);
            culturaLlengua = null;
        }

        // ---------- Llengua del Windows → codi de MediaWiki ----------
        public static string LlenguaDelWindows(List<string> disp)
        {
            foreach (string c in CodisPerCultura(CultureInfo.CurrentUICulture.Name))
                if (disp.Contains(c)) return c;
            return null;
        }

        // Codis de MediaWiki que poden correspondre a una cultura de .NET, del més precís al més general.
        // ca-ES-valencia → ca-valencia; zh-CN → zh-hans; zh-TW → zh-hant; sr-Cyrl-RS → sr-ec; pt-BR → pt-br; ku-Arab-IQ → ckb...
        public static List<string> CodisPerCultura(string nom)
        {
            List<string> r = new List<string>();
            string n = (nom ?? "").ToLowerInvariant();
            if (n.Length == 0) return r;
            string[] t = n.Split('-');
            string llengua = t[0], escriptura = null, regio = null;
            for (int k = 1; k < t.Length; k++)
            {
                if (t[k].Length == 4 && escriptura == null) escriptura = t[k];
                else if ((t[k].Length == 2 || (t[k].Length == 3 && char.IsDigit(t[k][0]))) && regio == null) regio = t[k];
            }
            Action<string> afegeix = c => { if (!string.IsNullOrEmpty(c) && !r.Contains(c)) r.Add(c); };
            switch (llengua)
            {
                case "ca":
                    if (n.EndsWith("-valencia", StringComparison.Ordinal)) afegeix("ca-valencia");
                    if (regio == "it") afegeix("ca-alguer");
                    break;
                case "zh":
                    if (escriptura == "hant" || regio == "tw" || regio == "hk" || regio == "mo") afegeix("zh-hant");
                    else afegeix("zh-hans");
                    break;
                case "sr": afegeix(escriptura == "latn" ? "sr-el" : "sr-ec"); break;
                case "pt": if (regio == "br") afegeix("pt-br"); break;
                case "en": if (regio == "gb") afegeix("en-gb"); if (regio == "ca") afegeix("en-ca"); break;
                case "de": if (regio == "ch") afegeix("de-ch"); if (regio == "at") afegeix("de-at"); break;
                case "ku": afegeix(escriptura == "arab" ? "ckb" : "ku-latn"); break;
                case "pa": if (escriptura == "arab") afegeix("pnb"); break;
                case "no": case "nb": afegeix("nb"); break;
                case "iw": afegeix("he"); break;
                case "in": afegeix("id"); break;
                case "ji": afegeix("yi"); break;
                case "tg": afegeix("tg-cyrl"); break;
                case "kk": afegeix(escriptura == "arab" ? "kk-arab" : "kk-cyrl"); break;
                case "ug": afegeix("ug-arab"); break;
                case "uz": if (escriptura == "cyrl") afegeix("uz-cyrl"); break;
                case "fil": afegeix("tl"); break;
                case "ks": afegeix(escriptura == "deva" ? "ks-deva" : "ks-arab"); break;
            }
            afegeix(n);
            if (escriptura != null) afegeix(llengua + "-" + escriptura);
            if (regio != null) afegeix(llengua + "-" + regio);
            afegeix(llengua);
            try { afegeix(CultureInfo.GetCultureInfo(nom).ThreeLetterISOLanguageName); } catch { }   // p. ex. ast
            return r;
        }

        // ---------- Dreta a esquerra ----------
        static readonly HashSet<string> LlenguesRtl = new HashSet<string> {
            "ar", "aeb", "aeb-arab", "apc", "arc", "arq", "ary", "arz", "azb", "bcc", "bgn", "bqi", "ckb", "dv", "fa", "glk",
            "he", "khw", "kk-arab", "ks", "ks-arab", "ku-arab", "lki", "lrc", "luz", "mzn", "nqo", "ota", "pnb", "prd", "ps",
            "sd", "sdh", "skr", "skr-arab", "syc", "ug", "ug-arab", "ur", "yi", "ydd", "iw", "ji", "mdh" };

        public static bool EsDeDretaAEsquerra(string codi)
        {
            string c = (codi ?? "").ToLowerInvariant();
            if (LlenguesRtl.Contains(c)) return true;
            if (c.EndsWith("-arab", StringComparison.Ordinal) || c.EndsWith("-hebr", StringComparison.Ordinal)) return true;
            if (c.EndsWith("-latn", StringComparison.Ordinal) || c.EndsWith("-cyrl", StringComparison.Ordinal)) return false;
            try { return CultureInfo.GetCultureInfo(c).TextInfo.IsRightToLeft; } catch { return false; }
        }

        // ---------- Format dels números segons la llengua ----------
        static CultureInfo culturaLlengua;
        public static CultureInfo CulturaLlengua
        {
            get
            {
                if (culturaLlengua == null)
                {
                    Dictionary<string, string> equiv = new Dictionary<string, string> {
                        { "ca-valencia", "ca-ES-valencia" }, { "ca-alguer", "ca-IT" }, { "ca", "ca-ES" }, { "oc-aranes", "oc-FR" }, { "oc", "oc-FR" },
                        { "sr-ec", "sr-Cyrl" }, { "sr-el", "sr-Latn" }, { "ckb", "ku-Arab" }, { "pt-br", "pt-BR" }, { "pt", "pt-PT" } };
                    List<string> prova = new List<string>();
                    foreach (string c in cadenaCodis ?? new List<string> { Llengua })
                    {
                        string e; if (equiv.TryGetValue(c, out e)) prova.Add(e);
                        prova.Add(c);
                    }
                    foreach (string c in prova)
                    {
                        try { culturaLlengua = CultureInfo.GetCultureInfo(c); break; }
                        catch { }
                    }
                    if (culturaLlengua == null) culturaLlengua = CultureInfo.InvariantCulture;
                }
                return culturaLlengua;
            }
        }

        // Codi de llengua per al cercador de llocs (Open-Meteo/GeoNames): les variants fan servir la llengua mare
        public static string LlenguaCercador()
        {
            int k = Llengua.IndexOf('-');
            return k > 0 ? Llengua.Substring(0, k) : Llengua;
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

        static void Assegura() { if (cadena == null) CarregaLlengua(Llengua); }

        public static bool TeText(string clau)
        {
            Assegura();
            return cadena.Any(d => d.ContainsKey(clau));
        }

        // El text existeix en la llengua triada o en una llengua mare, sense haver d'anar a l'anglès
        public static bool TeTextPropi(string clau)
        {
            Assegura();
            for (int i = 0; i < cadena.Count; i++)
            {
                if (cadenaCodis[i] == LlenguaBase && Llengua != LlenguaBase) break;
                string t; if (cadena[i].TryGetValue(clau, out t) && !string.IsNullOrEmpty(t)) return true;
            }
            return false;
        }

        // Text d'una clau en la llengua en ús, amb els valors $1, $2... i els plurals resolts
        public static string T(string clau, params object[] valors)
        {
            Assegura();
            for (int i = 0; i < cadena.Count; i++)
            {
                string t;
                if (cadena[i].TryGetValue(clau, out t) && !string.IsNullOrEmpty(t))
                {
                    if (i > 0 && cadenaCodis[i] == LlenguaBase) TextsSenseTraduir.Add(clau);
                    return TextDireccio(Omple(t, valors, cadenaCodis[i]));   // plurals amb les regles de la llengua del text
                }
            }
            TextsQueFalten.Add(clau);
            return "⟨" + clau + "⟩";
        }

        static readonly Regex rePlural = new Regex(@"\{\{PLURAL:\s*\$(\d+)\s*\|([^{}]*)\}\}", RegexOptions.IgnoreCase);
        static readonly Regex reValor = new Regex(@"\$(\d+)");
        // En una llengua de dreta a esquerra, cada número amb la seva unitat (-3°, 15 °C, 17:30, 1.013 hPa, 75%) es manté
        // d'esquerra a dreta: s'envolta de marques LRM (U+200E), que el WPF respecta (les LRE/PDF, no).
        static readonly Regex reXifres = new Regex(@"(?<![\p{L}\d.,$])[-\u2212+]?\d[\d.,:\u00A0\u202F]*(?:\s?(?:km/h|hPa|°C|°|mm|µg/m³|km|min|%|m|s|h)(?!\p{L}))?");
        public static string TextDireccio(string s)
        {
            if (!Dreta || string.IsNullOrEmpty(s) || s.IndexOf('\u200E') >= 0) return s;
            return reXifres.Replace(s, m => "\u200E" + m.Value + "\u200E");
        }

        public static string Omple(string t, object[] valors, string codiPlural)
        {
            if (t.IndexOf("{{", StringComparison.Ordinal) >= 0)
                t = rePlural.Replace(t, m =>
                {
                    int i = int.Parse(m.Groups[1].Value) - 1;
                    string num = (valors != null && i >= 0 && i < valors.Length) ? NumeroPerPlural(valors[i]) : "0";
                    return Plurals.Forma(codiPlural, num, m.Groups[2].Value.Split('|'));
                });
            if (valors != null && valors.Length > 0 && t.IndexOf('$') >= 0)
                t = reValor.Replace(t, m =>
                {
                    int i = int.Parse(m.Groups[1].Value) - 1;
                    if (i < 0 || i >= valors.Length) return m.Value;
                    return Convert.ToString(valors[i], CulturaLlengua);
                });
            return t;
        }

        // Número escrit amb punt decimal i sense separador de milers, per a les regles de plural
        public static string NumeroPerPlural(object v)
        {
            if (v == null) return "0";
            if (v is int || v is long || v is short || v is byte || v is uint || v is ulong) return Convert.ToString(v, CultureInfo.InvariantCulture);
            if (v is double) return ((double)v).ToString("0.###############", CultureInfo.InvariantCulture);
            if (v is float) return ((float)v).ToString("0.#######", CultureInfo.InvariantCulture);
            if (v is decimal) return ((decimal)v).ToString(CultureInfo.InvariantCulture);
            string s = Convert.ToString(v, CultureInfo.InvariantCulture).Replace("\u00A0", "").Replace("\u202F", "").Replace(" ", "").Replace("−", "-");
            // Primer, com l'escriu la llengua (en català, 1.013 són mil tretze i 0,4 és zero coma quatre); si no, amb punt decimal
            NumberFormatInfo f = CulturaLlengua.NumberFormat;
            string net = s;
            if (f.NumberGroupSeparator.Trim().Length > 0) net = net.Replace(f.NumberGroupSeparator, "");
            net = net.Replace(f.NumberDecimalSeparator, ".");
            if (Regex.IsMatch(net, @"^-?\d+(\.\d+)?$")) return net;
            return Regex.IsMatch(s, @"^-?\d+(\.\d+)?$") ? s : "0";
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
