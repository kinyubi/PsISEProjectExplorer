﻿using System.Threading;
using PsISEProjectExplorer.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PsISEProjectExplorer.Services
{
    public static class FileSystemChangeNotifier
    {
        public static event EventHandler<FileSystemChangedInfo> FileSystemChanged;

        private static readonly ISet<string> ChangePool = new HashSet<string>();

        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        private static readonly FileSystemWatcher Watcher = new FileSystemWatcher();

        private static FilesPatternProvider FilesPatternProvider { get; set; }

        static FileSystemChangeNotifier()
        {
            Task.Factory.StartNew(ChangeNotifier);
        }

        public static void Watch(string path, FilesPatternProvider filesPatternProvider)
        {
            Watcher.EnableRaisingEvents = false;
            if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }
            FilesPatternProvider = filesPatternProvider;
            Watcher.Path = path;
            Watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Security;
            Watcher.IncludeSubdirectories = true;
            Watcher.Changed += OnFileChanged;
            Watcher.Created += OnFileChanged;
            Watcher.Deleted += OnFileChanged;
            Watcher.Renamed += OnFileRenamed;
            Watcher.EnableRaisingEvents = true;
        }

        // runs on a separate thread (from system)
        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            bool isDir = Directory.Exists(e.FullPath);
            if (isDir && !FilesPatternProvider.DoesDirectoryMatch(e.FullPath))
            {
                return;
            }
            if (!isDir && !FilesPatternProvider.DoesFileMatch(e.FullPath))
            {
                return;
            }
            lock (ChangePool)
            {
                ChangePool.Add(e.FullPath);
            }
        }

        // runs on a separate thread (from system)
        private static void OnFileRenamed(object source, RenamedEventArgs e)
        {
            bool isDir = Directory.Exists(e.FullPath) || Directory.Exists(e.OldFullPath);
            lock (ChangePool)
            {
                if (isDir || FilesPatternProvider.DoesFileMatch(e.OldFullPath))
                {
                    ChangePool.Add(e.OldFullPath);
                }
                if ((isDir || FilesPatternProvider.DoesFileMatch(e.FullPath)) &&
                    e.FullPath.ToLowerInvariant() != e.OldFullPath.ToLowerInvariant())
                {
                    ChangePool.Add(e.FullPath);
                }
            }
        }

        // runs on a separate thread (created in constructor)
        private static void ChangeNotifier()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (CancellationTokenSource.Token.IsCancellationRequested)
                {
                    return;
                }
                lock (ChangePool)
                {
                    if (ChangePool.Any())
                    {
                        IList<string> pathsChanged = RemoveSubdirectories(ChangePool);
                        var changedInfo = new FileSystemChangedInfo(pathsChanged);
                        FileSystemChanged(null, changedInfo);
                        ChangePool.Clear();
                    }
                }
            }
        }

        private static IList<string> RemoveSubdirectories(ISet<string> dirList)
        {
            IList<string> result = new List<string>();
            foreach (string dir in dirList)
            {
                if (dirList.Where(d => d != dir).All(d => !FileSystemOperationsService.IsSubdirectory(d, dir)))
                {
                    result.Add(dir);
                }
            }
            return result;
        }
    }
}