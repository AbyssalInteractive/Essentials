using Life.Network;
using Life;
using System.IO;
using UnityEngine;

namespace Essentials
{
    public class EssentialsPlugin : Plugin
    {
        public static string essentialDirectoryPath;
        public static string essentialConfigPath;
        public EssentialsConfig config;

        private readonly EssentialAnnouncer announcer = new EssentialAnnouncer();

        public EssentialsPlugin(IGameAPI api) : base(api)
        {

        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            InitDirectory();

            announcer.Init(this, Nova.server);
        }

        void InitDirectory()
        {
            essentialDirectoryPath = $"{pluginsPath}/Essentials";
            essentialConfigPath = $"{essentialDirectoryPath}/config.json";

            if (!Directory.Exists(essentialDirectoryPath))
                Directory.CreateDirectory(essentialDirectoryPath);

            if(!File.Exists(essentialConfigPath))
            {
                config = new EssentialsConfig()
                {
                    serverName = Nova.serverInfo.serverName
                };

                string json = JsonUtility.ToJson(config);

                File.WriteAllText(essentialConfigPath, json);
            }else
            {
                string json = File.ReadAllText(essentialConfigPath);
                
                try
                {
                    config = JsonUtility.FromJson<EssentialsConfig>(json);
                }catch(System.Exception e)
                {
                    Debug.LogException(e);   
                }
            }
        }
    }

    [System.Serializable]
    public class EssentialsConfig
    {
        public string serverName;

        public void Save()
        {
            string json = JsonUtility.ToJson(this);

            File.WriteAllText(EssentialsPlugin.essentialConfigPath, json);
        }
    }
}