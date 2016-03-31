//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Elmah.Gelf.Extensions
//{
//    public static class ExceptionExtensions
//    {
//        private const string CallerInfoKey = "ElmahCallerInfo";

//        /// <summary>
//        /// Attempts to install a <see cref="CallerInfo"/> into an
//        /// <see cref="Exception"/> via <see cref="Exception.Data"/>.
//        /// </summary>
//        /// <returns>
//        /// Returns <see cref="CallerInfo"/> that was replaced otherwise
//        /// <c>null</c>.
//        /// </returns>

//        public static CallerInfo TrySetCallerInfo(this Exception exception, CallerInfo info)
//        {
//            if (exception == null) throw new ArgumentNullException("exception");
//            CallerInfo previous = null;
//            if (exception.IsWritableData())
//            {
//                previous = exception.TryPopCallerInfo();
//                if (info != null)
//                    exception.Data[CallerInfoKey] = info;
//            }
//            return previous;
//        }

//        public static IDisposable TryScopeCallerInfo(this Exception exception, CallerInfo info)
//        {
//            if (exception == null) throw new ArgumentNullException("exception");
//            var previous = exception.TrySetCallerInfo(info);
//            var dispose = previous != null
//                        ? (Action)(() => exception.TrySetCallerInfo(previous))
//                        : (() => exception.TryClearCallerInfo());
//            return new DelegatingDisposable(dispose);
//        }

//        public static CallerInfo TryGetCallerInfo(this Exception exception)
//        {
//            if (exception == null) throw new ArgumentNullException("exception");
//            var datum = exception.IsData()
//                      ? exception.Data[CallerInfoKey]
//                      : null;
//            return datum as CallerInfo;
//        }

//        public static CallerInfo TryPopCallerInfo(this Exception exception)
//        {
//            if (exception == null) throw new ArgumentNullException("exception");
//            var info = exception.TryGetCallerInfo();
//            TryClearCallerInfo(exception);
//            return info;
//        }

//        public static void TryClearCallerInfo(this Exception exception)
//        {
//            if (exception == null) throw new ArgumentNullException("exception");
//            if (exception.IsWritableData())
//                exception.Data.Remove(CallerInfoKey);
//        }

//        public static IEnumerable<KeyValuePair<object, object>> EnumerateDataWithoutCallerInfo(this Exception exception)
//        {
//            if (exception == null) throw new ArgumentNullException("exception");

//            return from datum in exception.EnumerateData()
//                   where datum.Key as string != CallerInfoKey
//                   select datum;
//        }

//        public static IEnumerable<KeyValuePair<object, object>> EnumerateData(this Exception exception)
//        {
//            if (exception == null) throw new ArgumentNullException("exception");

//            var data = exception.Data;
//            if (exception.IsData() || data.Count == 0)
//                yield break;

//            var pairs = from DictionaryEntry e in data
//                        select KeyValuePair.Create(e.Key, e.Value);

//            foreach (var pair in pairs)
//                yield return pair;
//        }

//        static bool IsWritableData(this Exception exception)
//        {
//            Debug.Assert(exception != null);
//            return exception.IsData(true);
//        }

//        static bool IsData(this Exception exception, bool writable = false)
//        {
//            Debug.Assert(exception != null);

//            var data = exception.Data;

//            // "The ExecutionEngineException, OutOfMemoryException, 
//            //  StackOverflowException and ThreadAbortException classes 
//            //  always return null for the value of the Data property."
//            //
//            // http://msdn.microsoft.com/en-us/library/system.exception.data(v=vs.80).aspx

//            // ReSharper disable ConditionIsAlwaysTrueOrFalse
//            return data != null // ReSharper restore ConditionIsAlwaysTrueOrFalse
//                && (!writable || !data.IsReadOnly);
//        }
//    }
//}
