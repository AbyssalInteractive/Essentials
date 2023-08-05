using Life.Network;
using Life.DB;
using Life.UI;
using Life;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Essentials.Roleplay;

namespace Essentials
{
    public class EssentialRoleplay : BaseEssential
    {
        public static string roleplayConfigPath;

        public CarDealership carDealership = new CarDealership();

        private RoleplayConfig config;

        /// <summary>
        /// Init EssentialRoleplay system
        /// </summary>
        /// <param name="essentials"></param>
        /// <param name="server"></param>
        public override void Init(EssentialsPlugin essentials, LifeServer server)
        {
            base.Init(essentials, server);

            InitConfig();
            CreatePlayerCommands();

            carDealership.Init(config.carDealershipConfig);
        }

        /// <summary>
        /// Create all player rp commands
        /// </summary>
        void CreatePlayerCommands()
        {
            SChatCommand meCommand = new SChatCommand("/me", "Say action", "/me <message>", (player, args) =>
            {
                string message = "";

                if(args.Length > 0)
                {
                    for (int i = 0; i < args.Length; i++)
                        message += $" {args[i]}";

                    server.SendLocalText($"<color={LifeServer.COLOR_ME}>{player.character.Firstname} {player.character.Lastname}{message}</color>", 10f, player.setup.transform.position);
                }
                else
                {
                    player.SendText($"<color={LifeServer.COLOR_RED}>USAGE: <color=white>/me <action></color></color>");
                }
            });

            meCommand.Register();

            SChatCommand playerCountCommand = new SChatCommand("/playercount", new string[] { "/p", "/players"}, "See player count", "/p(layercount)", (player, args) =>
            {
                player.SendText($"<color={LifeServer.COLOR_BLUE}>Il y a {server.PlayerCount} citoyen(s) en ville.</color>");
            });

            playerCountCommand.Register();

            SChatCommand rulesCommand = new SChatCommand("/regles", new string[] { "/r", "/rules" }, "See rules", "/r(egles)", (player, args) =>
            {
                player.SendText($"<color={LifeServer.COLOR_BLUE}>Règles du serveur</color>");
                player.SendText($"<color={LifeServer.COLOR_BLUE}>1. Voici ma première règle</color>");
                player.SendText($"<color={LifeServer.COLOR_BLUE}>2. Voici ma seconde règle, n'oubliez pas de bien lire.</color>");
                player.SendText($"<color={LifeServer.COLOR_BLUE}>3. [...]</color>");
            });

            rulesCommand.Register();
        }

