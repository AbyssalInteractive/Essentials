using Life.Network;

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
        }
    }
}