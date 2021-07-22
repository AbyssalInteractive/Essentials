using Life.Network;
using UnityEngine;
using System.IO;
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
            shopPosition = new Position(625.585f, 50.05f, 973.712f),
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