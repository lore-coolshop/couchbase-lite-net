// 
//  Log.cs
// 
//  Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

using System;
using System.IO;
using System.Runtime.InteropServices;

using Couchbase.Lite.DI;
using Couchbase.Lite.Interop;
using Couchbase.Lite.Sync;
using Couchbase.Lite.Util;

using JetBrains.Annotations;

using LiteCore.Interop;

using ObjCRuntime;

namespace Couchbase.Lite.Logging
{
    /// <summary>
    /// Centralized logging facility.
    /// </summary>
    internal static unsafe class Log
    {
        #region Constants

        [NotNull]
        private static readonly LogTo _To;

        internal static readonly C4LogDomain* LogDomainBLIP = c4log_getDomain("BLIP", false);

        internal static readonly C4LogDomain* LogDomainWebSocket = c4log_getDomain("WS", false);

        internal static readonly C4LogDomain* LogDomainSyncBusy = c4log_getDomain("SyncBusy", false);

        #endregion

        #region Variables

        private static AtomicBool _Initialized = new AtomicBool(false);
        private static string _BinaryLogDirectory;
        private static ILogger TextLogger;

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        private static readonly C4LogCallback _LogCallback = LiteCoreLog;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        internal static string BinaryLogDirectory
        {
            get => _BinaryLogDirectory;
            set {
                if (_BinaryLogDirectory == value) {
                    return;
                }

                _BinaryLogDirectory = value ?? DefaultBinaryLogDirectory();
                try {
                    Directory.CreateDirectory(_BinaryLogDirectory);
                } catch(Exception e) {
                    Console.WriteLine($"COUCHBASE LITE WARNING: FAILED TO CREATE BINARY LOGGING DIRECTORY {_BinaryLogDirectory}: {e}");
                    return;
                }

                C4Error err;
                #if DEBUG
                var defaultLevel = C4LogLevel.Debug;
                #else
                var defaultLevel = C4LogLevel.Verbose;
                #endif

                var success = Native.c4log_writeToBinaryFile(defaultLevel, 
                    Path.Combine(_BinaryLogDirectory, 
                        $"log-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"), 
                    &err);
                if(!success) {
                    Console.WriteLine($"COUCHBASE LITE WARNING: FAILED TO INITIALIZE LOGGING FILE IN {_BinaryLogDirectory}");
                    Console.WriteLine($"ERROR {err.domain} / {err.code}");
                }
            }
        }

        #endregion

        #region Properties

        [NotNull]
        internal static LogTo To
        {
            get {
                if (!_Initialized.Set(true)) {
                    if (BinaryLogDirectory == null) {
                        BinaryLogDirectory = DefaultBinaryLogDirectory();
                    }

                    var oldLevel = Database.GetLogLevels(LogDomain.Couchbase)[LogDomain.Couchbase];
                    Database.SetLogLevel(LogDomain.Couchbase, LogLevel.Info);
                    To.Couchbase.I("Startup", HTTPLogic.UserAgent);
                    Database.SetLogLevel(LogDomain.Couchbase, oldLevel);
                }

                return _To;
            }
        }

        #endregion

        #region Constructors

        static Log()
        {
            _To = new LogTo();
        }

        #endregion

        #region Internal Methods

        internal static void EnableTextLogging(ILogger textLogger)
        {
            Native.c4log_writeToCallback(C4LogLevel.Debug, _LogCallback, true);
            TextLogger = textLogger;
        }

        internal static void DisableTextLogging()
        {
            Native.c4log_writeToCallback(C4LogLevel.Debug, null, true);
            IDisposable loggerOld = TextLogger as IDisposable;
            TextLogger = null;
            loggerOld?.Dispose();
        }

        #endregion

        #region Private Methods

        private static string DefaultBinaryLogDirectory()
        {
            var dir = Service.GetRequiredInstance<IDefaultDirectoryResolver>();
            return Path.Combine(dir.DefaultDirectory(), "Logs");
        }

        private static C4LogDomain* c4log_getDomain(string name, bool create)
        {
            var bytes = Marshal.StringToHGlobalAnsi(name);
            return Native.c4log_getDomain((byte*) bytes, create);
        }

        [MonoPInvokeCallback(typeof(C4LogCallback))]
        private static void LiteCoreLog(C4LogDomain* domain, C4LogLevel level, string message, IntPtr ignored)
        {
            var domainName = Native.c4log_getDomainName(domain);
            foreach (var logger in To.All) {
                if (logger.Domain == domainName) {
                    logger.QuickWrite(level, message, TextLogger);
                    return;
                }
            }

            To.LiteCore.QuickWrite(level, message, TextLogger);
        }

        #endregion
    }
}
