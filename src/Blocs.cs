// Quina Barra! · versió 1.1 · el temps del teu poble a la barra de tasques, en català i altres llengües
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Blocs del panell que es poden mostrar, amagar i endreçar («Personalitza el panell») i menú ⋯.
// L'ordre es desa a config.json com a "blocs": [ { "id": "...", "visible": true }, ... ].
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace TempsBarra
{
    class InfoBloc
    {
        public string Id, Nom; public bool Ample;   // Nom: clau del text
        public InfoBloc(string id, string nom, bool ample) { Id = id; Nom = nom; Ample = ample; }
    }

    class EstatBloc { public string Id; public bool Visible; }

    static partial class G
    {
        // Ordre original. Els blocs amples ocupen tota l'amplada; els petits van de dos en dos.
        static readonly InfoBloc[] TotsBlocs = {
            new InfoBloc("Resum", "bloc-resum", true),
            new InfoBloc("Hores", "bloc-hores", true),
            new InfoBloc("Dies", "bloc-dies", true),
            new InfoBloc("Pluja", "bloc-pluja", false),
            new InfoBloc("Vent", "bloc-vent", false),
            new InfoBloc("Sol", "bloc-sol", false),
            new InfoBloc("UV", "bloc-uv", false),
            new InfoBloc("Aire", "bloc-aire", false),
            new InfoBloc("Visib", "bloc-visibilitat", false),
            new InfoBloc("Humitat", "bloc-humitat", false),
            new InfoBloc("Pressio", "bloc-pressio", false),
            new InfoBloc("Mar", "bloc-mar-personalitza", true),
            new InfoBloc("Radar", "bloc-radar", true)
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
            if (modeEndreca) cont.Children.Insert(0, AvisEndreca());
        }

        // ---------- Mode «Endreça»: arrossegar els blocs al panell mateix ----------
        public static bool modeEndreca;
        static readonly Dictionary<FrameworkElement, string> idBloc = new Dictionary<FrameworkElement, string>();
        static FrameworkElement endArr; static Point endP0; static bool endMou;
        static System.Windows.Threading.DispatcherTimer endTimer;

        // Cada bloc del panell s'apunta als esdeveniments del ratolí (només fan res en mode «Endreça»)
        static void PreparaEndreca()
        {
            idBloc.Clear();
            foreach (InfoBloc b in TotsBlocs)
            {
                FrameworkElement e = ElementBloc(b.Id);
                if (e == null) continue;
                idBloc[e] = b.Id;
                e.PreviewMouseLeftButtonDown += (s, ev) =>
                {
                    if (!modeEndreca) return;
                    endArr = (FrameworkElement)s; endMou = false;
                    endP0 = ev.GetPosition(det.F<StackPanel>("Blocs"));
                    endArr.CaptureMouse(); ev.Handled = true;
                };
                e.PreviewMouseMove += (s, ev) => { if (endArr == s) EndMou(); };
                e.PreviewMouseLeftButtonUp += (s, ev) => { if (endArr == s) { ev.Handled = true; EndAmunt(); } };
                e.LostMouseCapture += (s, ev) => { if (endArr == s) { endArr = null; if (endMou) ColocaBlocs(); } };
            }
        }

        public static void ModeEndreca(bool si)
        {
            modeEndreca = si;
            AmagaSubpagina();
            foreach (KeyValuePair<FrameworkElement, string> kv in idBloc)
            {
                kv.Key.Cursor = si ? Cursors.SizeAll : (det.Rajoles.ContainsKey(kv.Value) ? Cursors.Hand : null);
                Border bd = kv.Key as Border;
                if (bd != null) bd.BorderBrush = Pinzell(si ? det.P["ACCENT"] : det.P["BORDER"]);
            }
            ColocaBlocs();
            if (si) det.Desplaca.ScrollToTop();
        }

        static FrameworkElement AvisEndreca()
        {
            Dictionary<string, string> p = det.P;
            Border b = new Border(); b.Background = Pinzell(p["SEL"]); b.BorderBrush = Pinzell(p["ACCENT"]); b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(8); b.Margin = new Thickness(4); b.Padding = new Thickness(14, 10, 10, 10);
            Grid gr = new Grid(); gr.ColumnDefinitions.Add(new ColumnDefinition()); gr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBlock t = Text(T("endreca-ajuda"), 13, p["FG"]); t.TextWrapping = TextWrapping.Wrap; t.VerticalAlignment = VerticalAlignment.Center; gr.Children.Add(t);
            Button fet = new Button(); fet.Style = (Style)det.W.FindResource("Accent"); fet.Content = T("endreca-fet"); fet.Margin = new Thickness(10, 0, 0, 0);
            fet.Click += (s, e) => ModeEndreca(false);
            Grid.SetColumn(fet, 1); gr.Children.Add(fet);
            b.Child = gr;
            return b;
        }

        static void EndMou()
        {
            if (endArr == null || Mouse.LeftButton != MouseButtonState.Pressed) return;
            StackPanel cont = det.F<StackPanel>("Blocs");
            Point pt = Mouse.GetPosition(cont);
            double dx = pt.X - endP0.X, dy = pt.Y - endP0.Y;
            if (!endMou)
            {
                if (Math.Abs(dx) < 6 && Math.Abs(dy) < 6) return;
                endMou = true;
                endArr.Opacity = 0.8;
                Panel.SetZIndex(endArr, 10);
                UIElement pare = endArr.Parent as UIElement;
                if (pare != null && pare != cont) Panel.SetZIndex(pare, 10);
                if (endTimer == null)
                {
                    // A prop de dalt o de baix del panell, es desplaça sol
                    endTimer = new System.Windows.Threading.DispatcherTimer();
                    endTimer.Interval = TimeSpan.FromMilliseconds(30);
                    endTimer.Tick += (s, e) =>
                    {
                        if (endArr == null || !endMou) { endTimer.Stop(); return; }
                        ScrollViewer sv = det.Desplaca;
                        double ys = Mouse.GetPosition(sv).Y;
                        if (ys < 40) sv.ScrollToVerticalOffset(sv.VerticalOffset - 12);
                        else if (ys > sv.ViewportHeight - 40) sv.ScrollToVerticalOffset(sv.VerticalOffset + 12);
                        else return;
                        EndMou();
                    };
                }
                endTimer.Start();
            }
            endArr.RenderTransform = new TranslateTransform(dx, dy);
        }

        static void EndAmunt()
        {
            FrameworkElement f = endArr; bool mogut = endMou;
            endArr = null;
            if (endTimer != null) endTimer.Stop();
            f.ReleaseMouseCapture();
            if (!mogut) return;
            StackPanel cont = det.F<StackPanel>("Blocs");
            Point pt = Mouse.GetPosition(cont);
            string idArr = idBloc[f];
            // El bloc de sota el ratolí (o el més proper en vertical) i si va abans o després
            string idObj = null; bool despres = false; double millor = double.MaxValue;
            foreach (KeyValuePair<FrameworkElement, string> kv in idBloc)
            {
                FrameworkElement o = kv.Key;
                if (o == f || !o.IsVisible || !BlocVisible(kv.Value)) continue;
                Point a = o.TranslatePoint(new Point(0, 0), cont);
                Rect r = new Rect(a, new Size(o.ActualWidth, o.ActualHeight));
                double dist = r.Contains(pt) ? 0 : Math.Min(Math.Abs(pt.Y - r.Top), Math.Abs(pt.Y - r.Bottom)) + (pt.X >= r.Left && pt.X <= r.Right ? 0 : 1000);
                if (dist < millor)
                {
                    millor = dist; idObj = kv.Value;
                    bool ample = Info(kv.Value).Ample;
                    despres = ample ? pt.Y > r.Top + r.Height / 2 : (pt.X > r.Left + r.Width / 2 || pt.Y > r.Bottom);
                }
            }
            if (idObj == null) { ColocaBlocs(); return; }
            List<EstatBloc> ordre = OrdreBlocs();
            EstatBloc x = ordre.First(e => e.Id == idArr);
            ordre.Remove(x);
            int i = ordre.FindIndex(e => e.Id == idObj);
            ordre.Insert(Math.Min(ordre.Count, i + (despres ? 1 : 0)), x);
            DesaOrdreBlocs(ordre);
            f.Opacity = 1; f.RenderTransform = null; Panel.SetZIndex(f, 0);
            ColocaBlocs();
        }

        // ---------- Pàgina «Configuració»: llengua i «Personalitza el panell» ----------
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
            det.SubTitol.Text = T("configuracio-titol");
            SeccioLlengua();
            TextBlock tp = Text(T("personalitza-titol"), 15, p["FG"], "SemiBold"); tp.Margin = new Thickness(6, 18, 6, 0);
            det.SubCos.Children.Add(tp);
            Paragraf("", T("personalitza-explicacio"));
            StackPanel sp = Targeta();
            List<EstatBloc> ordre = OrdreBlocs();
            for (int i = 0; i < ordre.Count; i++)
            {
                EstatBloc eb = ordre[i]; InfoBloc info = Info(eb.Id); int idx = i;
                Grid fila = new Grid(); fila.Margin = new Thickness(0, i == 0 ? 0 : 4, 0, 0); fila.Tag = idx; fila.Background = Pinzell("#01000000");
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                fila.ColumnDefinitions.Add(new ColumnDefinition());

                // Tres ratlles: s'arrossega la fila amunt o avall
                Border nansa = new Border(); nansa.Background = Pinzell("#01000000"); nansa.Padding = new Thickness(2, 6, 12, 6); nansa.Cursor = Cursors.SizeNS;
                nansa.VerticalAlignment = VerticalAlignment.Center; nansa.ToolTip = T("personalitza-arrossega");
                TextBlock gl = Text(((char)0xE700).ToString(), 14, p["SUB"]); gl.FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
                nansa.Child = gl; fila.Children.Add(nansa);
                Grid filaG = fila;
                nansa.PreviewMouseLeftButtonDown += (s, e) => { filaArr = filaG; filaSp = sp; filaY0 = e.GetPosition(sp).Y; filaMou = false; filaG.CaptureMouse(); e.Handled = true; };
                fila.MouseMove += (s, e) => FilaMou();
                fila.PreviewMouseLeftButtonUp += (s, e) => { if (filaArr == s) { e.Handled = true; FilaAmunt(); } };
                fila.LostMouseCapture += (s, e) => { if (filaArr == s) { filaArr = null; if (filaMou) MostraSubpagina("Personalitza"); } };
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                CheckBox chk = new CheckBox(); chk.IsChecked = eb.Visible; chk.VerticalAlignment = VerticalAlignment.Center; chk.Margin = new Thickness(0, 0, 10, 0);
                Grid.SetColumn(chk, 1);
                chk.ToolTip = eb.Visible ? T("personalitza-amaga") : T("personalitza-mostra");
                string id = eb.Id;
                bool calen = id == "Aire" || id == "Mar";
                chk.Checked += (s, e) => CanviaBloc(o => o[idx].Visible = true, calen);
                chk.Unchecked += (s, e) => CanviaBloc(o => o[idx].Visible = false, false);
                fila.Children.Add(chk);

                StackPanel noms = new StackPanel(); noms.VerticalAlignment = VerticalAlignment.Center;
                TextBlock tn = Text(T(info.Nom), 13.5, p["FG"], "SemiBold"); if (!eb.Visible) tn.Opacity = 0.5;
                noms.Children.Add(tn);
                noms.Children.Add(Text(info.Ample ? T("personalitza-ample") : T("personalitza-petita"), 11.5, p["SUB"]));
                Grid.SetColumn(noms, 2); fila.Children.Add(noms);

                Button amunt = BotoGlif("", T("personalitza-puja")); amunt.IsEnabled = i > 0;
                amunt.Click += (s, e) => CanviaBloc(o => { EstatBloc x = o[idx]; o.RemoveAt(idx); o.Insert(idx - 1, x); }, false);
                Grid.SetColumn(amunt, 3); fila.Children.Add(amunt);
                Button avall = BotoGlif("", T("personalitza-baixa")); avall.IsEnabled = i < ordre.Count - 1;
                avall.Click += (s, e) => CanviaBloc(o => { EstatBloc x = o[idx]; o.RemoveAt(idx); o.Insert(idx + 1, x); }, false);
                Grid.SetColumn(avall, 4); fila.Children.Add(avall);
                sp.Children.Add(fila);
            }
            Button rest = new Button(); rest.Content = T("personalitza-restableix"); rest.Margin = new Thickness(4, 12, 4, 0);
            rest.HorizontalAlignment = HorizontalAlignment.Left;
            rest.Click += (s, e) => CanviaBloc(o => { o.Clear(); foreach (InfoBloc b in TotsBlocs) o.Add(new EstatBloc { Id = b.Id, Visible = true }); }, true);
            det.SubCos.Children.Add(rest);
        }

        // ---------- Arrossegar les files de «Personalitza el panell» ----------
        static Grid filaArr; static StackPanel filaSp; static double filaY0; static bool filaMou;
        static System.Windows.Threading.DispatcherTimer filaTimer;

        static void FilaMou()
        {
            if (filaArr == null || Mouse.LeftButton != MouseButtonState.Pressed) return;
            double dy = Mouse.GetPosition(filaSp).Y - filaY0;
            if (!filaMou)
            {
                if (Math.Abs(dy) < 4) return;
                filaMou = true;
                filaArr.Opacity = 0.85; Panel.SetZIndex(filaArr, 10); filaArr.Background = Pinzell(det.P["HOVER"]);
                if (filaTimer == null)
                {
                    // A prop de dalt o de baix, la pàgina es desplaça sola
                    filaTimer = new System.Windows.Threading.DispatcherTimer();
                    filaTimer.Interval = TimeSpan.FromMilliseconds(30);
                    filaTimer.Tick += (s, e) =>
                    {
                        if (filaArr == null || !filaMou) { filaTimer.Stop(); return; }
                        ScrollViewer sv = det.SubDesplaca;
                        double ys = Mouse.GetPosition(sv).Y;
                        if (ys < 30) sv.ScrollToVerticalOffset(sv.VerticalOffset - 10);
                        else if (ys > sv.ViewportHeight - 30) sv.ScrollToVerticalOffset(sv.VerticalOffset + 10);
                        else return;
                        FilaMou();
                    };
                }
                filaTimer.Start();
            }
            filaArr.RenderTransform = new TranslateTransform(0, dy);
        }

        static void FilaAmunt()
        {
            Grid f = filaArr; bool mogut = filaMou; StackPanel sp = filaSp;
            filaArr = null;
            if (filaTimer != null) filaTimer.Stop();
            f.ReleaseMouseCapture();
            if (!mogut) return;
            int de = (int)f.Tag;
            double centre = f.TranslatePoint(new Point(0, f.ActualHeight / 2), sp).Y;
            int a = 0;
            foreach (UIElement c in sp.Children)
            {
                Grid o = c as Grid;
                if (o == null || o == f || !(o.Tag is int)) continue;
                if (o.TranslatePoint(new Point(0, o.ActualHeight / 2), sp).Y < centre) a++;
            }
            if (a == de) { MostraSubpagina("Personalitza"); return; }
            CanviaBloc(o => { EstatBloc x = o[de]; o.RemoveAt(de); o.Insert(Math.Min(a, o.Count), x); }, false);
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
            cm.Items.Add(OpcioMenu("\uE713", T("configuracio-titol"), () => MostraSubpagina("Personalitza")));
            cm.Items.Add(OpcioMenu("\uE946", VersioNova != null ? T("menu-quant-a-versio-nova") : T("menu-quant-a"), () => MostraSubpagina("QuantA")));
            cm.Items.Add(OpcioMenu("\uE8BB", T("menu-tanca"), () => app.Shutdown()));
            cm.Opened += (s, e) => menuObert = true;
            cm.Closed += (s, e) => { menuObert = false; if (det.W.IsVisible) det.W.Activate(); };
            cm.PlacementTarget = det.BQuantA;
            cm.Placement = PlacementMode.Bottom;
            cm.IsOpen = true;
            return cm;
        }
    }
}
