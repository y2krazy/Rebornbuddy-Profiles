using System;
using System.IO;
using ff14bot.Behavior;
using ff14bot.Interfaces;
using ProtoBuf;
using TreeSharp;

namespace Zekken
{
    public class ZekkenPlugin : IBotPlugin
    {
        private AvoidanceManager avoidanceManager;
        private Composite avoidanceBehavoir;
        private SettingsWindow settingsWindow;

        public static ZekkenSettings Settings { get; set; }

        public ShapeDatabase Database { get; set; }

        public bool Enabled { get; private set; }

        #region Plugin Implementation
        public string Author
        {
            get { return " Saga"; }
        }

        public string ButtonText
        {
            get { return "Settings"; }
        }

        public string Description
        {
            get { return "Telegraphed spell avoidance."; }
        }

        public string Name
        {
            get { return "Zekken"; }
        }

        public void OnButtonPress()
        {
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow {Plugin = this};
            }

            settingsWindow.ShowDialog();
        }

        public void OnDisabled()
        {
            TreeHooks.Instance.OnHooksCleared -= OnHooksCleared;

            if (Enabled) { UnhookAvoidance(); }
            Enabled = false;
        }

        public void OnEnabled()
        {
            Database = new ShapeDatabase();
            Database.Load();

            TreeHooks.Instance.OnHooksCleared += OnHooksCleared;

            const string path = Directories.SETTINGS_PATH;

            if (FileTools.PrepareLoad(path))
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    try { Settings = Serializer.Deserialize<ZekkenSettings>(stream); }
                    catch { Logger.WriteError("Failed to load settings from {0}.", path); }
                }
            }
            else
            {
                Settings = new ZekkenSettings();
                SettingsWindow.Save(path, Settings);
                Logger.WriteMessage("Created new settings file.");
            }

            if (!Enabled) { HookAvoidance(); }
            Enabled = true;
        }

        public void OnInitialize()
        {
            
        }

        public void OnPulse()
        {
            
        }

        public void OnShutdown()
        {
            if (Enabled) { UnhookAvoidance(); }
            Enabled = false;
        }

        public Version Version
        {
            get { return new Version(1, 4, 2); }
        }

        public bool WantButton
        {
            get { return true; }
        }

        public bool Equals(IBotPlugin other)
        {
            return other.Name == Name && other.Author == Author;
        }
        #endregion

        private void OnHooksCleared(object sender, EventArgs args)
        {
            HookAvoidance();
        }

        public void HookAvoidance()
        {
            avoidanceManager = new AvoidanceManager(Database);
            avoidanceBehavoir = avoidanceManager.GetAvoidanceBehavior();

            TreeHooks.Instance.AddHook("PreCombatLogic", avoidanceBehavoir);
            Logger.WriteMessage("Avoidance hooked.");
        }

        public void UnhookAvoidance()
        {
            avoidanceManager = null;

            if (avoidanceBehavoir != null) { TreeHooks.Instance.RemoveHook("PreCombatLogic", avoidanceBehavoir); }
            avoidanceBehavoir = null;

            Logger.WriteMessage("Avoidance unhooked.");
        }
    }
}
