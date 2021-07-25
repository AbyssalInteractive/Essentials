using Life.Network;
using Life;
using System.IO;
using UnityEngine;
using Life.UI;
using Mirror;
using Life.DB;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Essentials
{
    public class EssentialsPlugin : Plugin
    {
        public static List<PlayerData> players = new List<PlayerData>();

        public static string essentialDirectoryPath;
        public static string essentialPlayersPath;
        public static string essentialConfigPath;
        public EssentialsConfig config;

        private readonly EssentialAnnouncer announcer = new EssentialAnnouncer();
        private readonly EssentialAdmin admin = new EssentialAdmin();
        private readonly EssentialRoleplay roleplay = new EssentialRoleplay();
        private readonly EssentialGlobal global = new EssentialGlobal();
        private readonly EssentialWhitelist whitelist = new EssentialWhitelist();

        private LifeServer server;

        public EssentialsPlugin(IGameAPI api) : base(api)
        {

        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            server = Nova.server;

            InitDirectory();

            announcer.Init(this, server);
            admin.Init(this, server);
            roleplay.Init(this, server);
            global.Init(this, server);
            whitelist.Init(this, server);
        }

        public override void OnPlayerInput(Player player, KeyCode keyCode, bool onUI)
        {
            base.OnPlayerInput(player, keyCode, onUI);

            if(keyCode == KeyCode.N && !onUI)
            {
                if (player.ticketOpen)
                {
                    UIPanel ticketPanel = new UIPanel("Ticket support", UIPanel.PanelType.Text)
                        .SetText("Vous avez déjà un ticket actif !")
                        .AddButton("Fermer", (ui) => { player.ClosePanel(ui); })
                        .AddButton("Clôturer mon ticket", (ui) => {
                            player.ticketOpen = false;
                            admin.tickets.Remove(player);
                            player.ClosePanel(ui);
                        });

                    player.ShowPanelUI(ticketPanel);
                }
                else
                {
                    UIPanel ticketPanel = new UIPanel("Ticket support", UIPanel.PanelType.Input)
                    .SetInputPlaceholder("Quel est votre problème ?")
                    .AddButton("Fermer", (ui) => { player.ClosePanel(ui); })
                    .AddButton("Envoyer", (ui) => {
                        if (player.ticketOpen)
                            player.SendText(string.Format("<color={0}>Vous avez déjà un ticket d'ouvert.</color>", LifeServer.COLOR_RED));
                        else
                        {
                            player.ClosePanel(ui);

                            if (ui.inputText != null || ui.inputText.Length > 0)
                            {
                                admin.OnPlayerTicket(player, ui.inputText);
                                player.SendText(string.Format("<color={0}>Votre ticket a bien été envoyé à l'équipe de modération.</color>", LifeServer.COLOR_GREEN));
                            }
                            else
                            {
                                player.SendText($"<color={LifeServer.COLOR_RED}>Vous ne pouvez pas envoyer un ticket vide.</color>");
                            }
                        }
                    });
                    player.ShowPanelUI(ticketPanel);
                }
            }
        }

        public override void OnPlayerSpawnCharacter(Player player, NetworkConnection conn, Characters character)
        {
            PlayerData.Load(player);

            base.OnPlayerSpawnCharacter(player, conn, character);

            if (config.giveBcr && !character.HasBCR)
            {
                character.HasBCR = true;
            }

            if(config.useWhitelist)
                whitelist.OnPlayerWhitelist(player);
        }

        public override void OnPlayerDisconnect(NetworkConnection conn)
        {
            base.OnPlayerDisconnect(conn);

            Player player = server.Players.Where(p => p.conn == conn).FirstOrDefault();

            if (player != null)
            {
                if (server.Players.Contains(player))
                {
                    PlayerData data = PlayerData.GetPlayerData(player.steamId, player.character.Id);

                    data.Save();

                    players.Remove(data);

                    if (admin.tickets.ContainsKey(player))
                        admin.tickets.Remove(player);
                }
            }
        }

        void InitDirectory()
        {
            essentialDirectoryPath = $"{pluginsPath}/Essentials";
            essentialConfigPath = $"{essentialDirectoryPath}/config.json";
            essentialPlayersPath = $"{essentialDirectoryPath}/PlayerDatas";

            if (!Directory.Exists(essentialDirectoryPath))
                Directory.CreateDirectory(essentialDirectoryPath);

            if (!Directory.Exists(essentialPlayersPath))
                Directory.CreateDirectory(essentialPlayersPath);

            if (!File.Exists(essentialConfigPath))
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
                    config = new EssentialsConfig();
                    config = JsonUtility.FromJson<EssentialsConfig>(json);

                    string newJson = JsonConvert.SerializeObject(config);

                    File.WriteAllText(essentialConfigPath, json);
                }
                catch(System.Exception e)
                {
                    Debug.LogException(e);   
                }
            }
        }
    }

    [System.Serializable]
    public class EssentialsConfig
    {
        public string serverName = "My Server";
        public bool giveBcr = false;
        public bool useWhitelist = false;

        public void Save()
        {
            string json = JsonUtility.ToJson(this);

            File.WriteAllText(EssentialsPlugin.essentialConfigPath, json);
        }
    }

    static class PlayerExtension
    {
        public static PlayerData GetPlayerData(this Player player)
        {
            return EssentialsPlugin.players.Where(p => p.player == player).FirstOrDefault();
        }
    }

    [System.Serializable]
    public class PlayerData
    {
        public ulong steamId;
        public int characterId;

        public bool whitelisted;
        public PlayerWhitelist lastWhitelist;

        [System.NonSerialized]
        public Player player;

        public PlayerData(ulong steamId, int characterId, Player player)
        {
            this.steamId = steamId;
            this.characterId = characterId;
            this.player = player;
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this);

            File.WriteAllText($"{EssentialsPlugin.essentialPlayersPath}/{steamId}-{characterId}.json", json);
        }

        public static PlayerData LoadFromJson(string data)
        {
            return JsonConvert.DeserializeObject<PlayerData>(data);
        }

        public static PlayerData GetPlayerData(ulong steamId, int characterId)
        {
            string playerPath = $"{EssentialsPlugin.essentialPlayersPath}/{steamId}-{characterId}.json";

            PlayerData playerData = null;

            if (File.Exists(playerPath))
            {
                string jsonData = File.ReadAllText(playerPath);

                playerData = JsonConvert.DeserializeObject<PlayerData>(jsonData);
            }

            PlayerData foundData = EssentialsPlugin.players.Where(p => p.steamId == steamId && p.characterId == characterId).FirstOrDefault();

            if (foundData != null)
                playerData = foundData;

            return playerData;
        }

        public static PlayerData Load(Player player)
        {
            ulong steamId = player.steamId;
            int characterId = player.character.Id;

            string playerPath = $"{EssentialsPlugin.essentialPlayersPath}/{steamId}-{characterId}.json";

            PlayerData playerData = null;

            if (File.Exists(playerPath))
            {
                string jsonData = File.ReadAllText(playerPath);

                playerData = JsonConvert.DeserializeObject<PlayerData>(jsonData);
                playerData.player = player;
            }else
            {
                playerData = new PlayerData(steamId, characterId, player);

                playerData.Save();
            }

            if(playerData == null)
                Debug.LogError($"Essential Error: Unable to load player data {steamId}-{characterId}");

            PlayerData foundData = EssentialsPlugin.players.Where(p => p.steamId == steamId && p.characterId == characterId).FirstOrDefault();

            if (foundData != null)
            {
                playerData = foundData;
                playerData.player = player;
            }
            else if (playerData != null)
                EssentialsPlugin.players.Add(playerData);

            return playerData;
        }
    }
}