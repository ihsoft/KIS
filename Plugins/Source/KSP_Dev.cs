// Kerbal Development tools.
// Author: igor.zavoychinskiy@gmail.com
// This software is distributed under Public domain license.

using System;
using System.Collections.Generic;
using Time = UnityEngine.Time;

// The methods in this module are somehow optimized for the performance. If you see code
// duplications, bulky statements or "unnecessary" checks don't be fast fixing them. There may be
// a reason why it's done as it is. 

namespace KSPDev {

    public enum LogLevel {
        EXCEPTION = -1000,  // You cannot stop it.
        NONE = 0,
        ERROR = 1, 
        WARNING = 2,  // Setting level above this value in Prod is strongly discouraged.
        INFO = 3,
        TRACE = 4,
    }
    
    /// <summary>Logger abstraction class.</summary>
    /// <remarks>
    /// <para>Use <c>logLevel</c> to set the desired level of logs verbosity. Keep in mind that
    /// too verbose logs may overflow in-game debug window.</para>
    /// <para>Every logging method has a pair with suffix <c>Repeated</c>. These methods should be
    /// used when logging rate is high and not every single record is needed. E.g. even a single
    /// log call from <c>UntityObject.Update()</c> method will be spamming on every frame resulting
    /// in the debug log overflow. "Repeated" methods show first occurence right away and then only
    /// count the following calls giving a summary every <seealso cref="aggregationPeriod"/>
    /// seconds. Aggregation is done by the method name and the message content.</para>
    /// </remarks>
    public static class Logger {
        public static LogLevel logLevel = LogLevel.INFO;
        public static float aggregationPeriod = 10.0f;  // Seconds.

        public delegate void logStackTrace_Delegate(LogLevel level, string tag);
        public delegate void logStackTraceRepeated_Delegate(LogLevel level, string tag);
        public delegate void logTrace_Delegate(String fmt, params object[] args);
        public delegate void logTraceRepeated_Delegate(String fmt, params object[] args);
        public delegate void logInfo_Delegate(String fmt, params object[] args);
        public delegate void logInfoRepeated_Delegate(String fmt, params object[] args);
        public delegate void logWarning_Delegate(String fmt, params object[] args);
        public delegate void logWarningRepeated_Delegate(String fmt, params object[] args);
        public delegate void logError_Delegate(String fmt, params object[] args);
        public delegate void logErrorRepeated_Delegate(String fmt, params object[] args);
        public delegate void logException_Delegate(Exception ex);
        public delegate void logExceptionRepeated_Delegate(Exception ex);

        public static logStackTrace_Delegate logStackTrace =
            NullLogger.logStackTrace;

        public static logStackTraceRepeated_Delegate logStackTraceRepeated =
            NullLogger.logStackTraceRepeated;
        public static logTrace_Delegate logTrace =
            NullLogger.logTrace;
        public static logTraceRepeated_Delegate logTraceRepeated =
            NullLogger.logTraceRepeated;
        public static logInfo_Delegate logInfo =
            NullLogger.logInfo;
        public static logInfoRepeated_Delegate logInfoRepeated =
            NullLogger.logInfoRepeated;
        public static logWarning_Delegate logWarning =
            NullLogger.logWarning;
        public static logWarningRepeated_Delegate logWarningRepeated =
            NullLogger.logWarningRepeated;
        public static logError_Delegate logError =
            NullLogger.logError;
        public static logErrorRepeated_Delegate logErrorRepeated =
            NullLogger.logErrorRepeated;

        /// <summary>Logs an exception with the stack trace.</summary>
        /// <param name="ex">An exception to log.</param>
        public static logException_Delegate logException =
            NullLogger.logException;

        /// <summary>Logs an exception that happens frequently.</summary>
        /// <remarks>Same as <seealso cref="logException"/> but does aggregation of the repeated
        /// events.</remarks>
        /// <param name="ex">An exception to log.</param>
        public static logExceptionRepeated_Delegate logExceptionRepeated =
            NullLogger.logExceptionRepeated;
    }

    /// <summary>Logger implementation that does nothing. I.e. no logging.</summary>
    static class NullLogger {
        public static void logStackTrace(LogLevel level, string tag) {}
        public static void logStackTraceRepeated(LogLevel level, string tag) {}
        public static void logTrace(String fmt, params object[] args) {}
        public static void logTraceRepeated(String fmt, params object[] args) {}
        public static void logInfo(String fmt, params object[] args) {}
        public static void logInfoRepeated(String fmt, params object[] args) {}
        public static void logWarning(String fmt, params object[] args) {}
        public static void logWarningRepeated(String fmt, params object[] args) {}
        public static void logError(String fmt, params object[] args) {}
        public static void logErrorRepeated(String fmt, params object[] args) {}
        public static void logException(Exception ex) {}
        public static void logExceptionRepeated(Exception ex) {}
    }
    
