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

        public override void Init(EssentialsPlugin essentials, LifeServer server)
        {
            base.Init(essentials, server);

            InitConfig();
            CreatePlayerCommands();
        }

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

        void InitConfig()
        {
            roleplayConfigPath = $"{EssentialsPlugin.essentialDirectoryPath}/roleplay.json";

            if (!File.Exists(roleplayConfigPath))
            {
                config = new RoleplayConfig()
                {

                };

                string json = JsonConvert.SerializeObject(config);

                File.WriteAllText(roleplayConfigPath, json);
            }
            else
            {
                string json = File.ReadAllText(roleplayConfigPath);

                try
                {
                    config = JsonConvert.DeserializeObject<RoleplayConfig>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Setup();
        }

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