using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace ConsoleApplication1
{
    public class SkadeChat
    {
        private static Question[] bil =
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


        public static void Main(string[] args)
        {
            var state = new State();
            state.Set("carmodel", "Ford Mustang 1978");
            var hist = new List<string>();
            string q = null;
            Result<List<QA>> res = Exec(hist);
            while (res.IsFailure || res.Value.Count > 0)
            {
                if (res.IsFailure)
                {
                    Console.WriteLine(res.Error);
                    res = Exec(f2.ToList(), state, hist);
                }
                else if (!string.IsNullOrEmpty(res.Value[0]._answer))
                {
                    hist.Add(res.Value[0]._answer);
                }
                foreach (var qa in res.Value)
                {
                    if (qa._question != null) { 
                        Console.WriteLine(qa._question);
                    }
                }
                var newHist = new List<string>(hist);
                newHist.Add(Console.ReadLine());
                res = Exec(f2.ToList(), state, newHist);
            }
        }



        public static Result<List<QA>> Exec(List<string> history)
        {
            if (history.Count == 0)
            {
                var res = new List<QA> { new QA(f2[0], "", f2[0].Q) };
                return Result.Ok(res);
            }
            return Exec(f2.ToList(), new State(), history);
        }

        static Result<List<QA>> Exec(List<Question> flow, State st, List<string> history)
        {
            var res = new List<QA>();
            while (flow.Count > 0)
            {
                Question q = flow[0];
                flow.RemoveAt(0);
                var answer = ask(q, st, history);
                Boolean lastAnswer = history.Count == 0 && (q.Id != null || q.Routes.Count > 0);
                var isOk = q.IsOk(answer, st);
                if (isOk.IsFailure)
                {
                    return Result.Fail<List<QA>>("!!!Du skal svare rigtigt!!!!");
                }
                if (q.Id != null)
                {
                    st.Set(q.Id, answer);
                }
                if (q.Routes.Count > 0 && q.Routes.ContainsKey(isOk.Value))
                {
                    flow.InsertRange(0, q.Routes[isOk.Value]);
                }
                if (lastAnswer && flow.Count > 0)
                {
                    res.Add(new QA(flow[0], answer, question(flow[0], st)));
                    if ((flow[0].Id != null || (flow[0].Preds.Count > 0 && !String.IsNullOrEmpty(flow[0].Q))) && history.Count == 0)
                    {
                        return Result.Ok(res);
                    }
                }
            }
            return Result.Ok(res);
        }

        private static Question q(string q)
        {
            return new Question(q, null);
        }

        private static Question q(string q, string id)
        {
            return new Question(q, id);
        }

        private static string question(Question q, State st)
        {
            var s = q.Q + (q.Preds.Keys.Count > 0 ? " (" + String.Join("/", q.Preds.Keys) + ")" : "");
            return s.StringFormat(st.s);
        }

        private static string ask(Question q, State st, List<string> history)
        {
            if (history.Count > 0 && (q.Id != null || q.Preds.Count > 0))
            {
                var res = history[0];
                history.RemoveAt(0);
                return res;
            }
            return null;
        }
    }

    public class QA
    {
        public string _question { get; }

        public string _answer { get; }

        public List<string> _alternatives;

        public QA(Question q, string a, string question)
        {
            _question = (string.IsNullOrEmpty(q.Q)) ? null : question;
            _alternatives = new List<string>(q.Preds.Keys);
            _answer = a;
        }
    }

    public class Question
    {
        public readonly string Id;

        public string Q { get; }

        public Dictionary<string, Question[]> Routes = new Dictionary<string, Question[]>();
        public Dictionary<string, Func<State, object, bool>> Preds = new Dictionary<string, Func<State, object, bool>>();

        public Question(string q, string id)
        {
            this.Id = id;
            this.Q = q;
        }

        public Question RegisterRoute(string key, Question[] q)
        {
            Routes[key] = q;
            return this;
        }
    
        public Question RegisterPred(string key, Predicate<Object> p)
        {
            Preds[key] = (s, e) => p(e);
            return this;
        }

        public Result<String> IsOk(string key, State s)
        {
            if (Preds.Count == 0)
            {
                return Result.Ok(key ?? "");
            }
            foreach (var p in Preds)
            {
                if (Preds[p.Key](s, key))
                {
                    return Result.Ok(p.Key);
                }
            }
            if (key == null && !String.IsNullOrEmpty(Q))
            {
                return Result.Fail<String>("Fejl1");
            }
            return Result.Ok("");
        }
    }

    public static class QuestionExtensions
    {

        public static Question OnYes(this Question jn, params Question[] q)
        {
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

