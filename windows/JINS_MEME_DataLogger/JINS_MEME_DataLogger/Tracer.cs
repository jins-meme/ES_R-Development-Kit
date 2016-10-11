using System;
using System.Diagnostics;
using System.Security;
using System.Reflection;

namespace JINS_MEME_DataLogger
{
    public class Tracer
    {
        private static TraceSource trace = new TraceSource("LogSource");
        private static bool isInitialized = false;

        private Tracer()
        {
        }

        private static void initialize()
        {
            if (!isInitialized)
            {
                isInitialized = true;
            }

        }

        [DynamicSecurityMethod]
        public static void WriteException(Exception exception)
        {
            if (trace.Switch.Level == SourceLevels.Critical || trace.Switch.Level == SourceLevels.Error
                || trace.Switch.Level == SourceLevels.Warning || trace.Switch.Level == SourceLevels.Information
                || trace.Switch.Level == SourceLevels.Verbose)
            {
                string format = string.Format("[Exception] [{0}] {1}\n{2}", CallerMethodName(), exception.Message, exception.StackTrace);
                Write(format);
            }
        }

        [DynamicSecurityMethod]
        public static void WriteCritical(String format, params object[] args)
        {
            if (trace.Switch.Level == SourceLevels.Critical || trace.Switch.Level == SourceLevels.Error
                || trace.Switch.Level == SourceLevels.Warning || trace.Switch.Level == SourceLevels.Information
                || trace.Switch.Level == SourceLevels.Verbose)
            {
                format = string.Format("[Critical] [{0}] {1}", CallerMethodName(), format);
                Write(format, args);
            }
        }

        [DynamicSecurityMethod]
        public static void WriteError(String format, params object[] args)
        {
            if (trace.Switch.Level == SourceLevels.Error
                || trace.Switch.Level == SourceLevels.Warning || trace.Switch.Level == SourceLevels.Information
                || trace.Switch.Level == SourceLevels.Verbose)
            {
                format = string.Format("[Error] [{0}] {1}", CallerMethodName(), format);
                Write(format, args);
            }
        }

        [DynamicSecurityMethod]
        public static void WriteWarning(String format, params object[] args)
        {
            if (trace.Switch.Level == SourceLevels.Warning || trace.Switch.Level == SourceLevels.Information
                || trace.Switch.Level == SourceLevels.Verbose)
            {
                format = string.Format("[Warning] [{0}] {1}", CallerMethodName(), format);
                Write(format, args);
            }
        }

        [DynamicSecurityMethod]
        public static void WriteInformation(String format, params object[] args)
        {
            if (trace.Switch.Level == SourceLevels.Information
                || trace.Switch.Level == SourceLevels.Verbose)
            {
                format = string.Format("[Information] [{0}] {1}", CallerMethodName(), format);
                Write(format, args);
            }
        }


        [DynamicSecurityMethod]
        public static void WriteVerbose(String format, params object[] args)
        {
            if (trace.Switch.Level == SourceLevels.Verbose)
            {
                format = string.Format("[Verbose] [{0}] {1}", CallerMethodName(), format);
                Write(format, args);
            }
        }

        private static void Write(String format, params object[] args)
        {
            initialize();
            string body = DateTime.Now.ToString("yyyy/MM/dd HH:mm.ss.fff") + " : " + String.Format(format, args);
            Trace.WriteLine(body);
        }


        /// <summary>
        /// 呼び出し元を取得します。
        /// </summary>
        /// <returns>
        /// 呼び出し元関数名（namespace 含む）
        /// </returns>
        private static string CallerMethodName()
        {
            const int CallerFrameIndex = 2;
            StackFrame callerFrame = new StackFrame(CallerFrameIndex);
            MethodBase callerMethod = callerFrame.GetMethod();
            return string.Format("{0}.{1}", callerMethod.DeclaringType, callerMethod.Name);
        }
    }
}


// 呼び出し元関数名を取得する為の御呪い（リリースモードでも使用可）
namespace System.Security
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    internal sealed class DynamicSecurityMethodAttribute : Attribute
    {
    }
}
