// Quina Barra! · proves dels plurals, de les llengües de reserva i dels codis de llengua
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// No va dins de l'exe: es compila a part amb proves\prova-plurals.ps1 (Textos.cs, Plurals.cs, Json.cs i aquest fitxer).
//  1. Tots els exemples de CLDR i de MediaWiki (@integer, @decimal) han de caure a la seva categoria, a totes les llengües.
//  2. {{PLURAL}} tria la forma que toca, en l'ordre de MediaWiki, a les llengües habituals de translatewiki.
//  3. Cadena de llengües de reserva, codis del Windows → MediaWiki i llengües de dreta a esquerra.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace TempsBarra
{
    static class ProvesPlurals
    {
        static int ok, ko;
        static readonly List<string> errors = new List<string>();

        static void Igual<T>(T esperat, T obtingut, string que)
        {
            if (EqualityComparer<T>.Default.Equals(esperat, obtingut)) ok++;
            else { ko++; errors.Add(que + ": s'esperava «" + esperat + "» i ha sortit «" + obtingut + "»"); }
        }

        static int Main()
        {
            ExemplesCldr();
            FormesPlural();
            Reserva();
            CodisWindows();
            DretaEsquerra();
            foreach (string e in errors) Console.WriteLine("ERROR " + e);
            Console.WriteLine("Proves: " + ok + " bé, " + ko + " malament.");
            return ko == 0 ? 0 : 1;
        }

        // ---------- 1. Exemples de CLDR ----------
        static void ExemplesCldr()
        {
            int llengues = 0, exemples = 0;
            foreach (KeyValuePair<string, List<KeyValuePair<string, string>>> ll in Plurals.Totes())
            {
                llengues++;
                foreach (KeyValuePair<string, string> regla in ll.Value)
                    foreach (string n in Exemples(regla.Value))
                    {
                        exemples++;
                        Igual(regla.Key, Plurals.Categoria(ll.Key, n), "CLDR " + ll.Key + " " + n);
                    }
            }
            Console.WriteLine("CLDR: " + llengues + " codis de llengua, " + exemples + " exemples comprovats.");
            if (llengues < 200) { ko++; errors.Add("Hi ha massa poques llengües a les regles: " + llengues); }
        }

        // «@integer 0, 5~19, 100, … @decimal 0.0~1.5» → 0, 5, 6, ..., 19, 100, 0.0, 0.1, ..., 1.5 (sense la notació compacta 1c6)
        static IEnumerable<string> Exemples(string regla)
        {
            foreach (Match m in Regex.Matches(regla, @"@(integer|decimal)([^@]*)"))
                foreach (string tros in Regex.Split(m.Groups[2].Value, @"[,\s]+"))
                {
                    string t = tros.Trim();
                    if (!Regex.IsMatch(t, @"^\d+(\.\d+)?(~\d+(\.\d+)?)?$")) continue;   // «…», notació compacta (1c6) i errates
                    int k = t.IndexOf('~');
                    if (k < 0) { yield return t; continue; }
                    string a = t.Substring(0, k), b = t.Substring(k + 1);
                    int dec = a.IndexOf('.') >= 0 ? a.Length - a.IndexOf('.') - 1 : 0;
                    decimal pas = 1; for (int i = 0; i < dec; i++) pas /= 10;
                    decimal x = decimal.Parse(a, CultureInfo.InvariantCulture), fi = decimal.Parse(b, CultureInfo.InvariantCulture);
                    for (int n = 0; x <= fi && n < 200; x += pas, n++) yield return x.ToString("F" + dec, CultureInfo.InvariantCulture);
                }
        }

        // ---------- 2. {{PLURAL}} amb les formes de MediaWiki ----------
        static void Forma(string codi, string formes, object n, string esperat)
        {
            G.CarregaLlengua(codi);
            Igual(esperat, G.Omple("{{PLURAL:$1|" + formes + "}}", new[] { n }, codi), "PLURAL " + codi + " " + n);
        }

        static void Serie(string codi, string formes, params object[] parells)
        {
            for (int i = 0; i < parells.Length; i += 2) Forma(codi, formes, parells[i], (string)parells[i + 1]);
        }

        static void FormesPlural()
        {
            // Dues formes: 1 i la resta
            foreach (string c in new[] { "en", "ca", "ca-valencia", "de", "es", "it", "nl", "sv", "gl", "eu", "ast", "an", "el", "hu", "tr", "fi", "nb" })
                Serie(c, "u|p", 1, "u", 0, "p", 2, "p", 21, "p", 1000000, "p");
            Serie("en", "u|p", "1.0", "p", "1.5", "p");   // 1.0 té una xifra decimal: en anglès és plural («1.0 hours»)
            Serie("da", "u|p", 1, "u", "0.5", "u", 2, "p");
            Serie("is", "u|p", 1, "u", 21, "u", 11, "p", 2, "p");
            Serie("mk", "u|p", 1, "u", 21, "u", 11, "p", 2, "p");
            // Francès i portuguès: 0 i 1 en singular (com a MediaWiki, «pt» fa servir la regla de CLDR «pt»)
            foreach (string c in new[] { "fr", "pt", "pt-br", "oc", "oc-aranes" })
                Serie(c, "u|p", 0, "u", 1, "u", "1.5", "u", 2, "p", 100, "p", 1000000, "p");
            // Persa, hindi, bengalí: 0 i 1 en singular
            Serie("fa", "u|p", 0, "u", 1, "u", 2, "p");
            Serie("hi", "u|p", 0, "u", 1, "u", 2, "p");
            // Una sola forma
            foreach (string c in new[] { "ja", "zh-hans", "zh-hant", "ko", "id", "vi", "th" })
                Serie(c, "x|y", 1, "x", 5, "x");
            // Rus, ucraïnès i bielorús: one|few|many (els decimals, «other», cauen a l'última forma escrita)
            foreach (string c in new[] { "ru", "uk", "be", "be-tarask" })
                Serie(c, "1|2|5", 1, "1", 2, "2", 4, "2", 5, "5", 11, "5", 12, "5", 14, "5", 21, "1", 22, "2", 25, "5", 101, "1", 111, "5", 0, "5", "1.5", "5");
            Serie("ru", "1|2|5|d", "1.5", "d");
            // Polonès: one|few|many
            Serie("pl", "1|2|5", 1, "1", 2, "2", 4, "2", 5, "5", 12, "5", 22, "2", 25, "5", 0, "5", 101, "5", 102, "2");
            // Txec i eslovac: one|few|many(decimals)|other
            foreach (string c in new[] { "cs", "sk" })
            {
                Serie(c, "1|2|d|5", 1, "1", 2, "2", 4, "2", 5, "5", 0, "5", 22, "5", "1.5", "d");
                Serie(c, "1|2|5", 1, "1", 3, "2", 5, "5");
            }
            // Croat, serbi (també sr-ec i sr-el de MediaWiki) i bosnià: one|few|other
            foreach (string c in new[] { "hr", "sr", "sr-ec", "sr-el", "bs", "sh" })
                Serie(c, "1|2|5", 1, "1", 21, "1", 2, "2", 24, "2", 5, "5", 11, "5", 12, "5", 0, "5", "0.1", "1", "0.2", "2");
            // Eslovè: one|two|few|other
            Serie("sl", "1|2|3|5", 1, "1", 101, "1", 2, "2", 102, "2", 3, "3", 4, "3", 5, "5", 11, "5", "1.5", "3");
            // Lituà: one|few|many(decimals)|other
            Serie("lt", "1|2|d|10", 1, "1", 21, "1", 2, "2", 9, "2", 10, "10", 11, "10", 19, "10", 20, "10", "0.5", "d");
            Serie("lt", "1|2|10", 1, "1", 3, "2", 10, "10", 15, "10");
            // Letó: zero|one|other
            Serie("lv", "0|1|2", 0, "0", 10, "0", 11, "0", 19, "0", 1, "1", 21, "1", 2, "2", 22, "2");
            // Irlandès: one|two|few|many|other
            Serie("ga", "1|2|3|7|11", 1, "1", 2, "2", 3, "3", 6, "3", 7, "7", 10, "7", 11, "11", 0, "11", 100, "11");
            // Gal·lès: zero|one|two|few|many|other
            Serie("cy", "0|1|2|3|6|x", 0, "0", 1, "1", 2, "2", 3, "3", 6, "6", 4, "x", 5, "x", 7, "x", 100, "x");
            // Bretó: one|two|few|many|other
            Serie("br", "1|2|3|M|x", 1, "1", 21, "1", 11, "x", 71, "x", 2, "2", 22, "2", 12, "x", 3, "3", 9, "3", 19, "x", 1000000, "M", 5, "x");
            // Romanès: one|few|other
            Serie("ro", "1|2|20", 1, "1", 0, "2", 2, "2", 19, "2", 101, "2", 119, "2", 20, "20", 100, "20", 120, "20", "1.5", "2");
            // Àrab: zero|one|two|few|many|other
            Serie("ar", "0|1|2|3|11|100", 0, "0", 1, "1", 2, "2", 3, "3", 10, "3", 103, "3", 11, "11", 99, "11", 100, "100", 102, "100", 1000, "100");
            // Hebreu: MediaWiki conserva quatre formes, one|two|many|other
            Serie("he", "1|2|20|x", 1, "1", 2, "2", 20, "20", 30, "20", 100, "20", 3, "x", 10, "x", 15, "x", 0, "x");
            Serie("he", "1|2|x", 20, "x", 3, "x");   // amb menys formes, la darrera
            // Maltès, gaèlic escocès, baix i alt sòrab
            Serie("mt", "1|2|3|11|20", 1, "1", 2, "2", 0, "3", 3, "3", 10, "3", 11, "11", 19, "11", 20, "20");
            Serie("gd", "1|2|3|20", 1, "1", 11, "1", 2, "2", 12, "2", 3, "3", 19, "3", 20, "20");
            Serie("hsb", "1|2|3|5", 1, "1", 101, "1", 2, "2", 3, "3", 4, "3", 5, "5");
            // Formes explícites de MediaWiki: «0=...», «12=...»
            Serie("en", "0=none|one $1|$1 more", 0, "none", 1, "one 1", 5, "5 more");
            Serie("ru", "12=dotze|1|2|5", 12, "dotze", 2, "2", 13, "5");
            Forma("en", "a|b", 1, "a");
            Igual("b", G.Omple("{{PLURAL:$1|a|b}}", new object[0], "en"), "PLURAL sense valor (compta com a 0)");
            // Números ja escrits amb el format de la llengua
            G.CarregaLlengua("ca");
            Igual("1013", G.NumeroPerPlural("1.013"), "ca: 1.013 són mil tretze");
            Igual("0.4", G.NumeroPerPlural("0,4"), "ca: 0,4");
            Igual("1", G.NumeroPerPlural(1), "int");
            Igual("2.5", G.NumeroPerPlural(2.5), "double");
            G.CarregaLlengua("en");
            Igual("1013", G.NumeroPerPlural("1,013"), "en: 1,013");
            Igual("0.4", G.NumeroPerPlural("0.4"), "en: 0.4");
            // Textos de veritat de l'exe
            G.CarregaLlengua("en");
            Igual("1 result", G.T("tria-n-resultats", 1), "en tria-n-resultats 1");
            Igual("7 results", G.T("tria-n-resultats", 7), "en tria-n-resultats 7");
            Igual("Data: Open-Meteo · average of 3 models", G.T("ara-font-models", 3), "en ara-font-models 3");
            G.CarregaLlengua("ca");
            Igual("1 resultat", G.T("tria-n-resultats", 1), "ca tria-n-resultats 1");
            Igual("0 resultats", G.T("tria-n-resultats", 0), "ca tria-n-resultats 0");
        }

        // ---------- 3. Llengües de reserva ----------
        static void Reserva()
        {
            List<string> disp = new List<string> { "an", "ast", "ca", "ca-alguer", "ca-valencia", "en", "eu", "gl", "oc", "oc-aranes", "pt" };
            Igual("ca-valencia > ca > en", string.Join(" > ", G.Cadena("ca-valencia", disp)), "cadena ca-valencia");
            Igual("ca-alguer > ca > en", string.Join(" > ", G.Cadena("ca-alguer", disp)), "cadena ca-alguer");
            Igual("oc-aranes > oc > en", string.Join(" > ", G.Cadena("oc-aranes", disp)), "cadena oc-aranes");
            Igual("ca > en", string.Join(" > ", G.Cadena("ca", disp)), "cadena ca");
            Igual("en", string.Join(" > ", G.Cadena("en", disp)), "cadena en");
            Igual("pt > en", string.Join(" > ", G.Cadena("pt-br", disp)), "cadena pt-br sense pt-br");
            Igual("gl > en", string.Join(" > ", G.Cadena("gl", disp)), "cadena gl");

            // L'exe porta l'anglès i les nou llengües de la 1.1, i qqq no compta com a llengua
            List<string> reals = G.LlenguesDisponibles();
            foreach (string c in new[] { "en", "ca", "ca-valencia", "ca-alguer", "gl", "eu", "ast", "an", "oc", "oc-aranes" })
                Igual(true, reals.Contains(c), "llengua disponible " + c);
            Igual(false, reals.Contains("qqq"), "qqq no és una llengua");

            // Un text que falta en valencià surt en català, i un que falta a totes, en anglès
            G.CarregaLlengua("ca-valencia");
            Igual(G.T("ara-font"), "Dades: Open-Meteo", "ca-valencia ara-font");
            Igual("Llengua", G.T("llengua-titol"), "ca-valencia llengua-titol");
            G.CarregaLlengua("en");
            Igual("Settings", G.T("configuracio-titol"), "en configuracio-titol");
            Igual("⟨no-existeix⟩", G.T("no-existeix"), "clau inexistent");
            // La nota de les traduccions no es pot treure d'una llengua de reserva anglesa
            G.CarregaLlengua("gl");
            Igual(true, G.TeTextPropi("llengua-nota"), "gl té nota pròpia");
        }

        // ---------- 4. Llengua del Windows → codi de MediaWiki ----------
        static string Primer(string cultura) { List<string> l = G.CodisPerCultura(cultura); return l.Count > 0 ? l[0] : ""; }

        static void CodisWindows()
        {
            Igual("ca-valencia", Primer("ca-ES-valencia"), "ca-ES-valencia");
            Igual("ca-es", Primer("ca-ES"), "ca-ES");
            Igual(true, G.CodisPerCultura("ca-ES").Contains("ca"), "ca-ES → ca");
            Igual("zh-hans", Primer("zh-CN"), "zh-CN");
            Igual("zh-hans", Primer("zh-Hans"), "zh-Hans");
            Igual("zh-hant", Primer("zh-TW"), "zh-TW");
            Igual("zh-hant", Primer("zh-HK"), "zh-HK");
            Igual("sr-ec", Primer("sr-Cyrl-RS"), "sr-Cyrl-RS");
            Igual("sr-el", Primer("sr-Latn-RS"), "sr-Latn-RS");
            Igual("pt-br", Primer("pt-BR"), "pt-BR");
            Igual(true, G.CodisPerCultura("pt-PT").Contains("pt"), "pt-PT → pt");
            Igual("ckb", Primer("ku-Arab-IQ"), "ku-Arab-IQ (kurd central)");
            Igual("nb", Primer("nb-NO"), "nb-NO");
            Igual("en-gb", Primer("en-GB"), "en-GB");
            Igual(true, G.CodisPerCultura("en-US").Contains("en"), "en-US → en");
            Igual(true, G.CodisPerCultura("he-IL").Contains("he"), "he-IL → he");
            Igual(true, G.CodisPerCultura("ast-ES").Contains("ast"), "ast-ES → ast");
            Igual(true, G.CodisPerCultura("eu-ES").Contains("eu"), "eu-ES → eu");
            Igual(true, G.CodisPerCultura("de-CH").IndexOf("de-ch") < G.CodisPerCultura("de-CH").IndexOf("de"), "de-CH: primer de-ch, després de");

            // Una llengua que no tenim → cap (i llavors el giny surt en anglès)
            List<string> disp = G.LlenguesDisponibles();
            bool cap = true;
            foreach (string c in G.CodisPerCultura("ja-JP")) if (disp.Contains(c)) cap = false;
            Igual(true, cap, "ja-JP no la tenim");
            Igual("ca-valencia", G.CodisPerCultura("ca-ES-valencia").First(disp.Contains), "ca-ES-valencia → ca-valencia (disponible)");
            Igual("ca", G.CodisPerCultura("ca-ES").First(disp.Contains), "ca-ES → ca (disponible)");
            Igual("gl", G.CodisPerCultura("gl-ES").First(disp.Contains), "gl-ES → gl (disponible)");
            Igual("en", G.CodisPerCultura("en-US").First(disp.Contains), "en-US → en (disponible)");

            // Llengua de reserva quan no tenim la del Windows: es fa com si el Windows fos en castellà, que no tenim
            System.Globalization.CultureInfo abans = System.Threading.Thread.CurrentThread.CurrentUICulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-ES");
            Igual(null, G.LlenguaDelWindows(disp), "es-ES no la tenim");
            G.LlenguaReserva = "en"; G.TriaLlengua(null);
            Igual("en", G.Llengua, "instal·lació nova amb el Windows en castellà: anglès");
            G.LlenguaReserva = "ca"; G.TriaLlengua(null);
            Igual("ca", G.Llengua, "actualització des de la 1.1 amb el Windows en castellà: català");
            G.TriaLlengua("gl");
            Igual("gl", G.Llengua, "la llengua desada mana");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("eu-ES");
            G.LlenguaReserva = "ca"; G.TriaLlengua(null);
            Igual("eu", G.Llengua, "actualització amb el Windows en basc: basc");
            System.Threading.Thread.CurrentThread.CurrentUICulture = abans;
            G.LlenguaReserva = "en";
        }

        // ---------- 5. Dreta a esquerra ----------
        static void DretaEsquerra()
        {
            foreach (string c in new[] { "ar", "he", "fa", "ur", "ps", "ckb", "yi", "sd", "ug-arab", "arz", "azb", "dv", "pnb", "mzn", "glk", "lrc", "ks-arab" })
                Igual(true, G.EsDeDretaAEsquerra(c), "RTL " + c);
            foreach (string c in new[] { "en", "ca", "ca-valencia", "oc-aranes", "ru", "zh-hans", "sr-ec", "ja", "tr", "kk-cyrl", "uz", "ku-latn", "ug-latn" })
                Igual(false, G.EsDeDretaAEsquerra(c), "LTR " + c);
            // Els números dins d'un text RTL es protegeixen; les paraules no
            G.CarregaLlengua("en"); G.Dreta = true;
            Igual("\u200E-3 °C\u200E", G.TextDireccio("-3 °C"), "RTL número");
            Igual("\u200E17:30\u200E", G.TextDireccio("17:30"), "RTL hora");
            Igual("a \u200E15 °C\u200E b \u200E75%\u200E", G.TextDireccio("a 15 °C b 75%"), "RTL números dins d'una frase");
            Igual("PM2,5 PM10", G.TextDireccio("PM2,5 PM10"), "RTL: les xifres enganxades a lletres no es toquen");
            Igual("Barcelona", G.TextDireccio("Barcelona"), "RTL nom de lloc");
            Igual("Made by $1.", G.TextDireccio("Made by $1."), "RTL: els $1 sense omplir no es toquen");
            G.Dreta = false;
            Igual("-3 °C", G.TextDireccio("-3 °C"), "LTR número");
        }
    }
}
