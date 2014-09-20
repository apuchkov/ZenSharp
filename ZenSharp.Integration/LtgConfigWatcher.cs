﻿using System;
using System.IO;

using Github.Ulex.ZenSharp.Core;

using JetBrains.Application;
using JetBrains.Application.Settings;

using NLog;

namespace Github.Ulex.ZenSharp.Integration
{
    [ShellComponent]
    internal sealed class LtgConfigWatcher : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private FileSystemWatcher _watcher;

        private GenerateTree _tree;

        private readonly IContextBoundSettingsStore _boundSettings;
        
        public LtgConfigWatcher(ISettingsStore settingsStore)
        {
            _boundSettings = settingsStore.BindToContextTransient(ContextRange.ApplicationWide);

            var path = ZenSettings.GetTreePath;
            try
            {
                ReinitializeWatcher(path);
                Reload(path);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void ReinitializeWatcher(string path)
        {
            if (_watcher != null)
            {
                var watcher = _watcher;
                _watcher = null;
                watcher.Dispose();
            }

            _watcher = new FileSystemWatcher(Path.GetDirectoryName(path), "*.ltg")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };
            _watcher.Changed += (sender, args) => Reload(path);
        }

        private ZenSharpSettings ZenSettings
        {
            get
            {
                return _boundSettings.GetKey<ZenSharpSettings>(SettingsOptimization.DoMeSlowly);
            }
        }

        public GenerateTree Tree
        {
            get
            {
                return _tree;
            }
        }

        public void Reload(string file)
        {
            try
            {
                var path = file;
                _tree = new LtgParser().ParseAll(File.ReadAllText(path));
                Log.Info("Config reloaded from {0}", path);
            }
            catch (Exception e)
            {
                Log.Error("Error loading config", e);
                throw;
            }
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}