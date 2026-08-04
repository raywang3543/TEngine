using System;
using System.IO;
using GameLogic.Core.Model;
using UnityEngine;

namespace GameLogic.Core
{
    public interface IStacklandsSaveStore
    {
        StacklandsProfileData LoadProfile();
        StacklandsRunData LoadRun();
        void SaveProfile(StacklandsProfileData profile);
        void SaveRun(StacklandsRunData run);
        void DeleteRun();
    }

    public sealed class JsonStacklandsSaveStore : IStacklandsSaveStore
    {
        private readonly string _profilePath;
        private readonly string _runPath;

        public JsonStacklandsSaveStore(string directory = null)
        {
            string root = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
            Directory.CreateDirectory(root);
            _profilePath = Path.Combine(root, "stacklands-original-profile.json");
            _runPath = Path.Combine(root, "stacklands-original-run.json");
        }

        public StacklandsProfileData LoadProfile() => Load(_profilePath, new StacklandsProfileData());
        public StacklandsRunData LoadRun() => Load<StacklandsRunData>(_runPath, null);
        public void SaveProfile(StacklandsProfileData profile) => Save(_profilePath, profile);
        public void SaveRun(StacklandsRunData run) => Save(_runPath, run);

        public void DeleteRun()
        {
            if (File.Exists(_runPath)) File.Delete(_runPath);
        }

        private static T Load<T>(string path, T fallback)
        {
            try
            {
                if (!File.Exists(path) && File.Exists(path + ".bak")) File.Copy(path + ".bak", path);
                return File.Exists(path) ? JsonUtility.FromJson<T>(File.ReadAllText(path)) : fallback;
            }
            catch
            {
                try { return JsonUtility.FromJson<T>(File.ReadAllText(path + ".bak")); }
                catch { return fallback; }
            }
        }

        private static void Save<T>(string path, T value)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(value, true));
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }
}
