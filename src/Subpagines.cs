// Quina Barra! · versió 1.1 · el temps del teu poble a la barra de tasques, en català i altres llengües
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Pàgines de detall de les rajoles, «Quant a» i el mapa de radar.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using System.Windows.Threading;

namespace TempsBarra
{
    static partial class G
    {
        // Enrere: de la pàgina de detall a la principal. Endavant: torna a obrir l'última pàgina de detall.
        static void Navega(bool endavant)
        {
            Det g = det; if (g == null) return;
            if (endavant) { if (g.Sub.Visibility != Visibility.Visible && ultimaSub != null) MostraSubpagina(ultimaSub); }
            else { if (g.Sub.Visibility == Visibility.Visible) AmagaSubpagina(); }
        }

        public static void AmagaSubpagina()
        {
            Det g = det; if (g == null) return;
            g.Sub.Visibility = Visibility.Collapsed; g.Desplaca.Visibility = Visibility.Visible;
        }

        static StackPanel Targeta()
        {
            Border b = new Border();
            b.Background = Pinzell(det.P["CARD"]); b.BorderBrush = Pinzell(det.P["BORDER"]);
            b.BorderThickness = new Thickness(1); b.CornerRadius = new CornerRadius(8); b.Margin = new Thickness(4); b.Padding = new Thickness(16, 14, 16, 14);
            StackPanel sp = new StackPanel(); b.Child = sp;
            det.SubCos.Children.Add(b);
            return sp;
        }

        static void Paragraf(string negreta, string text)
        {
            TextBlock tb = new TextBlock();
            tb.TextWrapping = TextWrapping.Wrap; tb.FontSize = 13.5; tb.LineHeight = 20; tb.Margin = new Thickness(6, 10, 6, 0);
            tb.Foreground = Pinzell(det.P["FG"]);
            if (!string.IsNullOrEmpty(negreta)) { Run r = new Run(negreta + " "); r.FontWeight = FontWeights.SemiBold; tb.Inlines.Add(r); }
            tb.Inlines.Add(new Run(text));
            det.SubCos.Children.Add(tb);
        }

        static void Xifra(StackPanel sp, string etiqueta, string valor, string extra = "")
        {
            Dictionary<string, string> p = det.P;
            sp.Children.Add(Text(etiqueta, 12.5, p["SUB"]));
            StackPanel fila = new StackPanel(); fila.Orientation = Orientation.Horizontal;
            fila.Children.Add(Text(valor, 34, p["FG"], "SemiBold"));
            if (!string.IsNullOrEmpty(extra)) { TextBlock e = Text(extra, 17, p["FG"]); e.VerticalAlignment = VerticalAlignment.Bottom; e.Margin = new Thickness(10, 0, 0, 7); fila.Children.Add(e); }
            sp.Children.Add(fila);
        }

        // Gràfic de barres per hores, desplaçable. Cada element: Frac (0-1), Color, Dalt (text o element), Sota (textos)
        static void GraficHores(StackPanel sp, IEnumerable<ElGrafic> elements, bool pastilla = true)
        {
            Dictionary<string, string> p = det.P;
            ScrollViewer sv = new ScrollViewer();
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden; sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled; sv.Margin = new Thickness(0, 14, 0, 0);
            sv.PreviewMouseWheel += RodaHoritzontal;
            StackPanel fila = new StackPanel(); fila.Orientation = Orientation.Horizontal;
            double alt = 84.0;
            foreach (ElGrafic el in elements)
            {
                StackPanel col = new StackPanel(); col.Width = 50;
                string dt = el.Dalt as string;
                FrameworkElement dalt = el.Dalt as FrameworkElement;
                if (dt != null) { TextBlock d = Text(dt, 12, p["FG"], "SemiBold"); d.HorizontalAlignment = HorizontalAlignment.Center; d.Height = 18; col.Children.Add(d); }
                else if (dalt != null) { dalt.HorizontalAlignment = HorizontalAlignment.Center; col.Children.Add(dalt); }
                else { TextBlock d = Text(" ", 12, p["FG"]); d.Height = 18; col.Children.Add(d); }
                Grid zona = new Grid(); zona.Height = alt; zona.Margin = new Thickness(0, 4, 0, 6);
                double amp = pastilla ? 22 : 16;
                if (pastilla)
                {
                    Border fons = new Border(); fons.Width = amp; fons.CornerRadius = new CornerRadius(amp / 2); fons.Background = Pinzell(p["BORDER"]);
                    zona.Children.Add(fons);
                }
                double f = Math.Max(0.0, Math.Min(1.0, el.Frac));
                Border barra = new Border(); barra.Width = amp; barra.CornerRadius = new CornerRadius(amp / 2);
                barra.VerticalAlignment = VerticalAlignment.Bottom; barra.Background = Pinzell(el.Color);
                barra.Height = Math.Max(6.0, f * alt); barra.CornerRadius = new CornerRadius(Math.Min(amp / 2.0, barra.Height / 2.0));
                if (f <= 0.0 && !pastilla) { barra.Height = 3; barra.Opacity = 0.5; }
                zona.Children.Add(barra);
                col.Children.Add(zona);
                bool primer = true;
                foreach (string s in el.Sota)
                {
                    TextBlock tx = Text(s, 12, primer ? p["FG"] : p["SUB"]); tx.HorizontalAlignment = HorizontalAlignment.Center; col.Children.Add(tx); primer = false;
                }
                fila.Children.Add(col);
            }
            sv.Content = fila;
            // Fletxes ‹ › per desplaçar el gràfic amb el ratolí
            StackPanel fletxes = new StackPanel(); fletxes.Orientation = Orientation.Horizontal; fletxes.HorizontalAlignment = HorizontalAlignment.Right; fletxes.Margin = new Thickness(0, 8, 0, -8);
            foreach (int pas in new[] { -250, 250 })
            {
                Button b = new Button();
                b.Style = (Style)det.W.FindResource("Pla");
                b.Content = ((char)(pas < 0 ? 0xE76B : 0xE76C)).ToString(); b.FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");
                b.FontSize = 12; b.Padding = new Thickness(10, 6, 10, 6); b.Tag = pas;
                b.ToolTip = pas < 0 ? T("grafic-anteriors") : T("grafic-seguents");
                b.Click += (s, e) => sv.ScrollToHorizontalOffset(sv.HorizontalOffset + (int)((Button)s).Tag);
                fletxes.Children.Add(b);
            }
            sp.Children.Add(fletxes);
            sp.Children.Add(sv);
        }

