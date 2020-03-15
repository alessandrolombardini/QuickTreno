using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Telegram.Bot;

namespace QuickTreno
{
    static class Bot
    {
        // Bot
        public static Api _bot = new Api(tokenTelegram);

        // Toket 
        private const string tokenTelegram = "353211045:AAG-q8PMMSYeQRouHIM4sHEFDdzpNfEOV9M";

        /// <summary>
        /// Storico comandi
        /// long: Identificativo della chat
        /// string: Comando
        /// </summary>
        public static Dictionary<long, string> _comandi = new Dictionary<long, string>();
    }

}
