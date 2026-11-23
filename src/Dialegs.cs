// Quina Barra! · versió 1.0 · el temps del teu poble a la barra de tasques, en català
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Finestra per triar la ubicació i diàleg per canviar el nom d'un lloc.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TempsBarra
{
    // =====================================================================
    //  Finestra per triar la ubicació
    // =====================================================================
    class Tria
    {
        public Window W;
        Dictionary<string, string> P;
        Grid Capcalera; Button BTanca, BCerca, BCancela, BDesa; Border CaixaCerca; TextBlock Pista, Estat; ListBox Llista;
        public TextBox TCerca, TNom;
        List<Dictionary<string, object>> Resultats = new List<Dictionary<string, object>>();
        Dictionary<string, object> Triat;
        DispatcherTimer Espera;

        public Tria(Dictionary<string, object> actual)
        {
            P = G.Paleta();
            W = (Window)G.LlegeixXaml(G.XamlFinestra("Tria.xaml", P));
            Capcalera = (Grid)W.FindName("Capcalera"); BTanca = (Button)W.FindName("BTanca"); CaixaCerca = (Border)W.FindName("CaixaCerca");
            Pista = (TextBlock)W.FindName("Pista"); TCerca = (TextBox)W.FindName("TCerca"); BCerca = (Button)W.FindName("BCerca");
            Estat = (TextBlock)W.FindName("Estat"); Llista = (ListBox)W.FindName("Llista"); TNom = (TextBox)W.FindName("TNom");
            BCancela = (Button)W.FindName("BCancela"); BDesa = (Button)W.FindName("BDesa");
            if (actual != null) TNom.Text = G.Nom(actual);

            Capcalera.MouseLeftButtonDown += (s, e) => { try { W.DragMove(); } catch { } };
            BTanca.Click += (s, e) => W.Close();
            BCancela.Click += (s, e) => W.Close();
            TCerca.GotFocus += (s, e) => CaixaCerca.BorderBrush = G.Pinzell(P["ACCENT"]);
            TCerca.LostFocus += (s, e) => CaixaCerca.BorderBrush = G.Pinzell(P["BORDER"]);

            // Cerca mentre escrius (amb una petita espera) o en prémer Retorn
            Espera = new DispatcherTimer();
            Espera.Interval = TimeSpan.FromMilliseconds(500);
            Espera.Tick += (s, e) => { Espera.Stop(); Cerca(); };
            TCerca.TextChanged += (s, e) =>
            {
                Pista.Visibility = TCerca.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
                Espera.Stop(); Espera.Start();
            };
            TCerca.KeyDown += (s, e) => { if (e.Key == Key.Return) { Espera.Stop(); Cerca(); } };
            BCerca.Click += (s, e) => { Espera.Stop(); Cerca(); };

            Llista.SelectionChanged += (s, e) =>
            {
                int i = Llista.SelectedIndex;
                if (i >= 0 && i < Resultats.Count) { TNom.Text = J.S(J.G(Resultats[i], "name")); BDesa.IsEnabled = true; }
            };
            BDesa.Click += (s, e) => Desa();
            Llista.MouseDoubleClick += (s, e) => Desa();
            W.Loaded += (s, e) => TCerca.Focus();
            W.KeyDown += (s, e) => { if (e.Key == Key.Escape) W.Close(); };
        }

        static string Nom(Dictionary<string, object> x) { return J.S(J.G(x, "name")); }

        public void Cerca()
        {
            Espera.Stop();
            string q = TCerca.Text.Trim();
            if (q.Length < 2) return;
            Llista.Items.Clear(); BDesa.IsEnabled = false;
            Estat.Text = "Cercant...";
            W.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.Background);
            HashSet<string> vistos = new HashSet<string>();
            List<Dictionary<string, object>> tots = new List<Dictionary<string, object>>();
            foreach (string variant in new[] { q, G.TreuAccents(q) }.Distinct(StringComparer.Ordinal))
            {
                try
                {
                    object r = G.Web("https://geocoding-api.open-meteo.com/v1/search?count=15&language=ca&format=json&name=" + Uri.EscapeDataString(variant));
                    foreach (object o in J.A(J.G(r, "results")))
                    {
                        Dictionary<string, object> x = o as Dictionary<string, object>;
                        if (x != null && vistos.Add(J.S(J.G(x, "id")) ?? "")) tots.Add(x);
                    }
                }
                catch { }
            }
            // Si el que s'ha escrit s'assembla a un municipi oficial que el cercador no ha trobat
            // (p. ex. «seu d'urgell» sense l'article), es busca pel nom oficial complet
            string clauQ0 = G.Clau(q);
            if (clauQ0.Length >= 3)
            {
                HashSet<string> presents = new HashSet<string>();
                foreach (Dictionary<string, object> x in tots) presents.Add(G.Clau(Nom(x)));
                List<KeyValuePair<string, Municipi>> cands = G.IndexMunicipis()
                    .Where(kv => kv.Key.Contains(clauQ0) && !presents.Contains(kv.Key))
                    .OrderBy(kv => kv.Key == clauQ0 ? 0 : 1).ThenBy(kv => kv.Key.Length).Take(3).ToList();
                foreach (KeyValuePair<string, Municipi> cand in cands)
                {
                    try
                    {
                        object r = G.Web("https://geocoding-api.open-meteo.com/v1/search?count=5&language=ca&format=json&name=" + Uri.EscapeDataString(cand.Value.Nom));
                        Dictionary<string, object> y = J.A(J.G(r, "results")).OfType<Dictionary<string, object>>()
                            .FirstOrDefault(o => J.S(J.G(o, "admin1")) == "Catalunya");
                        if (y != null && vistos.Add(J.S(J.G(y, "id")) ?? "")) tots.Add(y);
                    }
                    catch { }
                }
            }
            foreach (Dictionary<string, object> x in tots)
            {
                if (J.S(J.G(x, "admin1")) != "Catalunya") continue;
                string codi = null;
                Municipi mun = G.MunicipiOficial(Nom(x));
                if (mun != null)
                {
                    // El lloc és un municipi: nom oficial i el seu propi codi
                    x["name"] = mun.Nom;
                    x["admin3"] = mun.Nom;
                    codi = mun.Id;
                }
                else
                {
                    // Barri o nucli: el municipi és el que diu el cercador
                    mun = G.MunicipiOficial(J.S(J.G(x, "admin3")));
                    if (mun != null) { x["admin3"] = mun.Nom; codi = mun.Id; }
                }
                x["codiMeteocat"] = codi;
            }
            string clauQ = G.Clau(q);
            Resultats = tots
                .OrderBy(x => J.S(J.G(x, "admin1")) == "Catalunya" ? 0 : (J.S(J.G(x, "country_code")) == "ES" ? 1 : 2))
                .ThenBy(x => G.Clau(Nom(x)) == clauQ ? 0 : 1)
                .ThenBy(x => -(J.D(J.G(x, "population")) ?? 0))
                .ToList();
            if (Resultats.Count == 0) { Estat.Text = "No he trobat res. Prova-ho sense l'article o amb un altre nom."; return; }
            Estat.Text = Resultats.Count == 1 ? "1 resultat" : Resultats.Count + " resultats";
            foreach (Dictionary<string, object> x in Resultats)
            {
                string nom = Nom(x);
                List<string> parts = new List<string>();
                foreach (string k in new[] { "admin3", "admin2", "admin1", "country" })
                {
                    string v = J.S(J.G(x, k));
                    if (!string.IsNullOrEmpty(v) && v != nom && !parts.Contains(v)) parts.Add(v);
                }
                StackPanel sp = new StackPanel();
                TextBlock t1 = new TextBlock(); t1.Text = nom; t1.FontSize = 14; t1.FontWeight = FontWeights.SemiBold; t1.Foreground = G.Pinzell(P["FG"]);
                TextBlock t2 = new TextBlock(); t2.Text = string.Join(" · ", parts); t2.FontSize = 12; t2.Foreground = G.Pinzell(P["SUB"]); t2.TextTrimming = TextTrimming.CharacterEllipsis;
                sp.Children.Add(t1); sp.Children.Add(t2);
                Llista.Items.Add(sp);
            }
        }

        void Desa()
        {
            int i = Llista.SelectedIndex;
            if (i < 0) return;
            Dictionary<string, object> x = Resultats[i];
            object codi = J.G(x, "codiMeteocat");
            string nom = TNom.Text.Trim(); if (nom.Length == 0) nom = Nom(x);
            Dictionary<string, object> t = new Dictionary<string, object>();
            t["nom"] = nom;
            t["latitud"] = J.Dz(J.G(x, "latitude"));
            t["longitud"] = J.Dz(J.G(x, "longitude"));
            t["codiMeteocat"] = codi;
            Triat = t;
            W.Close();
        }

        public Dictionary<string, object> Mostra()
        {
            W.ShowDialog();
            return Triat;
        }
    }

    // =====================================================================
    //  Diàleg per escriure un text (canviar el nom d'un lloc)
    // =====================================================================
    class DemanaText
    {
        public Window W;
        public TextBox T;
        string R;

        public DemanaText(string titol, string valor)
        {
            Dictionary<string, string> p = G.Paleta();
            W = (Window)G.LlegeixXaml(G.XamlFinestra("DemanaText.xaml", p));
            ((TextBlock)W.FindName("Titol")).Text = titol;
            T = (TextBox)W.FindName("T");
            T.Text = valor;
            ((Button)W.FindName("BNo")).Click += (s, e) => W.Close();
            ((Button)W.FindName("BSi")).Click += (s, e) => { R = T.Text.Trim(); W.Close(); };
            T.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Return) { R = T.Text.Trim(); W.Close(); }
                else if (e.Key == Key.Escape) W.Close();
            };
            W.Loaded += (s, e) => { T.Focus(); T.SelectAll(); };
        }

        public static string Mostra(string titol, string valor)
        {
            DemanaText d = new DemanaText(titol, valor);
            d.W.ShowDialog();
            return d.R;
        }
    }
}
