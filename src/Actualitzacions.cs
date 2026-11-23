// Quina Barra! · versió 1.0 · el temps del teu poble a la barra de tasques, en català
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Cerca actualitzacions (a les «releases» de GitHub) i opció «Engega amb el Windows».
// No baixa ni instal·la res sol: avisa i obre la pàgina de la versió nova.
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace TempsBarra
{
    static partial class G
    {
        public const string UrlRepo = "https://github.com/PepPepet/quina-barra";
        const string UrlApiDarrera = "https://api.github.com/repos/PepPepet/quina-barra/releases/latest";
        const string ClauArrencada = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string NomArrencada = "QuinaBarra";

        public static string VersioNova;          // null si no n'hi ha cap de més nova
        public static string UrlNova;
        public static string EstatActualitzacio;  // text per al «Quant a»
        static DateTime darreraCerca = DateTime.MinValue;
        static bool cercant;

        static Version LlegeixVersio(string s)
        {
            s = (s ?? "").Trim().TrimStart('v', 'V');
            Version v;
            if (Version.TryParse(s, out v)) return v;
            int n; if (int.TryParse(s, out n)) return new Version(n, 0);
            return null;
        }

        // Fa la consulta (sense interfície). Torna el text d'estat.
        static string ConsultaActualitzacio()
        {
            try
            {
                string txt;
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add("User-Agent", UserAgent);
                    wc.Headers.Add("Accept", "application/vnd.github+json");
                    txt = wc.DownloadString(UrlApiDarrera);
                }
                object d = J.Parse(txt);
                string tag = J.S(J.G(d, "tag_name"));
                Version remota = LlegeixVersio(tag), local = LlegeixVersio(Versio);
                if (remota != null && local != null && remota > local)
                {
                    VersioNova = remota.ToString();
                    UrlNova = J.S(J.G(d, "html_url"));
                    if (string.IsNullOrEmpty(UrlNova)) UrlNova = UrlRepo + "/releases/latest";
                    return "Tens la " + Versio + " i ja ha sortit la " + VersioNova + ".";
                }
                VersioNova = null; UrlNova = null;
                return "Tens la versió " + Versio + ", que és la darrera.";
            }
            catch (WebException ex)
            {
                HttpWebResponse r = ex.Response as HttpWebResponse;
                if (r != null && r.StatusCode == HttpStatusCode.NotFound) return "Tens la versió " + Versio + ". Encara no n'hi ha cap de publicada a GitHub.";
                return "No s'ha pogut comprovar: sense connexió amb GitHub.";
            }
            catch (Exception ex)
            {
                Registre("Error cercant actualitzacions: " + ex.Message);
                return "No s'ha pogut comprovar ara mateix.";
            }
        }

        // En segon pla, perquè el giny no es quedi encallat. «despres» s'executa al fil de la interfície.
        public static void CercaActualitzacions(Action despres)
        {
            if (Prova || cercant) return;
            cercant = true;
            darreraCerca = DateTime.Now;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string estat = ConsultaActualitzacio();
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    cercant = false;
                    EstatActualitzacio = estat;
                    if (VersioNova != null) Registre("Hi ha una versió nova: " + VersioNova);
                    MarcaActualitzacio();
                    if (despres != null) despres();
                }));
            });
        }

        // Un cop al dia, sense dir res si no n'hi ha
        public static void CercaActualitzacionsSiCal()
        {
            if ((DateTime.Now - darreraCerca).TotalHours >= 24) CercaActualitzacions(null);
        }

        // Puntet al botó ⓘ quan hi ha versió nova
        public static void MarcaActualitzacio()
        {
            if (det == null || det.BQuantA == null) return;
            if (VersioNova != null)
            {
                det.BQuantA.Foreground = Pinzell(det.P["ACCENT"]);
                det.BQuantA.ToolTip = "Més opcions · hi ha una versió nova (" + VersioNova + ")";
            }
            else
            {
                det.BQuantA.ClearValue(Control.ForegroundProperty);
                det.BQuantA.ToolTip = "Més opcions";
            }
        }

        // ---------- Pas del nom antic («El temps a la barra», carpeta TempsBarra) al nou ----------
        static void CopiaCarpeta(string de, string a)
        {
            Directory.CreateDirectory(a);
            foreach (string f in Directory.GetFiles(de)) File.Copy(f, Path.Combine(a, Path.GetFileName(f)), false);
            foreach (string sub in Directory.GetDirectories(de)) CopiaCarpeta(sub, Path.Combine(a, Path.GetFileName(sub)));
        }

        public static void MigraNomAntic()
        {
            try
            {
                string antiga = Path.Combine(Path.GetDirectoryName(Carpeta), "TempsBarra");
                if (!Directory.Exists(Carpeta) && Directory.Exists(antiga))
                {
                    CopiaCarpeta(antiga, Carpeta);
                    Registre("Nom nou: dades copiades de TempsBarra a QuinaBarra (la carpeta vella es conserva)");
                }
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(ClauArrencada, true))
                {
                    if (k != null && k.GetValue("TempsBarra") != null)
                    {
                        k.DeleteValue("TempsBarra");
                        k.SetValue(NomArrencada, "\"" + RutaExe() + "\"");
                        Registre("Engega amb el Windows: passat al nom nou");
                    }
                }
            }
            catch (Exception ex) { Registre("Error passant al nom nou: " + ex.Message); }
        }

        // ---------- Engega amb el Windows ----------
        static string RutaExe() { return System.Reflection.Assembly.GetExecutingAssembly().Location; }

        public static bool EngegaAmbWindows()
        {
            object v = ValorRegistre(ClauArrencada, NomArrencada);
            string s = v as string;
            return !string.IsNullOrEmpty(s) && s.IndexOf(RutaExe(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void PosaEngegaAmbWindows(bool si)
        {
            if (Prova) return;
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(ClauArrencada))
            {
                if (si) k.SetValue(NomArrencada, "\"" + RutaExe() + "\"");
                else if (k.GetValue(NomArrencada) != null) k.DeleteValue(NomArrencada);
            }
            Registre(si ? "Engega amb el Windows: activat" : "Engega amb el Windows: desactivat");
        }
    }
}