    /// <summary>
    /// A development logger that respects logging level and supports whitelists.
    /// </summary>
    /// <remarks>
    /// <para>Only use this logger in local development since it puts significant stress to the CPU
    /// and, hence, affects game's FPS.</para>
    /// <para>Use <seealso cref="allowedModules"/> to define modules that are allowed to log.
    /// "Module name" is a namespace of the module in the CS sources. E.g. logs from
    /// <c>namespace MyAddon { ... }</c> will be treated as logs from module <c>MyAddon</c>. Modules
    /// can be set via <c>KSPDev</c> settings file at <c>KSPDevConfig/DevLogger/whitelistedModule
    /// </c> path.</para>
    /// <para>Use <seealso cref="allowedSources"/> to define specific methods that are allowed to
    /// log. Method name includes namespace, class name and the name itself, e.g.
    /// <c>MyAddon.MyClass.MyMethod</c>. It must be a name of the immediate caller of the logging
    /// method. E.g. in case of call chain: <c>methodA() => methodB() => logInfo()</c> immediate
    /// caller name is <c>methodB</c>, and it will be checked against the whitelist. Methods can be
    /// set via <c>KSPDev</c> settings file at <c>KSPDevConfig/DevLogger/whitelistedMethod</c>
    /// path.</para>
    /// <para>Special case is stack trace. These logging methods take a <c>tag</c> parameter which
    /// is added to the method name, e.g. for the example above and tag "MyTag" the fully qualified
    /// name will be <c>MyAddon.MyClass.MyMethod:MyTag</c>.</para>
    /// <para>If no modules or methods are whitelisted then logging from any module and any function
    /// is permitted. Though, if at least one record is added to either modules or methods the mode
    /// changes to "restrict logging from all places except those, that match either moudles or
    /// methods whitelist".</para>
    /// </remarks>
    public static class DevLogger {
        // Settings names.
        private const string ModuleWhitelist = "whitelistedModule";
        private const string MethodWhitelist = "whitelistedMethod";

        public readonly static Dictionary<int, float> tsByContextHash =
            new Dictionary<int, float>();
        public readonly static Dictionary<int, int> countByContextHash =
            new Dictionary<int, int>();

        /// <summary>A list of modules allowed to log. When empty any module is allowed.</summary>
        public readonly static HashSet<string> allowedModules = new HashSet<string>();
        /// <summary>A list of methods allowed to log. When empty any methods is allowed.</summary>
        public readonly static HashSet<string> allowedSources = new HashSet<string>();

        /// <summary>Makes this logger the active one.</summary>
        /// <param name="node">A config node for this logger settings. Can be <c>null</c>.</param>
        public static void activate(ConfigNode node) {
            // Set logging functions.
            Logger.logStackTrace = DevLogger.logStackTrace;
            Logger.logStackTraceRepeated = DevLogger.logStackTraceRepeated;
            Logger.logTrace = DevLogger.logTrace;
            Logger.logTraceRepeated = DevLogger.logTraceRepeated;
            Logger.logInfo = DevLogger.logInfo;
            Logger.logInfoRepeated = DevLogger.logInfoRepeated;
            Logger.logWarning = DevLogger.logWarning;
            Logger.logWarningRepeated = DevLogger.logWarningRepeated;
            Logger.logError = DevLogger.logError;
            Logger.logErrorRepeated = DevLogger.logErrorRepeated;
            Logger.logExceptionRepeated = DevLogger.logExceptionRepeated;

            // Read filter settings.
            if (node != null) {
                allowedModules.Clear();
                foreach (string moduleName in node.GetValues(ModuleWhitelist)) {
                    allowedModules.Add(moduleName);
                }
                allowedSources.Clear();
                foreach (string sourceName in node.GetValues(MethodWhitelist)) {
                    allowedSources.Add(sourceName);
                }
            }

            UnityEngine.Debug.Log("DevLogger loaded");
        }

        public static void logStackTrace(LogLevel level, string tag) {
            if (Logger.logLevel >= level) {
                var st = new System.Diagnostics.StackTrace(1, true);
                var method = st.GetFrame(0).GetMethod();
                var source = method.DeclaringType + "." + method.Name + ":" + tag;
                log(level, source, "Stack trace:\n{0}", st);
            }
        }

