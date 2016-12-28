using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication
{
    public class Program
    {
        static question[] flow = {
            jn("Har du haft en skade?")
                .onYes(                    
                    jn("Er det din bil der er gået i stykker?")
                        .onNo(q("Så kan jeg ikke hjælpe dig"))
                        .onYes(
                            qInt("Hvor hurtigt kørte du?", i => i <= 130)
                                .onNo(q("Du har kørt for hurtigt. Det dækker vi ikke")),
                            jn("Kørte du selv bilen?")
                                .onNo(q("Så er du ikke dækket"))
                                .onYes(q("Der var du heldig"))
                        )
                ),
            q("Hold dig munter")
        };
        
        
        public static void Main(string[] args)
        {
            exec(flow.ToList());                         
        }

        public static void exec(List<question> flow) {
            foreach (question q in flow) {
                if (q.GetType().Equals(typeof(janej))) {
                    var res = askYesNo(q.q);
                    if (q.routes.ContainsKey(res)) {
                        exec(q.routes[res].ToList());
                    }
                } else if (q.GetType().Equals(typeof(intq))) {
                    var res = Int32.Parse(ask(q.q));
                    var ok = (((intq) q).isOk(res));
                    if (q.routes.ContainsKey(ok)) {
                        exec(q.routes[ok].ToList());
                        break;
                    }
                } else {
                    ask(q.q);
                }
            }
        }

        private static question q(string q) {
            return new question(q);
        }

        private static string ask(string q) {
            Console.WriteLine(q);
            return Console.ReadLine();
        }

        private static bool askYesNo(string q) {
            return ask(q + " (Ja/Nej)").Equals("Ja");
        }

        public static janej jn(string q) {
            return new janej(q);
        }

        private static intq qInt(string q, Predicate<int> p) {
            return new intq(q, p);
        }
    }

    
    

    public class question {

        public string q {get; private set;}
        
        public Dictionary<object, question[]> routes = new Dictionary<object, question[]>();

        public question(string q) {
            this.q = q;
        }

        public question registerRoute(object key, question[] q) {
            routes.Add(key, q);
            return this;
        }
    }

    public class janej : question {        
        
        public janej(string q) : base(q) {
        }
    }

    public class intq : question {
        public bool isOk(int v) {
            return p(v);
        } 
        private Predicate<int> p;

        public intq(string q, Predicate<int> p) : base(q) {
            this.p = p;
        }
    }

    public static class extensions {

        public static question onYes(this question jn, params question[] q) {
            jn.registerRoute(true, q);
            return jn;
        }

        public static question onNo(this question jn, params question[] q) {
            jn.registerRoute(false, q);
            return jn;
        }


    }
}
