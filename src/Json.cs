// Quina Barra! · versió 1.2 · el temps del teu poble a la barra de tasques, en català i altres llengües
// Copyright (c) 2026 Pep Payo · https://peppayo.com · llicència MIT (vegeu LICENSE)
// Lectura i escriptura de JSON, amb les mateixes conversions que feia la versió de PowerShell.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;

namespace TempsBarra
{
    static class J
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static object Parse(string text)
        {
            JavaScriptSerializer s = new JavaScriptSerializer();
            s.MaxJsonLength = int.MaxValue;
            s.RecursionLimit = 1000;
            return s.DeserializeObject(text);
        }

        // Propietat d'un objecte (null si no hi és)
        public static object G(object o, string k)
        {
            IDictionary<string, object> d = o as IDictionary<string, object>;
            object v;
            if (d != null && d.TryGetValue(k, out v)) return v;
            return null;
        }
        public static object G(object o, string k1, string k2) { return G(G(o, k1), k2); }

        // Llista (un sol element es torna llista d'un, com @() a PowerShell)
        public static object[] A(object o)
        {
            if (o == null) return new object[0];
            object[] a = o as object[];
            if (a != null) return a;
            if (o is string || o is IDictionary) return new object[] { o };
            IEnumerable e = o as IEnumerable;
            if (e != null) { List<object> l = new List<object>(); foreach (object x in e) l.Add(x); return l.ToArray(); }
            return new object[] { o };
        }

        // Element i d'una llista (null si no hi és)
        public static object At(object arr, int i)
        {
            if (arr == null || i < 0) return null;
            object[] a = arr as object[];
            if (a != null) return i < a.Length ? a[i] : null;
            IList l = arr as IList;
            if (l != null) return i < l.Count ? l[i] : null;
            return i == 0 ? arr : null;
        }

        public static int Len(object arr)
        {
            if (arr == null) return 0;
            object[] a = arr as object[];
            if (a != null) return a.Length;
            IList l = arr as IList;
            if (l != null) return l.Count;
            return 1;
        }

        // Número o null (sense errors)
        public static double? D(object v)
        {
            if (v == null) return null;
            if (v is double) return (double)v;
            if (v is int) return (int)v;
            if (v is long) return (long)v;
            if (v is decimal) return (double)(decimal)v;
            if (v is float) return (float)v;
            if (v is bool) return (bool)v ? 1 : 0;
            string s = v as string;
            if (s != null)
            {
                if (s.Trim().Length == 0) return 0;
                double r;
                if (double.TryParse(s.Trim(), NumberStyles.Float, Inv, out r)) return r;
                return null;
            }
            try { return Convert.ToDouble(v, Inv); } catch { return null; }
        }

        // Com [double]$x a PowerShell: null dona 0 i un text que no és número dona error
        public static double Dz(object v)
        {
            if (v == null) return 0;
            double? d = D(v);
            if (d == null) throw new FormatException("No és un número: " + v);
            return d.Value;
        }

        // Com [int]$x a PowerShell (arrodoniment bancari)
        public static int I(object v)
        {
            if (v == null) return 0;
            if (v is int) return (int)v;
            return Convert.ToInt32(Dz(v));
        }

        public static string S(object v)
        {
            if (v == null) return null;
            string s = v as string;
            if (s != null) return s;
            IFormattable f = v as IFormattable;
            if (f != null) return f.ToString(null, Inv);
            return v.ToString();
        }

        // Com a PowerShell: null, "", 0 i llistes buides són fals
        public static bool Truthy(object v)
        {
            if (v == null) return false;
            if (v is bool) return (bool)v;
            string s = v as string;
            if (s != null) return s.Length > 0;
            if (v is IDictionary) return true;
            ICollection c = v as ICollection;
            if (c != null)
            {
                if (c.Count == 0) return false;
                if (c.Count == 1) { foreach (object x in c) return Truthy(x); }
                return true;
            }
            double? d = D(v);
            if (d != null) return d.Value != 0;
            return true;
        }

        // ---------- Escriptura ----------
        public static string Escriu(object o)
        {
            StringBuilder sb = new StringBuilder();
            Escriu(sb, o, 0);
            return sb.ToString();
        }

        static void Sagnat(StringBuilder sb, int n) { sb.Append(' ', n * 4); }

        static void Escriu(StringBuilder sb, object o, int n)
        {
            if (o == null) { sb.Append("null"); return; }
            string s = o as string;
            if (s != null) { Text(sb, s); return; }
            if (o is bool) { sb.Append((bool)o ? "true" : "false"); return; }
            IDictionary<string, object> d = o as IDictionary<string, object>;
            if (d != null)
            {
                if (d.Count == 0) { sb.Append("{}"); return; }
                sb.Append("{\r\n");
                int k = 0;
                foreach (KeyValuePair<string, object> kv in d)
                {
                    Sagnat(sb, n + 1); Text(sb, kv.Key); sb.Append(": ");
                    Escriu(sb, kv.Value, n + 1);
                    if (++k < d.Count) sb.Append(',');
                    sb.Append("\r\n");
                }
                Sagnat(sb, n); sb.Append('}');
                return;
            }
            IEnumerable e = o as IEnumerable;
            if (e != null)
            {
                List<object> l = new List<object>();
                foreach (object x in e) l.Add(x);
                if (l.Count == 0) { sb.Append("[]"); return; }
                sb.Append("[\r\n");
                for (int i = 0; i < l.Count; i++)
                {
                    Sagnat(sb, n + 1); Escriu(sb, l[i], n + 1);
                    if (i < l.Count - 1) sb.Append(',');
                    sb.Append("\r\n");
                }
                Sagnat(sb, n); sb.Append(']');
                return;
            }
            if (o is double) { double v = (double)o; sb.Append(double.IsNaN(v) || double.IsInfinity(v) ? "null" : v.ToString("R", Inv)); return; }
            if (o is float) { sb.Append(((float)o).ToString("R", Inv)); return; }
            IFormattable f = o as IFormattable;
            if (f != null) { sb.Append(f.ToString(null, Inv)); return; }
            Text(sb, o.ToString());
        }

        static void Text(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