        /// <summary>
        /// Create roleplay configuration file or read it
        /// </summary>
        void InitConfig()
        {
            var settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            roleplayConfigPath = $"{EssentialsPlugin.essentialDirectoryPath}/roleplay.json";

            if (!File.Exists(roleplayConfigPath))
            {
                config = new RoleplayConfig();
                config.carDealershipConfig.shopPosition = new Position(MapManager.instance.concessionnaireShop.position.x, MapManager.instance.concessionnaireShop.position.y, MapManager.instance.concessionnaireShop.position.z);

                string json = JsonConvert.SerializeObject(config, settings);

                File.WriteAllText(roleplayConfigPath, json);
            }
            else
            {
                string json = File.ReadAllText(roleplayConfigPath);

                try
                {
                    config = JsonConvert.DeserializeObject<RoleplayConfig>(json, settings);

                    string newJson = JsonConvert.SerializeObject(config, settings);

                    File.WriteAllText(roleplayConfigPath, json);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Setup();
        }

        /// <summary>
        /// Setup all variables from essential roleplay config
        /// </summary>
        void Setup()
        {
            server.economy.startBank = config.startBank;
            server.economy.startMoney = config.startMoney;
            server.world.maxRentsPerCharacter = config.maxRentsPerCharacter;
            server.world.maxTerrainsPerCharacter = config.maxTerrainsPerCharacter;
        }

        public override void OnPlayerSpawnCharacter(Player player)
        {
            base.OnPlayerSpawnCharacter(player);

            Characters character = player.character;

            Terrains terrains = Nova.a.GetOwnedTerrains(character.Id);
            Terrains rents = Nova.a.GetOwnedRents(character.Id);

            PlayerData playerData = player.GetPlayerData();

            bool isWhitelisted = false;

            if (Nova.serverInfo.isWhitelisted)
            {
                try
                {
                    string data = player.character.WhitelistResponse;

                    if (data != null)
                    {
                        WhitelistResponse response = JsonUtility.FromJson<WhitelistResponse>(data);

                        player.setup.TargetLockCFM(!response.accepted);

                        isWhitelisted = response.accepted;
                    }
                }
                catch { }
            }
            else
            {
                player.setup.TargetLockCFM(false);
                isWhitelisted = true;
            }

            if (isWhitelisted && player.character.PrisonTime == 0)
            {
                if (Nova.UnixTimeNow() - 600 < player.character.LastDisconnect)
                {
                    player.SendText($"<color=green>Téléportation à votre dernière position.</color>");
                    player.setup.TargetSetPosition(new Vector3(player.character.LastPosX, player.character.LastPosY, player.character.LastPosZ));
                }
                else
                {
                    UIPanel spawnPanel = new UIPanel("Choix du point d'apparition", UIPanel.PanelType.Tab)
                        .SetText("Choisissez à quel endroit vous souhaitez apparaître.")
                        .AddButton("Fermer", (ui) =>
                        {
                            player.ClosePanel(ui);
                        })
                        .AddButton("Choisir", (ui) =>
                        {
                            ui.SelectTab();
                        });

                    spawnPanel.AddTabLine("Mairie", (ui) =>
                    {
                        player.setup.TargetSetPosition(MapManager.instance.mairiePosition);
                        player.ClosePanel(ui);
                    });

                    if (player.HasBiz())
                    {
                        spawnPanel.AddTabLine("Lieu de travail", (ui) =>
                        {
                            Vector3 position = Nova.a.GetSpawnablePosition((uint)player.biz.TerrainId);

                            if (position != Vector3.zero)
                            {
                                player.setup.TargetSetPosition(position);
                                player.ClosePanel(ui);
                            }
                            else
                            {
                                player.SendText($"<color={LifeServer.COLOR_RED}>Impossible d'apparaître à cet endroit.</color>");
                            }
                        });
                    }


                    for (int i = 0; i < terrains.terrains.Length; i++)
                    {
                        uint id = terrains.terrains[i].id;

                        Vector3 pos = Nova.a.GetSpawnablePosition(id);

                        if (pos == Vector3.zero)
                            continue;

                        spawnPanel.AddTabLine($"Terrain N°{terrains.terrains[i].id}", (ui) =>
                        {
                            Vector3 position = Nova.a.GetSpawnablePosition(id);

                            if (position != Vector3.zero)
                            {
                                player.setup.TargetSetPosition(position);
                                player.ClosePanel(ui);
                            }
                            else
                            {
                                player.ClosePanel(ui);
                            }
                        });
                    }

                    for (int i = 0; i < rents.terrains.Length; i++)
                    {
                        uint id = rents.terrains[i].id;

                        if (terrains.terrains.Where(t => t.id == id).Count() > 0)
                            continue;

                        Vector3 pos = Nova.a.GetSpawnablePosition(id);

                        if (pos == Vector3.zero)
                            continue;

                        spawnPanel.AddTabLine($"Location N°{rents.terrains[i].id}", (ui) =>
                        {
                            Vector3 position = Nova.a.GetSpawnablePosition(id);

                            if (position != Vector3.zero)
                            {
                                player.setup.TargetSetPosition(position);
                                player.ClosePanel(ui);
                            }
                            else
                            {
                                player.ClosePanel(ui);
                            }
                        });
                    }

                    player.ShowPanelUI(spawnPanel);
                }
            }
        }
    }

    [System.Serializable]
    public class RoleplayConfig
    {
        public int startMoney = 500;
        public int startBank;
        public ushort maxTerrainsPerCharacter = 3;
        public ushort maxRentsPerCharacter = 1;

        public CarDealershipConfig carDealershipConfig = new CarDealershipConfig()
        {
            carShopName = "Concessionnaire automobile",
            carForSales = new CarForSale[]
            {
                new CarForSale(16, 3500),
                new CarForSale(15, 5990),
                new CarForSale(0, 11990),
                new CarForSale(8, 17190),
                new CarForSale(13, 23558),
                new CarForSale(24, 39550),
                new CarForSale(10, 64650),
                new CarForSale(14, 94000)
            }
        };

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this);

            File.WriteAllText(EssentialRoleplay.roleplayConfigPath, json);
        }
    }
}