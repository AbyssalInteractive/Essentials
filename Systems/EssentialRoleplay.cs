using Life.Network;
using Life.UI;
using Life;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Life.VehicleSystem;
using Mirror;
using Life.PermissionSystem;
using Life.DB;
using Life.AreaSystem;

namespace Essentials
{
    public class EssentialRoleplay : BaseEssential
    {
        public override void Init(EssentialsPlugin essentials, LifeServer server)
        {
            base.Init(essentials, server);

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
    }
}