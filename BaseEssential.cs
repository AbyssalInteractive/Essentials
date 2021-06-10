using Life.Network;

namespace Essentials
{
    public abstract class BaseEssential
    {
        internal EssentialsPlugin essentials;
        internal LifeServer server;

        public virtual void Init(EssentialsPlugin essentials, LifeServer server)
        {
            this.server = server;
            this.essentials = essentials;
        }
    }
}