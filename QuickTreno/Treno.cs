using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

using HtmlAgilityPack;

using Telegram.Bot.Types.ReplyMarkups;

using Telegram;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


using System.Diagnostics;

using System.Net;

using System.Xml;
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace QuickTreno
{
    static class Treno
    {
        
        public static async void _Treno(long chat_id)
        {
            await Bot._bot.SendTextMessageAsync(chat_id, "🚝Inserisci il numero del treno", replyMarkup: Keyboard.OttieniTastoAnnulla());

            // Registra il comando "/treno" riferito all'identificativo della chat
            Bot._comandi[chat_id] = "/treno";
        }

        /// <summary>
        /// Metodo funzionante: https://msdn.microsoft.com/it-it/library/debx8sh9(v=vs.110).aspx
        /// Comandi ridotti(Rimosse chiusure,...) a causa di errore
        /// Il metodo sembra restituire tutto correttamente; l'interrogazione avviene tramite la stringa postdata
        /// </summary>
        /// <param name="e"></param>
        /// <param name="_messaggio"></param>
        public static async void CodiceTreno(Telegram.Bot.Args.MessageEventArgs e = null, string _codice = null, Telegram.Bot.Args.CallbackQueryEventArgs f = null)
        {
            WebRequest request = WebRequest.Create("http://viaggiatreno.it/vt_pax_internet/mobile/numero");

            request.Method = "POST";
            string postData = string.Empty;

            string _numerotreno = string.Empty;
            long _chatID;

            //****************************************
            // Gestione dei parametri variabili

            if (_codice == null)
                _numerotreno = e.Message.Text;
            else
                _numerotreno = _codice;

            if (f == null)
                _chatID = e.Message.Chat.Id;
            else
                _chatID = f.CallbackQuery.Message.Chat.Id;

            postData = "numeroTreno=" + _numerotreno;

            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";
            var dataStream = await request.GetRequestStreamAsync();
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            var response = await request.GetResponseAsync();
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();



            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(responseFromServer);

            // Codice treno
            string _treno = doc.DocumentNode.Descendants("h1").ToList()[0].InnerText;

            // Estrazione informazioni su:
            // 0: Stazione di partenza
            // 1: Ultima stazione di fermo
            // 2: Stazione di arrivo (Diventa 1 se il treno non è in movimento)

            List<HtmlNode> _informazioni = doc.DocumentNode.Descendants().Where
                (x => (x.Name == "div" && x.Attributes["class"] != null &&
                x.Attributes["class"].Value.Contains("corpocentrale"))).ToList();

            string _stazionepartenza = string.Empty;
            string _orariopartenza = string.Empty;

            // Stazione di partenza
            try
            {
                _stazionepartenza = _informazioni[0].Descendants("h2").ToList()[0].InnerText;
                _orariopartenza = FormatoStringhe.Pulisci(_informazioni[0].Descendants("strong").ToList()[0].InnerText);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                // Numero treno non valido
                await Bot._bot.SendTextMessageAsync(_chatID, "❌Numero treno non valido!", false, false, 0, Keyboard.OttieniTastieraHome());
                return;
            }

            _ultimotreno[_chatID] = _numerotreno;

            // Stazione di arrivo

            string _stazionearrivo = _informazioni[_informazioni.Count - 1].Descendants("h2").ToList()[0].InnerText;
            string _orarioarrivo = FormatoStringhe.Pulisci(_informazioni[_informazioni.Count - 1].Descendants("strong").ToList()[0].InnerText);

            string _laststazione = string.Empty;
            // Vero: Il treno è in movimento e all'interno del secondo nodo è presente l'ultima fermata effettuata
            if (_informazioni.Count > 2)
            {
                string _ultimastazione = _informazioni[1].Descendants("h2").ToList()[0].InnerText;
                string _ultimoorario = FormatoStringhe.Pulisci(_informazioni[1].Descendants("strong").ToList()[1].InnerText);

                _laststazione = "ℹUltimo fermata effettuata presso " + _ultimastazione + " alle ore " + _ultimoorario + "\n";
            }

            // Status treno
            _informazioni = doc.DocumentNode.Descendants().Where
                (x => (x.Name == "div" && x.Attributes["class"] != null &&
                x.Attributes["class"].Value.Contains("evidenziato"))).ToList();

            // Separazione stringa status
            string _statusglobale = FormatoStringhe.RimuoviCommento(FormatoStringhe.Pulisci(_informazioni[_informazioni.Count - 1].Descendants("strong").ToList()[0].InnerText));
            _statusglobale = WebUtility.HtmlDecode(_statusglobale);
            string _statusritardo = string.Empty;
            string _statusultimorilevamento = string.Empty;

            string _messaggiofinale;
            if (_statusglobale.Contains("Ultimo"))
            {
                int _index = _statusglobale.IndexOf("Ultimo");
                _statusritardo = _statusglobale.Substring(0, _index);
                _statusultimorilevamento = _statusglobale.Substring(_index);

                _messaggiofinale = string.Format("🚂Treno {0}\n\n🔸Partenza da {1} ore {2}\n🔹Arrivo a {3} ore {4}\n" + _laststazione + "⚠️{5}\n🚩{6}", _treno, _stazionepartenza, _orariopartenza, _stazionearrivo, _orarioarrivo, _statusritardo, _statusultimorilevamento);
            }
            else
            {

                // Creo il messaggio finale
                _messaggiofinale = string.Format("🚂Treno {0}\n\n🔸Partenza da {1} ore {2}\n🔹Arrivo a {3} ore {4}\n" + _laststazione + "⚠️{5}", _treno, _stazionepartenza, _orariopartenza, _stazionearrivo, _orarioarrivo, _statusglobale);
            }
            
            /*
            //_messaggiocompleto += string.Format("Treno {0}\nPer {1}\nDelle ore {2}\n", _codicetreno, _per, _ore);
            var items = new Dictionary<string, string>()
                {
                    // To do:
                    //{"Aggiorna", ""},
                    //{"Aggiungi ai preferiti", ""},
                    
                    {"Dettaglio", "/Dettaglio_codicetreno:"+_numerotreno},

                };

            var inlineKeyboardMarkup = Keyboard.InlineKeyboardMarkupMaker(items);*/
            
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
            
            var items = new Dictionary<string, string>();

            DataSet ds = new DataSet();
            SQLiteDataAdapter da = new SQLiteDataAdapter();
            da.SelectCommand = new SQLiteCommand("select * from LikedTrain where trainCode = @valore1 and chatID = @valore2", LocalDatabase.conn);
            da.SelectCommand.Parameters.AddWithValue("@valore1", _numerotreno);
            da.SelectCommand.Parameters.AddWithValue("@valore2", _chatID);
            da.Fill(ds);
            
            // Non sono memorizzati treni nei preferiti
            if (ds.Tables[0].Rows.Count != 0)
            {
                await Bot._bot.SendTextMessageAsync(_chatID, _messaggiofinale, false, false, 0, Keyboard.OttieniTastieraStatusTrenoSenzaPreferiti());
                return;
            }

            await Bot._bot.SendTextMessageAsync(_chatID, _messaggiofinale, false, false, 0, Keyboard.OttieniTastieraStatusTreno());
            // Keyboard.Invia_Tastiera_StatusTreno(_chatID);
        }

        /// <summary>
        /// Ultimo treno cercato
        /// </summary>
        static Dictionary<long, string> _ultimotreno = new Dictionary<long, string>();

        public static void Aggiorna(Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                CodiceTreno(e, _ultimotreno[e.Message.Chat.Id]);
            }
            catch { }
        }

        public static void TrovaDettaglio(Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                Dettaglio._Dettaglio(e, null, _ultimotreno[e.Message.Chat.Id]);
            }
            catch { }
        }



        public static void AggiungiTrenoAiPreferiti(Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                Preferiti.AggiungiTreno(e.Message.Chat.Id, _ultimotreno[e.Message.Chat.Id]);
            }
            catch { }
        }

        
    }
}
