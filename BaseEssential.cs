using Life.Network;
using Life.VehicleSystem;
using Mirror;

namespace Essentials
{
    public abstract class BaseEssential
    {
        internal EssentialsPlugin essentials;
        internal LifeServer server;

        /// <summary>
        /// Init BaseEssential class
        /// </summary>
        /// <param name="essentials"></param>
        /// <param name="server"></param>
        public virtual void Init(EssentialsPlugin essentials, LifeServer server)
        {
            this.server = server;
            this.essentials = essentials;

            this.server.OnPlayerConnectEvent += OnPlayerConnect;
            this.server.OnPlayerDisconnectEvent += OnPlayerDisconnect;
            this.server.OnPlayerSpawnCharacterEvent += OnPlayerSpawnCharacter;
            this.server.OnPlayerInputEvent += OnPlayerInput;
            this.server.OnHourPassedEvent += OnHourPassed;
            this.server.OnMinutePassedEvent += OnMinutePassed;
            this.server.OnPlayerMoneyEvent += OnPlayerMoney;
            this.server.OnPlayerReceiveItemEvent += OnPlayerReceiveItem;
            this.server.OnPlayerDropItemEvent += OnPlayerDropItem;
            this.server.OnPlayerUseCommandEvent += OnPlayerUseCommand;
            this.server.OnPlayerBuyTerrainEvent += OnPlayerBuyTerrain;
            this.server.OnPlayerSellDrugsEvent += OnPlayerSellDrugs;
            this.server.OnPlayerConsumeDrugEvent += OnPlayerConsumeDrug;
            this.server.OnPlayerDeathEvent += OnPlayerDeath;
            this.server.OnPlayerKillPlayerEvent += OnPlayerKillPlayer;
            this.server.OnPlayerDamagePlayerEvent += OnPlayerDamagePlayer;
            this.server.OnPlayerChangeVehiclePlateEvent += OnPlayerChangeVehiclePlate;
            this.server.OnPlayerChangeVehicleColorEvent += OnPlayerChangeVehicleColor;
        }

        public virtual void OnPlayerConnect(Player player) { }
        public virtual void OnPlayerSpawnCharacter(Player player) { }
        public virtual void OnPlayerDisconnect(NetworkConnection conn) { }
        public virtual void OnPlayerInput(Player player, UnityEngine.KeyCode key, bool onUI) { }
        public virtual void OnHourPassed() { }
        public virtual void OnMinutePassed() { }
        public virtual void OnPlayerMoney(Player player, int amount, string reason) { }
        public virtual void OnPlayerReceiveItem(Player player, int itemId, int slotId, int number) { }
        public virtual void OnPlayerDropItem(Player player, int itemId, int slotId, int number) { }
        public virtual void OnPlayerUseCommand(Player player, SChatCommand command) { }
        public virtual void OnPlayerBuyTerrain(Player player, int terrainId, int price) { }
        public virtual void OnPlayerSellDrugs(Player player, int number, int price) { }
        public virtual void OnPlayerConsumeDrug(Player player) { }
        public virtual void OnPlayerDeath(Player player) { }
        public virtual void OnPlayerKillPlayer(Player killer, Player killed) { }
        public virtual void OnPlayerDamagePlayer(Player fromPlayer, Player toPlayer, int damage) { }
        public virtual void OnPlayerChangeVehiclePlate(Player player, LifeVehicle vehicle, string oldPlate, string newPlate) { }
        public virtual void OnPlayerChangeVehicleColor(Player player, LifeVehicle vehicle, string oldColor, string newColor) { }
    }
}