        public static void logStackTraceRepeated(LogLevel level, string tag) {
            if (Logger.logLevel >= level) {
                var st = new System.Diagnostics.StackTrace(1, true);
                var method = st.GetFrame(0).GetMethod();
                var source = method.DeclaringType + "." + method.Name + ":" + tag;
                logRepeated(level, source, "Stack trace:\n{0}", st);
            }
        }

        public static void logTrace(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.TRACE) {
                log(LogLevel.TRACE, new System.Diagnostics.StackTrace(true).GetFrame(1),
                    fmt, args);
            }
        }

        public static void logTraceRepeated(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.TRACE) {
                logRepeated(
                    LogLevel.TRACE, new System.Diagnostics.StackTrace(true).GetFrame(1),
                    fmt, args);
            }
        }

        public static void logInfo(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.INFO) {
                log(LogLevel.INFO, new System.Diagnostics.StackTrace(true).GetFrame(1),
                    fmt, args);
            }
        }

        public static void logInfoRepeated(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.INFO) {
                logRepeated(
                    LogLevel.INFO, new System.Diagnostics.StackTrace(true).GetFrame(1),
                    fmt, args);
            }
        }

        public static void logWarning(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.WARNING) {
                log(LogLevel.WARNING, new System.Diagnostics.StackTrace(true).GetFrame(1),
                    fmt, args);
            }
        }

        public static void logWarningRepeated(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.WARNING) {
                logRepeated(
                    LogLevel.WARNING, new System.Diagnostics.StackTrace(true).GetFrame(1),
                    fmt, args);
            }
        }

        public static void logError(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.ERROR) {
                log(LogLevel.ERROR,
                    new System.Diagnostics.StackTrace(true).GetFrame(1),
                    fmt, args);
            }
        }

        public static void logErrorRepeated(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.ERROR) {
                logRepeated(
                    LogLevel.ERROR, new System.Diagnostics.StackTrace(true).GetFrame(1),
                    fmt, args);
            }
        }

        public static void logException(Exception ex) {
            log(LogLevel.EXCEPTION, new System.Diagnostics.StackTrace(true).GetFrame(1),
                ex.ToString());
        }
        
        public static void logExceptionRepeated(Exception ex) {
            logRepeated(
                LogLevel.EXCEPTION, new System.Diagnostics.StackTrace(true).GetFrame(1),
                ex.ToString());
        }

        private static void log(LogLevel level, System.Diagnostics.StackFrame frame,
                                String fmt, params object[] args) {
            var source = frame.GetMethod().DeclaringType + "." + frame.GetMethod().Name;
            log(level, source, fmt, args);
        }
        
        private static void log(LogLevel level, String source, String fmt, params object[] args) {
            if (!isWhitelisted(source)) {
                return;
            }
            var message = fmt;
            if (args.Length > 0) {  // Don't parse fmt string when no arguments.
                message = String.Format(fmt, args);
            }
            if (level == LogLevel.ERROR) {
                UnityEngine.Debug.LogError("[" + source + "]: " + message);
            } else if (level == LogLevel.WARNING) {
                UnityEngine.Debug.LogWarning("[" + source + "]: " + message);
            } else if (level == LogLevel.INFO) {
                UnityEngine.Debug.Log("[" + source + "]: " + message);
            } else if (level == LogLevel.EXCEPTION) {
                UnityEngine.Debug.LogError("[EXCEPTION] [" + source + "]: " + message);
            } else {  // Log any unknown level as TRACE.
                UnityEngine.Debug.Log("[TRACE] [" + source + "]: " + message);
            } 
        }

        private static void logRepeated(LogLevel level, System.Diagnostics.StackFrame frame,
                                        String fmt, params object[] args) {
            var source = frame.GetMethod().DeclaringType + "." + frame.GetMethod().Name;
            logRepeated(level, source, fmt, args);
        }

        private static void logRepeated(LogLevel level, String source,
                                        String fmt, params object[] args) {
            // A performance tweak: don't count blacklisted logs. 
            if (!isWhitelisted(source)) {
                return;
            }
            var message = fmt;
            if (args.Length > 0) {  // Don't parse fmt string when no arguments.
                message = String.Format(fmt, args);
            }
            var key = (level + source + message).GetHashCode();
            if (tsByContextHash.ContainsKey(key)) {
                countByContextHash[key] += 1;
                if (tsByContextHash[key] + Logger.aggregationPeriod < Time.unscaledTime) {
                    log(level, source, "Repeated {0} times in the last {1:F2} seconds:\n{2}",
                        countByContextHash[key], Time.unscaledTime - tsByContextHash[key],
                        message);
                    tsByContextHash[key] = Time.unscaledTime;
                    countByContextHash[key] = 0;
                }
                return;
            }
            tsByContextHash[key] = Time.unscaledTime;
            countByContextHash[key] = 1;
            log(level, source, message);
        }

        private static bool isWhitelisted(String source) {
            var allowed = allowedSources.Count == 0 && allowedModules.Count == 0
                || allowedSources.Count > 0 && allowedSources.Contains(source);
            if (!allowed && allowedModules.Count > 0) {
                var separatorIndex = source.IndexOf('.');
                allowed = separatorIndex >= 0
                    && allowedModules.Contains(source.Substring(0, separatorIndex));
            }
            return allowed;
        }
    }


    /// <summary>A wrapper to catch and log exceptions from the methods</summary>
    /// <remarks>Delegate all engine events and other sensitive methods to the appropriate
    /// <c>Action</c> handler. Any exception in execution will be logged via
    /// <seealso cref="Logger.logExceptionRepeated"/> giving the whole context and location of the
    /// problem.</remarks>
    static public class LoggedCallWrapper {
        /// <summary>Wraps a simple method with no arguments which returns nothing.</summary>
        /// <param name="action">A method to wrap.</param>
        public static void Action(Action action) {
            try {
                action.Invoke();
            } catch (Exception e) {
                Logger.logExceptionRepeated(e);
            }
        }

        /// <summary>Wraps a method with one argument which returns nothing.</summary>
        /// <param name="action">A method to wrap.</param>
        public static void Action<T1>(Action<T1> action, T1 arg1) {
            try {
                action.Invoke(arg1);
            } catch (Exception e) {
                Logger.logExceptionRepeated(e);
            }
        }
    }

    
    [KSPAddon(KSPAddon.Startup.Instantly, true /*once*/)]
    class Loader : UnityEngine.MonoBehaviour {
        private const string LOGGING_LEVEL = "loggingLevel";
        private const string LOGGER_NAME = "loggerName";
        
        public void Awake() {
            ConfigNode nodeSettings =
                GameDatabase.Instance.GetConfigNode("KSPDev/settings/KSPDevConfig");
            if (nodeSettings == null) {
                UnityEngine.Debug.LogWarning(
                    "[KSPDev]: settings.cfg not found or invalid. Assume disabled state.");
                return;
            }

            // Load logging level.
            var strLevel = nodeSettings.GetValue(LOGGING_LEVEL);
            if (string.IsNullOrEmpty(strLevel)) {
                UnityEngine.Debug.LogWarning(
                    "[KSPDev]: Logging level is not set or empty. Assume disabled state.");
                return;
            }
            switch (strLevel.ToUpper()) {
            case "NONE":
                Logger.logLevel = LogLevel.NONE;
                break;
            case "ERROR":
                Logger.logLevel = LogLevel.ERROR;
                break;
            case "WARNING":
                Logger.logLevel = LogLevel.WARNING;
                break;
            case "INFO":
                Logger.logLevel = LogLevel.INFO;
                break;
            case "TRACE":
                Logger.logLevel = LogLevel.TRACE;
                break;
            default:
                UnityEngine.Debug.LogError(String.Format(
                    "[KSPDev]: Logging level '{0}' is not recognized. Assume disabled state.",
                    strLevel));
                return;
            }
            if (Logger.logLevel == LogLevel.NONE) {
                UnityEngine.Debug.Log("[KSPDev]: Logging DISABLED");
            }
            UnityEngine.Debug.Log(String.Format(
                "[KSPDev]: Logging level set to: {0}", strLevel.ToUpper()));

            // Load and initialize the logger.
            string loggerName = nodeSettings.GetValue(LOGGER_NAME);
            if (!string.IsNullOrEmpty(loggerName)) {
                if (loggerName == "DevLogger") {
                    UnityEngine.Debug.Log("[KSPDev]: Activate logger: " + loggerName);
                    DevLogger.activate(nodeSettings.GetNode(loggerName));
                } else {
                    UnityEngine.Debug.LogWarning("[KSPDev]: Unknown logger: " + loggerName);
                }
            }

            // TODO: Load aggregationPeriod
        }
    }
}
