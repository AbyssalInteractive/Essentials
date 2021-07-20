using Life.Network;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using Newtonsoft.Json;

namespace Essentials
{
    public class EssentialAnnouncer : BaseEssential
    {
        public static string announcerConfigPath;

        public AnnouncerConfig config;

        /// <summary>
        /// Init Essential Announcer system
        /// </summary>
        /// <param name="essentials"></param>
        /// <param name="server"></param>
        public override void Init(EssentialsPlugin essentials, LifeServer server)
        {
            base.Init(essentials, server);

            InitConfig();
            LoadAnnounces();
            CreateConsoleCommands();
        }

        /// <summary>
        /// Create all console commands
        /// </summary>
        void CreateConsoleCommands()
        {
            SConsoleCommand reloadAnnouncerCommand = new SConsoleCommand("reloadannouncer", "Reload announcer config", "reloadannouncer", (args) =>
            {
                InitConfig();
            });

            SConsoleCommand createAnnounceCommand = new SConsoleCommand("createannounce", "Create an announce", "createannounce <message> <seconds interval>", (args) =>
            {
                List<Announce> announces = new List<Announce>();

                Debug.Log(JsonUtility.ToJson(args));

                for (int i = 0; i < config.announces.Length; i++)
                    announces.Add(config.announces[i]);

                Announce announce = new Announce()
                {
                    message = "salut",
                    secondsInterval = 5f
                };

                announces.Add(announce);

                config.announces = announces.ToArray();
                config.Save();
            });

            reloadAnnouncerCommand.Register();
            createAnnounceCommand.Register();
        }

        /// <summary>
        /// Create announcer configuration file or read it
        /// </summary>
        void InitConfig()
        {
            announcerConfigPath = $"{EssentialsPlugin.essentialDirectoryPath}/announcer.json";

            if (!File.Exists(announcerConfigPath))
            {
                config = new AnnouncerConfig() 
                { 
                    announces = new Announce[] { new Announce() { message = "This is an announce example. You can config this message in Plugins/Essentials/announcer.json", secondsInterval = 300f } }
                };

                string json = JsonConvert.SerializeObject(config);

                File.WriteAllText(announcerConfigPath, json);
            }
            else
            {
                string json = File.ReadAllText(announcerConfigPath);

                try
                {
                    config = JsonConvert.DeserializeObject<AnnouncerConfig>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Load all announces in configuration and run coroutines
        /// </summary>
        void LoadAnnounces()
        {
            for(int i = 0; i < config.announces.Length; i++)
            {
                Announce announce = config.announces[i];

                server.lifeManager.StartCoroutine(SendAnnounce(announce));
            }
        }

        /// <summary>
        /// Send delayed announce
        /// </summary>
        /// <param name="announce"></param>
        /// <returns></returns>
        public IEnumerator SendAnnounce(Announce announce)
        {
            yield return new WaitForSeconds(announce.secondsInterval);
            server.SendMessageToAll(announce.message);

            server.lifeManager.StartCoroutine(SendAnnounce(announce));
        }
    }

    [System.Serializable]
    public class AnnouncerConfig
    {
        public Announce[] announces;

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this);

            File.WriteAllText(EssentialAnnouncer.announcerConfigPath, json);
        }
    }

    [System.Serializable]
    public struct Announce
    {
        public string message;
        public float secondsInterval;
    }
}