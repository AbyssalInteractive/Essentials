using Life.Network;
using Life;
using System.IO;
using UnityEngine;
using Life.UI;
using Mirror;
using Life.DB;
using System.Linq;

namespace Essentials
{
    public class EssentialsPlugin : Plugin
    {
        public static string essentialDirectoryPath;
        public static string essentialConfigPath;
        public EssentialsConfig config;

        private readonly EssentialAnnouncer announcer = new EssentialAnnouncer();
        private readonly EssentialAdmin admin = new EssentialAdmin();
        private readonly EssentialRoleplay roleplay = new EssentialRoleplay();
        private readonly EssentialsGlobal global = new EssentialsGlobal();

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
            base.OnPlayerSpawnCharacter(player, conn, character);
            
            if(config.giveBcr && !character.HasBCR)
            {
                character.HasBCR = true;
            }
        }

        public override void OnPlayerDisconnect(NetworkConnection conn)
        {
            base.OnPlayerDisconnect(conn);

            Player player = server.Players.Where(p => p.conn == conn).FirstOrDefault();

            if (player != null)
            {
                if (server.Players.Contains(player))
                {
                    if (admin.tickets.ContainsKey(player))
                        admin.tickets.Remove(player);
                }
            }
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
        public bool giveBcr;

        public void Save()
        {
            string json = JsonUtility.ToJson(this);

            File.WriteAllText(EssentialsPlugin.essentialConfigPath, json);
        }
    }
}
