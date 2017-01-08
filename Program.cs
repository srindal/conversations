using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace ConsoleApplication1
{
    public class SkadeChat
    {
       /* private static Question[] bil =
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

        private static Question[] f2 =
        {
            q("Hvad hedder du?", "name"),
            q("").OnState("carmodel", s => s.s.ContainsKey("carmodel1"), q("Jeg er munken")),
            q("Hej {name}, det er aldrig rart når man er ude for et uheld."),
            q("Hvad er gået i stykker?")
                .On("Bil", bil)
                .On("Hest"),
            q("Hav en fortsat god dag")
        };
*/

        private static IStep[] f =
        {
            q("Hej Knallert"),
            q("Kan måger knalde?").OnYes(q("Du er ulækker)),
            q("Sved er lækkert {fugl}"),
            q("Er lort smagfuldt?", "kaj"),
            q("Vi ses {kaj}")
        };

        public static void Main(string[] args)
        {
            var state = new State();
            state.Set("carmodel", "Ford Mustang 1978");
            var hist = new List<string>();
            string q = null;
            var res = new List<Statement>();
            do
            {
                Console.WriteLine("========================");
                res = Exec(f.ToList(), state, localHist(hist, q));
                foreach (var statement in res)
                    {
                        if (statement.Speaker == Statement.Chatter.Person)
                        {
                            hist.Add(statement.Chat);
                        }
                        Console.WriteLine(statement.Speaker + ": " + statement.Chat);
                    }
                    q = res[res.Count - 1].IsBreaking ? Console.ReadLine() : null;
            } while (res[res.Count - 1].IsBreaking);
        }

        private static List<string> localHist(List<string> hist, string a)
        {
            List<string> s = new List<string>(hist);
            if (a != null)
            {
                s.Add(a);
            }
        return s;
        }

        static List<Statement> Exec(List<IStep> flow, State st, List<string> hist)
        {
            List<IStep> lines = RunToNextBreak(flow, st);
            List<Statement> res = new List<Statement>();
            foreach (var s in hist)
            {
                var q = lines.Last() as Question;
                st.Set(q.Key, s);
                res.AddRange(StepsToStatements(lines, st));
                res.Add(Statement.HumanSays(s));
                lines = RunToNextBreak(flow, st);
            }
            res.AddRange(StepsToStatements(lines, st));
            return res;
        }

        static List<Statement> StepsToStatements(List<IStep> steps, State st) {
            var res = new List<Statement>();
            foreach(var ss in steps) {
                res.Add(Statement.RobotSays(ss as ILabelStep, st));
            }
            return res;

        } 

        static List<IStep> RunToNextBreak(List<IStep> flow, State st)
        {
            var res = new List<IStep>();
            while (flow.Count > 0) {
                var step = flow.TakeNext();
                res.Add(step);
                if (step is Question) {
                    break;
                }
            }
            return res;
        }              

        private static IStep q(string q)
        {
            return new Line(q);
        }

        private static Question q(string q, string id)
        {
            return new Question(q, id);
        }
     
    }

    public class Statement
    {
        public enum Chatter {Robot, Person};

        public readonly Chatter Speaker;
        public readonly string Chat;

        public readonly bool IsBreaking;
        public List<string> Alternatives;

        private Statement(string q, Chatter chatter, bool IsBreaking)
        {
            Speaker = chatter;
            Chat = q;
            Alternatives = new List<string>();
            this.IsBreaking = IsBreaking;
        }

        public static Statement RobotSays(ILabelStep q, State st) {
            var s = q.Label.StringFormat(st.s);
            return new Statement(s, Chatter.Robot, q is IBreakingStep);
        }

        public static Statement HumanSays(string q) {
            return new Statement(q, Chatter.Person, false);
        }
    }

    public interface IStep
    {
    }

    public interface IBreakingStep : IStep
    {
    }

    public interface ILabelStep : IStep
    {
        string Label { get; }
    }
    


    public class Line : ILabelStep
    {
        public string Label { get; }

        public Line(string t)
        {
            this.Label = t;
        }
    }

    public class BranchOnLine : Line
    {
        public BranchOnLine(string t) : base(t)
        {
        }

        
    }

    public class BranchOnState : IStep
    {
        public Dictionary<Predicate<State>, IStep[]> Preds = new Dictionary<Predicate<State>, IStep[]>();

        public BranchOnState AddRoute(Predicate<State> p, params IStep[] qs)
        {
            Preds[p] = qs;
            return this;
        }
    }

    public class Question : Line, IBreakingStep
    {
        public readonly string Key;

        public Dictionary<string, IStep[]> Routes = new Dictionary<string, IStep[]>();
        public Dictionary<string, Func<State, object, bool>> Preds = new Dictionary<string, Func<State, object, bool>>();

        public Question(string t, string key) : base(t)
        {

            this.Key = key;
        }

        public Question RegisterRoute(string key, IStep[] q)
        {
            Routes[key] = q;
            return this;
        }
    
        public Question RegisterPred(string key, Predicate<Object> p)
        {
            Preds[key] = (s, e) => p(e);
            return this;
        }
    }

    public class BranchQuestion : Question
    {
        public BranchQuestion(string t, string key) : base(t, key)
        {

        }
    }

    public static class QuestionExtensions
    {

        public static IStep TakeNext(this List<IStep> flow) {
            var res = flow[0];
            flow.RemoveAt(0);
            return res;
        }


        public static Question OnYes(this IStep s, params Question[] q)
        {
            var q = new Question()
            var res = On(jn, "Ja", q);
            if (!jn.Routes.ContainsKey("Nej"))
            {
                jn.OnNo("");
            }
            return res;
        }

        public static Question OnYes(this Question q, string s)
        {
            return OnYes(q, new Question(s, null));
        }

        public static Question OnNo(this Question q, string s)
        {
            return OnNo(q, new Question(s, null));
        }


        public static Question OnNo(this Question jn, params Question[] q)
        {
            if (!jn.Routes.ContainsKey("Ja"))
            {
                jn.OnYes("");
            }
            return On(jn, "Nej", q);
        }

        public static Question On(this Question q, string key, Predicate<object> p, params Question[] qs)
        {
            return q.RegisterRoute(key, qs).RegisterPred(key, p);
        }

        public static Question On(this Question q, string key, params Question[] qs)
        {
            return On(q, key, s => s.Equals(key), qs);
        }

        public static Question OnInterval(this Question q, int from, int to, params Question[] qs)
        {
            return q.RegisterPred(from + "-" + to, s => Int32.Parse(s.ToString()) >= from && to >= Int32.Parse(s.ToString()));
        }

        public static Question OnState(this Question q, string key, Predicate<State> p, params Question[] qs)
        {
            q.Preds[key] = (s, i) => p(s);
            q.Routes[key] = qs;
            return q;
        }


        public static string StringFormat(this string format, IDictionary<string, string> values)
        {
            foreach (var p in values)
                format = format.Replace("{" + p.Key + "}", p.Value);
            return format;
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
        }
    }

    internal sealed class ResultCommonLogic
    {
        public bool IsFailure { get; }
        public bool IsSuccess => !IsFailure;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _error;

        public string Error
        {
            [DebuggerStepThrough]
            get
            {
                if (IsSuccess)
                    throw new InvalidOperationException("There is no error message for success.");

                return _error;
            }
        }

        [DebuggerStepThrough]
        public ResultCommonLogic(bool isFailure, string error)
        {
            if (isFailure)
            {
                if (string.IsNullOrEmpty(error))
                    throw new ArgumentNullException(nameof(error), "There must be error message for failure.");
            }
            else
            {
                if (error != null)
                    throw new ArgumentException("There should be no error message for success.", nameof(error));
            }

            IsFailure = isFailure;
            _error = error;
        }
    }

    public struct Result
    {
        private static readonly Result _okResult = new Result(false, null);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ResultCommonLogic _logic;

        public bool IsFailure => _logic.IsFailure;
        public bool IsSuccess => _logic.IsSuccess;
        public string Error => _logic.Error;

        [DebuggerStepThrough]
        private Result(bool isFailure, string error)
        {
            _logic = new ResultCommonLogic(isFailure, error);
        }

        [DebuggerStepThrough]
        public static Result Ok()
        {
            return _okResult;
        }

        [DebuggerStepThrough]
        public static Result Fail(string error)
        {
            return new Result(true, error);
        }

        [DebuggerStepThrough]
        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(false, value, null);
        }

        [DebuggerStepThrough]
        public static Result<T> Fail<T>(string error)
        {
            return new Result<T>(true, default(T), error);
        }

        /// <summary>
        /// Returns first failure in the list of <paramref name="results"/>. If there is no failure returns success.
        /// </summary>
        /// <param name="results">List of results.</param>
        [DebuggerStepThrough]
        public static Result FirstFailureOrSuccess(params Result[] results)
        {
            foreach (Result result in results)
            {
                if (result.IsFailure)
                    return Fail(result.Error);
            }

            return Ok();
        }

        /// <summary>
        /// Returns failure which combined from all failures in the <paramref name="results"/> list. Error messages are separated by <paramref name="errorMessagesSeparator"/>. 
        /// If there is no failure returns success.
        /// </summary>
        /// <param name="errorMessagesSeparator">Separator for error messages.</param>
        /// <param name="results">List of results.</param>
        [DebuggerStepThrough]
        public static Result Combine(string errorMessagesSeparator, params Result[] results)
        {
            List<Result> failedResults = results.Where(x => x.IsFailure).ToList();

            if (!failedResults.Any())
                return Ok();

            string errorMessage = string.Join(errorMessagesSeparator, failedResults.Select(x => x.Error).ToArray());
            return Fail(errorMessage);
        }

        [DebuggerStepThrough]
        public static Result Combine(params Result[] results)
        {
            return Combine(", ", results);
        }
    }

    public struct Result<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ResultCommonLogic _logic;

        public bool IsFailure => _logic.IsFailure;
        public bool IsSuccess => _logic.IsSuccess;
        public string Error => _logic.Error;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly T _value;

        public T Value
        {
            [DebuggerStepThrough]
            get
            {
                if (!IsSuccess)
                    throw new InvalidOperationException("There is no value for failure.");

                return _value;
            }
        }

        [DebuggerStepThrough]
        internal Result(bool isFailure, T value, string error)
        {
            if (!isFailure && value == null)
                throw new ArgumentNullException(nameof(value));

            _logic = new ResultCommonLogic(isFailure, error);
            _value = value;
        }

        public static implicit operator Result(Result<T> result)
        {
            return result.IsSuccess ? Result.Ok() : Result.Fail(result.Error);
        }
    }
}

