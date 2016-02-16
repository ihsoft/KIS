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
    /// Use <c>logLevel</c> to set the desired level of logs verbosity. Keep in mind that
    /// too verbose logs may overflow in-game debug window.
    /// </remarks>
    public static class Logger {
        public static LogLevel logLevel = LogLevel.INFO;

        public delegate void logStackTrace_Delegate(LogLevel level, string tag);
        public delegate void logTrace_Delegate(String fmt, params object[] args);
        public delegate void logInfo_Delegate(String fmt, params object[] args);
        public delegate void logWarning_Delegate(String fmt, params object[] args);
        public delegate void logError_Delegate(String fmt, params object[] args);
        public delegate void logException_Delegate(Exception ex);

        public static logStackTrace_Delegate logStackTrace =
            NullLogger.logStackTrace;

        public static logTrace_Delegate logTrace =
            NullLogger.logTrace;
        public static logInfo_Delegate logInfo =
            NullLogger.logInfo;
        public static logWarning_Delegate logWarning =
            NullLogger.logWarning;
        public static logError_Delegate logError =
            NullLogger.logError;

        /// <summary>Logs an exception with the stack trace.</summary>
        /// <param name="ex">An exception to log.</param>
        public static logException_Delegate logException =
            NullLogger.logException;
    }

    /// <summary>Logger implementation that does nothing. I.e. no logging.</summary>
    static class NullLogger {
        public static void logStackTrace(LogLevel level, string tag) {}
        public static void logTrace(String fmt, params object[] args) {}
        public static void logInfo(String fmt, params object[] args) {}
        public static void logWarning(String fmt, params object[] args) {}
        public static void logError(String fmt, params object[] args) {}
        public static void logException(Exception ex) {}
    }
    
    /// <summary>
    /// A development logger that respects logging level and supports string formatting.
    /// </summary>
    public static class DevLogger {
        /// <summary>Makes this logger the active one.</summary>
        /// <param name="node">A config node for this logger settings. Can be <c>null</c>.</param>
        public static void activate(ConfigNode node) {
            // Set logging functions.
            Logger.logStackTrace = DevLogger.logStackTrace;
            Logger.logTrace = DevLogger.logTrace;
            Logger.logInfo = DevLogger.logInfo;
            Logger.logWarning = DevLogger.logWarning;
            Logger.logError = DevLogger.logError;

            UnityEngine.Debug.Log("DevLogger loaded");
        }

        public static void logStackTrace(LogLevel level, string tag) {
            if (Logger.logLevel >= level) {
                var st = new System.Diagnostics.StackTrace(1, true);
                log(level, "Stack trace:\n{0}", st);
            }
        }

        public static void logTrace(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.TRACE) {
                log(LogLevel.TRACE, fmt, args);
            }
        }

        public static void logInfo(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.INFO) {
                log(LogLevel.INFO, fmt, args);
            }
        }

        public static void logWarning(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.WARNING) {
                log(LogLevel.WARNING, fmt, args);
            }
        }

        public static void logError(String fmt, params object[] args) {
            if (Logger.logLevel >= LogLevel.ERROR) {
                log(LogLevel.ERROR, fmt, args);
            }
        }

        public static void logException(Exception ex) {
            log(LogLevel.EXCEPTION, ex.ToString());
        }
        
        private static void log(LogLevel level, String fmt, params object[] args) {
            var message = fmt;
            if (args.Length > 0) {  // Don't parse fmt string when no arguments.
                message = String.Format(fmt, args);
            }
            if (level == LogLevel.ERROR) {
                UnityEngine.Debug.LogError(message);
            } else if (level == LogLevel.WARNING) {
                UnityEngine.Debug.LogWarning(message);
            } else if (level == LogLevel.INFO) {
                UnityEngine.Debug.Log(message);
            } else if (level == LogLevel.EXCEPTION) {
                UnityEngine.Debug.LogError(message);
            } else {  // Log any unknown level as TRACE.
                UnityEngine.Debug.Log("[TRACE] " + message);
            } 
        }
    }


    /// <summary>A wrapper to catch and log exceptions from the methods</summary>
    /// <remarks>Delegate all engine events and other sensitive methods to the appropriate
    /// <c>Action</c> handler. Any exception in execution will be logged via
    /// <seealso cref="Logger.logException"/> giving the whole context and location of the
    /// problem.</remarks>
    static public class LoggedCallWrapper {
        /// <summary>Wraps a simple method with no arguments which returns nothing.</summary>
        /// <param name="action">A method to wrap.</param>
        public static void Action(Action action) {
            try {
                action.Invoke();
            } catch (Exception e) {
                Logger.logException(e);
            }
        }

        /// <summary>Wraps a method with one argument which returns nothing.</summary>
        /// <param name="action">A method to wrap.</param>
        public static void Action<T1>(Action<T1> action, T1 arg1) {
            try {
                action.Invoke(arg1);
            } catch (Exception e) {
                Logger.logException(e);
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
        }
    }
}
