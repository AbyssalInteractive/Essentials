using Life.Network;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace Essentials
{
    class EssentialGlobal : BaseEssential
    {
        /// <summary>
        /// Init this class
        /// </summary>
        /// <param name="essentials"></param>
        /// <param name="server"></param>
        public override void Init(EssentialsPlugin essentials, LifeServer server)
        {
            base.Init(essentials, server);
            this.server = server;

            CreatePlayerCommands();
        }

        /// <summary>
        /// Create all player commands
        /// </summary>
        void CreatePlayerCommands()
        {
            SChatCommand helpCommand = new SChatCommand("/help", "List all commands", "/help (page)", (player, args) =>
            {
                int page = 0;

                if (args.Length > 0)
                {
                    if (int.TryParse(args[0], out int n))
                    {
                        page = n-1;
                    }
                }

                List<SChatCommand> commands = server.chat.commands;

                player.SendText($"<color={LifeServer.COLOR_ORANGE}>====== Liste des commandes ({page+1}/{Math.Ceiling(commands.Count/5.0)}) ======</color>");

                for(int i = 5*page; i < (5*page)+5; i++)
                {
                    if (commands.Count <= i) break;

                    SChatCommand command = commands[i];
                    player.SendText($"<color={LifeServer.COLOR_ORANGE}>{command.fullCommandName} : {command.description} | usage : {command.usage}");
                }

                player.SendText($"<color={LifeServer.COLOR_ORANGE}>====== Liste des commandes ({page + 1}/{Math.Ceiling(commands.Count / 5.0)}) ======</color>");

            });

            helpCommand.Register();
        }
    }
}
