// Quina Barra! · versió 1.2 · el temps del teu poble a la barra de tasques, en català i altres llengües
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Finestra de detall: capçalera, llocs desats, ara, resum, hores, dies, rajoles, mar i radar.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace TempsBarra
{
    // Elements amb nom de la finestra de detall
    class Det
    {
        public Window W; public Dictionary<string, string> P;
        public TextBlock FLloc, FHora, FTemp, FDesc, FSensacio, FMaxMin, FResum, MapaAvis, MapaHora, FFont, SubTitol;
        public Button BActualitza, BHEsq, BHDre, BPlay, BMeteocat, BUbicacio, BSurt, BTemaAuto, BTemaClar, BTemaFosc, BEnrere, BQuantA, BPEsq, BPDre;
        public ScrollViewer Desplaca, HoresScroll, SubDesplaca, PastScroll;
        public Viewbox FIcona;
        public StackPanel Hores, Dies, Marques, SubCos, Pastilles, PastFletxes;
        public Grid RPluja, RVent, RSol, RUV, RAire, RVisib, RHumitat, RPressio, RMar, Mapa, Sub;
        public Border MapaVora, TMar;
        public Canvas MapaBase, MapaRadar, MapaPin;
        public Dictionary<string, Border> Rajoles = new Dictionary<string, Border>();

        public T F<T>(string nom) where T : class { return W.FindName(nom) as T; }
    }

    class ElGrafic
    {
        public double Frac; public string Color; public object Dalt; public string[] Sota;
        public ElGrafic(double frac, string color, object dalt, params string[] sota) { Frac = frac; Color = color; Dalt = dalt; Sota = sota; }
    }

    static partial class G
    {
        public static Det det;
        public static Dades dades;
        public static string ultimaSub;
        public static bool dialeg, menuObert;
        public static DateTime? tancatA;
        public static Application app;

        static string Rajola(string nom, string glif, string titol)
        {
            // Targeta petita: glif + títol a dalt, contingut a sota (x:Name = R<nom>)
            return "<Border x:Name=\"T" + nom + "\" Background=\"@CARD@\" BorderBrush=\"@BORDER@\" BorderThickness=\"1\" CornerRadius=\"8\" Margin=\"4\" Padding=\"14,12\" Height=\"150\" Cursor=\"Hand\" ToolTip=\"" + XmlText(T("rajola-ajuda")) + "\">\n" +
                "  <DockPanel>\n" +
                "    <StackPanel DockPanel.Dock=\"Top\" Orientation=\"Horizontal\" Margin=\"0,0,0,6\" HorizontalAlignment=\"Center\">\n" +
                "      <Viewbox Width=\"14\" Height=\"14\" VerticalAlignment=\"Center\" Margin=\"0,0,7,0\" FlowDirection=\"LeftToRight\"><Path Width=\"14\" Height=\"14\" Data=\"" + glif + "\" Stroke=\"@SUB@\" StrokeThickness=\"1.3\" StrokeStartLineCap=\"Round\" StrokeEndLineCap=\"Round\" StrokeLineJoin=\"Round\"/></Viewbox>\n" +
                "      <TextBlock Text=\"" + XmlText(titol) + "\" FontSize=\"12.5\" Foreground=\"@SUB@\" VerticalAlignment=\"Center\"/>\n" +
                "      <TextBlock Text=\"" + FletxaEndavant() + "\" FontFamily=\"Segoe Fluent Icons, Segoe MDL2 Assets\" FontSize=\"9\" Foreground=\"@SUB@\" VerticalAlignment=\"Center\" Margin=\"6,1,0,0\" Opacity=\"0.7\"/>\n" +
                "    </StackPanel>\n" +
                "    <Grid x:Name=\"R" + nom + "\"/>\n" +
                "  </DockPanel>\n" +
                "</Border>\n";
        }

        public static Det NovaFinestraDetall()
        {
            Dictionary<string, string> p = Paleta();
            string rajoles = Rajola("Pluja", "M7,1.5 C7,1.5 2.5,7 2.5,9.3 A4.5,4.5 0 0 0 11.5,9.3 C11.5,7 7,1.5 7,1.5 Z", T("bloc-pluja")) +
                Rajola("Vent", "M1,5 H9.5 A2,2 0 1 0 7.5,3 M1,9 H11 A2,2 0 1 1 9,11", T("bloc-vent")) +
                Rajola("Sol", "M3,10 A4,4 0 0 1 11,10 M1,12.5 H13 M7,2 V4 M2.8,5.3 L4,6.5 M11.2,5.3 L10,6.5", T("rajola-sol")) +
                Rajola("UV", "M7,4.5 A2.5,2.5 0 1 1 6.99,4.5 Z M7,0.8 V2.3 M7,11.7 V13.2 M0.8,7 H2.3 M11.7,7 H13.2 M2.6,2.6 L3.7,3.7 M10.3,10.3 L11.4,11.4 M2.6,11.4 L3.7,10.3 M10.3,3.7 L11.4,2.6", T("bloc-uv")) +
                Rajola("Aire", "M1,4 C3,2.5 5,2.5 7,4 C9,5.5 11,5.5 13,4 M1,8 C3,6.5 5,6.5 7,8 C9,9.5 11,9.5 13,8 M1,12 C3,10.5 5,10.5 7,12 C9,13.5 11,13.5 13,12", T("bloc-aire")) +
                Rajola("Visib", "M1,7 C3,3 11,3 13,7 C11,11 3,11 1,7 Z M7,5 A2,2 0 1 1 6.99,5 Z", T("bloc-visibilitat")) +
                Rajola("Humitat", "M7,1.5 C7,1.5 2.5,7 2.5,9.3 A4.5,4.5 0 0 0 11.5,9.3 C11.5,7 7,1.5 7,1.5 Z M5,10 A2,2 0 0 0 7,12", T("bloc-humitat")) +
                Rajola("Pressio", "M2,11 A5.5,5.5 0 1 1 12,11 M7,8 L9.5,5", T("bloc-pressio"));
            string xaml = Recurs("Detall.xaml").Replace("@ESTILS@", Recurs("Estils.xaml")).Replace("@RAJOLES@", rajoles);
            Window w = (Window)LlegeixXaml(AmbPaleta(xaml, p));
            Direccio(w);
            Det g = new Det();
            g.W = w; g.P = p;
            g.FLloc = g.F<TextBlock>("FLloc"); g.FHora = g.F<TextBlock>("FHora"); g.BActualitza = g.F<Button>("BActualitza");
            g.Desplaca = g.F<ScrollViewer>("Desplaca"); g.FIcona = g.F<Viewbox>("FIcona"); g.FTemp = g.F<TextBlock>("FTemp");
            g.FDesc = g.F<TextBlock>("FDesc"); g.FSensacio = g.F<TextBlock>("FSensacio"); g.FMaxMin = g.F<TextBlock>("FMaxMin");
            g.FResum = g.F<TextBlock>("FResum"); g.BHEsq = g.F<Button>("BHEsq"); g.BHDre = g.F<Button>("BHDre");
            g.HoresScroll = g.F<ScrollViewer>("HoresScroll"); g.Hores = g.F<StackPanel>("Hores"); g.Dies = g.F<StackPanel>("Dies");
            g.RPluja = g.F<Grid>("RPluja"); g.RVent = g.F<Grid>("RVent"); g.RSol = g.F<Grid>("RSol"); g.RUV = g.F<Grid>("RUV");
            g.RAire = g.F<Grid>("RAire"); g.RVisib = g.F<Grid>("RVisib"); g.RHumitat = g.F<Grid>("RHumitat"); g.RPressio = g.F<Grid>("RPressio");
            g.MapaVora = g.F<Border>("MapaVora"); g.Mapa = g.F<Grid>("Mapa"); g.MapaBase = g.F<Canvas>("MapaBase");
            g.MapaRadar = g.F<Canvas>("MapaRadar"); g.MapaPin = g.F<Canvas>("MapaPin"); g.MapaAvis = g.F<TextBlock>("MapaAvis");
            g.BPlay = g.F<Button>("BPlay"); g.Marques = g.F<StackPanel>("Marques"); g.MapaHora = g.F<TextBlock>("MapaHora");
            g.BMeteocat = g.F<Button>("BMeteocat"); g.BUbicacio = g.F<Button>("BUbicacio"); g.FFont = g.F<TextBlock>("FFont");
            g.BSurt = g.F<Button>("BSurt"); g.BTemaAuto = g.F<Button>("BTemaAuto"); g.BTemaClar = g.F<Button>("BTemaClar"); g.BTemaFosc = g.F<Button>("BTemaFosc");
            g.Sub = g.F<Grid>("Sub"); g.BEnrere = g.F<Button>("BEnrere"); g.SubTitol = g.F<TextBlock>("SubTitol");
            g.SubDesplaca = g.F<ScrollViewer>("SubDesplaca"); g.SubCos = g.F<StackPanel>("SubCos"); g.BQuantA = g.F<Button>("BQuantA");
            g.TMar = g.F<Border>("TMar"); g.RMar = g.F<Grid>("RMar");
            g.PastScroll = g.F<ScrollViewer>("PastScroll"); g.Pastilles = g.F<StackPanel>("Pastilles"); g.PastFletxes = g.F<StackPanel>("PastFletxes");
            g.BPEsq = g.F<Button>("BPEsq"); g.BPDre = g.F<Button>("BPDre");

            // Dreta a esquerra: les fletxes de les pastilles i la d'enrere es giren; el carrusel d'hores, el mapa i les marques del radar no
            if (Dreta)
            {
                g.BPEsq.Content = FletxaEnrere(); g.BPDre.Content = FletxaEndavant();
                g.BEnrere.Content = ((char)0xE72A).ToString();
            }
            Fixa(g.HoresScroll); Fixa(g.BHEsq.Parent as FrameworkElement); Fixa(g.MapaVora); Fixa(g.Marques);

            // Rajoles clicables: obren la seva pàgina de detall
            foreach (string nom in new[] { "Pluja", "Vent", "Sol", "UV", "Aire", "Visib", "Humitat", "Pressio", "Mar" })
            {
                Border r = g.F<Border>("T" + nom);
                r.Tag = nom;
                g.Rajoles[nom] = r;
                r.MouseLeftButtonUp += (s, e) => { if (!modeEndreca) MostraSubpagina((string)((Border)s).Tag); };
                r.MouseEnter += (s, e) => ((Border)s).BorderBrush = Pinzell(det.P["SUB"]);
                r.MouseLeave += (s, e) => ((Border)s).BorderBrush = Pinzell(det.P["BORDER"]);
            }
            g.BEnrere.Click += (s, e) => AmagaSubpagina();
            g.BQuantA.Click += (s, e) => ObreMenuOpcions();
            g.F<Button>("BPersonalitza").Click += (s, e) => MostraSubpagina("Personalitza");
            g.F<Button>("BEndreca").Click += (s, e) => ModeEndreca(true);

            // Botó del mode actiu, ressaltat
            string tema = Conf.Tema;
            string actiu = (tema == "clar" || tema == "fosc") ? tema : "auto";
            foreach (KeyValuePair<string, Button> par in new[] {
                new KeyValuePair<string, Button>("auto", g.BTemaAuto), new KeyValuePair<string, Button>("clar", g.BTemaClar), new KeyValuePair<string, Button>("fosc", g.BTemaFosc) })
            {
                if (par.Key == actiu) { par.Value.Background = Pinzell(p["SEL"]); par.Value.Foreground = Pinzell(p["ACCENT"]); }
            }
            g.BTemaAuto.Click += (s, e) => CanviaTema("auto");
            g.BTemaClar.Click += (s, e) => CanviaTema("clar");
            g.BTemaFosc.Click += (s, e) => CanviaTema("fosc");

            w.Deactivated += (s, e) =>
            {
                if (dialeg || menuObert) return;
                if (modeEndreca) ModeEndreca(false);
                det.W.Hide(); tancatA = DateTime.Now; AturaRadar(); AmagaSubpagina();
            };
            w.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { if (det.Sub.Visibility == Visibility.Visible) AmagaSubpagina(); else det.W.Hide(); }
                // Alt+← / Alt+→ i les tecles de navegador, com al Chrome
                else if (e.Key == Key.BrowserBack || (e.Key == Key.System && e.SystemKey == Key.Left)) { Navega(false); e.Handled = true; }
                else if (e.Key == Key.BrowserForward || (e.Key == Key.System && e.SystemKey == Key.Right)) { Navega(true); e.Handled = true; }
            };
            // Botons laterals del ratolí: enrere (XButton1) i endavant (XButton2)
            w.PreviewMouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.XButton1) { Navega(false); e.Handled = true; }
                else if (e.ChangedButton == MouseButton.XButton2) { Navega(true); e.Handled = true; }
            };
            g.BActualitza.Click += (s, e) => { ActualitzaTemps(true); if (BlocVisible("Radar")) InicialitzaMapa(true); MouDetall(); };
            g.BMeteocat.Click += (s, e) =>
            {
                Dictionary<string, object> l = LlocVist(); det.W.Hide();
                if (Codi(l) != null) Obre("https://www.meteo.cat/prediccio/municipal/" + Codi(l));
            };
            g.BUbicacio.Click += (s, e) => AfegeixLloc();
            g.PastScroll.PreviewMouseWheel += RodaHoritzontal;
            g.BPEsq.Click += (s, e) => { ScrollViewer sv = det.PastScroll; sv.ScrollToHorizontalOffset(sv.HorizontalOffset - 200); };
            g.BPDre.Click += (s, e) => { ScrollViewer sv = det.PastScroll; sv.ScrollToHorizontalOffset(sv.HorizontalOffset + 200); };
            g.FFont.MouseLeftButtonUp += (s, e) => Obre("https://open-meteo.com/");
            g.BSurt.Click += (s, e) => app.Shutdown();

            // Carrusel d'hores: roda del ratolí i fletxes
            g.HoresScroll.PreviewMouseWheel += RodaHoritzontal;
            g.BHEsq.Click += (s, e) => { ScrollViewer sv = det.HoresScroll; sv.ScrollToHorizontalOffset(sv.HorizontalOffset - 280); };
            g.BHDre.Click += (s, e) => { ScrollViewer sv = det.HoresScroll; sv.ScrollToHorizontalOffset(sv.HorizontalOffset + 280); };

            // Radar
            g.BPlay.Click += (s, e) => { if (radarTimer.IsEnabled) AturaRadar(); else EngegaRadar(); };
            g.MapaVora.MouseLeftButtonUp += (s, e) =>
            {
                Dictionary<string, object> c = LlocVist();
                string url = Codi(c) != null ? "https://www.meteo.cat/observacions/radar"
                    : string.Format("https://www.rainviewer.com/map.html?loc={0},{1},8", Lat(c).ToString(Inv), Lon(c).ToString(Inv));
                Obre(url);
            };
            Det anterior = det; det = g;
            ColocaBlocs();
            PreparaEndreca();
            det = anterior ?? g;
            return g;
        }

        static void RodaHoritzontal(object s, MouseWheelEventArgs e)
        {
            ScrollViewer sv = (ScrollViewer)s;
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        public static void Obre(string url)
        {
            try { Process.Start(url); } catch (Exception ex) { Registre("No s'ha pogut obrir " + url + ": " + ex.Message); }
        }

        // ---------- Rajoles ----------
        static void ActualitzaMar(Dades d)
        {
            Det g = det; Dictionary<string, string> p = g.P; Mar m = d.Mar;
            if (m == null) { g.TMar.Visibility = Visibility.Collapsed; return; }
            g.TMar.Visibility = Visibility.Visible;
            g.RMar.Children.Clear();
            UniformGrid ug = new UniformGrid(); ug.Columns = 3;
            string[][] blocs = {
                new[] { T("mar-onades"), m.Alcada.ToString("0.0", Ca) + " m", m.Estat },
                new[] { T("mar-direccio", PuntCardinal(m.Dir)), N(Math.Round(m.Periode)) + " s", T("mar-fons", m.FonsAlcada.ToString("0.0", Ca)) },
                new[] { T("mar-aigua"), m.Aigua != null ? m.Aigua.Value.ToString("0.#", Ca) + " °C" : "--", T("mar-temperatura") }
            };
            foreach (string[] b in blocs)
            {
                StackPanel sp = new StackPanel(); sp.HorizontalAlignment = HorizontalAlignment.Center;
                TextBlock t0 = Text(b[0], 12.5, p["SUB"]), t1 = Text(b[1], 24, p["FG"], "SemiBold"), t2 = Text(b[2], 12.5, p["SUB"]);
                foreach (TextBlock tx in new[] { t0, t1, t2 }) { tx.HorizontalAlignment = HorizontalAlignment.Center; sp.Children.Add(tx); }
                ug.Children.Add(sp);
            }
            StackPanel tot = new StackPanel();
            tot.Children.Add(ug);
            if (d.Platges != null && d.Platges.Llista.Count > 0)
            {
                Platja pl = d.Platges.Llista[0];
                StackPanel fila = new StackPanel(); fila.Orientation = Orientation.Horizontal; fila.HorizontalAlignment = HorizontalAlignment.Center; fila.Margin = new Thickness(0, 10, 0, 0);
                Ellipse pt = new Ellipse(); pt.Width = 10; pt.Height = 10; pt.VerticalAlignment = VerticalAlignment.Center; pt.Margin = new Thickness(0, 0, 7, 0);
                string col; if (pl.Bandera == null || !BanderaColors.TryGetValue(pl.Bandera, out col)) col = p["SUB"];
                pt.Fill = Pinzell(col);
                fila.Children.Add(pt);
                string txt = d.Platges.EnTemporada ? T(!string.IsNullOrEmpty(pl.Meduses) ? "platja-resum-meduses" : "platja-resum", pl.Nom, NomBandera(pl.Bandera))
                    : T("platja-fora-temporada", d.Platges.Data.ToString("d/M", Inv));
                fila.Children.Add(Text(txt, 12.5, p["SUB"]));
                tot.Children.Add(fila);
            }
            g.RMar.Children.Add(tot);
        }

        static readonly object[][] NivellsUV = {
            new object[] { 0, "uv-0", "#3EA72D" }, new object[] { 3, "uv-1", "#E8D500" }, new object[] { 6, "uv-2", "#F18B00" },
            new object[] { 8, "uv-3", "#E53210" }, new object[] { 11, "uv-4", "#B567A4" } };   // llindar, clau del text, color
        static int NivellUV(double u)
        {
            int k = 0;
            for (int i = 0; i < 5; i++) if (Math.Round(u) >= (int)NivellsUV[i][0]) k = i;
            return k;
        }

        static void ActualitzaRajoles(Dades d)
        {
            Det g = det; Dictionary<string, string> p = g.P;
            ActualitzaMar(d);

            // Precipitació
            Contingut(g.RPluja,
                Text((d.PlujaAvui ?? 0).ToString("0.#", Ca) + " mm", 26, p["FG"], "SemiBold"),
                Text(T("pluja-total-avui"), 12.5, p["SUB"]),
                Text(T("pluja-probabilitat-maxima", Num(d.PlujaProb)), 12.5, p["SUB"]));

            // Vent: brúixola amb fletxa cap on va el vent
            g.RVent.Children.Clear();
            Grid gr = new Grid(); gr.HorizontalAlignment = HorizontalAlignment.Center; gr.VerticalAlignment = VerticalAlignment.Center;
            gr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); gr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Canvas cv = Fixa(new Canvas()); cv.Width = 70; cv.Height = 70; cv.Margin = new Thickness(0, 0, 12, 0);
            Ellipse cer = new Ellipse(); cer.Width = 62; cer.Height = 62; cer.Stroke = Pinzell(p["BORDER"]); cer.StrokeThickness = 2;
            Canvas.SetLeft(cer, 4); Canvas.SetTop(cer, 4); cv.Children.Add(cer);
            foreach (object[] l in new[] { new object[] { T("punt-0"), 31.0, -3.0 }, new object[] { T("punt-4"), 64.0, 26.0 }, new object[] { T("punt-8"), 31.5, 55.0 }, new object[] { T("punt-12"), -3.0, 26.0 } })
            {
                TextBlock tl = Text((string)l[0], 10, p["SUB"], "SemiBold"); Canvas.SetLeft(tl, (double)l[1]); Canvas.SetTop(tl, (double)l[2]); cv.Children.Add(tl);
            }
            Path fl = Cami("M35,12 L42,34 L35,30 L28,34 Z M35,30 L35,58", p["ACCENT"], 2.5, p["ACCENT"]);
            fl.RenderTransform = new RotateTransform(((d.VentDir ?? 0) + 180) % 360, 35, 35);
            cv.Children.Add(fl);
            gr.Children.Add(cv);
            StackPanel spv = new StackPanel(); spv.VerticalAlignment = VerticalAlignment.Center;
            spv.Children.Add(Text(Graus(d.Vent) + " km/h", 22, p["FG"], "SemiBold"));
            spv.Children.Add(Text(T("vent-des-de", PuntCardinal(d.VentDir ?? 0)), 12.5, p["SUB"]));
            spv.Children.Add(Text(T("vent-ratxes", Graus(d.Ratxes)), 12.5, p["SUB"]));
            Grid.SetColumn(spv, 1); gr.Children.Add(spv);
            g.RVent.Children.Add(gr);

            // Sortida i posta: corba del sol al llarg del dia
            g.RSol.Children.Clear();
            {
                double W = 176.0, H = 58.0, hz = 34.0;
                double L = (d.Posta - d.Sortida).TotalHours; DateTime migdia = d.Sortida.AddHours(L / 2);
                double bas = Math.Cos(Math.PI * L / 24);
                double esc = 26 / (1 - bas);
                double escNit = 18 / (1 + bas);
                Func<double, double> alcada = hores => Math.Cos(2 * Math.PI * (hores - migdia.TimeOfDay.TotalHours) / 24) - bas;
                Func<double, double> posY = hh => hh >= 0 ? hz - esc * hh : hz - escNit * hh;
                List<string> punts = new List<string>();
                for (int x = 0; x <= W; x += 4) { double hh = x / W * 24; punts.Add(x.ToString("0.#", Inv) + "," + posY(alcada(hh)).ToString("0.#", Inv)); }
                Canvas cvS = Fixa(new Canvas()); cvS.Width = W; cvS.Height = H; cvS.HorizontalAlignment = HorizontalAlignment.Center;
                cvS.Children.Add(Cami("M " + string.Join(" L ", punts), p["SUB"], 1.6));
                cvS.Children.Add(Cami("M0," + N(hz) + " L" + N(W) + "," + N(hz), p["BORDER"], 1.2));
                double ara = d.Ara.TimeOfDay.TotalHours;
                double sx = ara / 24 * W, sy = posY(alcada(ara));
                Ellipse sol = new Ellipse(); sol.Width = 12; sol.Height = 12; sol.Fill = Pinzell("#FFC83D");
                if (!d.Dia) sol.Opacity = 0.45;
                Canvas.SetLeft(sol, sx - 6); Canvas.SetTop(sol, sy - 6); cvS.Children.Add(sol);
                StackPanel spS = new StackPanel(); spS.HorizontalAlignment = HorizontalAlignment.Center; spS.VerticalAlignment = VerticalAlignment.Center; spS.Width = W;
                spS.Children.Add(cvS);
                Grid fila = Fixa(new Grid()); fila.Margin = new Thickness(0, 6, 0, 0);   // sortida a l'esquerra, com la corba
                TextBlock tS = Text(T("sol-surt", d.Sortida.ToString("H:mm", Inv)), 13, p["FG"], "SemiBold"); tS.HorizontalAlignment = HorizontalAlignment.Left;
                TextBlock tP = Text(T("sol-es-pon", d.Posta.ToString("H:mm", Inv)), 13, p["FG"], "SemiBold"); tP.HorizontalAlignment = HorizontalAlignment.Right;
                fila.Children.Add(tS); fila.Children.Add(tP); spS.Children.Add(fila);
                g.RSol.Children.Add(spS);
            }

            // Índex UV
            double uv = d.UV ?? 0;
            int idx = NivellUV(uv);
            StackPanel puntsUV = Fixa(new StackPanel()); puntsUV.Orientation = Orientation.Horizontal; puntsUV.Margin = new Thickness(8, 8, 0, 0);
            for (int i = 0; i < 5; i++)
            {
                Ellipse e = new Ellipse();
                double m = i == idx ? 16 : 11;
                e.Width = m; e.Height = m; e.Fill = Pinzell((string)NivellsUV[i][2]); e.Margin = new Thickness(0, 0, 8, 0); e.VerticalAlignment = VerticalAlignment.Center;
                if (i != idx) e.Opacity = 0.35; else { e.Stroke = Pinzell(p["FG"]); e.StrokeThickness = 1.5; }
                puntsUV.Children.Add(e);
            }
            Contingut(g.RUV, Text(N(Math.Round(uv)), 26, p["FG"], "SemiBold"), Text(T((string)NivellsUV[idx][1]), 13, p["FG"], "SemiBold"), puntsUV);

            // Qualitat de l'aire (índex ICA)
            if (d.Aire != null)
            {
                Ica a = d.Aire;
                double amp = 172.0;
                Grid barra = Fixa(new Grid()); barra.Height = 14; barra.Margin = new Thickness(0, 10, 0, 0); barra.Width = amp;
                StackPanel tira = new StackPanel(); tira.Orientation = Orientation.Horizontal; tira.VerticalAlignment = VerticalAlignment.Center;
                for (int i = 0; i < 6; i++) { Rectangle r = new Rectangle(); r.Width = amp / 6; r.Height = 6; r.Fill = Pinzell(IcaColors[i]); tira.Children.Add(r); }
                barra.Children.Add(tira);
                Ellipse marca = new Ellipse(); marca.Width = 14; marca.Height = 14; marca.Fill = Pinzell(IcaColors[a.Categoria]); marca.Stroke = Pinzell(p["FG"]); marca.StrokeThickness = 2;
                marca.HorizontalAlignment = HorizontalAlignment.Left; marca.Margin = new Thickness(Math.Min(0.99, a.Posicio) * amp - 7.0, 0, 0, 0);
                barra.Children.Add(marca);
                string nomCat = IcaNom(a.Categoria);
                double mida = nomCat.Length > 14 ? 16 : 20;
                g.RAire.ToolTip = T("aire-ajuda") + "\n" + a.Detall;
                Contingut(g.RAire, Text(nomCat, mida, p["FG"], "SemiBold"), Text(T("aire-valor", a.Nom, N(Math.Round(a.Valor))), 12.5, p["SUB"]), barra);
            }
            else
            {
                Contingut(g.RAire, Text(T("sense-dades"), 14, p["SUB"]));
            }

            // Visibilitat
            double km = (d.Visibilitat ?? 0) / 1000;
            string vt = T(km >= 20 ? "visibilitat-4" : km >= 10 ? "visibilitat-3" : km >= 4 ? "visibilitat-2" : km >= 1 ? "visibilitat-1" : "visibilitat-0");
            string kmTxt = km >= 10 ? Math.Round(km).ToString(Inv) : km.ToString("0.#", Ca);
            Contingut(g.RVisib, Text(kmTxt + " km", 26, p["FG"], "SemiBold"), Text(vt, 13, p["FG"], "SemiBold"));

            // Humitat
            Contingut(g.RHumitat, Text(Num(d.Humitat) + " %", 26, p["FG"], "SemiBold"), Text(T("humitat-rosada", Graus(d.Rosada)), 12.5, p["SUB"]));

            // Pressió: indicador d'arc entre 970 i 1050 hPa
            g.RPressio.Children.Clear();
            double pr = d.Pressio ?? 0;
            double f = Math.Max(0.0, Math.Min(1.0, (pr - 970.0) / 80.0));
            Canvas cvP = Fixa(new Canvas()); cvP.Width = 96; cvP.Height = 84; cvP.HorizontalAlignment = HorizontalAlignment.Center;
            cvP.Children.Add(Cami(Arc(48, 46, 38, -135, 135), p["BORDER"], 7));
            if (f > 0.01) cvP.Children.Add(Cami(Arc(48, 46, 38, -135, -135 + 270 * f), p["ACCENT"], 7));
            TextBlock tv = Text(pr.ToString("N0", Ca), 20, p["FG"], "SemiBold"); tv.Width = 96; tv.TextAlignment = TextAlignment.Center;
            Canvas.SetTop(tv, 32); cvP.Children.Add(tv);
            TextBlock tu = Text("hPa", 11, p["SUB"]); tu.Width = 96; tu.TextAlignment = TextAlignment.Center;
            Canvas.SetTop(tu, 56); cvP.Children.Add(tu);
            g.RPressio.Children.Add(cvP);
            g.RPressio.ToolTip = T(pr >= 1020 ? "pressio-alta" : (pr <= 1005 ? "pressio-baixa" : "pressio-normal"));
        }

        static void ActualitzaHoresDies(Dades d)
        {
            Det g = det; Dictionary<string, string> p = g.P;
            g.Hores.Children.Clear();
            foreach (Hora h in d.Hores)
            {
                StackPanel sp = new StackPanel(); sp.Width = 54; sp.Margin = new Thickness(0, 2, 0, 0);
                TextBlock t = Text(Graus(h.Temp) + "°", 14, p["FG"], "SemiBold"); t.HorizontalAlignment = HorizontalAlignment.Center; sp.Children.Add(t);
                Viewbox ic = IconaCaixa(h.Categoria, h.Dia, 30); ic.Margin = new Thickness(0, 6, 0, 4); sp.Children.Add(ic);
                TextBlock pl = Text(h.Pluja > 0 ? h.Pluja + " %" : " ", 12, p["BLAU"], "SemiBold"); pl.HorizontalAlignment = HorizontalAlignment.Center; sp.Children.Add(pl);
                TextBlock et = Text(h.Etiqueta, 12, p["SUB"]); et.HorizontalAlignment = HorizontalAlignment.Center; et.Margin = new Thickness(0, 3, 0, 0); sp.Children.Add(et);
                g.Hores.Children.Add(sp);
            }
            g.HoresScroll.ScrollToHorizontalOffset(0);

            // Dies, amb una barra de temperatura relativa a tota la setmana
            g.Dies.Children.Clear();
            if (d.Dies.Count == 0) return;
            double minG = d.Dies.Min(x => x.Min ?? 0);
            double maxG = d.Dies.Max(x => x.Max ?? 0);
            double rang = Math.Max(1.0, maxG - minG), ampBarra = 170.0;
            foreach (DiaPrev x in d.Dies)
            {
                Grid gr = new Grid(); gr.Margin = new Thickness(0, 5, 0, 5);
                foreach (double w in new[] { 62, 40, 46, 38, ampBarra + 16, 38 }) gr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });
                StackPanel sp = new StackPanel(); sp.VerticalAlignment = VerticalAlignment.Center;
                sp.Children.Add(Text(x.Etiqueta, 13.5, p["FG"], "SemiBold")); sp.Children.Add(Text(x.Data, 11, p["SUB"]));
                gr.Children.Add(sp);
                Viewbox ic = IconaCaixa(x.Categoria, true, 28); Grid.SetColumn(ic, 1); gr.Children.Add(ic);
                TextBlock pl = Text(x.Pluja >= 10 ? x.Pluja + " %" : "", 12, p["BLAU"], "SemiBold"); pl.VerticalAlignment = VerticalAlignment.Center; pl.TextAlignment = TextAlignment.Center;
                Grid.SetColumn(pl, 2); gr.Children.Add(pl);
                // La barra va sempre de fred (esquerra) a calor (dreta); en una llengua RTL, la mínima i la màxima s'intercanvien de columna
                TextBlock tmin = Text(Graus(x.Min) + "°", 13.5, p["SUB"]); tmin.VerticalAlignment = VerticalAlignment.Center; tmin.TextAlignment = Dreta ? TextAlignment.Left : TextAlignment.Right;
                Grid.SetColumn(tmin, Dreta ? 5 : 3); gr.Children.Add(tmin);
                Grid fons = Fixa(new Grid()); fons.Height = 6; fons.Width = ampBarra; fons.VerticalAlignment = VerticalAlignment.Center;
                Rectangle r0 = new Rectangle(); r0.RadiusX = 3; r0.RadiusY = 3; r0.Fill = Pinzell(p["BORDER"]); fons.Children.Add(r0);
                Rectangle r1 = new Rectangle(); r1.RadiusX = 3; r1.RadiusY = 3; r1.HorizontalAlignment = HorizontalAlignment.Left;
                double x0 = ((x.Min ?? 0) - minG) / rang * ampBarra, x1 = ((x.Max ?? 0) - minG) / rang * ampBarra;
                r1.Margin = new Thickness(x0, 0, 0, 0); r1.Width = Math.Max(6.0, x1 - x0);
                LinearGradientBrush gb = new LinearGradientBrush(); gb.StartPoint = new Point(0, 0); gb.EndPoint = new Point(1, 0);
                gb.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#6CB8F6"), 0));
                gb.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFB547"), 1));
                r1.Fill = gb; fons.Children.Add(r1);
                Grid.SetColumn(fons, 4); gr.Children.Add(fons);
                TextBlock tmax = Text(Graus(x.Max) + "°", 13.5, p["FG"], "SemiBold"); tmax.VerticalAlignment = VerticalAlignment.Center;
                if (Dreta) tmax.TextAlignment = TextAlignment.Right;
                Grid.SetColumn(tmax, Dreta ? 3 : 5); gr.Children.Add(tmax);
                g.Dies.Children.Add(gr);
            }
        }

        // ---------- Pintar les dades ----------
        public static void ActualitzaBarra(bool forca)
        {
            Dictionary<string, object> principal = Conf.Llocs[0];
            bool clarBarra = TemaClar("SystemUsesLightTheme");
            string colorText = clarBarra ? "#1B1B1B" : "#FFFFFF";
            gTemp.Foreground = Pinzell(colorText); gDesc.Foreground = Pinzell(colorText);
            gFons.Tag = Pinzell(clarBarra ? "#0F000000" : "#14FFFFFF");
            try
            {
                Dades d = DadesLloc(principal, forca);
                gIcona.Child = Icona(d.Categoria, d.Dia, clarBarra);
                gTemp.Text = TextDireccio(Graus(d.Temp) + " °C");
                gDesc.Text = d.Estat;
                giny.ToolTip = T("giny-ajuda", Nom(principal), Graus(d.Temp), d.Estat.ToLower());
            }
            catch (Exception ex)
            {
                gIcona.Child = null;
                gTemp.Text = "-- °C";
                gDesc.Text = T("sense-connexio");
                darreraErr = ex.ToString();
            }
        }

        public static void ActualitzaPanell(bool forca)
        {
            Dictionary<string, object> c = LlocVist();
            Det g = det;
            g.FLloc.Text = Nom(c);
            g.BMeteocat.Visibility = Codi(c) != null ? Visibility.Visible : Visibility.Collapsed;
            try
            {
                Dades d = DadesLloc(c, forca);
                dades = d;
                g.FIcona.Child = Icona(d.Categoria, d.Dia, ModeClar());
                g.FTemp.Text = TextDireccio(Graus(d.Temp) + "°");
                g.FDesc.Text = d.Estat;
                g.FSensacio.Text = T("ara-sensacio", Graus(d.Sensacio));
                g.FMaxMin.Text = T("ara-maxmin", Graus(d.Max), Graus(d.Min));
                g.FResum.Text = d.Resum;
                g.FHora.Text = T("ara-actualitzat", d.HoraText);
                g.FFont.Text = d.Models > 1 ? T("ara-font-models", d.Models) : T("ara-font");
                ActualitzaHoresDies(d);
                ActualitzaRajoles(d);
            }
            catch (Exception ex)
            {
                g.FHora.Text = T("ara-sense-connexio");
                darreraErr = ex.ToString();
            }
            ActualitzaPastilles();
        }

        public static void ActualitzaTemps(bool forca)
        {
            ActualitzaBarra(forca);
            ActualitzaPanell(forca);
            MarcaActualitzacio();
            darrera = DateTime.Now;
        }

        // ---------- Llocs desats (pastilles) ----------
        static MenuItem ElementMenu(string text, string accio, int i, bool actiu = true)
        {
            MenuItem mi = new MenuItem();
            mi.Header = text; mi.Tag = new object[] { accio, i }; mi.IsEnabled = actiu;
            mi.Style = (Style)det.W.FindResource(typeof(MenuItem));
            mi.Click += (s, e) => { object[] t = (object[])((MenuItem)s).Tag; AccioLloc((string)t[0], (int)t[1]); };
            return mi;
        }

        static void ActualitzaPastilles()
        {
            Det g = det; Dictionary<string, string> p = g.P;
            g.Pastilles.Children.Clear();
            List<Dictionary<string, object>> llocs = Conf.Llocs;
            for (int i = 0; i < llocs.Count; i++)
            {
                Dictionary<string, object> l = llocs[i];
                Border b = new Border();
                b.CornerRadius = new CornerRadius(17); b.Padding = new Thickness(12, 5, 14, 5); b.Margin = new Thickness(0, 0, 6, 0); b.Cursor = Cursors.Hand; b.Tag = i;
                bool sel = i == Vist;
                b.Background = Pinzell(sel ? p["SEL"] : p["CARD"]);
                b.BorderBrush = Pinzell(sel ? p["ACCENT"] : p["BORDER"]); b.BorderThickness = new Thickness(1);
                b.ToolTip = T("lloc-ajuda");
                StackPanel sp = new StackPanel(); sp.Orientation = Orientation.Horizontal;
                if (i == 0)
                {
                    TextBlock st = new TextBlock(); st.Text = ((char)0xE735).ToString(); st.FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
                    st.FontSize = 11; st.Foreground = Pinzell(p["ACCENT"]); st.VerticalAlignment = VerticalAlignment.Center; st.Margin = new Thickness(0, 0, 6, 0); st.ToolTip = T("lloc-principal");
                    sp.Children.Add(st);
                }
                Dades dd = DadesBreus(l);
                if (dd != null) { Viewbox ic = IconaCaixa(dd.Categoria, dd.Dia, 18); ic.Margin = new Thickness(0, 0, 6, 0); ic.VerticalAlignment = VerticalAlignment.Center; sp.Children.Add(ic); }
                TextBlock tn = Text(Nom(l), 13, p["FG"], "SemiBold"); tn.VerticalAlignment = VerticalAlignment.Center; sp.Children.Add(tn);
                if (dd != null) { TextBlock tt = Text(Graus(dd.Temp) + "°", 13, p["SUB"]); tt.Margin = new Thickness(6, 0, 0, 0); tt.VerticalAlignment = VerticalAlignment.Center; sp.Children.Add(tt); }
                b.Child = sp;
                // Clic: mira el temps d'aquí. Arrossegar: canvia l'ordre (el primer és el principal, el de la barra).
                b.PreviewMouseLeftButtonDown += PastillaAvall;
                b.MouseMove += (s, e) => PastillaMou();
                b.PreviewMouseLeftButtonUp += PastillaAmunt;
                b.LostMouseCapture += (s, e) => { if (arrB == s) { arrB = null; if (arrMou) ActualitzaPastilles(); } };
                ContextMenu cm = new ContextMenu(); Direccio(cm);
                cm.Style = (Style)g.W.FindResource(typeof(ContextMenu));
                cm.Items.Add(ElementMenu(T("lloc-fes-principal"), "principal", i, i > 0));
                cm.Items.Add(ElementMenu(T("lloc-canvia-nom-menu"), "nom", i));
                // «Mou a l'esquerra» és a l'esquerra de la pantalla: en una llengua RTL, cap al final de la llista
                cm.Items.Add(ElementMenu(T("lloc-mou-esquerra"), Dreta ? "dreta" : "esquerra", i, Dreta ? i < llocs.Count - 1 : i > 0));
                cm.Items.Add(ElementMenu(T("lloc-mou-dreta"), Dreta ? "esquerra" : "dreta", i, Dreta ? i > 0 : i < llocs.Count - 1));
                cm.Items.Add(ElementMenu(T("lloc-treu"), "treu", i, llocs.Count > 1));
                cm.Opened += (s, e) => menuObert = true;
                cm.Closed += (s, e) => { menuObert = false; if (det.W.IsVisible) det.W.Activate(); };
                b.ContextMenu = cm;
                g.Pastilles.Children.Add(b);
            }
            // Pastilla per afegir un lloc
            Border mes = new Border();
            mes.CornerRadius = new CornerRadius(17); mes.Padding = new Thickness(12, 5, 14, 5); mes.Cursor = Cursors.Hand; mes.Background = Pinzell(p["CARD"]);
            mes.BorderBrush = Pinzell(p["BORDER"]); mes.BorderThickness = new Thickness(1); mes.ToolTip = T("lloc-afegeix-ajuda");
            StackPanel spm = new StackPanel(); spm.Orientation = Orientation.Horizontal;
            TextBlock plu = new TextBlock(); plu.Text = ((char)0xE710).ToString(); plu.FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
            plu.FontSize = 11; plu.Foreground = Pinzell(p["FG"]); plu.VerticalAlignment = VerticalAlignment.Center; plu.Margin = new Thickness(0, 0, 6, 0);
            spm.Children.Add(plu);
            TextBlock tx = Text(T("lloc-afegeix"), 13, p["FG"]); tx.VerticalAlignment = VerticalAlignment.Center; spm.Children.Add(tx);
            mes.Child = spm;
            mes.MouseLeftButtonUp += (s, e) => AfegeixLloc();
            g.Pastilles.Children.Add(mes);
            g.Pastilles.UpdateLayout(); g.PastScroll.UpdateLayout();
            g.PastFletxes.Visibility = g.PastScroll.ScrollableWidth > 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------- Arrossegar les pastilles per endreçar els llocs ----------
        static Border arrB;          // pastilla que s'arrossega
        static int arrIdx;
        static double arrX0;         // on s'ha premut (coordenades de la tira de pastilles)
        static bool arrMou;          // ja s'ha mogut prou per ser un arrossegament
        static System.Windows.Threading.DispatcherTimer arrTimer;

        static void PastillaAvall(object s, MouseButtonEventArgs e)
        {
            arrB = (Border)s; arrIdx = (int)arrB.Tag; arrMou = false;
            arrX0 = e.GetPosition(det.Pastilles).X;
            arrB.CaptureMouse();
            e.Handled = true;
        }

        static void PastillaMou()
        {
            if (arrB == null || Mouse.LeftButton != MouseButtonState.Pressed) return;
            double dx = Mouse.GetPosition(det.Pastilles).X - arrX0;
            if (!arrMou)
            {
                if (Math.Abs(dx) < 6) return;
                arrMou = true;
                arrB.Opacity = 0.85; Panel.SetZIndex(arrB, 10); arrB.Cursor = Cursors.SizeWE;
                ToolTipService.SetIsEnabled(arrB, false);
                if (arrTimer == null)
                {
                    // Si el ratolí és a la vora del carrusel, el carrusel es desplaça sol
                    arrTimer = new System.Windows.Threading.DispatcherTimer();
                    arrTimer.Interval = TimeSpan.FromMilliseconds(30);
                    arrTimer.Tick += (s2, e2) =>
                    {
                        if (arrB == null || !arrMou) { arrTimer.Stop(); return; }
                        ScrollViewer sv = det.PastScroll;
                        double xs = Mouse.GetPosition(sv).X;
                        if (xs < 30) sv.ScrollToHorizontalOffset(sv.HorizontalOffset - 10);
                        else if (xs > sv.ViewportWidth - 30) sv.ScrollToHorizontalOffset(sv.HorizontalOffset + 10);
                        else return;
                        PastillaMou();
                    };
                }
                arrTimer.Start();
            }
            arrB.RenderTransform = new TranslateTransform(dx, 0);
        }

        static void PastillaAmunt(object s, MouseButtonEventArgs e)
        {
            if (arrB == null || s != arrB) return;
            Border b = arrB; int de = arrIdx; bool mogut = arrMou;
            arrB = null;
            if (arrTimer != null) arrTimer.Stop();
            b.ReleaseMouseCapture();
            e.Handled = true;
            if (!mogut) { TriaLloc(de); return; }
            // Nova posició: quantes pastilles queden a l'esquerra del centre de la que s'ha deixat anar
            double centre = b.TranslatePoint(new Point(b.ActualWidth / 2, 0), det.Pastilles).X;
            int a = 0;
            foreach (UIElement c in det.Pastilles.Children)
            {
                Border o = c as Border;
                if (o == null || o == b || !(o.Tag is int)) continue;
                if (o.TranslatePoint(new Point(o.ActualWidth / 2, 0), det.Pastilles).X < centre) a++;
            }
            MouLloc(de, a);
        }

        static void MouLloc(int de, int a)
        {
            List<Dictionary<string, object>> ll = new List<Dictionary<string, object>>(Conf.Llocs);
            if (de < 0 || de >= ll.Count || de == a) { ActualitzaPastilles(); return; }
            Dictionary<string, object> vist = ll[Vist];
            Dictionary<string, object> x = ll[de];
            ll.RemoveAt(de); ll.Insert(Math.Min(a, ll.Count), x);
            Conf.Llocs = ll;
            Vist = Math.Max(0, ll.IndexOf(vist));
            DesaConfig(Conf);
            ActualitzaTemps(false);
            MouDetall();
        }

        public static void TriaLloc(int i)
        {
            Vist = i;
            AmagaSubpagina();
            ActualitzaPanell(false);
            det.Desplaca.ScrollToTop();
            mapaClau = null;
            if (det.W.IsVisible) { if (BlocVisible("Radar")) { InicialitzaMapa(false); EngegaRadar(); } MouDetall(); }
        }

        // Mentre hi ha un diàleg obert, el panell no s'amaga
        static void Dialeg(Action accio)
        {
            dialeg = true;
            try { accio(); } finally { dialeg = false; }
            if (det.W.IsVisible) det.W.Activate();
        }

        static void AfegeixLloc()
        {
            Dialeg(() =>
            {
                Dictionary<string, object> nou = new Tria(null).Mostra();
                if (nou != null)
                {
                    Conf.Llocs.Add(nou);
                    DesaConfig(Conf);
                    TriaLloc(Conf.Llocs.Count - 1);
                }
            });
        }

        static void AccioLloc(string accio, int i)
        {
            List<Dictionary<string, object>> ll = new List<Dictionary<string, object>>(Conf.Llocs);
            Dictionary<string, object> vist = ll[Vist];
            Dictionary<string, object> x;
            switch (accio)
            {
                case "principal": x = ll[i]; ll.RemoveAt(i); ll.Insert(0, x); break;
                case "esquerra": if (i > 0) { x = ll[i]; ll.RemoveAt(i); ll.Insert(i - 1, x); } break;
                case "dreta": if (i < ll.Count - 1) { x = ll[i]; ll.RemoveAt(i); ll.Insert(i + 1, x); } break;
                case "treu": if (ll.Count > 1) { ll.RemoveAt(i); if (vist == Conf.Llocs[i]) vist = ll[0]; } break;
                case "nom":
                    string nou = null;
                    Dialeg(() => { nou = DemanaText.Mostra(T("lloc-canvia-nom"), Nom(ll[i])); });
                    if (!string.IsNullOrEmpty(nou)) ll[i]["nom"] = nou;
                    break;
            }
            Conf.Llocs = ll;
            Vist = Math.Max(0, ll.IndexOf(vist));
            DesaConfig(Conf);
            ActualitzaTemps(false);
            MouDetall();
        }

        static void CanviaTema(string tema)
        {
            Conf.Tema = tema;
            DesaConfig(Conf);
            // Les finestres porten els colors dins: es torna a fer la de detall
            AturaRadar();
            Window vella = det.W;
            det = NovaFinestraDetall();
            mapaClau = null;
            vella.Close();
            ActualitzaTemps(false);
            tancatA = null;
            MostraDetall();
        }

        // Canvia la llengua: es desa, es tornen a fer els textos i el panell, i es torna a «Configuració»
        static void CanviaLlengua(string codi)
        {
            Conf.Llengua = codi;
            DesaConfig(Conf);
            TriaLlengua(codi);
            Registre("Llengua: " + Llengua + (codi == null ? " (la del Windows)" : ""));
            cache.Clear();
            EstatActualitzacio = null;
            AturaRadar();
            Window vella = det.W;
            det = NovaFinestraDetall();
            mapaClau = null;
            vella.Close();
            ActualitzaTemps(false);
            tancatA = null;
            MostraDetall();
            MostraSubpagina("Personalitza");
        }

        public static void MostraDetall()
        {
            if (det.W.IsVisible) { det.W.Hide(); AturaRadar(); return; }
            if (tancatA != null && (DateTime.Now - tancatA.Value).TotalMilliseconds < 350) return;   // el clic que l'ha tancat
            Window w = det.W;
            double e = escala > 0 ? escala : 1;
            // Que càpiga per sobre de la barra (es pot desplaçar amb la roda del ratolí)
            det.Desplaca.MaxHeight = Math.Max(300.0, Math.Min(1000.0, (barraOk ? barra.Top : 0) / e - 200.0));
            det.Desplaca.ScrollToTop();
            w.Show();
            MouDetall();
            w.Activate();
            if (BlocVisible("Radar")) { InicialitzaMapa(false); EngegaRadar(); }
        }

        public static void MouDetall()
        {
            Window w = det.W;
            if (!w.IsVisible || Prova) return;
            double e = escala > 0 ? escala : 1;
            w.UpdateLayout();
            IntPtr h = new WindowInteropHelper(w).Handle;
            int altPx = Convert.ToInt32(w.ActualHeight * e);
            int x = ginyX - Convert.ToInt32(10 * e);
            int y = Math.Max(0, (barraOk ? barra.Top : 0) - altPx + Convert.ToInt32(6 * e));
            Win.SetWindowPos(h, new IntPtr(-1), x, y, 0, 0, 0x11);
        }
    }
}
