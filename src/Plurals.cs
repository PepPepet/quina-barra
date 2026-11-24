// Quina Barra! · el temps del teu poble a la barra de tasques
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Regles de plural de cada llengua, les mateixes que fa servir MediaWiki (i translatewiki.net) per a {{PLURAL}}.
//
// Les regles són les de CLDR (Unicode): src\plurals\plurals.xml és la còpia que porta MediaWiki
// (languages/data/plurals.xml, llicència Unicode-3.0) i src\plurals\plurals-mediawiki.xml són les excepcions
// de MediaWiki (hebreu de quatre formes, sr-ec, sr-el, be-tarask...). Van dins de l'exe com a recursos.
// Les formes de {{PLURAL:$1|...}} van en l'ordre de les regles del fitxer (zero, one, two, few, many, other);
// si n'hi ha menys, es fa servir l'última. Una forma «N=text» val només per al número N, com a MediaWiki.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace TempsBarra
{
    static class Plurals
    {
        // codi de llengua (minúscules, amb guionet) → regles en ordre: (categoria, condició)
        static Dictionary<string, List<KeyValuePair<string, string>>> regles;

        // Llengües sense regles pròpies: de qui les hereten (com a MediaWiki). Si no, es treu l'últim tros del codi; al final, l'anglès.
        static readonly Dictionary<string, string> Herencia = new Dictionary<string, string> {
            { "oc", "fr" }, { "frp", "fr" }, { "wa", "fr" }, { "co", "it" }, { "lmo", "it" }, { "nap", "it" },
            { "ext", "es" }, { "gn", "es" }, { "qu", "es" }, { "ay", "es" }, { "lad", "es" }, { "mwl", "pt" },
            { "rue", "uk" }, { "tt", "ru" }, { "ba", "ru" },
            { "szl", "pl" }, { "csb", "pl" }, { "rmy", "ro" }, { "aeb", "ar" }, { "arq", "ar" }, { "ary", "ar" }, { "arz", "ar" },
            { "lrc", "fa" }, { "mzn", "fa" }, { "glk", "fa" }, { "azb", "fa" }
        };

        public static Dictionary<string, List<KeyValuePair<string, string>>> Totes()
        {
            if (regles == null)
            {
                Dictionary<string, List<KeyValuePair<string, string>>> r = new Dictionary<string, List<KeyValuePair<string, string>>>();
                Llegeix("plurals.xml", r);
                Llegeix("plurals-mediawiki.xml", r);   // les excepcions de MediaWiki substitueixen les de CLDR
                regles = r;
            }
            return regles;
        }

        static void Llegeix(string nom, Dictionary<string, List<KeyValuePair<string, string>>> r)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("TempsBarra.plurals." + nom))
            {
                if (s == null) return;
                XmlReaderSettings cfg = new XmlReaderSettings();
                cfg.DtdProcessing = DtdProcessing.Ignore; cfg.XmlResolver = null;
                XmlDocument doc = new XmlDocument();
                using (XmlReader xr = XmlReader.Create(s, cfg)) doc.Load(xr);
                foreach (XmlElement pr in doc.GetElementsByTagName("pluralRules"))
                {
                    List<KeyValuePair<string, string>> llista = new List<KeyValuePair<string, string>>();
                    foreach (XmlElement regla in pr.GetElementsByTagName("pluralRule"))
                        llista.Add(new KeyValuePair<string, string>(regla.GetAttribute("count"), regla.InnerText));
                    foreach (string loc in pr.GetAttribute("locales").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                        r[loc.ToLowerInvariant().Replace('_', '-')] = llista;
                }
            }
        }

        // Regles que fa servir una llengua (amb l'herència); mai null
        public static List<KeyValuePair<string, string>> De(string codi)
        {
            Dictionary<string, List<KeyValuePair<string, string>>> t = Totes();
            string c = (codi ?? "en").ToLowerInvariant();
            HashSet<string> vistos = new HashSet<string>();
            while (!string.IsNullOrEmpty(c) && vistos.Add(c))
            {
                List<KeyValuePair<string, string>> l;
                if (t.TryGetValue(c, out l)) return l;
                string h;
                if (Herencia.TryGetValue(c, out h)) { c = h; continue; }
                int k = c.LastIndexOf('-');
                c = k > 0 ? c.Substring(0, k) : null;
            }
            List<KeyValuePair<string, string>> en;
            return t.TryGetValue("en", out en) ? en : new List<KeyValuePair<string, string>>();
        }

        // Categoria CLDR (zero, one, two, few, many, other) d'un número escrit amb punt decimal («1», «2.50»)
        public static string Categoria(string codi, string numero)
        {
            Operands o = Operands.De(numero);
            foreach (KeyValuePair<string, string> r in De(codi))
                if (r.Key != "other" && Compleix(Condicio(r.Value), o)) return r.Key;
            return "other";
        }

        // Índex de la forma de {{PLURAL}}: la posició de la categoria a la llista de la llengua
        public static int Index(string codi, string numero)
        {
            List<KeyValuePair<string, string>> l = De(codi);
            string cat = Categoria(codi, numero);
            int i = 0;
            foreach (KeyValuePair<string, string> r in l)
            {
                if (r.Key == "other") continue;
                if (r.Key == cat) return i;
                i++;
            }
            return i;   // other: després de totes les altres
        }

        // Tria la forma: primer les «N=text» exactes, després per categoria (si falten formes, l'última)
        public static string Forma(string codi, string numero, string[] formes)
        {
            List<string> normals = new List<string>();
            Operands o = Operands.De(numero);
            foreach (string f in formes)
            {
                Match m = reExplicita.Match(f);
                if (m.Success)
                {
                    decimal v;
                    if (decimal.TryParse(m.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out v) && v == o.Valor) return m.Groups[2].Value;
                    continue;
                }
                normals.Add(f);
            }
            if (normals.Count == 0) return "";
            return normals[Math.Min(Index(codi, numero), normals.Count - 1)];
        }
        static readonly Regex reExplicita = new Regex(@"^\s*(\d+(?:\.\d+)?)\s*=(.*)$", RegexOptions.Singleline);

        // La condició sense els exemples (@integer, @decimal)
        public static string Condicio(string regla)
        {
            int k = regla.IndexOf('@');
            return (k >= 0 ? regla.Substring(0, k) : regla).Trim();
        }

        // ---------- Avaluació de les condicions de CLDR ----------
        // condició = relació ("and" relació)* ("or" ...)*; relació = operand ["%" n] ("=" | "!=") llista de valors i intervals a..b
        static readonly Regex reRelacio = new Regex(@"^\s*([nivwftce])\s*(?:%\s*(\d+))?\s*(!=|=)\s*([\d.,\s]+)$");

        public static bool Compleix(string condicio, Operands o)
        {
            if (string.IsNullOrEmpty(condicio)) return true;
            foreach (string alternativa in Regex.Split(condicio, @"\s+or\s+"))
            {
                bool totes = true;
                foreach (string rel in Regex.Split(alternativa, @"\s+and\s+"))
                    if (!Relacio(rel, o)) { totes = false; break; }
                if (totes) return true;
            }
            return false;
        }

        static bool Relacio(string rel, Operands o)
        {
            Match m = reRelacio.Match(rel);
            if (!m.Success) throw new FormatException("Regla de plural que no entenc: " + rel);
            decimal x = o.Get(m.Groups[1].Value[0]);
            if (m.Groups[2].Success) x = x % decimal.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            bool dins = false;
            foreach (string part in m.Groups[4].Value.Split(','))
            {
                string p = part.Trim();
                int k = p.IndexOf("..", StringComparison.Ordinal);
                if (k >= 0)
                {
                    decimal a = decimal.Parse(p.Substring(0, k), CultureInfo.InvariantCulture), b = decimal.Parse(p.Substring(k + 2), CultureInfo.InvariantCulture);
                    if (x == decimal.Truncate(x) && x >= a && x <= b) { dins = true; break; }   // els intervals són d'enters
                }
                else if (x == decimal.Parse(p, CultureInfo.InvariantCulture)) { dins = true; break; }
            }
            return m.Groups[3].Value == "=" ? dins : !dins;
        }
    }

    // Operands de CLDR: n (valor absolut), i (part entera), v (xifres decimals visibles), w (sense zeros finals),
    // f i t (les xifres decimals com a enter, amb i sense zeros finals), c i e (exponent compacte: sempre 0 aquí)
    class Operands
    {
        public decimal Valor, N, I, V, W, F, T;

        public decimal Get(char c)
        {
            switch (c)
            {
                case 'n': return N; case 'i': return I; case 'v': return V; case 'w': return W;
                case 'f': return F; case 't': return T; default: return 0;
            }
        }

        // «-1.50» → n=1.5, i=1, v=2, w=1, f=50, t=5. Si no és un número, compta com a 0.
        public static Operands De(string s)
        {
            Operands o = new Operands();
            s = (s ?? "").Trim();
            Match m = Regex.Match(s, @"^-?(\d+)(?:\.(\d+))?$");
            if (!m.Success) return o;
            string ent = m.Groups[1].Value, dec = m.Groups[2].Success ? m.Groups[2].Value : "";
            string decT = dec.TrimEnd('0');
            o.I = decimal.Parse(ent, CultureInfo.InvariantCulture);
            o.V = dec.Length; o.W = decT.Length;
            o.F = dec.Length > 0 ? decimal.Parse(dec, CultureInfo.InvariantCulture) : 0;
            o.T = decT.Length > 0 ? decimal.Parse(decT, CultureInfo.InvariantCulture) : 0;
            o.N = decimal.Parse(ent + (dec.Length > 0 ? "." + dec : ""), CultureInfo.InvariantCulture);
            o.Valor = s.StartsWith("-", StringComparison.Ordinal) ? -o.N : o.N;
            return o;
        }
    }
}
