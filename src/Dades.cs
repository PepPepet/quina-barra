// Quina Barra! · versió 1.1 · el temps del teu poble a la barra de tasques, en català i altres llengües
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Dades del temps: Open-Meteo (previsió, aire, mar), platges de la Generalitat i càlculs.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace TempsBarra
{
    public class Hora
    {
        public string Etiqueta; public double? Temp; public string Categoria; public bool Dia; public int Pluja;
        public DateTime Data; public double PlujaMm, Vent, VentDir, Ratxes, UV, Humitat, Visib, Pressio;
    }
    public class DiaPrev
    {
        public string Etiqueta, Data; public double? Max, Min; public string Categoria; public int Pluja;
    }
    public class IcaItem { public string Nom; public double Valor; public int Categoria; public int Hores; }
    public class Ica
    {
        public int Categoria; public double Posicio; public string Nom; public double Valor; public string Detall; public List<IcaItem> Llista;
    }
    public class HoraMar { public string Etiqueta; public double Alcada, Dir, Periode, Fons; }
    public class Mar
    {
        public double Alcada, Dir, Periode, FonsAlcada, FonsPeriode, FonsDir; public double? Aigua; public string Estat; public List<HoraMar> Hores;
    }
    public class Platja
    {
        public string Nom; public DateTime Data; public string Bandera, Motiu, Meduses, Transparencia, Aigua; public double Distancia;
    }
    public class Platges { public List<Platja> Llista; public DateTime Data; public bool EnTemporada; }

    public class Dades
    {
        public double? Temp, Sensacio, Max, Min;
        public string Estat, Categoria; public bool Dia;
        public double? Vent, VentDir, Ratxes, Humitat, Rosada, Pressio, Visibilitat, UV, PlujaAvui, PlujaProb;
        public Ica Aire; public DateTime Ara, Sortida, Posta; public int Offset;
        public List<Hora> Hores; public List<DiaPrev> Dies; public string Resum, HoraText; public int Models;
        public DateTime? Alba, Capvespre; public DateTime SortidaDema, PostaDema;
        public Mar Mar; public Platges Platges;
    }

    class EntradaCache { public Dades D; public DateTime Quan; public bool Complet; }

    static partial class G
    {
        public const string UserAgent = "QuinaBarra/1.1";

        // Codis WMO del temps que fa servir Open-Meteo → clau del text
        static readonly Dictionary<int, string> Estats = new Dictionary<int, string> {
            {0,"estat-0"},{1,"estat-1"},{2,"estat-2"},{3,"estat-3"},
            {45,"estat-45"},{48,"estat-48"},
            {51,"estat-51"},{53,"estat-53"},{55,"estat-55"},{56,"estat-56"},{57,"estat-56"},
            {61,"estat-61"},{63,"estat-63"},{65,"estat-65"},{66,"estat-66"},{67,"estat-66"},
            {71,"estat-71"},{73,"estat-73"},{75,"estat-75"},{77,"estat-77"},
            {80,"estat-80"},{81,"estat-81"},{82,"estat-82"},{85,"estat-85"},{86,"estat-85"},
            {95,"estat-95"},{96,"estat-96"},{99,"estat-96"}
        };

        // Dies de la setmana abreujats (0 = diumenge, com DayOfWeek)
        public static string DiaCurt(DayOfWeek d) { return T("dia-curt-" + (int)d); }

        // ---------- Utilitats ----------
        public static object Web(string url)
        {
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers.Add("User-Agent", UserAgent);
                return J.Parse(wc.DownloadString(url));
            }
        }

        public static DateTime Data(object v) { return DateTime.Parse(J.S(v), Inv); }

        public static string Graus(double? v)
        {
            if (v == null) return "--";
            return Math.Round(v.Value, MidpointRounding.AwayFromZero).ToString("0", Inv);
        }

        public static double? Mitjana(IEnumerable<double?> valors)
        {
            List<double> v = new List<double>();
            foreach (double? x in valors) if (x != null) v.Add(x.Value);
            if (v.Count == 0) return null;
            return v.Average();
        }

        // Número com el mostrava PowerShell dins d'un text ("75", "1.5")
        public static string Num(double? v) { return v == null ? "" : v.Value.ToString(Inv); }

        public static string Categoria(int codi)
        {
            if (codi == 0 || codi == 1) return "clar";
            if (codi == 2) return "mig";
            if (codi == 3) return "nuvol";
            if (codi == 45 || codi == 48) return "boira";
            if (codi >= 51 && codi <= 57) return "plugim";
            if (codi >= 61 && codi <= 67) return "pluja";
            if (codi >= 80 && codi <= 82) return "pluja";
            if (codi >= 71 && codi <= 77) return "neu";
            if (codi == 85 || codi == 86) return "neu";
            if (codi >= 95) return "tempesta";
            return "nuvol";
        }

        static double? ValorModels(object obj, string prefix, int i)
        {
            if (obj == null) return null;
            List<double?> vals = new List<double?>();
            foreach (string m in Models)
            {
                object arr = J.G(obj, prefix + "_" + m);
                if (J.Truthy(arr) && i < J.Len(arr)) vals.Add(J.D(J.At(arr, i)));
            }
            return Mitjana(vals);
        }

        static string Resum(object d, int i0, List<DiaPrev> dies)
        {
            List<string> frases = new List<string>();
            object hourly = J.G(d, "hourly");
            object probsArr = J.G(hourly, "precipitation_probability");
            object timeArr = J.G(hourly, "time");
            List<int> probs = new List<int>();
            List<string> hores = new List<string>();
            for (int k = 0; k < 12; k++) { int i = i0 + k; if (i < J.Len(probsArr)) probs.Add(J.I(J.At(probsArr, i))); }
            for (int k = 0; k < 12; k++) { int i = i0 + k; if (i < J.Len(timeArr)) hores.Add(Data(J.At(timeArr, i)).ToString("H:mm", Inv)); }
            string catAra = Categoria(J.I(J.G(d, "current", "weather_code")));
            if (catAra == "pluja" || catAra == "plugim" || catAra == "tempesta" || catAra == "neu")
            {
                int para = 0;
                for (int k = 1; k < probs.Count; k++) if (probs[k] < 30) { para = k; break; }
                frases.Add(para > 0 && para < hores.Count ? T("resum-plou-parara", hores[para]) : T("resum-plou-seguira"));
            }
            else
            {
                int k50 = 0, k30 = 0;
                for (int k = 1; k < probs.Count; k++) if (probs[k] >= 50) { k50 = k; break; }
                for (int k = 1; k < probs.Count; k++) if (probs[k] >= 30) { k30 = k; break; }
                if (k50 > 0 && k50 < hores.Count) frases.Add(T("resum-pluja-probable", hores[k50]));
                else if (k30 > 0 && k30 < hores.Count) frases.Add(T("resum-pluja-possible", hores[k30]));
                else frases.Add(T("resum-sense-pluja"));
            }
            if (dies.Count >= 3)
            {
                double dif = (((dies[1].Max ?? 0) + (dies[2].Max ?? 0)) / 2) - (dies[0].Max ?? 0);
                if (dif <= -3) frases.Add(T("resum-temp-baixen-molt"));
                else if (dif <= -1) frases.Add(T("resum-temp-baixen"));
                else if (dif >= 3) frases.Add(T("resum-temp-pugen-molt"));
                else if (dif >= 1) frases.Add(T("resum-temp-pugen"));
                else frases.Add(T("resum-temp-estables"));
            }
            object ratxes = J.G(d, "current", "wind_gusts_10m");
            if (J.Dz(ratxes) >= 50) frases.Add(T("resum-ratxes", Graus(J.D(ratxes))));
            if (J.Dz(J.At(J.G(d, "daily", "uv_index_max"), 0)) >= 6 && J.I(J.G(d, "current", "is_day")) == 1) frases.Add(T("resum-uv-alt"));
            return string.Join(" ", frases);
        }

        // ---------- Índex de Qualitat de l'Aire (ICA) del MITECO, el que fa servir també la Generalitat ----------
        // Partícules: mitjana de 24 h; ozó: mitjana de 8 h; NO2 i SO2: darrera hora. Llindars en µg/m³.
        public static string IcaNom(int categoria) { return T("ica-" + categoria); }   // 0 = bona ... 5 = extremadament desfavorable
        public static readonly string[] IcaColors = { "#50F0E6", "#50CCAA", "#F0E641", "#FF5050", "#960032", "#7D2181" };
        class Contaminant { public string Clau, Nom;  /* Nom: clau del text */ public int Hores; public double[] Llindars; }
        static readonly Contaminant[] IcaContaminants = {
            new Contaminant { Clau = "pm2_5", Nom = "ica-pm25", Hores = 24, Llindars = new double[] { 10, 20, 25, 50, 75, 800 } },
            new Contaminant { Clau = "pm10", Nom = "ica-pm10", Hores = 24, Llindars = new double[] { 20, 40, 50, 100, 150, 1200 } },
            new Contaminant { Clau = "ozone", Nom = "ica-ozo", Hores = 8, Llindars = new double[] { 50, 100, 130, 240, 380, 800 } },
            new Contaminant { Clau = "nitrogen_dioxide", Nom = "ica-no2", Hores = 1, Llindars = new double[] { 40, 90, 120, 230, 340, 1000 } },
            new Contaminant { Clau = "sulphur_dioxide", Nom = "ica-so2", Hores = 1, Llindars = new double[] { 100, 200, 350, 500, 750, 1250 } }
        };

        static Ica CalculaIca(object aq)
        {
            DateTime araLoc = DateTime.UtcNow.AddSeconds(J.I(J.G(aq, "utc_offset_seconds")));
            object temps = J.G(aq, "hourly", "time");
            int i = Array.IndexOf(J.A(temps).Select(x => J.S(x)).ToArray(), araLoc.ToString("yyyy-MM-ddTHH:00", Inv));
            if (i < 0) return null;
            Ica pitjor = null;
            List<string> detall = new List<string>();
            List<IcaItem> llista = new List<IcaItem>();
            foreach (Contaminant c in IcaContaminants)
            {
                object serie = J.G(aq, "hourly", c.Clau);
                List<double?> vals = new List<double?>();
                for (int k = Math.Max(0, i - c.Hores + 1); k <= i; k++) vals.Add(J.D(J.At(serie, k)));
                double? vv = Mitjana(vals);
                if (vv == null) continue;
                double v = vv.Value;
                int cat = 0;
                while (cat < 5 && v > c.Llindars[cat]) cat++;
                double baix = cat == 0 ? 0.0 : c.Llindars[cat - 1];
                double frac = Math.Max(0.0, Math.Min(1.0, (v - baix) / (c.Llindars[cat] - baix)));
                Ica o = new Ica { Categoria = cat, Posicio = (cat + frac) / 6.0, Nom = T(c.Nom), Valor = v };
                detall.Add(T("ica-detall", T(c.Nom), Math.Round(v).ToString(Inv), IcaNom(cat).ToLower()));
                llista.Add(new IcaItem { Nom = T(c.Nom), Valor = v, Categoria = cat, Hores = c.Hores });
                if (pitjor == null || o.Posicio > pitjor.Posicio) pitjor = o;
            }
            if (pitjor != null) { pitjor.Detall = string.Join("\n", detall); pitjor.Llista = llista; }
            return pitjor;
        }

        // Alba i capvespre (crepuscle civil, sol a 6 graus per sota de l'horitzó)
        static DateTime? Crepuscle(DateTime dia, double lat, double lon, int offset, bool mati)
        {
            double rad = Math.PI / 180;
            double n = dia.DayOfYear, lh = lon / 15;
            double tt = mati ? n + ((6 - lh) / 24) : n + ((18 - lh) / 24);
            double m = 0.9856 * tt - 3.289;
            double l = (m + 1.916 * Math.Sin(m * rad) + 0.020 * Math.Sin(2 * m * rad) + 282.634) % 360; if (l < 0) l += 360;
            double ra = (Math.Atan(0.91764 * Math.Tan(l * rad)) / rad) % 360; if (ra < 0) ra += 360;
            ra = (ra + (Math.Floor(l / 90) * 90 - Math.Floor(ra / 90) * 90)) / 15;
            double sinDec = 0.39782 * Math.Sin(l * rad), cosDec = Math.Cos(Math.Asin(sinDec));
            double cosH = (Math.Cos(96 * rad) - sinDec * Math.Sin(lat * rad)) / (cosDec * Math.Cos(lat * rad));
            if (cosH > 1 || cosH < -1) return null;
            double h = Math.Acos(cosH) / rad; if (mati) h = 360 - h; h /= 15;
            double ut = (h + ra - 0.06571 * tt - 6.622 - lh) % 24; if (ut < 0) ut += 24;
            return dia.Date.AddHours(ut).AddSeconds(offset);
        }

        static double Distancia(double la1, double lo1, double la2, double lo2)
        {
            double r = Math.PI / 180;
            double a = Math.Pow(Math.Sin((la2 - la1) * r / 2), 2) + Math.Cos(la1 * r) * Math.Cos(la2 * r) * Math.Pow(Math.Sin((lo2 - lo1) * r / 2), 2);
            return 6371 * 2 * Math.Asin(Math.Sqrt(a));
        }

        // Escala de Douglas (estat de la mar), amb els noms que fa servir Meteocat
        static string EstatMar(double h)
        {
            if (h < 0.05) return T("mar-douglas-0");
            if (h <= 0.1) return T("mar-douglas-1");
            if (h <= 0.5) return T("mar-douglas-2");
            if (h <= 1.25) return T("mar-douglas-3");
            if (h <= 2.5) return T("mar-douglas-4");
            if (h <= 4) return T("mar-douglas-5");
            if (h <= 6) return T("mar-douglas-6");
            if (h <= 9) return T("mar-douglas-7");
            return T("mar-douglas-8");
        }

        static int IndexHora(object temps, DateTime ara)
        {
            string clau = ara.ToString("yyyy-MM-ddTHH:00", Inv);
            object[] a = J.A(temps);
            for (int i = 0; i < a.Length; i++) if (J.S(a[i]) == clau) return i;
            return -1;
        }

        static Mar LlegeixMar(Dictionary<string, object> c)
        {
            string lat = Lat(c).ToString(Inv), lon = Lon(c).ToString(Inv);
            object m;
            try
            {
                m = Web("https://marine-api.open-meteo.com/v1/marine?latitude=" + lat + "&longitude=" + lon + "&timezone=auto&forecast_days=2" +
                    "&current=wave_height,wave_direction,wave_period,swell_wave_height,swell_wave_period,swell_wave_direction,sea_surface_temperature" +
                    "&hourly=wave_height,wave_direction,wave_period,swell_wave_height");
            }
            catch { return null; }
            object cur = J.G(m, "current");
            if (J.G(cur, "wave_height") == null) return null;
            // Només als llocs de costa: el punt de mar més proper ha de ser a prop
            if (Distancia(Lat(c), Lon(c), J.Dz(J.G(m, "latitude")), J.Dz(J.G(m, "longitude"))) > 18) return null;
            DateTime ara = Data(J.G(cur, "time"));
            object h = J.G(m, "hourly");
            object temps = J.G(h, "time");
            int i0 = IndexHora(temps, ara); if (i0 < 0) i0 = 0;
            List<HoraMar> hores = new List<HoraMar>();
            for (int k = 0; k < 24; k++)
            {
                int i = i0 + k;
                if (i >= J.Len(temps) || J.At(J.G(h, "wave_height"), i) == null) break;
                hores.Add(new HoraMar
                {
                    Etiqueta = k == 0 ? T("hora-ara") : Data(J.At(temps, i)).ToString("H:mm", Inv),
                    Alcada = J.Dz(J.At(J.G(h, "wave_height"), i)), Dir = J.Dz(J.At(J.G(h, "wave_direction"), i)),
                    Periode = J.Dz(J.At(J.G(h, "wave_period"), i)), Fons = J.Dz(J.At(J.G(h, "swell_wave_height"), i))
                });
            }
            return new Mar
            {
                Alcada = J.Dz(J.G(cur, "wave_height")), Dir = J.Dz(J.G(cur, "wave_direction")), Periode = J.Dz(J.G(cur, "wave_period")),
                FonsAlcada = J.Dz(J.G(cur, "swell_wave_height")), FonsPeriode = J.Dz(J.G(cur, "swell_wave_period")), FonsDir = J.Dz(J.G(cur, "swell_wave_direction")),
                Aigua = J.D(J.G(cur, "sea_surface_temperature")), Estat = EstatMar(J.Dz(J.G(cur, "wave_height"))), Hores = hores
            };
        }

        // Estat diari de les platges (dades obertes de la Generalitat): bandera, meduses, transparència, aigua.
        // Només hi ha dades en temporada de bany (de juny a setembre); fora de temporada es mostra l'últim estat.
        // El color de la bandera arriba en català a les dades («verda», «groga», «vermella»)
        public static string NomBandera(string b)
        {
            if (string.IsNullOrEmpty(b)) return b;
            string clau = "platja-bandera-" + b.ToLowerInvariant();
            return TeText(clau) ? T(clau) : b;
        }

        public static readonly Dictionary<string, string> BanderaColors = new Dictionary<string, string> { { "verda", "#2FB344" }, { "groga", "#F2C200" }, { "vermella", "#E5484D" } };

        static bool NoNA(string s) { return !string.IsNullOrEmpty(s) && s != "N/A"; }

        static Platges LlegeixPlatges(Dictionary<string, object> c)
        {
            string codi = Codi(c);
            if (codi == null) return null;
            string bas = "https://analisi.transparenciacatalunya.cat/resource/4baz-cjv2.json";
            object[] files = new object[0];
            foreach (int any in new int[] { DateTime.Now.Year, DateTime.Now.Year - 1 })
            {
                try
                {
                    string w = Uri.EscapeDataString("municipi_codimunicipi='" + codi + "' and estat_data like '%/" + any + "%'");
                    files = J.A(Web(bas + "?$where=" + w + "&$order=:created_at%20DESC&$limit=300"));
                }
                catch { files = new object[0]; }
                if (files.Length > 0) break;
            }
            if (files.Length == 0) return null;
            Dictionary<string, Platja> darrer = new Dictionary<string, Platja>();
            foreach (object r in files)
            {
                DateTime dt;
                try { dt = DateTime.ParseExact(J.S(J.G(r, "estat_data")).Substring(0, 10), "dd/MM/yyyy", Inv); }
                catch { continue; }
                string nom = J.S(J.G(r, "platja")) ?? "";
                Platja ant;
                if (!darrer.TryGetValue(nom, out ant) || dt > ant.Data)
                {
                    string med = null;
                    string meduses = J.S(J.G(r, "estat_meduses"));
                    if (NoNA(meduses))
                    {
                        string[] pm = meduses.Split(',');
                        med = pm.Length >= 2 ? pm[0].Trim() + " (" + pm[1].Trim() + ")" : pm[0];
                    }
                    double dist;
                    try { dist = Distancia(Lat(c), Lon(c), J.Dz(J.G(r, "coordenada_x")), J.Dz(J.G(r, "coordenada_y"))); }
                    catch { dist = 99; }
                    string motiu = J.S(J.G(r, "estat_motiubandera"));
                    string transp = J.S(J.G(r, "estat_transparenciaaigua"));
                    string aigua = J.S(J.G(r, "estat_temperatura"));
                    darrer[nom] = new Platja
                    {
                        Nom = nom, Data = dt, Bandera = J.S(J.G(r, "estat_bandera")),
                        Motiu = NoNA(motiu) ? motiu : null,
                        Meduses = med, Transparencia = transp != "N/A" ? transp : null,
                        Aigua = NoNA(aigua) ? aigua : null,
                        Distancia = dist
                    };
                }
            }
            List<Platja> llista = darrer.Values.OrderBy(x => x.Distancia).ToList();
            DateTime mesRecent = llista.Max(x => x.Data);
            return new Platges { Llista = llista, Data = mesRecent, EnTemporada = (DateTime.Now.Date - mesRecent).TotalDays <= 3 };
        }

        public static string TextPlatja(Platja pl)
        {
            List<string> parts = new List<string>();
            parts.Add(T("platja-bandera", NomBandera(pl.Bandera)));
            if (!string.IsNullOrEmpty(pl.Motiu)) parts[0] += " (" + pl.Motiu.ToLower() + ")";
            if (!string.IsNullOrEmpty(pl.Transparencia)) parts.Add(T("platja-aigua", pl.Transparencia));
            if (!string.IsNullOrEmpty(pl.Aigua)) parts.Add(pl.Aigua + " °C");
            parts.Add(!string.IsNullOrEmpty(pl.Meduses) ? T("platja-meduses", pl.Meduses) : T("platja-sense-meduses"));
            return string.Join(" · ", parts);
        }

        // ---------- Memòria cau per lloc: les dades completes es baixen quan cal i duren 15 minuts ----------
        static readonly Dictionary<string, EntradaCache> cache = new Dictionary<string, EntradaCache>();

        public static Dades DadesLloc(Dictionary<string, object> lloc, bool forca)
        {
            string k = ClauLloc(lloc);
            EntradaCache e;
            cache.TryGetValue(k, out e);
            if (!forca && e != null && e.Complet && (DateTime.Now - e.Quan).TotalMinutes < Minuts) return e.D;
            Dades d = LlegeixTemps(lloc);
            Mar mar = BlocVisible("Mar") ? LlegeixMar(lloc) : null;
            d.Mar = mar;
            d.Platges = mar != null ? LlegeixPlatges(lloc) : null;
            cache[k] = new EntradaCache { D = d, Quan = DateTime.Now, Complet = true };
            return d;
        }

        // Per a les pastilles n'hi ha prou amb la temperatura i el cel d'ara
        public static Dades DadesBreus(Dictionary<string, object> lloc)
        {
            string k = ClauLloc(lloc);
            EntradaCache e;
            cache.TryGetValue(k, out e);
            if (e != null && (DateTime.Now - e.Quan).TotalMinutes < Minuts) return e.D;
            try
            {
                string lat = Lat(lloc).ToString(Inv), lon = Lon(lloc).ToString(Inv);
                object r = Web("https://api.open-meteo.com/v1/forecast?latitude=" + lat + "&longitude=" + lon + "&timezone=auto&current=temperature_2m,weather_code,is_day");
                Dades d = new Dades
                {
                    Temp = J.D(J.G(r, "current", "temperature_2m")),
                    Categoria = Categoria(J.I(J.G(r, "current", "weather_code"))),
                    Dia = J.I(J.G(r, "current", "is_day")) == 1
                };
                if (e == null) cache[k] = new EntradaCache { D = d, Quan = DateTime.Now, Complet = false };
                return d;
            }
            catch { return null; }
        }

        static Dades LlegeixTemps(Dictionary<string, object> c)
        {
            string lat = Lat(c).ToString(Inv), lon = Lon(c).ToString(Inv);
            string bas = "https://api.open-meteo.com/v1/forecast?latitude=" + lat + "&longitude=" + lon + "&timezone=auto";
            object d = Web(bas + "&forecast_days=10" +
                "&current=temperature_2m,apparent_temperature,weather_code,is_day,wind_speed_10m,wind_direction_10m,wind_gusts_10m,relative_humidity_2m,dew_point_2m,pressure_msl,visibility,uv_index" +
                "&hourly=temperature_2m,weather_code,precipitation_probability,is_day,precipitation,wind_speed_10m,wind_direction_10m,wind_gusts_10m,uv_index,relative_humidity_2m,visibility,pressure_msl" +
                "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,precipitation_sum,sunrise,sunset,uv_index_max");

            // Temperatures: mitjana de tres models (el model per defecte sol anar curt a ciutat)
            object mm = null;
            try { mm = Web(bas + "&forecast_days=10&hourly=temperature_2m&daily=temperature_2m_max,temperature_2m_min&models=" + string.Join(",", Models)); } catch { }
            List<double?> t = new List<double?>(), s = new List<double?>();
            foreach (string m in Models)
            {
                try
                {
                    object r = Web(bas + "&forecast_days=1&models=" + m + "&current=temperature_2m,apparent_temperature");
                    t.Add(J.D(J.G(r, "current", "temperature_2m")));
                    s.Add(J.D(J.G(r, "current", "apparent_temperature")));
                }
                catch { }
            }
            object cur = J.G(d, "current");
            double? temp = Mitjana(t); if (temp == null) temp = J.D(J.G(cur, "temperature_2m"));
            double? sens = Mitjana(s); if (sens == null) sens = J.D(J.G(cur, "apparent_temperature"));

            // Qualitat de l'aire: índex ICA (escala oficial del MITECO i la Generalitat)
            Ica aqi = null;
            if (BlocVisible("Aire")) try { aqi = CalculaIca(Web("https://air-quality-api.open-meteo.com/v1/air-quality?latitude=" + lat + "&longitude=" + lon + "&hourly=pm2_5,pm10,ozone,nitrogen_dioxide,sulphur_dioxide&past_days=1&forecast_days=1&timezone=auto")); }
            catch { }

            DateTime ara = Data(J.G(cur, "time"));
            object h = J.G(d, "hourly");
            object ht = J.G(h, "time");
            int i0 = IndexHora(ht, ara); if (i0 < 0) i0 = 0;

            List<Hora> hores = new List<Hora>();
            object mmH = J.G(mm, "hourly");
            for (int k = 0; k < 24; k++)
            {
                int i = i0 + k; if (i >= J.Len(ht)) break;
                double? th;
                if (k == 0) th = temp;
                else { th = ValorModels(mmH, "temperature_2m", i); if (th == null) th = J.D(J.At(J.G(h, "temperature_2m"), i)); }
                DateTime hi = Data(J.At(ht, i));
                hores.Add(new Hora
                {
                    Etiqueta = k == 0 ? T("hora-ara") : hi.ToString("H:mm", Inv),
                    Temp = th, Categoria = Categoria(J.I(J.At(J.G(h, "weather_code"), i))), Dia = J.I(J.At(J.G(h, "is_day"), i)) == 1,
                    Pluja = J.I(J.At(J.G(h, "precipitation_probability"), i)),
                    Data = hi.Date,
                    PlujaMm = J.Dz(J.At(J.G(h, "precipitation"), i)),
                    Vent = J.Dz(J.At(J.G(h, "wind_speed_10m"), i)), VentDir = J.Dz(J.At(J.G(h, "wind_direction_10m"), i)), Ratxes = J.Dz(J.At(J.G(h, "wind_gusts_10m"), i)),
                    UV = J.Dz(J.At(J.G(h, "uv_index"), i)), Humitat = J.Dz(J.At(J.G(h, "relative_humidity_2m"), i)),
                    Visib = J.Dz(J.At(J.G(h, "visibility"), i)), Pressio = J.Dz(J.At(J.G(h, "pressure_msl"), i))
                });
            }
            object dl = J.G(d, "daily");
            object mmD = J.G(mm, "daily");
            List<DiaPrev> dies = new List<DiaPrev>();
            for (int j = 0; j < J.Len(J.G(dl, "time")); j++)
            {
                DateTime data = Data(J.At(J.G(dl, "time"), j));
                double? mx = ValorModels(mmD, "temperature_2m_max", j); if (mx == null) mx = J.D(J.At(J.G(dl, "temperature_2m_max"), j));
                double? mn = ValorModels(mmD, "temperature_2m_min", j); if (mn == null) mn = J.D(J.At(J.G(dl, "temperature_2m_min"), j));
                dies.Add(new DiaPrev
                {
                    Etiqueta = j == 0 ? T("dia-avui") : (j == 1 ? T("dia-dema") : DiaCurt(data.DayOfWeek)),
                    Data = data.ToString("d/M", Inv), Max = mx, Min = mn,
                    Categoria = Categoria(J.I(J.At(J.G(dl, "weather_code"), j))), Pluja = J.I(J.At(J.G(dl, "precipitation_probability_max"), j))
                });
            }

            int codi = J.I(J.G(cur, "weather_code"));
            DateTime sortida = Data(J.At(J.G(dl, "sunrise"), 0)), posta = Data(J.At(J.G(dl, "sunset"), 0));
            string cat = Categoria(codi);
            if (cat == "clar" || cat == "mig")
            {
                if (Math.Abs((ara - sortida).TotalMinutes) <= 30) cat = "sortida";
                else if (Math.Abs((ara - posta).TotalMinutes) <= 30) cat = "posta";
            }
            string estat;
            if (Estats.TryGetValue(codi, out estat)) estat = T(estat); else estat = T("estat-desconegut");
            int offset = J.I(J.G(d, "utc_offset_seconds"));

            return new Dades
            {
                Temp = temp, Sensacio = sens, Max = dies.Count > 0 ? dies[0].Max : null, Min = dies.Count > 0 ? dies[0].Min : null,
                Estat = estat, Categoria = cat, Dia = J.I(J.G(cur, "is_day")) == 1,
                Vent = J.D(J.G(cur, "wind_speed_10m")), VentDir = J.D(J.G(cur, "wind_direction_10m")), Ratxes = J.D(J.G(cur, "wind_gusts_10m")),
                Humitat = J.D(J.G(cur, "relative_humidity_2m")), Rosada = J.D(J.G(cur, "dew_point_2m")),
                Pressio = J.D(J.G(cur, "pressure_msl")), Visibilitat = J.D(J.G(cur, "visibility")), UV = J.D(J.G(cur, "uv_index")),
                PlujaAvui = J.D(J.At(J.G(dl, "precipitation_sum"), 0)), PlujaProb = J.D(J.At(J.G(dl, "precipitation_probability_max"), 0)),
                Aire = aqi, Ara = ara, Sortida = sortida, Posta = posta, Offset = offset,
                Hores = hores, Dies = dies, Resum = Resum(d, i0, dies),
                HoraText = DateTime.Now.ToString("HH:mm", Inv), Models = t.Count,
                Alba = Crepuscle(ara.Date, Lat(c), Lon(c), offset, true),
                Capvespre = Crepuscle(ara.Date, Lat(c), Lon(c), offset, false),
                SortidaDema = Data(J.At(J.G(dl, "sunrise"), 1)), PostaDema = Data(J.At(J.G(dl, "sunset"), 1))
            };
        }
    }
}
