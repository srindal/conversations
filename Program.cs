using System;
using System.Collections.Generic;
using System.Linq;
 
namespace ConsoleApplication1
{
    public class Program
    { 
        private static question[] bil = 
        {
            q("I Alka håndterer vi over 30.000 bilskader hvert år. Vil du have nogle generelle råd?")
                .OnYes("Jeg har ingen råd, men nice try"),
            q("Inden vi starter skal vi lige tjekke de oplysninger vi har om dig"),
            q("Er din bil en {carmodel}?")
                .OnNo(
                    q("Så er der noget galt med vores oplysninger. Indtast din bil", "carmodel"),
                    q("Vi har nu registreret at din bil er en {carmodel}")),
            q("Hvor og hvornår skete skaden?", "whereandwhen"),
            q("Hvilke dele af bilen blev beskadiget?", "damage")
                .On("fronten")
                .On("bagsmækken")
                .On("lygterne")
                .On("venstre fordør"),
            q("Hvor mange var I i bilen?", "passengers").OnInterval(1, 4),
        q("Kære {name}. Vi har registreret at du {whereandwhen} har haft en skade på din {carmodel}, hvor I var {passengers} i bilen og {damage} blev beskadiget."),
            q("Helmers autoværksted ringer til dig i morgen, så I kan aftale en tid")
        };

        private static question[] f2 =
        {
            q("Hvad hedder du?", "name"),
            q("Hej {name}, det er aldrig rart når man er ude for et uheld."),
            q("Hvad er gået i stykker?")
                .On("Bil", bil)
                .On("Hest"),
            q("Hav en fortsat god dag")
        };
  
        public static void Main(string[] args)
        {
            var state = new State();
            state.Set("carmodel", "Ford Mustang 1978");
            exec(f2.ToList(), state);
        }
 
        public static void exec(List<question> flow, State st)
        {
            foreach (question q in flow)
            {
                Boolean accepted = false;
                do
                {
                    var res = ask(q, st);
                    accepted = q.isOk(res);
                    if (accepted && q.id != null)
                    {
                        st.Set(q.id, res);
                    }
                    if (!accepted)
                    {
                        Console.WriteLine("!!!Du skal svare rigtigt!!!!");
                    } else if (q.routes.Count > 0) {
                        exec(q.routes[res].ToList(), st);                
                    }
                } while (!accepted);                
            }
        }
 
        private static question q(string q)
        {
            return new question(q, null);
        }
 
        private static question q(string q, string id)
        {
            return new question(q, id);
        }
 
        private static string ask(question q, State st)
        {
            var s = q.q + (q.preds.Keys.Count > 0 ? " (" + String.Join("/", q.preds.Keys) + ")" : "");
            Console.WriteLine(s.StringFormat(st.s));
            return q.id != null || q.preds.Count > 0 ? Console.ReadLine() : null;
        }
 
 
    }
 
 
    public class question
    {
        public readonly string id;
 
        public string q { get; }
 
        public Dictionary<string, question[]> routes = new Dictionary<string, question[]>();
        public Dictionary<string, Predicate<object>> preds = new Dictionary<string, Predicate<object>>();
 
        public question(string q, string id)
        {
            this.id = id;
            this.q = q;
        }
 
        public question RegisterRoute(string key, question[] q)
        {
            routes[key] = q;
            return this;
        }
 
        public question RegisterPred(string key, Predicate<object> p)
        {
            preds[key] = p;
            return this;
        }

        public bool isOk(string key) {
            if (preds.Count == 0) {
                return true;
            }
            if (key == null) {
                return false;
            }
            if (preds.ContainsKey(key)) {
                return preds[key](key);
            } else {
                foreach (var p in preds) {
                    if (preds[p.Key](key)) {
                        return true;
                    }
                }
            
            }
            return false;
        }
    }
 
 
    public class State
    {
        public Dictionary<string, string> s = new Dictionary<string, string>();
 
        public string Get(string key)
        {
            return s.ContainsKey(key) ? s[key] : "";
        }
 
        public State Set(string key, string val)
        {
            s[key] = val;
            return this;
;        }
    }
 
    public static class Extensions
    {
 
        public static question OnYes(this question jn, params question[] q)
        {
            var res = On(jn, "Ja", q);
            if (!jn.routes.ContainsKey("Nej")) {
                jn.OnNo("");
            }
            return res;
        }

        public static question OnYes(this question q, string s) {
            return OnYes(q, new question(s, null));
        }

        public static question OnNo(this question q, string s) {
            return OnNo(q, new question(s, null));
        }


        public static question OnNo(this question jn, params question[] q)
        {
            if (!jn.routes.ContainsKey("Ja")) {
                jn.OnYes("");
            }
            return On(jn, "Nej", q);
        }
 
        public static question On(this question q, string key, Predicate<object> p, params question[] qs)
        {
            return q.RegisterRoute(key, qs).RegisterPred(key, p);
        }

        public static question On(this question q, string key, params question[] qs) {
            return On(q, key, s => s.Equals(key), qs);
        }

        public static question OnInterval(this question q, int from, int to, params question[] qs) {
            return q.RegisterPred(from+"-"+to, s => Int32.Parse(s.ToString()) >= from && to >= Int32.Parse(s.ToString()));
        }
 
 
        public static string StringFormat(this string format, IDictionary<string, string> values)
        {
            foreach (var p in values)
                format = format.Replace("{" + p.Key + "}", p.Value);
            return format;
        }
    }
}