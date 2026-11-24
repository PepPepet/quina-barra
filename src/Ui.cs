// Quina Barra! · versió 1.2 · el temps del teu poble a la barra de tasques, en català i altres llengües
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Colors, XAML, icones del temps i peces petites de la interfície.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace TempsBarra
{
    static partial class G
    {
        // ---------- Tema i colors (segueix el mode clar o fosc de Windows) ----------
        public static Dictionary<string, string> Paleta()
        {
            if (ModeClar())
                return new Dictionary<string, string> {
                    {"BG","#F3F3F3"},{"CARD","#FFFFFF"},{"FG","#1B1B1B"},{"SUB","#616161"},{"BORDER","#E0E0E0"},{"HOVER","#0A000000"},
                    {"SEL","#14005FB8"},{"ACCENT","#005FB8"},{"ACCENTFG","#FFFFFF"},{"BLAU","#0F6CBD"},{"MAPA","clar"} };
            return new Dictionary<string, string> {
                {"BG","#202020"},{"CARD","#2C2C2C"},{"FG","#FFFFFF"},{"SUB","#C8C8C8"},{"BORDER","#3B3B3B"},{"HOVER","#14FFFFFF"},
                {"SEL","#2E4CC2FF"},{"ACCENT","#4CC2FF"},{"ACCENTFG","#000000"},{"BLAU","#6CB8F6"},{"MAPA","fosc"} };
        }

        public static string AmbPaleta(string xaml, Dictionary<string, string> p)
        {
            foreach (KeyValuePair<string, string> kv in p) xaml = xaml.Replace("@" + kv.Key + "@", kv.Value);
            return xaml;
        }

        public const string XmlNs = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

        static readonly Dictionary<string, string> recursos = new Dictionary<string, string>();
        public static string Recurs(string nom)
        {
            string t;
            if (!recursos.TryGetValue(nom, out t))
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("TempsBarra." + nom))
                using (StreamReader r = new StreamReader(s, Encoding.UTF8, true))
                    t = r.ReadToEnd();
                recursos[nom] = t;
            }
            return TradueixXaml(t);
        }

        // XAML d'una finestra amb els estils comuns i la paleta
        public static string XamlFinestra(string nom, Dictionary<string, string> p)
        {
            return AmbPaleta(Recurs(nom).Replace("@ESTILS@", Recurs("Estils.xaml")), p);
        }

        public static object LlegeixXaml(string xaml) { return XamlReader.Parse(xaml); }

        public static Brush Pinzell(string hex) { return (Brush)new BrushConverter().ConvertFromString(hex); }

        // ---------- Icones del temps (vectorials, 32 x 32) ----------
        static string Nuvol(string color, int x, int y)
        {
            return "<Path Fill=\"" + color + "\" Data=\"M9,26 H24 A6,6 0 0 0 24.5,14 A8,8 0 0 0 9.5,16 A5,5 0 0 0 9,26 Z\"><Path.RenderTransform><TranslateTransform X=\""
                + x.ToString(Inv) + "\" Y=\"" + y.ToString(Inv) + "\"/></Path.RenderTransform></Path>";
        }

        public static UIElement Icona(string tipus, bool dia, bool clar)
        {
            string nuvolClar = clar ? "#B9C3CF" : "#E3E8EE";
            string nuvolFosc = clar ? "#7E8A99" : "#9EA9B7";
            const string sol = "<Canvas><Ellipse Canvas.Left=\"9\" Canvas.Top=\"9\" Width=\"14\" Height=\"14\" Fill=\"#FFC83D\"/><Path Stroke=\"#FFC83D\" StrokeThickness=\"2.2\" StrokeStartLineCap=\"Round\" StrokeEndLineCap=\"Round\" Data=\"M16,2.5 L16,5.5 M16,26.5 L16,29.5 M2.5,16 L5.5,16 M26.5,16 L29.5,16 M6.5,6.5 L8.6,8.6 M23.4,23.4 L25.5,25.5 M6.5,25.5 L8.6,23.4 M23.4,8.6 L25.5,6.5\"/></Canvas>";
            const string lluna = "<Path Fill=\"#F4E7B5\"><Path.Data><CombinedGeometry GeometryCombineMode=\"Exclude\"><CombinedGeometry.Geometry1><EllipseGeometry Center=\"15,17\" RadiusX=\"11\" RadiusY=\"11\"/></CombinedGeometry.Geometry1><CombinedGeometry.Geometry2><EllipseGeometry Center=\"22.5,10.5\" RadiusX=\"9.5\" RadiusY=\"9.5\"/></CombinedGeometry.Geometry2></CombinedGeometry></Path.Data></Path>";
            string astre = dia ? sol : lluna;
            string petit = "<Canvas><Canvas.RenderTransform><TransformGroup><ScaleTransform ScaleX='0.62' ScaleY='0.62'/><TranslateTransform X='12.5' Y='0'/></TransformGroup></Canvas.RenderTransform>" + astre + "</Canvas>";
            const string sortidaPosta = "<Canvas><Path Fill=\"@C@\" Data=\"M9,23 A7,7 0 0 1 23,23 Z\"/><Path Stroke=\"@C@\" StrokeThickness=\"2.2\" StrokeStartLineCap=\"Round\" StrokeEndLineCap=\"Round\" Data=\"M16,9 L16,12.5 M6.5,13.5 L8.7,15.7 M25.5,13.5 L23.3,15.7 M2.5,23 L5.5,23 M26.5,23 L29.5,23 M4,27 H28\"/></Canvas>";

            string cos;
            switch (tipus)
            {
                case "clar": cos = astre; break;
                case "mig": cos = petit + Nuvol(nuvolClar, -2, 3); break;
                case "nuvol": cos = Nuvol(nuvolFosc, 3, -3) + Nuvol(nuvolClar, -2, 1); break;
                case "boira": cos = Nuvol(nuvolClar, 0, -6) + "<Path Stroke=\"" + nuvolFosc + "\" StrokeThickness=\"2.2\" StrokeStartLineCap=\"Round\" StrokeEndLineCap=\"Round\" Data=\"M5,24 H25 M9,28.5 H28\"/>"; break;
                case "plugim": cos = Nuvol(nuvolClar, 0, -5) + "<Path Stroke=\"#4FA3F7\" StrokeThickness=\"2\" StrokeStartLineCap=\"Round\" StrokeEndLineCap=\"Round\" Data=\"M11,24.5 L10.3,26.5 M17,26 L16.3,28 M23,24.5 L22.3,26.5 M14,29 L13.3,31\"/>"; break;
                case "pluja": cos = Nuvol(nuvolFosc, 0, -5) + "<Path Stroke=\"#4FA3F7\" StrokeThickness=\"2.2\" StrokeStartLineCap=\"Round\" StrokeEndLineCap=\"Round\" Data=\"M11.5,24 L9.5,30 M17.5,24 L15.5,30 M23.5,24 L21.5,30\"/>"; break;
                case "neu": cos = Nuvol(nuvolClar, 0, -5) + "<Canvas><Ellipse Canvas.Left=\"9\" Canvas.Top=\"24\" Width=\"3.4\" Height=\"3.4\" Fill=\"#FFFFFF\" Stroke=\"#9EC9F5\" StrokeThickness=\"0.8\"/><Ellipse Canvas.Left=\"15.3\" Canvas.Top=\"27\" Width=\"3.4\" Height=\"3.4\" Fill=\"#FFFFFF\" Stroke=\"#9EC9F5\" StrokeThickness=\"0.8\"/><Ellipse Canvas.Left=\"21.6\" Canvas.Top=\"24\" Width=\"3.4\" Height=\"3.4\" Fill=\"#FFFFFF\" Stroke=\"#9EC9F5\" StrokeThickness=\"0.8\"/></Canvas>"; break;
                case "tempesta": cos = Nuvol(nuvolFosc, 0, -6) + "<Path Fill=\"#FFC83D\" Data=\"M17.5,19 L12,26.5 H16 L14,32 L21.5,23.5 H17.3 L19.5,19 Z\"/>"; break;
                case "sortida": cos = sortidaPosta.Replace("@C@", "#FFB547"); break;
                case "posta": cos = sortidaPosta.Replace("@C@", "#FF8A4C"); break;
                default: cos = Nuvol(nuvolClar, 0, 0); break;
            }
            return (UIElement)LlegeixXaml("<Canvas " + XmlNs + " FlowDirection=\"LeftToRight\" Width=\"32\" Height=\"32\">" + cos + "</Canvas>");
        }

        public static Viewbox IconaCaixa(string tipus, bool dia, double mida)
        {
            Viewbox vb = new Viewbox();
            vb.Width = mida; vb.Height = mida;
            vb.Child = Icona(tipus, dia, ModeClar());
            return vb;
        }

        // ---------- Peces de les rajoles ----------
        public static TextBlock Text(string text, double mida, string color, string pes = "Normal")
        {
            TextBlock t = new TextBlock();
            t.Text = TextDireccio(text); t.FontSize = mida; Direccio(t); t.Foreground = Pinzell(color); t.TextWrapping = TextWrapping.NoWrap;
            t.FontWeight = pes == "SemiBold" ? FontWeights.SemiBold : (pes == "Light" ? FontWeights.Light : FontWeights.Normal);
            return t;
        }

        // ---------- Dreta a esquerra ----------
        // Una llengua de dreta a esquerra gira les finestres (FlowDirection). Els gràfics, la brúixola, el mapa,
        // les barres i les icones es fixen d'esquerra a dreta: el temps i les escales hi van sempre igual.
        public static void Direccio(FrameworkElement e)
        {
            if (e != null) e.FlowDirection = Dreta ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        public static T Fixa<T>(T e) where T : FrameworkElement
        {
            if (e != null) e.FlowDirection = FlowDirection.LeftToRight;
            return e;
        }

        // Fletxa de pàgina següent (›) o anterior (‹) dels Segoe Fluent Icons, girada en una llengua RTL
        public static string FletxaEndavant() { return ((char)(Dreta ? 0xE76B : 0xE76C)).ToString(); }
        public static string FletxaEnrere() { return ((char)(Dreta ? 0xE76C : 0xE76B)).ToString(); }

        public static void Contingut(Grid graella, params FrameworkElement[] elements)
        {
            graella.Children.Clear();
            StackPanel sp = new StackPanel();
            sp.VerticalAlignment = VerticalAlignment.Center; sp.HorizontalAlignment = HorizontalAlignment.Center;
            foreach (FrameworkElement e in elements)
            {
                e.HorizontalAlignment = HorizontalAlignment.Center;
                TextBlock tb = e as TextBlock;
                if (tb != null) tb.TextAlignment = TextAlignment.Center;
                sp.Children.Add(e);
            }
            graella.Children.Add(sp);
        }

        // angle en graus, 0 = amunt, sentit horari
        static string PuntArc(double cx, double cy, double r, double angle)
        {
            double rad = angle * Math.PI / 180;
            return (cx + r * Math.Sin(rad)).ToString("0.##", Inv) + "," + (cy - r * Math.Cos(rad)).ToString("0.##", Inv);
        }
        public static string Arc(double cx, double cy, double r, double a0, double a1)
        {
            int gran = (a1 - a0) > 180 ? 1 : 0;
            return "M " + PuntArc(cx, cy, r, a0) + " A " + N(r) + "," + N(r) + " 0 " + gran + " 1 " + PuntArc(cx, cy, r, a1);
        }
        public static Path Cami(string dades, string traC, double gruix, string farciment = null)
        {
            Path p = new Path();
            p.Data = Geometry.Parse(dades);
            if (!string.IsNullOrEmpty(traC))
            {
                p.Stroke = Pinzell(traC); p.StrokeThickness = gruix;
                p.StrokeStartLineCap = PenLineCap.Round; p.StrokeEndLineCap = PenLineCap.Round;
            }
            if (!string.IsNullOrEmpty(farciment)) p.Fill = Pinzell(farciment);
            return p;
        }

        // Número dins d'un text, com l'escrivia PowerShell
        public static string N(double v) { return v.ToString(Inv); }

        // Punt cardinal abreujat (16 direccions: punt-0 = N, punt-4 = E, punt-8 = S, punt-12 = O)
        public static string PuntCardinal(double graus)
        {
            int i = (int)Math.Round((graus % 360) / 22.5) % 16;
            if (i < 0) i += 16;
            return T("punt-" + i);
        }

        // Text dins d'un atribut XAML
        public static string XmlText(string s) { return System.Security.SecurityElement.Escape(s); }
    }
}
