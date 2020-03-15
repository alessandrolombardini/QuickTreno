using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;
using System.Net;
using System.Xml;
using Telegram.Bot.Types.ReplyMarkups;

namespace QuickTreno
{
    static class Keyboard
    {
        public static InlineKeyboardMarkup InlineKeyboardMarkupMaker(Dictionary<string, string> items)
        {
            InlineKeyboardButton[][] ik = items.Select(item => new[]
            {
                new InlineKeyboardButton(item.Key, item.Value)
            }).ToArray();
            return new InlineKeyboardMarkup(ik);


        }

        /// <summary>
        /// Ritorna il setup del bottone 'Torna Indietro'
        /// </summary>
        /// <returns></returns>
        public static InlineKeyboardMarkup OttieniTastoAnnulla()
        {
            var items = new Dictionary<string, string>()
                {
                    {"⛔️Annulla", "/🏠Home"},

                };

            return Keyboard.InlineKeyboardMarkupMaker(items);
        }

        public static ReplyKeyboardMarkup OttieniTastieraTabellone()
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("⭐️Aggiungi stazione ai preferiti")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🔄Aggiorna"),
                        new KeyboardButton("↩️Cambia stazione")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("📍Posizione"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🏠Home"),
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;
            return rmu;
        }
        public static ReplyKeyboardMarkup OttieniTastieraTabelloneSenzaPreferiti()
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🔄Aggiorna"),
                        new KeyboardButton("↩️Cambia stazione")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("📍Posizione"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🏠Home"),
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;
            return rmu;
        }

        public static ReplyKeyboardMarkup OttieniTastieraHome()
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🚂Status treno"),
                        new KeyboardButton("🔔Tabellone stazione")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🔎Trova treno"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("⭐️Treni preferiti"),
                        new KeyboardButton("📦Altro"),
                        new KeyboardButton("⭐️Stazioni preferite")
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;
            return rmu;
        }

        public static ReplyKeyboardMarkup OttieniTastieraAltro()
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🗒Modifica preferiti"),
                        new KeyboardButton("❓Aiuto")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("ℹ️Informazioni"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🏠Home"),
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;
            return rmu;
        }

        public static ReplyKeyboardMarkup OttieniTastieraStatusTreno()
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("⭐️Aggiungi treno ai preferiti")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🔄Aggiorna status"),
                        new KeyboardButton("▶️Dettaglio"),
                        new KeyboardButton("↩️Cambia treno")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🏠Home"),
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;
            return rmu;
        }
        public static ReplyKeyboardMarkup OttieniTastieraStatusTrenoSenzaPreferiti()
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🔄Aggiorna status"),
                        new KeyboardButton("▶️Dettaglio"),
                        new KeyboardButton("↩️Cambia treno")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🏠Home"),
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;
            return rmu;
        }

        // ***************************************************************

        public async static void Invia_Tastiera_StatusTreno(long chat_id)
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("⭐️Aggiungi treno ai preferiti")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🔄Aggiorna status"),
                        new KeyboardButton("▶️Dettaglio"),
                        new KeyboardButton("↩️Cambia treno")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🏠Home"),
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;

            await Bot._bot.SendTextMessageAsync(chat_id, "Descrizione", false, false, 0, rmu);

        }

        public async static void Invia_Tastiera_Altro(long chat_id)
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🗒Modifica preferiti"),
                        new KeyboardButton("❓Aiuto")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("ℹ️Informazioni"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🏠Home"),
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;

            await Bot._bot.SendTextMessageAsync(chat_id, "Cosa posso fare per te?\n 🗒Rimuovere i preferiti\n ❓Chiedere supporto\n ℹ️Scoprire di più su QuickBot", false, false, 0, rmu);

        }

        public async static void InviaCambioTastiera(long chat_id)
        {
            var rmu = new ReplyKeyboardMarkup();
            rmu.Keyboard =
                new KeyboardButton[][]
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🚂Status treno"),
                        new KeyboardButton("🔔Tabellone stazione")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("🔎Trova treno"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("⭐️Treni preferiti"),
                        new KeyboardButton("📦Altro"),
                        new KeyboardButton("⭐️Stazioni preferite")
                    }
                };
            // Viene elminata non appena viene utilizzata
            rmu.OneTimeKeyboard = true;

            //Ridimensiono la keyboard
            rmu.ResizeKeyboard = true;

            await Bot._bot.SendTextMessageAsync(chat_id, "Desideri essere informarto sui treni?\n 🚂 Ottieni lo stato attuale di un treno\n 🔔 Ottieni il quadro orario di una stazione \n 🔎 Cerca un itinerario \n ⭐️ Controlla i tuoi treni e le tue stazioni preferite \n 📦 Altro", false, false, 0, rmu);

        }



    }
}
