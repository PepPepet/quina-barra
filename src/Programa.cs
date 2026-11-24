// Quina Barra! · versió 1.0 · el temps del teu poble a la barra de tasques, en català
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Fet per Pep Payo amb l'ajuda de Claude (IA d'Anthropic).
//
// Giny propi a l'esquerra de la barra de tasques de Windows 11 amb el temps del teu poble, en català.
// La ubicació la tria cadascú a mà (mai per IP).
// Dades: Open-Meteo.com (CC BY 4.0): previsió, qualitat de l'aire. Temperatura: mitjana de Météo-France, ICON i ECMWF.
// Radar: RainViewer. Mapa: © col·laboradors d'OpenStreetMap (rajoles desades 30 dies al disc). Municipis catalans (enllaç a Meteocat): Idescat.
//
// Arrencada: Main. Amb /prova [carpeta] no agafa el mutex, no toca config.json ni el registre,
// pinta el giny, el panell, les subpàgines i els diàlegs fora de la pantalla i en desa captures PNG.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TempsBarra
{
    static partial class G
    {
        // Giny de la barra de tasques
        public static Window giny;
        static Border gFons; static Viewbox gIcona; static TextBlock gTemp, gDesc;
        static IntPtr hGiny;
        static Win.RECT barra; static bool barraOk;
        static int ginyX; static double escala;
        static DateTime darrera;
        static string darreraErr;
        static DispatcherTimer rellotge;
        static Mutex mutex;

        [STAThread]
        static int Main(string[] args)
        {
            Win.SetProcessDPIAware();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (args.Length > 0 && (args[0].Equals("/prova", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-prova", StringComparison.OrdinalIgnoreCase)))
                return ProvaGiny.Executa(args.Length > 1 ? args[1] : null, args.Length > 2 ? args[2] : null);

            // Una sola instància
            bool creat;
            mutex = new Mutex(true, "Local\\QuinaBarra", out creat);
            if (!creat) return 0;
            MigraNomAntic();
            if (!Directory.Exists(Carpeta)) Directory.CreateDirectory(Carpeta);

            app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            app.DispatcherUnhandledException += (s, e) => { Registre("Error: " + e.Exception.Message); e.Handled = true; };
            CreaRadarTimer();

            Vist = 0;
            Conf = LlegeixConfig();
            TriaLlengua(Conf != null ? Conf.Llengua : null);
            if (Conf != null) Registre("Arrencada: " + Conf.Llocs.Count + " llocs (" + string.Join(", ", Conf.Llocs.Select(l => Nom(l))) + ")");
            if (Conf == null && File.Exists(FitxerConf))
            {
                Registre("Arrencada: hi ha config.json però no s'ha pogut llegir; no es demana cap lloc nou");
                MessageBox.Show(T("error-config", FitxerConf),
                    "Quina Barra!", MessageBoxButton.OK, MessageBoxImage.Warning);
                mutex.ReleaseMutex(); return 0;
            }
            if (Conf == null)
            {
                Registre("Arrencada: primera vegada, sense configuració");
                Dictionary<string, object> primer = new Tria(null).Mostra();
                if (primer == null) { mutex.ReleaseMutex(); return 0; }
                Conf = NovaConfig(primer);
                DesaConfig(Conf);
            }

            CreaGiny();
            det = NovaFinestraDetall();

            giny.Show();
            ActualitzaTemps(false);
            ActualitzaPosicio();

            // Es manté damunt la barra (que també és "sempre visible") i actualitza les dades
            rellotge = new DispatcherTimer();
            rellotge.Interval = TimeSpan.FromMilliseconds(300);
            rellotge.Tick += (s, e) =>
            {
                try
                {
                    ActualitzaPosicio();
                    if ((DateTime.Now - darrera).TotalMinutes >= Minuts) { if (!det.W.IsVisible) Vist = 0; ActualitzaTemps(false); }
                    CercaActualitzacionsSiCal();
                }
                catch { }
            };
            rellotge.Start();

            app.Run();

            rellotge.Stop();
            mutex.ReleaseMutex();
            return 0;
        }

        static void CreaGiny()
        {
            giny = (Window)LlegeixXaml(Recurs("Giny.xaml"));
            gFons = (Border)giny.FindName("Fons"); gIcona = (Viewbox)giny.FindName("Icona");
            gTemp = (TextBlock)giny.FindName("Temp"); gDesc = (TextBlock)giny.FindName("Desc");
            giny.SourceInitialized += (s, e) =>
            {
                hGiny = new WindowInteropHelper(giny).Handle;
                // WS_EX_TOOLWINDOW (no surt a Alt+Tab) | WS_EX_NOACTIVATE (no roba el focus)
                int ex = Win.GetWindowLong(hGiny, -20);
                Win.SetWindowLong(hGiny, -20, ex | 0x80 | 0x08000000);
            };
            gFons.MouseEnter += (s, e) => gFons.Background = (Brush)gFons.Tag;
            gFons.MouseLeave += (s, e) => gFons.Background = Pinzell("#01000000");
            gFons.MouseLeftButtonUp += (s, e) => MostraDetall();
            gFons.MouseRightButtonUp += (s, e) => MostraDetall();
        }

        // ---------- Posició damunt de la barra de tasques ----------
        static bool PantallaCompleta()
        {
            IntPtr fg = Win.GetForegroundWindow();
            if (fg == IntPtr.Zero || fg == hGiny) return false;
            StringBuilder sb = new StringBuilder(64); Win.GetClassName(fg, sb, 64);
            string cls = sb.ToString();
            if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd") return false;
            Win.RECT r = Win.Rect(fg);
            System.Drawing.Rectangle pant = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            return r.Left <= pant.Left && r.Top <= pant.Top && r.Right >= pant.Right && r.Bottom >= pant.Bottom;
        }

        static void ActualitzaPosicio()
        {
            IntPtr barraH = Win.FindWindow("Shell_TrayWnd", null);
            if (barraH == IntPtr.Zero) return;
            Win.RECT b = Win.Rect(barraH);
            if (PantallaCompleta()) { if (giny.Visibility == Visibility.Visible) giny.Visibility = Visibility.Hidden; return; }
            if (giny.Visibility != Visibility.Visible) giny.Visibility = Visibility.Visible;

            PresentationSource src = PresentationSource.FromVisual(giny);
            if (src == null) return;
            double esc = src.CompositionTarget.TransformToDevice.M11;
            double altDip = Math.Round((b.Bottom - b.Top) / esc);
            if (Math.Abs(giny.Height - altDip) > 0.5) giny.Height = altDip;
            int ampPx = (int)Math.Ceiling(giny.ActualWidth * esc);

            // Barra centrada (la de sèrie a Windows 11): a l'esquerra del tot. Barra a l'esquerra: just abans de la safata.
            object al = ValorRegistre("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAl");
            int x;
            if (al is int && (int)al == 0)
            {
                IntPtr safata = Win.FindWindowEx(barraH, IntPtr.Zero, "TrayNotifyWnd", null);
                x = safata != IntPtr.Zero ? Win.Rect(safata).Left - ampPx - Convert.ToInt32(8 * esc) : b.Right - ampPx - Convert.ToInt32(420 * esc);
            }
            else
            {
                x = b.Left + Convert.ToInt32(6 * esc);
            }
            // HWND_TOPMOST = -1; SWP_NOSIZE 0x1 | SWP_NOACTIVATE 0x10
            Win.SetWindowPos(hGiny, new IntPtr(-1), x, b.Top, 0, 0, 0x11);
            barra = b; barraOk = true; ginyX = x; escala = esc;
        }
    }

    // =====================================================================
    //  Prova (/prova): captures PNG sense tocar res
    // =====================================================================
    static class ProvaGiny
    {
        static string carpeta;
        static readonly List<string> informe = new List<string>();

        static void Bomba(int ms)
        {
            DateTime fi = DateTime.Now.AddMilliseconds(ms);
            do
            {
                DispatcherFrame f = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                    new DispatcherOperationCallback(o => { ((DispatcherFrame)o).Continue = false; return null; }), f);
                Dispatcher.PushFrame(f);
                if (ms > 0) Thread.Sleep(40);
            } while (DateTime.Now < fi);
        }

        static void Desa(Window w, string nom)
        {
            try
            {
                w.UpdateLayout();
                Bomba(150);
                PresentationSource src = PresentationSource.FromVisual(w);
                double e = src != null ? src.CompositionTarget.TransformToDevice.M11 : 1;
                int amp = (int)Math.Ceiling(w.ActualWidth * e), alt = (int)Math.Ceiling(w.ActualHeight * e);
                RenderTargetBitmap rtb = new RenderTargetBitmap(amp, alt, 96 * e, 96 * e, PixelFormats.Pbgra32);
                rtb.Render(w);
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                string fitxer = Path.Combine(carpeta, nom + ".png");
                using (FileStream fs = File.Create(fitxer)) enc.Save(fs);
                informe.Add("PNG " + nom + ".png " + amp + "x" + alt);
            }
            catch (Exception ex) { informe.Add("ERROR en desar " + nom + ": " + ex); }
        }

        // llengua: opcional, per fer les captures en una llengua concreta sense tocar la configuració
        public static int Executa(string dir, string llengua)
        {
            carpeta = dir ?? Path.Combine(Path.GetTempPath(), "QuinaBarra-prova");
            Directory.CreateDirectory(carpeta);
            G.Prova = true;
            Stopwatch rt = Stopwatch.StartNew();
            try
            {
                G.app = new Application();
                G.app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                G.app.DispatcherUnhandledException += (s, e) => { informe.Add("ERROR no controlat: " + e.Exception); e.Handled = true; };
                G.CreaRadarTimer();
                G.Vist = 0;
                G.Conf = G.LlegeixConfig();
                if (G.Conf == null) { informe.Add("ERROR: no hi ha configuració llegible a " + G.FitxerConf); return Acaba(1); }
                G.TriaLlengua(llengua ?? G.Conf.Llengua);
                informe.Add("Llengua: " + G.Llengua + " (disponibles: " + string.Join(", ", G.LlenguesDisponibles()) + ")");
                informe.Add("Configuració: " + G.Conf.Llocs.Count + " llocs (" + string.Join(", ", G.Conf.Llocs.Select(l => G.Nom(l))) + "), tema " + (G.Conf.Tema ?? "null"));

                // Giny (fora de la pantalla; no es col·loca a la barra)
                CreaGinyProva();
                G.det = G.NovaFinestraDetall();
                G.det.W.ShowActivated = false;
                G.det.W.Show();
                PreparaAlcada();
                Stopwatch sw = Stopwatch.StartNew();
                G.ActualitzaTemps(false);
                informe.Add("Dades baixades en " + sw.ElapsedMilliseconds + " ms");
                if (G.dades == null) informe.Add("ERROR: no hi ha dades (" + G.Err() + ")");
                G.InicialitzaMapa(false);
                Bomba(6000);   // que arribin les imatges del radar
                Desa(G.giny, "giny");
                Desa(G.det.W, "panell");
                G.det.Desplaca.ScrollToEnd();
                Desa(G.det.W, "panell-radar");
                G.det.Desplaca.ScrollToTop();

                // Menú ⋯
                try
                {
                    System.Windows.Controls.ContextMenu cm = G.ObreMenuOpcions();
                    Bomba(300); cm.UpdateLayout();
                    PresentationSource srcM = PresentationSource.FromVisual(G.det.W);
                    double em = srcM != null ? srcM.CompositionTarget.TransformToDevice.M11 : 1;
                    RenderTargetBitmap rm = new RenderTargetBitmap((int)Math.Ceiling(cm.ActualWidth * em), (int)Math.Ceiling(cm.ActualHeight * em), 96 * em, 96 * em, PixelFormats.Pbgra32);
                    rm.Render(cm);
                    PngBitmapEncoder encM = new PngBitmapEncoder(); encM.Frames.Add(BitmapFrame.Create(rm));
                    using (FileStream fsM = File.Create(Path.Combine(carpeta, "menu.png"))) encM.Save(fsM);
                    cm.IsOpen = false;
                    informe.Add("PNG menu.png");
                }
                catch (Exception ex) { informe.Add("Error menú: " + ex.Message); }
                foreach (string clau in new[] { "Pluja", "Vent", "Sol", "UV", "Aire", "Visib", "Humitat", "Pressio", "Mar", "QuantA", "Personalitza" })
                {
                    if (clau == "Mar" && (G.dades == null || G.dades.Mar == null)) { informe.Add("Sense pàgina de mar: el lloc no és de costa"); continue; }
                    try { G.MostraSubpagina(clau); Desa(G.det.W, "sub-" + clau); }
                    catch (Exception ex) { informe.Add("ERROR a la subpàgina " + clau + ": " + ex); }
                    G.AmagaSubpagina();
                }

                // L'altre tema, només a la memòria (no es desa)
                string tema = G.ModeClar() ? "fosc" : "clar";
                G.Conf.Tema = tema;
                G.det.W.Close();
                G.det = G.NovaFinestraDetall();
                G.det.W.ShowActivated = false;
                G.det.W.Show();
                PreparaAlcada();
                G.ActualitzaPanell(false);
                G.InicialitzaMapa(true);
                Bomba(5000);
                Desa(G.det.W, "panell-" + tema);
                G.det.W.Close();

                // Diàleg de la ubicació amb una cerca de «ripoll»
                Tria t = new Tria(null);
                t.W.WindowStartupLocation = WindowStartupLocation.Manual;
                t.W.Left = -3000; t.W.Top = -3000; t.W.ShowActivated = false;
                t.W.Show();
                t.TCerca.Text = "ripoll";
                sw.Restart();
                t.Cerca();
                informe.Add("Cerca de «ripoll» en " + sw.ElapsedMilliseconds + " ms");
                Desa(t.W, "dialeg-ubicacio");
                t.W.Close();

                // Diàleg per canviar el nom
                DemanaText dt = new DemanaText(G.T("lloc-canvia-nom"), G.Nom(G.Conf.Llocs[0]));
                dt.W.WindowStartupLocation = WindowStartupLocation.Manual;
                dt.W.Left = -3000; dt.W.Top = -3000; dt.W.ShowActivated = false;
                dt.W.Show();
                Desa(dt.W, "dialeg-nom");
                dt.W.Close();
            }
            catch (Exception ex) { informe.Add("ERROR: " + ex); return Acaba(1); }
            if (G.TextsQueFalten.Count > 0) informe.Add("ERROR: claus sense text: " + string.Join(", ", G.TextsQueFalten));
            informe.Add("Temps total: " + rt.ElapsedMilliseconds + " ms");
            return Acaba(0);
        }

        static void PreparaAlcada()
        {
            // La mateixa alçada màxima que tindria el panell damunt de la barra real (només es llegeix la posició de la barra)
            IntPtr barraH = Win.FindWindow("Shell_TrayWnd", null);
            PresentationSource src = PresentationSource.FromVisual(G.det.W);
            double e = src != null ? src.CompositionTarget.TransformToDevice.M11 : 1;
            double top = barraH != IntPtr.Zero ? Win.Rect(barraH).Top : 1040;
            G.det.Desplaca.MaxHeight = Math.Max(300.0, Math.Min(1000.0, top / e - 200.0));
        }

        static void CreaGinyProva()
        {
            G.CreaGinyPublic();
            G.giny.ShowActivated = false;
            G.giny.Show();
        }

        static int Acaba(int codi)
        {
            try
            {
                Process p = Process.GetCurrentProcess();
                p.Refresh();
                informe.Add("Memòria: working set " + (p.WorkingSet64 / 1048576) + " MB, pic " + (p.PeakWorkingSet64 / 1048576) + " MB, privada " + (p.PrivateMemorySize64 / 1048576) + " MB");
                File.WriteAllLines(Path.Combine(carpeta, "prova.txt"), informe.ToArray(), new UTF8Encoding(true));
            }
            catch { }
            try { if (G.app != null) G.app.Shutdown(); } catch { }
            return codi;
        }
    }

    static partial class G
    {
        public static void CreaGinyPublic() { CreaGiny(); }
        public static string Err() { return darreraErr; }
    }
}
