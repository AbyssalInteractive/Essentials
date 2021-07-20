using Life.Network;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace Essentials
{
    public class EssentialRoleplay : BaseEssential
    {
        public static string roleplayConfigPath;

        public RoleplayConfig config;

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
            roleplayConfigPath = $"{EssentialsPlugin.essentialDirectoryPath}/roleplay.json";

            if (!File.Exists(roleplayConfigPath))
            {
                config = new RoleplayConfig();

                string json = JsonConvert.SerializeObject(config);

                File.WriteAllText(roleplayConfigPath, json);
            }
            else
            {
                string json = File.ReadAllText(roleplayConfigPath);

                try
                {
                    config = JsonConvert.DeserializeObject<RoleplayConfig>(json);

                    string newJson = JsonConvert.SerializeObject(config);

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
    }

    [System.Serializable]
    public class RoleplayConfig
    {
        public int startMoney = 500;
        public int startBank;
        public ushort maxTerrainsPerCharacter = 3;
        public ushort maxRentsPerCharacter = 1;

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this);

            File.WriteAllText(EssentialRoleplay.roleplayConfigPath, json);
        }
    }
}