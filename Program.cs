using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
 
namespace ConsoleApplication1
{
    public class Program
    {
 
        private static Predicate<object> f = o => (o.ToString().Equals("Bil"));
 
 
        private static question[] f3 = { q("Det var ?rgerligt")};
 
        private static question[] f2 =
        {
            q("Hvad hedder du?", "name"),
            q("Hej {name}"),
            q("Hvad er g?et i stykker?", "skaden")
                .On("Bil", f, f3)
                .On("Hest", f, f3),
            q("Det var kedelig med din {skaden} {name}")
        };
 
 
        /*private static question[] f1 =
        {
           qInt("Hvor hurtigt k?rte du?", i => i <= 130)
                .OnNo(q("Du har k?rt for hurtigt. Det d?kker vi ikke")),
            jn("K?rte du selv bilen?")
                .OnNo(q("S? er du ikke d?kket"))
                .OnYes(q("Der var du heldig"))
        };
 
        static question[] flow = {
            q("K?re John. Det er aldrig sjovt at f? en skade, men lad os sammen lave en skadesanmeldelse, s? klarer vi nemt og hurtigt papirarbejdet. Hvad er der sket?"),
            jn("Vil du registrere noget med en bil").OnYes(f1),
            qInt("Hvor gammel er du?", i => i >= 100).OnYes(q("Det tror jeg ikke p?")),
 
            jn("I Alka h?ndterer vi hvert ?r 30.000 bilskader. Vil du have r?d?")
                .OnYes(q("hej med digh"))
                .OnNo(
                    q("Inden vi starter vil vi gerne lige tjekke at vi har de rigtige oplysninger om dig."),
                    q("Tak for det Peter. Nu g?r vi igang med anmeldelsen. Hvilken bil har f?et skade?"),
                    q("Hvor og hvorn?r skete skaden?"),
                    jn("K?rte du selv bilen?")
                        .OnNo(q("S? kan vi ikke hj?lpe dig")),
                    q("Hvilke dele af bilen blev beskadiget?"))
        };
 
*/
 
        public static void Main(string[] args)
        {
            exec(f2.ToList(), new State());
        }
 
        public static void exec(List<question> flow, State st)
        {
            foreach (question q in flow)
            {
                Boolean accepted = false;
                do
                {
                    var res = ask(q, st);
                    accepted = q.preds.Count == 0 || q.preds.ContainsKey(res) && q.preds[res](res);
                    if (accepted && q.id != null)
                    {
                        st.Set(q.id, res);
                    }
                    if (!accepted)
                    {
                        Console.WriteLine("!!!Du skal svare rigtigt!!!!");
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
            var s = q.q + (q.preds.Keys.Count > 0 ? "(" + String.Join("/", q.preds.Keys) + ")" : "");
            Console.WriteLine(s.StringFormat(st.s));
            return Console.ReadLine();
        }
 
 
    }
 
 
    public class question
    {
        public readonly string id;
 
        public string q { get; }
 
        public Dictionary<object, question[]> routes = new Dictionary<object, question[]>();
        public Dictionary<object, Predicate<object>> preds = new Dictionary<object, Predicate<object>>();
 
        public question(string q, string id)
        {
            this.id = id;
            this.q = q;
        }
 
        public question RegisterRoute(object key, question[] q)
        {
            routes.Add(key, q);
            return this;
        }
 
        public question RegisterPred(object key, Predicate<object> p)
        {
            preds.Add(key, p);
            return this;
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
            return On(jn, "Ja", s => !s.ToString().Equals("Nej"), q);
        }
 
        public static question OnNo(this question jn, params question[] q)
        {
            return On(jn, "Nej", s => s.ToString().Equals("Nej"), q);
        }
 
        public static question On(this question q, string key, Predicate<object> p, params question[] qs)
        {
            return q.RegisterRoute(key, qs).RegisterPred(key, p);
        }
 
        public static string StringFormat(this string format, IDictionary<string, string> values)
        {
            foreach (var p in values)
                format = format.Replace("{" + p.Key + "}", p.Value);
            return format;
        }
    }
}