        static List<Hora> HoresAvui(Dades d) { return d.Hores.Where(h => h.Data == d.Ara.Date).ToList(); }

        static StackPanel FletxaDireccio(double dir)
        {
            Dictionary<string, string> p = det.P;
            StackPanel sf = new StackPanel(); sf.Height = 34;
            Path fl = Cami("M6,0 L10,9 L6,7 L2,9 Z", p["FG"], 1, p["FG"]); fl.Width = 12; fl.Height = 12; fl.HorizontalAlignment = HorizontalAlignment.Center;
            fl.RenderTransformOrigin = new Point(0.5, 0.5); fl.RenderTransform = new RotateTransform((dir + 180) % 360);
            sf.Children.Add(fl);
            TextBlock tc = Text(PuntCardinal(dir), 11.5, p["SUB"], "SemiBold"); tc.HorizontalAlignment = HorizontalAlignment.Center; sf.Children.Add(tc);
            return sf;
        }

        static string F1(double v) { return v.ToString("0.#", Ca); }

        public static void MostraSubpagina(string clau)
        {
            Det g = det; Dades d = dades; Dictionary<string, string> p = g.P;
            if (d == null) return;
            ultimaSub = clau;
            AturaRadar();
            g.SubCos.Children.Clear();
            StackPanel sp;

            switch (clau)
            {
                case "Pluja":
                    {
                        g.SubTitol.Text = T("bloc-pluja");
                        sp = Targeta();
                        Xifra(sp, T("pluja-total-previst"), F1(d.PlujaAvui ?? 0) + " mm");
                        double max = Math.Max(1.0, d.Hores.Count > 0 ? d.Hores.Max(h => h.PlujaMm) : 0);
                        GraficHores(sp, d.Hores.Select(h =>
                        {
                            Viewbox ic = IconaCaixa(h.Categoria, h.Dia, 28); ic.Height = 28;
                            return new ElGrafic(h.PlujaMm / max, p["BLAU"], ic, (h.PlujaMm > 0 ? F1(h.PlujaMm) : "0") + " mm", h.Pluja + " %", h.Etiqueta);
                        }).ToList());
                        Paragraf("", T("pluja-explicacio"));
                        Paragraf(T("pluja-idea-titol"), T("pluja-idea"));
                        break;
                    }
                case "Vent":
                    {
                        g.SubTitol.Text = T("bloc-vent");
                        sp = Targeta();
                        List<Hora> resta = HoresAvui(d); if (resta.Count == 0) resta = d.Hores;
                        double? maxV = resta.Count > 0 ? (double?)resta.Max(h => h.Vent) : null;
                        double? maxR = resta.Count > 0 ? (double?)resta.Max(h => h.Ratxes) : null;
                        Xifra(sp, T("maxima-resta-dia"), Graus(maxV) + " km/h", T("vent-ratxes-de", Graus(maxR)));
                        double escalaV = Math.Max(20.0, d.Hores.Count > 0 ? d.Hores.Max(h => h.Vent) : 0);
                        GraficHores(sp, d.Hores.Select(h => new ElGrafic(h.Vent / escalaV, p["ACCENT"], FletxaDireccio(h.VentDir), Graus(h.Vent), h.Etiqueta)).ToList());
                        double v = d.Vent ?? 0;
                        string desc = T(v < 6 ? "vent-desc-0" : v < 20 ? "vent-desc-1" : v < 39 ? "vent-desc-2" : v < 62 ? "vent-desc-3" : "vent-desc-4");
                        Paragraf(T("ara-mateix-titol"), T("vent-ara", Graus(v), PuntCardinal(d.VentDir ?? 0), desc));
                        Paragraf(T("vent-velocitat-titol"), T("vent-velocitat"));
                        Paragraf(T("vent-ratxes-titol"), T("vent-ratxes-explicacio"));
                        break;
                    }
                case "Sol":
                    {
                        g.SubTitol.Text = T("bloc-sol");
                        sp = Targeta();
                        // Corba gran amb la part de dia ombrejada
                        double W = 400.0, H = 150.0, hz = 92.0;
                        double L = (d.Posta - d.Sortida).TotalHours; double migdia = d.Sortida.AddHours(L / 2).TimeOfDay.TotalHours;
                        double bas = Math.Cos(Math.PI * L / 24), esc = 70 / (1 - bas), escNit = 44 / (1 + bas);
                        Func<double, double> posY = hores => { double a = Math.Cos(2 * Math.PI * (hores - migdia) / 24) - bas; return a >= 0 ? hz - esc * a : hz - escNit * a; };
                        List<string> punts = new List<string>();
                        for (double x = 0.0; x <= W; x += 3) punts.Add(x.ToString("0.#", Inv) + "," + posY(x / W * 24).ToString("0.#", Inv));
                        Canvas cv = new Canvas(); cv.Width = W; cv.Height = H; cv.ClipToBounds = true; cv.HorizontalAlignment = HorizontalAlignment.Center;
                        Path area = Cami("M 0," + N(H) + " L " + string.Join(" L ", punts) + " L " + N(W) + "," + N(H) + " Z", null, 0, p["BORDER"]);
                        cv.Children.Add(area);
                        cv.Children.Add(Cami("M " + string.Join(" L ", punts), p["SUB"], 2));
                        cv.Children.Add(Cami("M0," + N(hz) + " L" + N(W) + "," + N(hz), p["SUB"], 1));
                        double ara = d.Ara.TimeOfDay.TotalHours;
                        Ellipse sol = new Ellipse(); sol.Width = 22; sol.Height = 22; sol.Fill = Pinzell("#FFC83D"); sol.Stroke = Pinzell(p["CARD"]); sol.StrokeThickness = 3;
                        if (!d.Dia) sol.Opacity = 0.5;
                        Canvas.SetLeft(sol, ara / 24 * W - 11); Canvas.SetTop(sol, posY(ara) - 11);
                        cv.Children.Add(sol);
                        sp.Children.Add(cv);
                        UniformGrid gr = new UniformGrid(); gr.Columns = 4; gr.Margin = new Thickness(0, 14, 0, 0);
                        foreach (KeyValuePair<string, DateTime?> par in new[] {
                            new KeyValuePair<string, DateTime?>(T("sol-alba"), d.Alba), new KeyValuePair<string, DateTime?>(T("sol-sortida"), d.Sortida),
                            new KeyValuePair<string, DateTime?>(T("sol-posta"), d.Posta), new KeyValuePair<string, DateTime?>(T("sol-capvespre"), d.Capvespre) })
                        {
                            StackPanel c = new StackPanel();
                            TextBlock a = Text(par.Key, 12.5, p["SUB"]); a.HorizontalAlignment = HorizontalAlignment.Center; c.Children.Add(a);
                            TextBlock b = Text(par.Value != null ? par.Value.Value.ToString("H:mm", Inv) : "--", 20, p["FG"], "SemiBold"); b.HorizontalAlignment = HorizontalAlignment.Center; c.Children.Add(b);
                            gr.Children.Add(c);
                        }
                        sp.Children.Add(gr);
                        TimeSpan llum = d.Posta - d.Sortida;
                        TimeSpan dema = (d.PostaDema - d.SortidaDema) - llum;
                        double min = Math.Round(Math.Abs(dema.TotalMinutes));
                        string canvi = min < 1 ? T("sol-dema-igual") : (dema.TotalMinutes < 0 ? T("sol-dema-menys", N(min)) : T("sol-dema-mes", N(min)));
                        // Hores senceres de sol (la versió de PowerShell arrodonia amb [int] i podia sumar una hora de més)
                        Paragraf(T("sol-avui-titol"), T("sol-avui", (int)Math.Floor(llum.TotalHours), llum.Minutes) + " " + canvi);
                        Paragraf(T("sol-alba-titol"), T("sol-alba-explicacio"));
                        Paragraf(T("sol-sortida-titol"), T("sol-sortida-explicacio"));
                        Paragraf(T("sol-posta-titol"), T("sol-posta-explicacio"));
                        Paragraf(T("sol-capvespre-titol"), T("sol-capvespre-explicacio"));
                        break;
                    }
                case "UV":
                    {
                        g.SubTitol.Text = T("bloc-uv");
                        sp = Targeta();
                        List<Hora> resta = HoresAvui(d); if (resta.Count == 0) resta = d.Hores;
                        double maxU = resta.Count > 0 ? resta.Max(h => h.UV) : 0;
                        Xifra(sp, T("maxima-resta-dia"), N(Math.Round(maxU)), T((string)NivellsUV[NivellUV(maxU)][1]));
                        GraficHores(sp, d.Hores.Select(h => new ElGrafic(h.UV / 11.0, (string)NivellsUV[NivellUV(h.UV)][2], N(Math.Round(h.UV)), h.Etiqueta)).ToList());
                        Paragraf("", T("uv-explicacio"));
                        Paragraf(T("uv-0-titol"), T("uv-0-consell"));
                        Paragraf(T("uv-1-titol"), T("uv-1-consell"));
                        Paragraf(T("uv-2-titol"), T("uv-2-consell"));
                        Paragraf(T("uv-3-titol"), T("uv-3-consell"));
                        break;
                    }
                case "Aire":
                    {
                        g.SubTitol.Text = T("bloc-aire");
                        sp = Targeta();
                        Ica a = d.Aire;
                        if (a == null) { sp.Children.Add(Text(T("aire-sense-dades"), 14, p["SUB"])); break; }
                        sp.Children.Add(Text(T("aire-index"), 12.5, p["SUB"]));
                        sp.Children.Add(Text(IcaNom(a.Categoria), 28, p["FG"], "SemiBold"));
                        double amp = 400.0;
                        Grid barra = new Grid(); barra.Height = 16; barra.Margin = new Thickness(0, 10, 0, 6); barra.Width = amp; barra.HorizontalAlignment = HorizontalAlignment.Left;
                        StackPanel tira = new StackPanel(); tira.Orientation = Orientation.Horizontal; tira.VerticalAlignment = VerticalAlignment.Center;
                        for (int i = 0; i < 6; i++) { Rectangle r = new Rectangle(); r.Width = amp / 6; r.Height = 7; r.Fill = Pinzell(IcaColors[i]); tira.Children.Add(r); }
                        barra.Children.Add(tira);
                        Ellipse marca = new Ellipse(); marca.Width = 16; marca.Height = 16; marca.Fill = Pinzell(IcaColors[a.Categoria]); marca.Stroke = Pinzell(p["FG"]); marca.StrokeThickness = 2;
                        marca.HorizontalAlignment = HorizontalAlignment.Left; marca.Margin = new Thickness(Math.Min(0.99, a.Posicio) * amp - 8.0, 0, 0, 0);
                        barra.Children.Add(marca); sp.Children.Add(barra);
                        string consell = T(a.Categoria <= 1 ? "aire-consell-0" : a.Categoria == 2 ? "aire-consell-2" : a.Categoria == 3 ? "aire-consell-3" : "aire-consell-4");
                        TextBlock tc = Text(consell, 13.5, p["FG"]); tc.TextWrapping = TextWrapping.Wrap; sp.Children.Add(tc);
                        // Contaminants
                        StackPanel sp2 = Targeta();
                        sp2.Children.Add(Text(T("aire-contaminants"), 12.5, p["SUB"]));
                        foreach (IcaItem c in a.Llista)
                        {
                            Grid fila = new Grid(); fila.Margin = new Thickness(0, 8, 0, 0);
                            foreach (double w in new double[] { 22, 170, 90 }) fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(w) });
                            fila.ColumnDefinitions.Add(new ColumnDefinition());
                            Ellipse pt = new Ellipse(); pt.Width = 12; pt.Height = 12; pt.Fill = Pinzell(IcaColors[c.Categoria]); pt.HorizontalAlignment = HorizontalAlignment.Left;
                            fila.Children.Add(pt);
                            TextBlock n1 = Text(c.Nom, 13.5, p["FG"], "SemiBold"); Grid.SetColumn(n1, 1); fila.Children.Add(n1);
                            TextBlock n2 = Text(N(Math.Round(c.Valor)) + " µg/m³", 13.5, p["FG"]); Grid.SetColumn(n2, 2); fila.Children.Add(n2);
                            TextBlock n3 = Text(IcaNom(c.Categoria).ToLower(), 13, p["SUB"]); Grid.SetColumn(n3, 3); fila.Children.Add(n3);
                            sp2.Children.Add(fila);
                        }
                        Paragraf("", T("aire-explicacio"));
                        Paragraf(T("aire-calcul-titol"), T("aire-calcul"));
                        Paragraf(T("font-titol"), T("aire-font"));
                        break;
                    }
                case "Visib":
                    {
                        g.SubTitol.Text = T("bloc-visibilitat");
                        sp = Targeta();
                        double km = (d.Visibilitat ?? 0) / 1000;
                        Xifra(sp, T("ara-mateix"), (km >= 10 ? N(Math.Round(km)) : km.ToString("0.#", Ca)) + " km");
                        GraficHores(sp, d.Hores.Select(h => { double k = h.Visib / 1000; return new ElGrafic(Math.Min(k, 50) / 50, p["ACCENT"], N(Math.Round(k)), h.Etiqueta); }).ToList());
                        Paragraf("", T("visibilitat-explicacio"));
                        Paragraf(T("visibilitat-escala-titol"), T("visibilitat-escala"));
                        break;
                    }
                case "Humitat":
                    {
                        g.SubTitol.Text = T("bloc-humitat");
                        sp = Targeta();
                        List<Hora> avui = HoresAvui(d); if (avui.Count == 0) avui = d.Hores;
                        double mitj = avui.Count > 0 ? avui.Average(h => h.Humitat) : 0;
                        Xifra(sp, T("humitat-mitjana"), N(Math.Round(mitj)) + " %");
                        GraficHores(sp, d.Hores.Select(h => new ElGrafic(h.Humitat / 100, p["BLAU"], "", N(Math.Round(h.Humitat)) + " %", h.Etiqueta)).ToList(), true);
                        double r = d.Rosada ?? 0;
                        string sensa = T(r < 10 ? "rosada-desc-0" : r < 16 ? "rosada-desc-1" : r < 20 ? "rosada-desc-2" : "rosada-desc-3");
                        Paragraf(T("rosada-titol"), T("rosada-ara", Graus(r), sensa));
                        Paragraf(T("humitat-relativa-titol"), T("humitat-relativa"));
                        Paragraf(T("rosada-perque-titol"), T("rosada-perque"));
                        break;
                    }
                case "Pressio":
                    {
                        g.SubTitol.Text = T("bloc-pressio");
                        sp = Targeta();
                        double pr = d.Pressio ?? 0;
                        double d3 = d.Hores.Count > 3 ? d.Hores[3].Pressio - pr : 0;
                        string tend = T(d3 > 1 ? "pressio-pujant" : (d3 < -1 ? "pressio-baixant" : "pressio-estable"));
                        Xifra(sp, T("ara-mateix"), pr.ToString("N0", Ca) + " hPa", tend);
                        double amp = 400.0;
                        Grid fons = new Grid(); fons.Width = amp; fons.Height = 8; fons.HorizontalAlignment = HorizontalAlignment.Left; fons.Margin = new Thickness(0, 10, 0, 2);
                        Border b0 = new Border(); b0.CornerRadius = new CornerRadius(4); b0.Background = Pinzell(p["BORDER"]); fons.Children.Add(b0);
                        Border b1 = new Border(); b1.CornerRadius = new CornerRadius(4); b1.Background = Pinzell(p["ACCENT"]); b1.HorizontalAlignment = HorizontalAlignment.Left;
                        b1.Width = Math.Max(8.0, Math.Min(1.0, Math.Max(0.0, (pr - 970.0) / 80.0)) * amp); fons.Children.Add(b1);
                        sp.Children.Add(fons);
                        Grid esc = new Grid(); esc.Width = amp; esc.HorizontalAlignment = HorizontalAlignment.Left;
                        TextBlock e1 = Text(T("pressio-escala-baixa", "970"), 11, p["SUB"]); esc.Children.Add(e1);
                        TextBlock e2 = Text(T("pressio-escala-alta", 1050.0.ToString("N0", Ca)), 11, p["SUB"]); e2.HorizontalAlignment = HorizontalAlignment.Right; esc.Children.Add(e2);
                        sp.Children.Add(esc);
                        GraficHores(sp, d.Hores.Select(h => new ElGrafic((h.Pressio - 990) / 50, p["ACCENT"], Math.Round(h.Pressio).ToString("N0", Ca), h.Etiqueta)).ToList());
                        Paragraf("", T("pressio-explicacio"));
                        Paragraf(T("pressio-significat-titol"), T("pressio-significat"));
                        Paragraf(T("pressio-cos-titol"), T("pressio-cos"));
                        break;
                    }
            }
            if (clau == "QuantA") QuantA();
            if (clau == "Personalitza") PaginaPersonalitza();
            if (clau == "Mar" && d.Mar != null) PaginaMar(d);
            g.SubDesplaca.MaxHeight = g.Desplaca.ActualHeight;
            g.SubDesplaca.ScrollToTop();
            g.Desplaca.Visibility = Visibility.Hidden; g.Sub.Visibility = Visibility.Visible;
        }

        static void Enllac(TextBlock tb, string text, string url)
        {
            Hyperlink h = new Hyperlink(new Run(text));
            h.NavigateUri = new Uri(url); h.Foreground = Pinzell(det.P["ACCENT"]); h.TextDecorations = null; h.Cursor = Cursors.Hand;
            h.RequestNavigate += (s, e) => { Obre(e.Uri.AbsoluteUri); e.Handled = true; };
            tb.Inlines.Add(h);
        }

        // Línia amb un enllaç. El text de la clau porta $1 on va l'enllaç.
        static void LiniaT(StackPanel sp, string clau, string textEnllac, string url)
        {
            string t = T(clau);
            int i = t.IndexOf("$1", StringComparison.Ordinal);
            if (i < 0) { Linia(sp, t + " ", textEnllac, url); return; }
            Linia(sp, t.Substring(0, i), textEnllac, url, t.Substring(i + 2));
        }

        static void Linia(StackPanel sp, string abans, string text, string url, string despres = "")
        {
            TextBlock tb = new TextBlock(); tb.TextWrapping = TextWrapping.Wrap; tb.FontSize = 13.5; tb.LineHeight = 20; tb.Margin = new Thickness(0, 6, 0, 0);
            tb.Foreground = Pinzell(det.P["FG"]);
            if (!string.IsNullOrEmpty(abans)) tb.Inlines.Add(new Run(abans));
            Enllac(tb, text, url);
            if (!string.IsNullOrEmpty(despres)) tb.Inlines.Add(new Run(despres));
            sp.Children.Add(tb);
        }

        static void QuantA()
        {
            Dictionary<string, string> p = det.P;
            det.SubTitol.Text = T("menu-quant-a");
            StackPanel sp = Targeta();
            StackPanel cap = new StackPanel(); cap.Orientation = Orientation.Horizontal;
            Viewbox ic = IconaCaixa("mig", true, 44); ic.Margin = new Thickness(0, 0, 12, 0); cap.Children.Add(ic);
            StackPanel tt = new StackPanel(); tt.VerticalAlignment = VerticalAlignment.Center;
            tt.Children.Add(Text("Quina Barra!", 22, p["FG"], "SemiBold"));
            tt.Children.Add(Text(T("quanta-versio", Versio), 12.5, p["SUB"]));
            cap.Children.Add(tt); sp.Children.Add(cap);
            TextBlock ds = Text(T("quanta-descripcio"), 13.5, p["FG"]); ds.TextWrapping = TextWrapping.Wrap; ds.Margin = new Thickness(0, 12, 0, 0);
            sp.Children.Add(ds);
            LiniaT(sp, "quanta-autor", "Pep Payo", UrlAutor);
            LiniaT(sp, "quanta-codi", "github.com/PepPepet/quina-barra", UrlRepo);

            // Llengua
            StackPanel spL = Targeta();
            spL.Children.Add(Text(T("llengua-titol"), 15, p["FG"], "SemiBold"));
            // Botó amb la llengua d'ara que obre un menú (amb els mateixos colors que la resta del giny)
            List<string> codis = new List<string> { null };
            codis.AddRange(LlenguesDisponibles());
            Func<string, string> nomOpcio = c => c == null ? T("llengua-windows") : NomLlengua(c);
            Button bL = new Button(); bL.Margin = new Thickness(0, 8, 0, 0); bL.HorizontalAlignment = HorizontalAlignment.Left; bL.MinWidth = 220;
            Grid contL = new Grid(); contL.ColumnDefinitions.Add(new ColumnDefinition()); contL.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBlock txL = Text(nomOpcio(Conf.Llengua), 13.5, p["FG"]); txL.Margin = new Thickness(0, 0, 16, 0); contL.Children.Add(txL);
            TextBlock flL = Text(((char)0xE70D).ToString(), 11, p["SUB"]); flL.FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"); flL.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(flL, 1); contL.Children.Add(flL);
            bL.Content = contL; bL.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            bL.Click += (s, e) =>
            {
                ContextMenu cm = new ContextMenu();
                cm.Style = (Style)det.W.FindResource(typeof(ContextMenu));
                foreach (string c in codis)
                {
                    string codi = c;
                    cm.Items.Add(OpcioMenu(codi == Conf.Llengua ? "" : "", nomOpcio(codi), () => { if (codi != Conf.Llengua) CanviaLlengua(codi); }));
                }
                cm.Opened += (s2, e2) => menuObert = true;
                cm.Closed += (s2, e2) => { menuObert = false; if (det.W.IsVisible) det.W.Activate(); };
                cm.PlacementTarget = bL; cm.Placement = PlacementMode.Bottom;
                cm.IsOpen = true;
            };
            spL.Children.Add(bL);
            if (Llengua != LlenguaBase)
            {
                TextBlock nt = Text(T("llengua-nota"), 12.5, p["SUB"]); nt.TextWrapping = TextWrapping.Wrap; nt.Margin = new Thickness(0, 10, 0, 0);
                spL.Children.Add(nt);
            }

            // Actualitzacions i arrencada
            StackPanel spA = Targeta();
            spA.Children.Add(Text(T("quanta-actualitzacions"), 15, p["FG"], "SemiBold"));
            TextBlock est = Text(string.IsNullOrEmpty(EstatActualitzacio) ? T("actualitzacio-actual", Versio) : EstatActualitzacio, 13.5, p["FG"]);
            est.TextWrapping = TextWrapping.Wrap; est.Margin = new Thickness(0, 6, 0, 10); spA.Children.Add(est);
            StackPanel bots = new StackPanel(); bots.Orientation = Orientation.Horizontal;
            Button bCerca = new Button(); bCerca.Content = T("actualitzacio-cerca");
            Button bBaixa = new Button(); bBaixa.Content = VersioNova != null ? T("actualitzacio-baixa", VersioNova) : T("actualitzacio-baixa-la"); bBaixa.Style = (Style)det.W.FindResource("Accent");
            bBaixa.Margin = new Thickness(8, 0, 0, 0);
            bBaixa.Visibility = VersioNova != null ? Visibility.Visible : Visibility.Collapsed;
            bCerca.Click += (s, e) =>
            {
                est.Text = T("actualitzacio-comprovant"); bCerca.IsEnabled = false;
                CercaActualitzacions(() =>
                {
                    est.Text = EstatActualitzacio; bCerca.IsEnabled = true;
                    bBaixa.Visibility = VersioNova != null ? Visibility.Visible : Visibility.Collapsed;
                    if (VersioNova != null) bBaixa.Content = T("actualitzacio-baixa", VersioNova);
                });
            };
            bBaixa.Click += (s, e) => { if (UrlNova != null) Obre(UrlNova); };
            bots.Children.Add(bCerca); bots.Children.Add(bBaixa); spA.Children.Add(bots);
            TextBlock nota = Text(T("actualitzacio-nota"), 12.5, p["SUB"]);
            nota.TextWrapping = TextWrapping.Wrap; nota.Margin = new Thickness(0, 10, 0, 0); spA.Children.Add(nota);
            CheckBox chk = new CheckBox(); chk.Content = T("quanta-engega"); chk.FontSize = 13.5;
            chk.Foreground = Pinzell(p["FG"]); chk.Margin = new Thickness(0, 14, 0, 0); chk.VerticalContentAlignment = VerticalAlignment.Center;
            chk.IsChecked = EngegaAmbWindows();
            chk.Checked += (s, e) => PosaEngegaAmbWindows(true);
            chk.Unchecked += (s, e) => PosaEngegaAmbWindows(false);
            spA.Children.Add(chk);

            StackPanel sp2 = Targeta();
            sp2.Children.Add(Text(T("quanta-fonts"), 15, p["FG"], "SemiBold"));
            LiniaT(sp2, "font-open-meteo", "Open-Meteo", "https://open-meteo.com/");
            LiniaT(sp2, "font-radar", "RainViewer", "https://www.rainviewer.com/");
            LiniaT(sp2, "font-mapa", T("font-mapa-enllac"), "https://www.openstreetmap.org/copyright");
            LiniaT(sp2, "font-ica", "MITECO", "https://www.miteco.gob.es/ca/calidad-y-evaluacion-ambiental/temas/atmosfera-y-calidad-del-aire/visualizacion-datos-calidad-del-aire/ica.html");
            LiniaT(sp2, "font-municipis", "Idescat", "https://www.idescat.cat/");
            LiniaT(sp2, "font-platges", T("font-platges-enllac"), "https://analisi.transparenciacatalunya.cat/d/4baz-cjv2");
            LiniaT(sp2, "font-meteocat", "Meteocat", "https://www.meteo.cat/");

            StackPanel sp3 = Targeta();
            sp3.Children.Add(Text(T("quanta-privadesa"), 15, p["FG"], "SemiBold"));
            TextBlock pv = Text(T("quanta-privadesa-text"), 13.5, p["FG"]);
            pv.TextWrapping = TextWrapping.Wrap; pv.LineHeight = 20; pv.Margin = new Thickness(0, 6, 0, 0); sp3.Children.Add(pv);

            StackPanel sp4 = Targeta();
            sp4.Children.Add(Text(T("quanta-llicencia"), 15, p["FG"], "SemiBold"));
            TextBlock lc = Text(T("quanta-llicencia-text"), 13.5, p["FG"]);
            lc.TextWrapping = TextWrapping.Wrap; lc.LineHeight = 20; lc.Margin = new Thickness(0, 6, 0, 0); sp4.Children.Add(lc);
            TextBlock cr = Text("© 2026 Pep Payo", 12.5, p["SUB"]); cr.Margin = new Thickness(0, 8, 0, 0); sp4.Children.Add(cr);
        }

        static void PaginaMar(Dades d)
        {
            Dictionary<string, string> p = det.P;
            Mar m = d.Mar;
            Func<double, string> fmt = v => v.ToString("0.0", Ca);
            det.SubTitol.Text = T("bloc-mar");
            StackPanel sp = Targeta();
            Xifra(sp, T("mar-alcada-ara"), fmt(m.Alcada) + " m", m.Estat.ToLower());
            double maxH = Math.Max(1.0, m.Hores.Count > 0 ? m.Hores.Max(h => h.Alcada) : 0);
            GraficHores(sp, m.Hores.Select(h => new ElGrafic(h.Alcada / maxH, p["BLAU"], FletxaDireccio(h.Dir), fmt(h.Alcada) + " m", N(Math.Round(h.Periode)) + " s", h.Etiqueta)).ToList());
            string aigua = m.Aigua != null ? " " + T("mar-ara-aigua", m.Aigua.Value.ToString("0.#", Ca)) : "";
            Paragraf(T("ara-mateix-titol"), T("mar-ara", PuntCardinal(m.Dir), fmt(m.Alcada), N(Math.Round(m.Periode)), PuntCardinal(m.FonsDir), fmt(m.FonsAlcada), N(Math.Round(m.FonsPeriode))) + aigua);
            Paragraf(T("mar-alcada-titol"), T("mar-alcada"));
            Paragraf(T("mar-periode-titol"), T("mar-periode"));
            Paragraf(T("mar-fons-titol"), T("mar-fons-explicacio"));
            Paragraf(T("mar-estat-titol"), T("mar-estat"));
            Paragraf(T("font-titol"), T("mar-font"));
            if (d.Platges != null && d.Platges.Llista.Count > 0)
            {
                StackPanel sp2 = Targeta();
                Dictionary<string, object> lloc = LlocVist();
                sp2.Children.Add(Text(T("platges-de", Nom(lloc)), 15, p["FG"], "SemiBold"));
                string nota = d.Platges.EnTemporada ? T("platges-avui")
                    : T("platges-fora-temporada", d.Platges.Data.ToString("d/M/yyyy", Inv));
                TextBlock tn = Text(nota, 12.5, p["SUB"]); tn.TextWrapping = TextWrapping.Wrap; tn.Margin = new Thickness(0, 2, 0, 4); sp2.Children.Add(tn);
                foreach (Platja pl in d.Platges.Llista)
                {
                    Grid fila = new Grid(); fila.Margin = new Thickness(0, 10, 0, 0);
                    fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); fila.ColumnDefinitions.Add(new ColumnDefinition());
                    Ellipse pt = new Ellipse(); pt.Width = 12; pt.Height = 12; pt.VerticalAlignment = VerticalAlignment.Top; pt.Margin = new Thickness(0, 4, 10, 0);
                    string col; if (pl.Bandera == null || !BanderaColors.TryGetValue(pl.Bandera, out col)) col = p["SUB"];
                    pt.Fill = Pinzell(col); fila.Children.Add(pt);
                    StackPanel sp3 = new StackPanel();
                    string nm = pl.Nom; if (pl.Distancia < 50) nm += "  ·  " + T("platja-distancia", pl.Distancia.ToString("0.#", Ca));
                    sp3.Children.Add(Text(nm, 13.5, p["FG"], "SemiBold"));
                    TextBlock dt = Text(TextPlatja(pl), 12.5, p["SUB"]); dt.TextWrapping = TextWrapping.Wrap; sp3.Children.Add(dt);
                    Grid.SetColumn(sp3, 1); fila.Children.Add(sp3);
                    sp2.Children.Add(fila);
                }
                Paragraf(T("platges-banderes-titol"), T("platges-banderes"));
                Paragraf(T("platges-font-titol"), T("platges-font"));
            }
        }

        // ---------- Mapa de radar ----------
        public static DispatcherTimer radarTimer;
        static List<Canvas> radarCapes = new List<Canvas>();
        static List<string> radarHores = new List<string>();
        static int radarActual;
        static string mapaClau;
        static DateTime? mapaQuan;

        public static void CreaRadarTimer()
        {
            radarTimer = new DispatcherTimer();
            radarTimer.Interval = TimeSpan.FromMilliseconds(650);
            radarTimer.Tick += (s, e) =>
            {
                int n = radarCapes.Count; if (n == 0) return;
                MostraFotograma((radarActual + 1) % n);
            };
        }

        public static void AturaRadar() { radarTimer.Stop(); if (det != null) det.BPlay.Content = ((char)0xE768).ToString(); }
        static void EngegaRadar() { if (radarCapes.Count > 1) { radarTimer.Start(); det.BPlay.Content = ((char)0xE769).ToString(); } }

        static Image NovaImatge(string url, double x, double y)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit(); bi.UriSource = new Uri(url); bi.EndInit();
            Image im = new Image();
            im.Source = bi; im.Width = 256; im.Height = 256;
            Canvas.SetLeft(im, x); Canvas.SetTop(im, y);
            return im;
        }

        static string RajolaMapa(int z, int x, int y, bool fosc)
        {
            string dir = System.IO.Path.Combine(Carpeta, "mapa\\" + (fosc ? "fosc" : "clar"));
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string fitxer = System.IO.Path.Combine(dir, z + "-" + x + "-" + y + ".png");
            if (File.Exists(fitxer) && (DateTime.Now - File.GetLastWriteTime(fitxer)).TotalDays < 30) return fitxer;
            try
            {
                byte[] bytes;
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "QuinaBarra/1.1 (giny del temps per a Windows)");
                    bytes = wc.DownloadData("https://tile.openstreetmap.org/" + z + "/" + x + "/" + y + ".png");
                }
                MemoryStream ms = new MemoryStream(bytes);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit(); bi.CacheOption = BitmapCacheOption.OnLoad; bi.StreamSource = ms; bi.EndInit();
                FormatConvertedBitmap src = new FormatConvertedBitmap(bi, PixelFormats.Bgra32, null, 0);
                int w = src.PixelWidth, h = src.PixelHeight, stride = w * 4;
                byte[] px = new byte[h * stride];
                src.CopyPixels(px, stride, 0);
                if (fosc) Win.Enfosqueix(px);   // inverteix la lluminositat i en conserva una mica el color
                WriteableBitmap wb = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
                wb.WritePixels(new Int32Rect(0, 0, w, h), px, stride, 0);
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(wb));
                using (FileStream fs = File.Create(fitxer)) enc.Save(fs);
                return fitxer;
            }
            catch
            {
                if (File.Exists(fitxer)) return fitxer;
                return null;
            }
        }

        static void MostraFotograma(int i)
        {
            Det g = det;
            for (int k = 0; k < radarCapes.Count; k++) radarCapes[k].Visibility = k == i ? Visibility.Visible : Visibility.Hidden;
            for (int k = 0; k < g.Marques.Children.Count; k++)
            {
                FrameworkElement m = (FrameworkElement)g.Marques.Children[k];
                m.Height = k == i ? 18 : 10;
                m.Opacity = k == i ? 1 : 0.45;
            }
            radarActual = i;
            g.MapaHora.Text = radarHores[i];
        }

        public static void InicialitzaMapa(bool forca)
        {
            Det g = det; Dictionary<string, object> c = LlocVist();
            string clau = J.S(J.G(c, "latitud")) + "," + J.S(J.G(c, "longitud")) + "," + g.P["MAPA"];
            if (!forca && mapaClau == clau && mapaQuan != null && (DateTime.Now - mapaQuan.Value).TotalMinutes < 10) return;
            AturaRadar();
            g.MapaBase.Children.Clear(); g.MapaRadar.Children.Clear(); g.MapaPin.Children.Clear(); g.Marques.Children.Clear();
            g.MapaAvis.Visibility = Visibility.Visible; g.MapaAvis.Text = T("radar-carregant");
            radarCapes = new List<Canvas>(); radarHores = new List<string>();

            int z = 7; double W = 424.0, H = 230.0;
            double n = Math.Pow(2, z) * 256;
            double lat = Lat(c) * Math.PI / 180;
            double cx = (Lon(c) + 180) / 360 * n;
            double cy = (1 - Math.Log(Math.Tan(lat) + 1 / Math.Cos(lat)) / Math.PI) / 2 * n;
            double esq = cx - W / 2, dalt = cy - H / 2;
            List<double[]> rajoles = new List<double[]>();
            for (double tx = Math.Floor(esq / 256); tx <= Math.Floor((esq + W) / 256); tx++)
                for (double ty = Math.Floor(dalt / 256); ty <= Math.Floor((dalt + H) / 256); ty++)
                    rajoles.Add(new double[] { tx, ty, tx * 256 - esq, ty * 256 - dalt });
            foreach (double[] r in rajoles)
            {
                string fitxer = RajolaMapa(z, (int)r[0], (int)r[1], g.P["MAPA"] == "fosc");
                if (fitxer != null) g.MapaBase.Children.Add(NovaImatge(fitxer, r[2], r[3]));
            }

            // Marcador de la ubicació
            Path pin = Cami("M0,0 C0,-7 -6,-10 -6,-15 A6,6 0 1 1 6,-15 C6,-10 0,-7 0,0 Z", "#FFFFFF", 1.5, "#E5484D");
            Canvas.SetLeft(pin, W / 2); Canvas.SetTop(pin, H / 2);
            g.MapaPin.Children.Add(pin);

            try
            {
                object rv = Web("https://api.rainviewer.com/public/weather-maps.json");
                int offset = dades != null ? dades.Offset : 0;
                foreach (object f in J.A(J.G(rv, "radar", "past")))
                {
                    Canvas capa = new Canvas(); capa.Visibility = Visibility.Hidden;
                    foreach (double[] r in rajoles)
                        capa.Children.Add(NovaImatge(J.S(J.G(rv, "host")) + J.S(J.G(f, "path")) + "/512/" + z + "/" + (int)r[0] + "/" + (int)r[1] + "/2/1_1.png", r[2], r[3]));
                    g.MapaRadar.Children.Add(capa);
                    radarCapes.Add(capa);
                    radarHores.Add(DateTimeOffset.FromUnixTimeSeconds((long)J.Dz(J.G(f, "time"))).UtcDateTime.AddSeconds(offset).ToString("H:mm", Inv));
                    Rectangle mq = new Rectangle(); mq.Width = 3; mq.Height = 10; mq.RadiusX = 1.5; mq.RadiusY = 1.5;
                    mq.Fill = Pinzell(g.P["FG"]); mq.Margin = new Thickness(0, 0, 9, 0); mq.VerticalAlignment = VerticalAlignment.Center; mq.Cursor = Cursors.Hand;
                    mq.Tag = radarCapes.Count - 1;
                    mq.MouseLeftButtonUp += (s, e) => { AturaRadar(); MostraFotograma((int)((Rectangle)s).Tag); };
                    g.Marques.Children.Add(mq);
                }
                g.MapaAvis.Visibility = Visibility.Collapsed;
                if (radarCapes.Count > 0) MostraFotograma(radarCapes.Count - 1);
            }
            catch
            {
                g.MapaAvis.Text = T("radar-error");
            }
            mapaClau = clau; mapaQuan = DateTime.Now;
        }
    }
}
