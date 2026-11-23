// Quina Barra! · versió 1.0 · el temps del teu poble a la barra de tasques, en català
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Blocs del panell que es poden mostrar, amagar i endreçar («Personalitza el panell») i menú ⋯.
// L'ordre es desa a config.json com a "blocs": [ { "id": "...", "visible": true }, ... ].
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace TempsBarra
{
    class InfoBloc
    {
        public string Id, Nom; public bool Ample;
        public InfoBloc(string id, string nom, bool ample) { Id = id; Nom = nom; Ample = ample; }
    }

    class EstatBloc { public string Id; public bool Visible; }

    static partial class G
    {
        // Ordre original. Els blocs amples ocupen tota l'amplada; els petits van de dos en dos.
        static readonly InfoBloc[] TotsBlocs = {
            new InfoBloc("Resum", "Què ens espera", true),
            new InfoBloc("Hores", "Pròximes 24 hores", true),
            new InfoBloc("Dies", "Pròxims 10 dies", true),
            new InfoBloc("Pluja", "Precipitació", false),
            new InfoBloc("Vent", "Vent", false),
            new InfoBloc("Sol", "Sortida i posta del sol", false),
            new InfoBloc("UV", "Índex UV", false),
            new InfoBloc("Aire", "Qualitat de l'aire", false),
            new InfoBloc("Visib", "Visibilitat", false),
            new InfoBloc("Humitat", "Humitat", false),
            new InfoBloc("Pressio", "Pressió", false),
            new InfoBloc("Mar", "Mar (només als llocs de costa)", true),
            new InfoBloc("Radar", "Radar de pluja", true)
        };

        static InfoBloc Info(string id) { return TotsBlocs.FirstOrDefault(b => b.Id == id); }

        public static List<EstatBloc> OrdreBlocs()
        {
            List<EstatBloc> r = new List<EstatBloc>();
            object llista = Conf != null ? J.G(Conf.Arrel, "blocs") : null;
            foreach (object o in J.A(llista))
            {
                string id = J.S(J.G(o, "id"));
                if (Info(id) == null || r.Any(x => x.Id == id)) continue;
                object v = J.G(o, "visible");
                r.Add(new EstatBloc { Id = id, Visible = v == null || J.Truthy(v) });
            }
            // Blocs nous (d'una versió més moderna) o que faltin: al final, visibles
            foreach (InfoBloc b in TotsBlocs)
                if (!r.Any(x => x.Id == b.Id)) r.Add(new EstatBloc { Id = b.Id, Visible = true });
            return r;
        }

        static void DesaOrdreBlocs(List<EstatBloc> ordre)
        {
            List<object> l = new List<object>();
            foreach (EstatBloc e in ordre)
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["id"] = e.Id; d["visible"] = e.Visible;
                l.Add(d);
            }
            Conf.Arrel["blocs"] = l;
            DesaConfig(Conf);
        }

        public static bool BlocVisible(string id)
        {
            foreach (EstatBloc e in OrdreBlocs()) if (e.Id == id) return e.Visible;
            return true;
        }

        static FrameworkElement ElementBloc(string id)
        {
            switch (id)
            {
                case "Resum": return det.F<FrameworkElement>("TResum");
                case "Hores": return det.F<FrameworkElement>("THores");
                case "Dies": return det.F<FrameworkElement>("TDies");
                case "Radar": return det.F<FrameworkElement>("TRadar");
                default:
                    Border b; return det.Rajoles.TryGetValue(id, out b) ? b : null;
            }
        }

        static void Desenganxa(FrameworkElement e)
        {
            Panel pare = e.Parent as Panel;
            if (pare != null) pare.Children.Remove(e);
        }

        // Torna a muntar el panell segons l'ordre i la visibilitat desats
        public static void ColocaBlocs()
        {
            StackPanel cont = det.F<StackPanel>("Blocs");
            if (cont == null) return;
            foreach (InfoBloc b in TotsBlocs) { FrameworkElement e = ElementBloc(b.Id); if (e != null) Desenganxa(e); }
            cont.Children.Clear();

            List<FrameworkElement> pendents = new List<FrameworkElement>();
            Action buida = () =>
            {
                if (pendents.Count == 1) cont.Children.Add(pendents[0]);   // sola: ocupa tota l'amplada
                else if (pendents.Count == 2)
                {
                    Grid fila = new Grid();
                    fila.ColumnDefinitions.Add(new ColumnDefinition());
                    fila.ColumnDefinitions.Add(new ColumnDefinition());
                    Grid.SetColumn(pendents[0], 0); Grid.SetColumn(pendents[1], 1);
                    fila.Children.Add(pendents[0]); fila.Children.Add(pendents[1]);
                    cont.Children.Add(fila);
                }
                pendents.Clear();
            };
            foreach (EstatBloc eb in OrdreBlocs())
            {
                if (!eb.Visible) continue;
                FrameworkElement e = ElementBloc(eb.Id);
                InfoBloc info = Info(eb.Id);
                if (e == null || info == null) continue;
                Grid.SetColumn(e, 0);
                if (info.Ample) { buida(); cont.Children.Add(e); }
                else { pendents.Add(e); if (pendents.Count == 2) buida(); }
            }
            buida();
        }

        // ---------- Pàgina «Personalitza el panell» ----------
        static void CanviaBloc(Action<List<EstatBloc>> canvi, bool calenDades)
        {
            List<EstatBloc> ordre = OrdreBlocs();
            canvi(ordre);
            DesaOrdreBlocs(ordre);
            ColocaBlocs();
            if (calenDades) { cache.Clear(); ActualitzaPanell(false); }
            double off = det.SubDesplaca.VerticalOffset;
            MostraSubpagina("Personalitza");
            det.SubDesplaca.UpdateLayout();
            det.SubDesplaca.ScrollToVerticalOffset(off);
        }

        static void PaginaPersonalitza()
        {
            Dictionary<string, string> p = det.P;
            det.SubTitol.Text = "Personalitza el panell";
            Paragraf("", "Tria quins blocs vols veure i en quin ordre. Les rajoles petites van de dues en dues; si en queda una de sola, ocupa tota l'amplada. El que amaguis ja no es baixa d'internet, i el giny va més lleuger.");
            StackPanel sp = Targeta();
            List<EstatBloc> ordre = OrdreBlocs();
            for (int i = 0; i < ordre.Count; i++)
            {
                EstatBloc eb = ordre[i]; InfoBloc info = Info(eb.Id); int idx = i;
                Grid fila = new Grid(); fila.Margin = new Thickness(0, i == 0 ? 0 : 4, 0, 0);
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                fila.ColumnDefinitions.Add(new ColumnDefinition());
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                CheckBox chk = new CheckBox(); chk.IsChecked = eb.Visible; chk.VerticalAlignment = VerticalAlignment.Center; chk.Margin = new Thickness(0, 0, 10, 0);
                chk.ToolTip = eb.Visible ? "Amaga'l" : "Mostra'l";
                string id = eb.Id;
                bool calen = id == "Aire" || id == "Mar";
                chk.Checked += (s, e) => CanviaBloc(o => o[idx].Visible = true, calen);
                chk.Unchecked += (s, e) => CanviaBloc(o => o[idx].Visible = false, false);
                fila.Children.Add(chk);

                StackPanel noms = new StackPanel(); noms.VerticalAlignment = VerticalAlignment.Center;
                TextBlock tn = Text(info.Nom, 13.5, p["FG"], "SemiBold"); if (!eb.Visible) tn.Opacity = 0.5;
                noms.Children.Add(tn);
                noms.Children.Add(Text(info.Ample ? "tota l'amplada" : "rajola petita", 11.5, p["SUB"]));
                Grid.SetColumn(noms, 1); fila.Children.Add(noms);

                Button amunt = BotoGlif("", "Puja'l"); amunt.IsEnabled = i > 0;
                amunt.Click += (s, e) => CanviaBloc(o => { EstatBloc x = o[idx]; o.RemoveAt(idx); o.Insert(idx - 1, x); }, false);
                Grid.SetColumn(amunt, 2); fila.Children.Add(amunt);
                Button avall = BotoGlif("", "Baixa'l"); avall.IsEnabled = i < ordre.Count - 1;
                avall.Click += (s, e) => CanviaBloc(o => { EstatBloc x = o[idx]; o.RemoveAt(idx); o.Insert(idx + 1, x); }, false);
                Grid.SetColumn(avall, 3); fila.Children.Add(avall);
                sp.Children.Add(fila);
            }
            Button rest = new Button(); rest.Content = "Restableix l'ordre original"; rest.Margin = new Thickness(4, 12, 4, 0);
            rest.HorizontalAlignment = HorizontalAlignment.Left;
            rest.Click += (s, e) => CanviaBloc(o => { o.Clear(); foreach (InfoBloc b in TotsBlocs) o.Add(new EstatBloc { Id = b.Id, Visible = true }); }, true);
            det.SubCos.Children.Add(rest);
        }

        static Button BotoGlif(string glif, string ajuda)
        {
            Button b = new Button();
            b.Style = (Style)det.W.FindResource("Pla");
            b.Content = glif; b.FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
            b.FontSize = 12; b.Padding = new Thickness(10, 7, 10, 7); b.ToolTip = ajuda; b.VerticalAlignment = VerticalAlignment.Center;
            return b;
        }

        // ---------- Menú ⋯ ----------
        static MenuItem OpcioMenu(string glif, string text, Action accio)
        {
            MenuItem mi = new MenuItem();
            // Icona (del joc d'icones del Windows) + text; tota la fila és clicable
            StackPanel fila = new StackPanel(); fila.Orientation = Orientation.Horizontal;
            TextBlock ic = new TextBlock(); ic.Text = glif; ic.FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
            ic.FontSize = 14; ic.Width = 20; ic.Margin = new Thickness(0, 0, 12, 0); ic.VerticalAlignment = VerticalAlignment.Center; ic.TextAlignment = TextAlignment.Center;
            TextBlock tx = new TextBlock(); tx.Text = text; tx.VerticalAlignment = VerticalAlignment.Center;
            // Color del tema explícit: el text del menú no l'hereta de la plantilla
            ic.Foreground = Pinzell(det.P["FG"]); tx.Foreground = Pinzell(det.P["FG"]); tx.FontSize = 13;
            fila.Children.Add(ic); fila.Children.Add(tx);
            mi.Header = fila;
            mi.Style = (Style)det.W.FindResource(typeof(MenuItem));
            mi.Click += (s, e) => accio();
            return mi;
        }

        public static ContextMenu ObreMenuOpcions()
        {
            ContextMenu cm = new ContextMenu();
            cm.Style = (Style)det.W.FindResource(typeof(ContextMenu));
            cm.Items.Add(OpcioMenu("\uE70F", "Personalitza el panell", () => MostraSubpagina("Personalitza")));
            cm.Items.Add(OpcioMenu("\uE946", VersioNova != null ? "Quant a  ·  hi ha una versió nova" : "Quant a", () => MostraSubpagina("QuantA")));
            cm.Items.Add(OpcioMenu("\uE8BB", "Tanca el giny", () => app.Shutdown()));
            cm.Opened += (s, e) => menuObert = true;
            cm.Closed += (s, e) => { menuObert = false; if (det.W.IsVisible) det.W.Activate(); };
            cm.PlacementTarget = det.BQuantA;
            cm.Placement = PlacementMode.Bottom;
            cm.IsOpen = true;
            return cm;
        }
    }
}
