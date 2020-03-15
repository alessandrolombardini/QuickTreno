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
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace QuickTreno
{
    static class Dettaglio
    {
        /// <summary>
        /// Richiesta codice treno di cui si vuole conosce il dettaglio
        /// </summary>
        /// <param name="e"></param>
        public static async void DettaglioRichiesta(Telegram.Bot.Args.MessageEventArgs e)
        {
            await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "Inserire numero del treno");
            Bot._comandi[e.Message.Chat.Id] = "/dettagliorichiesta";
        }

        /// <summary>
        /// Scheda di dettaglio di un treno
        /// </summary>
        /// <param name="e"></param>
        public static async void _Dettaglio(Telegram.Bot.Args.MessageEventArgs e, Telegram.Bot.Args.CallbackQueryEventArgs _callback = null, string numeroTreno = null)
        {
            try
            {
                // Gestione diversi tipi di richiami del metodo
                long chatID;

                if (_callback == null)
                    chatID = e.Message.Chat.Id;
                else
                    chatID = _callback.CallbackQuery.Message.Chat.Id;


                WebRequest request = WebRequest.Create("http://viaggiatreno.it/vt_pax_internet/mobile/numero");
                request.Method = "POST";


                string _num = string.Empty;
                if (numeroTreno == null)
                    _num = e.Message.Text;
                else
                    _num = numeroTreno;

                string postData = "numeroTreno=" + _num;

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


                // Primo parsing: Ricerco l'indirizzo del treno ricercato
                List<HtmlNode> toftitle = doc.DocumentNode.Descendants().Where
                (x => (x.Name == "div" && x.Attributes["class"] != null &&
                 x.Attributes["class"].Value.Contains("evidenziato"))).ToList();

                string _c = string.Empty;
                int _start = 0;
                int _end = 0;
                try
                {
                    // Gestione stringa indirizzo
                    _c = toftitle[toftitle.Count - 2].InnerHtml;
                    _start = _c.IndexOf("<a href=\"");
                    _end = _c.IndexOf("\">");

                }
                catch
                {

                    DataSet _ds = new DataSet();
                    SQLiteDataAdapter _da = new SQLiteDataAdapter();
                    _da.SelectCommand = new SQLiteCommand("select * from LikedTrain where trainCode = @valore1", LocalDatabase.conn);
                    _da.SelectCommand.Parameters.AddWithValue("@valore1", _num);
                    _da.Fill(_ds);

                    // Non sono memorizzati treni nei preferiti
                    if (_ds.Tables[0].Rows.Count != 0)
                    {
                        await Bot._bot.SendTextMessageAsync(chatID, "❌Dettaglio non disponibile", false, false, 0, Keyboard.OttieniTastieraStatusTrenoSenzaPreferiti());
                        return;
                    }

                    await Bot._bot.SendTextMessageAsync(chatID, "❌Dettaglio non disponibile", false, false, 0, Keyboard.OttieniTastieraStatusTreno());
                    return;

                }

                // + 9 : Caratteri <a href=\"
                string _indirizzo = _c.Substring(_start + 9, _end - _start - 9);

                // Nuova richiesta al sito
                request = WebRequest.Create("http://viaggiatreno.it/" + _indirizzo);
                response = await request.GetResponseAsync();
                dataStream = response.GetResponseStream();
                reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();


                doc = new HtmlDocument();
                doc.LoadHtml(responseFromServer);

                // Codice treno (Formato: [NumeroTreno]
                var _codicetreno = doc.DocumentNode.Descendants("h1").ToList()[0].InnerText;

                // Parsing fermate effettuate
                List<HtmlNode> _nodidettaglio = doc.DocumentNode.Descendants().Where
                (x => (x.Name == "div" && x.Attributes["class"] != null &&
                x.Attributes["class"].Value.Contains("giaeffettuate"))).ToList();

                string _messaggiofinale = "🚇Treno " + _codicetreno + "\n\n";

                if (_nodidettaglio.Count != 0)
                    _messaggiofinale += "🔻Fermate effettuate:\n";

                for (int i = 0; i < _nodidettaglio.Count; i++)
                {
                    string _stazione = _nodidettaglio[i].Descendants("h2").ToList()[0].InnerText;

                    string _partenzaprogrammata = FormatoStringhe.Pulisci(_nodidettaglio[i].Descendants("strong").ToList()[0].InnerText);
                    string _partenzaeffettiva = FormatoStringhe.Pulisci(_nodidettaglio[i].Descendants("strong").ToList()[1].InnerText);

                    _messaggiofinale += " ⚫️Stazione di " + _stazione + "\n";


                    if (_nodidettaglio[i].InnerText.Contains("Partenza programmata:"))
                    {
                        _messaggiofinale += "  🔸Partenza programmata: " + _partenzaprogrammata + "\n";
                        _messaggiofinale += "  🔹Partenza effettiva: " + _partenzaeffettiva + "\n";
                    }
                    else
                    {
                        _messaggiofinale += "  🔸Arrivo programmato: " + _partenzaprogrammata + "\n";
                        _messaggiofinale += "  🔹Arrivo effettivo: " + _partenzaeffettiva + "\n";
                    }



                }

                // Parsing fermate non ancora effettuate

                _nodidettaglio = doc.DocumentNode.Descendants().Where
                (x => (x.Name == "div" && x.Attributes["class"] != null &&
                x.Attributes["class"].Value.Contains("corpocentrale"))).ToList();

                // Aggiunge le prossime fermate solo se presenti
                if (_nodidettaglio.Count != 0)
                {
                    _messaggiofinale += "🔺Prossime fermate:\n";
                    for (int i = 0; i < _nodidettaglio.Count; i++)
                    {
                        string _stazione = _nodidettaglio[i].Descendants("h2").ToList()[0].InnerText;

                        string _partenzaprogrammata = FormatoStringhe.Pulisci(_nodidettaglio[i].Descendants("strong").ToList()[0].InnerText);
                        string _partenzaeprevista = FormatoStringhe.Pulisci(_nodidettaglio[i].Descendants("strong").ToList()[1].InnerText);

                        _messaggiofinale += " ⚪️Stazione di " + _stazione + "\n";

                        _messaggiofinale += "  🔸Arrivo programmato: " + _partenzaprogrammata + "\n";
                        _messaggiofinale += "  🔹Arrivo previsto: " + _partenzaeprevista + "\n";
                    }
                }


                // Invio messaggio
                // await Bot._bot.SendTextMessageAsync(chatID, _messaggiofinale, false, false, 0 , Keyboard.OttieniTastieraStatusTreno());
                // Keyboard.InviaCambioTastiera(chatID);
                DataSet ds = new DataSet();
                SQLiteDataAdapter da = new SQLiteDataAdapter();
                da.SelectCommand = new SQLiteCommand("select * from LikedTrain where trainCode = @valore1", LocalDatabase.conn);
                da.SelectCommand.Parameters.AddWithValue("@valore1", _num);
                da.Fill(ds);

                // Non sono memorizzati treni nei preferiti
                if (ds.Tables[0].Rows.Count != 0)
                {
                    await Bot._bot.SendTextMessageAsync(chatID, _messaggiofinale, false, false, 0, Keyboard.OttieniTastieraStatusTrenoSenzaPreferiti());
                    return;
                }

                await Bot._bot.SendTextMessageAsync(chatID, _messaggiofinale, false, false, 0, Keyboard.OttieniTastieraStatusTreno());

            }
            catch { }

        }
    }
}
