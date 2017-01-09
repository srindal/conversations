using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace ConsoleApplication1
{
    public class SkadeChat
    {
        private static IStep[] bil =
         {
            q("I Alka håndterer vi over 30.000 bilskader hvert år. Vil du have nogle generelle råd?")
                .OnYes("Jeg har ingen råd, men nice try"),
            q("Inden vi starter skal vi lige tjekke de oplysninger vi har om dig"),
            If(s => !s.s.ContainsKey("carmodel"), 
                q("Vi har desværre ingen oplysninger om din bil."),
                q("Indtast dine biloplysninger", "carmodel"),
                q("Vi har nu registreret at din bil er en {carmodel}"))
            .Else(q("Er din bil en {carmodel}?")
                    .OnNo(
                        q("Så er der noget galt med vores oplysninger. Indtast din bil", "carmodel"),
                        q("Vi har nu registreret at din bil er en {carmodel}"))),
            q("Hvor og hvornår skete skaden?", "whereandwhen"),
            q("Hvilke dele af bilen blev beskadiget?", "damage")
                .On("fronten")
                .On("bagsmækken")
                .On("lygterne")
                .On("venstre fordør"),
            q("Hvor mange var I i bilen?", "passengers").On("1").On("2").On("3").On("4"),
        q("Kære {name}. Vi har registreret at du {whereandwhen} har haft en skade på din {carmodel}, hvor I var {passengers} i bilen og {damage} blev beskadiget."),
            q("Helmers autoværksted ringer til dig i morgen, så I kan aftale en tid")
        };

        private static IStep[] f2 =
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
            
            var hist = new List<string>();
            var newHist = new List<string>();
            
            string q = null;
            var res = Result.Ok(new List<Statement>());
            do
            {
                var state = new State();
                state.Set("carmodel", "Ford Mustang 1978");
                Console.WriteLine("========================");
                res = Exec(f2.ToList(), state, LocalHist(hist, q));
                if (res.IsFailure)
                {
                    Console.WriteLine(res.Error);
                    q = null;
                    continue;
                }
                foreach (var statement in res.Value)
                {
                    if (statement.Speaker == Statement.Chatter.Person)
                    {
                        newHist.Add(statement.Chat);
                    }
                    Console.WriteLine(statement.Speaker + ": " + statement.Chat);
                }
                q = res.Value[res.Value.Count - 1].IsBreaking ? Console.ReadLine() : null;
                hist = newHist;
                newHist = new List<String>();
            } while (res.IsFailure || res.Value[res.Value.Count - 1].IsBreaking);

        }

        private static List<string> LocalHist(List<string> hist, string a)
        {
            List<string> s = new List<string>(hist);
            if (a != null)
            {
                s.Add(a);
            }
            return s;
        }

        static Result<List<Statement>> Exec(List<IStep> flow, State st, List<string> hist)
        {
            List<IStep> lines = RunToNextBreak(flow, st);
            List<Statement> res = new List<Statement>();
            foreach (var s in hist)
            {
                var q = lines.Last();
                st.Set((q as Question)?.Key, s);

                var a = HandleAnswer(flow, q as IBranchStep, s, st);
                if (a.IsFailure)
                {
                    return Result.Fail<List<Statement>>(a.Error);
                }
                flow.InsertRange(0, a.Value);
                res.AddRange(StepsToStatements(lines, st));
                res.Add(Statement.HumanSays(s));
                lines = RunToNextBreak(flow, st);
            }
            res.AddRange(StepsToStatements(lines, st));
            return Result.Ok(res);
        }

        static List<Statement> StepsToStatements(List<IStep> steps, State st)
        {
            var res = new List<Statement>();
            foreach (var ss in steps)
            {
                res.Add(Statement.RobotSays(ss as ILabelStep, st));
            }
            return res;

        }

        private static Result<List<IStep>> HandleAnswer(List<IStep> flow, IBranchStep q, string answer, State st)
        {
            var res = Result.Ok(new List<IStep>());
            if (q == null || q.GetRoutes().Count == 0)
            {
                return res;
            }
            return FindRouteToPred(q, answer, st);
        }

        private static Result<List<IStep>> FindRouteToPred(IBranchStep q, string answer, State st)
        {
            foreach (var pred in q.GetPreds())
            {
                if (pred.Value(st, answer))
                {
                    {
                        var routes = q.GetRoutes();
                        var r = routes[pred.Key];
                        var s = r.ToList();
                        return Result.Ok(s);
                    }
                }
            }
            return Result.Fail<List<IStep>>($"Du kan ikke svare '{answer}' på dette spørgsmål");
        }

        static List<IStep> RunToNextBreak(List<IStep> flow, State st)
        {
            var res = new List<IStep>();
            while (flow.Count > 0)
            {
                var step = flow.TakeNext();

                if (step is BranchOnState)
                {
                    var r = FindRouteToPred(step as IBranchStep, "", st).Value;
                    flow.InsertRange(0, r);
                }
                else
                {
                    res.Add(step);
                }
                if (step is Question)
                {
                    break;
                }
            }
            return res;
        }

        private static ILabelStep q(string q)
        {
            return new Line(q);
        }

        private static Question q(string q, string id)
        {
            return new Question(q, id);
        }

        private static BranchOnState If(Predicate<State> p, params IStep[] qs)
        {
            return new BranchOnState(p, qs);
        }
    }

    public class Statement
    {
        public enum Chatter { Robot, Person };

        public readonly Chatter Speaker;
        public readonly string Chat;

        public readonly bool IsBreaking;
        public List<string> Alternatives;

        private Statement(string q, Chatter chatter, bool isBreaking)
        {
            Speaker = chatter;
            Chat = q;
            Alternatives = new List<string>();
            this.IsBreaking = isBreaking;
        }

        public static Statement RobotSays(ILabelStep q, State st)
        {
            var s = q.Label.StringFormat(st.s);
            if (q is IBreakingStep)
            {
                var qu = q as IBranchStep;
                if (qu is ILabelStep && qu.GetRoutes().Count > 0)
                {
                    s += " (" + String.Join(", ", qu.GetPreds().Keys) + ")";
                }
            }
            return new Statement(s, Chatter.Robot, q is IBreakingStep);
        }

        public static Statement HumanSays(string q)
        {
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

    public interface IBranchStep : IStep
    {
        Dictionary<string, IStep[]> GetRoutes();
        Dictionary<string, Func<State, object, bool>> GetPreds();
    }

    public class Line : ILabelStep
    {
        public string Label { get; }

        public Line(string t)
        {
            this.Label = t;
        }
    }

    public class BranchOnState : IBranchStep
    {
        private readonly Dictionary<string, IStep[]> routes = new Dictionary<string, IStep[]>();
        private readonly Dictionary<string, Func<State, object, bool>> preds = new Dictionary<string, Func<State, object, bool>>();


        public BranchOnState(Predicate<State> p, params IStep[] qs)
        {
            AddRoute("if", p, qs);
            AddRoute("else", s => !p(s));
        }
        

        public BranchOnState AddRoute(string key, Predicate<State> p, params IStep[] qs)
        {
            routes[key] = qs;
            preds[key] = (s, e) => p(s);
            return this;
        }


        public Dictionary<string, IStep[]> GetRoutes()
        {
            return routes;
        }

        public Dictionary<string, Func<State, object, bool>> GetPreds()
        {
            return preds;
        }
    }

    public class Question : Line, IBreakingStep
    {
        public readonly string Key;

        public Question(string t, string key) : base(t)
        {
            this.Key = key;
        }


    }

    public class BranchQuestion : Question, IBranchStep
    {
        private readonly Dictionary<string, IStep[]> routes = new Dictionary<string, IStep[]>();
        private readonly Dictionary<string, Func<State, object, bool>> preds = new Dictionary<string, Func<State, object, bool>>();

        public BranchQuestion(string t, string key) : base(t, key)
        {
        }

        public BranchQuestion RegisterRoute(string key, IStep[] q)
        {
            routes[key] = q;
            return this;
        }

        public BranchQuestion RegisterPred(string key, Predicate<Object> p)
        {
            preds[key] = (s, e) => p(e);
            return this;
        }

        public Dictionary<string, IStep[]> GetRoutes()
        {
            return routes;
        }

        public Dictionary<string, Func<State, object, bool>> GetPreds()
        {
            return preds;
        }
    }

    public static class QuestionExtensions
    {
        public static IStep TakeNext(this List<IStep> flow)
        {
            var res = flow[0];
            flow.RemoveAt(0);
            return res;
        }

        public static BranchOnState Else(this BranchOnState b, params IStep[] qs)
        {
            var p = b.GetPreds()["if"];
            b.AddRoute("else", s => !p(s, null), qs);
            return b;
        }


        public static Question OnYes(this ILabelStep s, params IStep[] qs)
        {
            return s.ToQuestion()
                .On("Ja", e => "Ja".Equals(e), qs)
                .On("Nej", e => !"Ja".Equals(e));
        }

        public static Question OnNo(this ILabelStep s, params IStep[] qs)
        {
            return s.ToQuestion()
                .On("Ja", e => "Ja".Equals(e))
                .On("Nej", e => !"Ja".Equals(e), qs);
        }

        public static Question OnYes(this ILabelStep q, string s)
        {
            return OnYes(q, new Line(s));
        }

        public static Question OnNo(this ILabelStep q, string s)
        {
            return OnNo(q, new Question(s, null));
        }

        public static Question ToQuestion(this ILabelStep st)
        {
            return st as Question ?? new Question(st.Label, null);
        }

        public static BranchQuestion ToBranchQuestion(this Question st)
        {
            return st as BranchQuestion ?? new BranchQuestion(st.Label, st.Key);
        }


        public static BranchQuestion On(this ILabelStep q, string key, Predicate<object> p, params IStep[] qs)
        {
            var qq = q.ToQuestion().ToBranchQuestion();
            return qq.RegisterRoute(key, qs).RegisterPred(key, p);
        }

        public static BranchQuestion On(this ILabelStep q, string key, params IStep[] qs)
        {
            var qq = ToQuestion(q);
            return On(qq, key, s => s.Equals(key), qs);
        }

        //public static BranchQuestion OnInterval(this IBranchStep q, int from, int to, params IStep[] qs)
        //{
        //    return q.RegisterPred(from + "-" + to, s => Int32.Parse(s.ToString()) >= from && to >= Int32.Parse(s.ToString()));
        //}


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
            if (key != null)
            {
                s[key] = val;
            }
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
