using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace QuickTreno
{
    static class Tabellone
    {

        /// <summary>
        /// Richiesta codice treno 
        /// </summary>
        /// <param name="e"></param>
        public static async void TabelloneRichiesta(Telegram.Bot.Args.MessageEventArgs e)
        {
            await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "📌Inserire stazione", replyMarkup: Keyboard.OttieniTastoAnnulla());
            Bot._comandi[e.Message.Chat.Id] = "/tabellonerichiesta";
        }

        /// <summary>
        /// Identificatore della chat e dizionario contenente le possibili stazioni disponibili e relativo codice identifiativo
        /// </summary>
        static Dictionary<long, Dictionary<string, string>> _possibilità = new Dictionary<long, Dictionary<string, string>>();

        /// <summary>
        /// Identificatore della chat e stazione selezionata
        /// </summary>
        static Dictionary<long, string> _stazioneselezionata = new Dictionary<long, string>();

        // Gestire separatamente la multiple possibilità e la singola possibilità per poter inserire Andata e Ritorno

        /// <summary>
        /// Controlla la correttazza del nome della stazione e offre due ipotesi:
        /// Corretto: Rimanda direttamente alla selezione dei treni in arrivo oppure in partenza
        /// Sbagliato: Invia all'utente una serie di opzioni inerenti alla richiesta (Offre delle stazioni con nomi simili)
        /// </summary>
        /// <param name="e"></param>
        public static async void _Tabellone(Telegram.Bot.Args.MessageEventArgs e)
        {

            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create("http://viaggiatreno.it/vt_pax_internet/mobile/stazione");
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
            string postData = "stazione=" + e.Message.Text;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Get the request stream.
            var dataStream = await request.GetRequestStreamAsync();
            // Write the data to the request stream.
            await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            // Get the response.
            var response = await request.GetResponseAsync();
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();


            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(responseFromServer);

            // Il sito non accetta il mio input e mi offre delle soluzioni alternative
            if (doc.DocumentNode.Descendants("h1").ToList()[0].InnerText == "Cerca Treno Per Stazione")
            {
                OffriPossibilita(e, doc);
            }
            // Il sito accetta l'input ed io rimando alla selezione degli arrivi o delle partenze
            else
            {

                string _nomestazionecertificato = doc.DocumentNode.Descendants("h1").ToList()[0].InnerText;
                _stazioneselezionata[e.Message.Chat.Id] = "@" + _nomestazionecertificato.Substring(13);

                var items = new Dictionary<string, string>()
                {
                    {"Arrivi", "/Tabellone_arriviopartenze:arrivi"},
                    {"Partenze", "/Tabellone_arriviopartenze:partenze"},

                };
                items.Add("⛔️Annulla", "/🏠Home");
                var inlineKeyboardMarkup = Keyboard.InlineKeyboardMarkupMaker(items);


                await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "📜Treni nella stazione di " + _stazioneselezionata[e.Message.Chat.Id].Substring(1), replyMarkup: inlineKeyboardMarkup);

                // Ai nomi completi delle stazioni aggiungo una @ in modo da distinguerli dai 'codici stazione'
                //_stazioneselezionata[e.Message.Chat.Id] = "@" +  e.Message.Text;
            }
        }




        // Correzzione input utente
        //**************************************************************************************************************************************************************************
        /// <summary>
        /// Metodo utilizzato se l'input utente risulta sbagliato e il Bot fornisce le possibili stazioni simili all'input
        /// </summary>
        /// <param name="e"></param>
        /// <param name="doc"></param>
        private static async void OffriPossibilita(Telegram.Bot.Args.MessageEventArgs e, HtmlDocument doc)
        {


            List<HtmlNode> _stazioni = doc.DocumentNode.Descendants().Where
            (x => (x.Name == "select" && x.Attributes["name"] != null &&
            x.Attributes["name"].Value.Contains("codiceStazione"))).ToList();

            // Singole stazioni possibili
            List<HtmlNode> _stazionisingole = doc.DocumentNode.Descendants().Where
            (x => (x.Name == "option" && x.Attributes["value"] != null)).ToList();

            if (_stazioni.Count == 0)
            {
                await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "⚠️Stazione non trovata\nInserire nuova stazione:", replyMarkup: Keyboard.OttieniTastoAnnulla());
                Bot._comandi[e.Message.Chat.Id] = "/tabellonerichiesta";
                return;
            }


            // Codice identificativo della stazione richiesto dal sito
            string _codiceStazione = string.Empty;
            // Nome effettivo della stazione
            string _ns = string.Empty;

            var items = new Dictionary<string, string>();
            foreach (HtmlNode _c in _stazionisingole)
            {
                _codiceStazione = _c.Attributes["value"].Value;

                _ns = _codiceStazione.Substring(6, _codiceStazione.Length - 6);

                items.Add(_ns, "/Tabellone_SceltaStazione_Callback:" + _codiceStazione);
            }


            items.Add("⛔️Annulla", "/🏠Home");

            await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "☑️Seleziona la stazione", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
        }


        /// <summary>
        /// Ottiene la stazione selezionata attraverso gli InlineButtons e domanda se si vuole ottene i treni in arrivo o partenza
        /// </summary>
        /// <param name="e"></param>
        /// <param name="_stazione"></param>
        public static async void Risposta_CallBack_SelezioneStazione(Telegram.Bot.Args.CallbackQueryEventArgs e, string _stazione)
        {
            // Ai nomi completi delle stazioni aggiungo una @ in modo da distinguerli dai 'codici stazione'
            _stazioneselezionata[e.CallbackQuery.Message.Chat.Id] = _stazione;

            // Tastiera andata o ritorno
            var items = new Dictionary<string, string>()
                {
                    {"Arrivi", "/Tabellone_arriviopartenze:arrivi"},
                    {"Partenze", "/Tabellone_arriviopartenze:partenze"},

                };

            items.Add("⛔️Annulla", "/🏠Home");
            var inlineKeyboardMarkup = Keyboard.InlineKeyboardMarkupMaker(items);
            try
            {
                if (_stazioneselezionata[e.CallbackQuery.Message.Chat.Id].Substring(6, _stazioneselezionata[e.CallbackQuery.Message.Chat.Id].Length - 6) != "")
                    await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "📜Treni nella stazione di " + _stazioneselezionata[e.CallbackQuery.Message.Chat.Id].Substring(6, _stazioneselezionata[e.CallbackQuery.Message.Chat.Id].Length - 6), replyMarkup: inlineKeyboardMarkup);
                else
                {
                    DataSet ds = new DataSet();
                    SQLiteDataAdapter da = new SQLiteDataAdapter();
                    da.SelectCommand = new SQLiteCommand("select * from Station where stationID = @valore1", LocalDatabase.conn);
                    da.SelectCommand.Parameters.AddWithValue("@valore1", _stazioneselezionata[e.CallbackQuery.Message.Chat.Id]);
                    da.Fill(ds);

                    DataRow _dr = ds.Tables[0].Rows[0];
                    string Nome = _dr["Name"].ToString();
                    await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "📜Treni nella stazione di " + Nome, replyMarkup: inlineKeyboardMarkup);

                }
                //await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "📜Seleziona", replyMarkup: inlineKeyboardMarkup);
            }
            catch { }
        }

        //**************************************************************************************************************************************************************************
        /// <summary>
        /// Identificatore della chat e scelta fra "arrivi o partenze"
        /// </summary>
        static Dictionary<long, string> _modalitàselezionata = new Dictionary<long, string>();


        /// <summary>
        /// Metodo utilizzabile sono con stazione corretta: non prevede aiuto verso l'utente (Implementato attraverso metodi precedenti
        /// </summary>
        /// <param name="e">Parametro di CallBack</param>
        /// <param name="_arriviopartenze">Valori possibili: "arrivi" o "partente" deciso mediante pressione di bottoni di callback da parte dell'utente</param>
        /// <param name="f">Parametro eventuale MessageEventArgs</param>
        public static async void SoluzioneStazione(Telegram.Bot.Args.CallbackQueryEventArgs e = null, string _arriviopartenze = null, Telegram.Bot.Args.MessageEventArgs f = null)
        {

            long _chatID = 0;
            string _messaggiofinale = null;

            // Soluzione: messaggio troppo lungo
            string _messaggiouno = string.Empty;
            string _messaggiodue = string.Empty;

            try
            {

                if (f == null)
                    _chatID = e.CallbackQuery.Message.Chat.Id;
                else
                    _chatID = f.Message.Chat.Id;

                WebRequest request = WebRequest.Create("http://viaggiatreno.it/vt_pax_internet/mobile/stazione");
                request.Method = "POST";

                string postData;
                if (_stazioneselezionata[_chatID][0] == '@')
                    postData = "stazione=" + _stazioneselezionata[_chatID].Remove(0, 1); // Stazione identificata da un nome
                else
                    postData = "codiceStazione=" + _stazioneselezionata[_chatID]; // Stazione identificata da un codice

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


                // Studio tabellone stazione
                List<HtmlNode> toftitle = doc.DocumentNode.Descendants().Where
                    (x => (x.Name == "div" && x.Attributes["class"] != null &&
                     x.Attributes["class"].Value.Contains("bloccorisultato"))).ToList();


                //if (_arriviopartenze == "arrivi") _messaggiocompleto = "🖥Treni in arrivo presso la stazione di " + _stazioneselezionata[e.CallbackQuery.Message.Chat.Id].Substring(6, _stazioneselezionata[e.CallbackQuery.Message.Chat.Id].Length - 6) + "\n";

                //else if (_arriviopartenze == "partenze") _messaggiocompleto = "🖥Treni in partenza dalla stazione di " + _stazioneselezionata[e.CallbackQuery.Message.Chat.Id].Substring(6, _stazioneselezionata[e.CallbackQuery.Message.Chat.Id].Length - 6) + "\n";
                //else
                //{
                //    Keyboard.InviaCambioTastiera(e.CallbackQuery.Message.Chat.Id);
                //    return;
                //}

                // Analizzo il dettaglio di ciascun treno disponibile
                if (toftitle.Count == 0) { await Bot._bot.SendTextMessageAsync(_chatID, "❗️Nessun treno previsto a breve", false, false, 0, RitornaTastieraGiustaTabellone(e.CallbackQuery.Message.Chat.Id)); return; }




                // Comprende se è un aggiornamento oppure una nuova richiesta
                if (_arriviopartenze != null)
                    _modalitàselezionata[e.CallbackQuery.Message.Chat.Id] = _arriviopartenze;
                else
                    _arriviopartenze = _modalitàselezionata[_chatID];

                string _messaggiocompleto = null;


                string _codicetreno = string.Empty;
                for (int i = 0; i < toftitle.Count; i++)
                {
                    _messaggiocompleto = null;

                    // Aggiunta
                    string _messaggiopulito = FormatoStringhe.Pulisci(toftitle[i].InnerText);
                    int _c = _messaggiopulito.IndexOf("Binario Reale:");
                    string _spec = _messaggiopulito.Substring(_c);
                    string[] _sezioni = _messaggiopulito.Substring(_messaggiopulito.IndexOf("Binario Previsto:")).Split(' ');

                    int a = _messaggiopulito.IndexOf("Binario Previsto:") + 18;
                    int b = _messaggiopulito.IndexOf("Binario Reale:");
                    string _binarioprevisto = _messaggiopulito.Substring(a, b - a);
                    string _ritardo = string.Empty;
                    int c;
                    if (_messaggiopulito.Contains("in orario"))
                        c = _messaggiopulito.IndexOf("in orario");
                    else
                    {
                        c = _messaggiopulito.IndexOf("ritardo");
                        _ritardo = _messaggiopulito.Substring(c + 8, _messaggiopulito.IndexOf(" » Vedi scheda ") - c - 8);

                    }
                    string _binarioreale = _messaggiopulito.Substring(b + 14, c - b - 14);



                    // Codice treno
                    _codicetreno = toftitle[i].Descendants("h2").ToList()[0].InnerText;

                    string _per = toftitle[i].Descendants("strong").ToList()[0].InnerText;
                    string _ore = toftitle[i].Descendants("strong").ToList()[1].InnerText;

                    //_messaggiocompleto = string.Empty;


                    // Treno in partenza oppure in arrivo
                    if (toftitle[i].InnerHtml.Contains("<div class=\"bloccotreno\">\r\n\t\t\t\t\tPer") && _arriviopartenze == "partenze")
                    {
                        _messaggiocompleto += "🚉Treno " + _codicetreno;
                        _messaggiocompleto += string.Format("\n   📍Per " + _per);
                        _messaggiocompleto += string.Format("\n   🕓Delle ore {0}\n   ▪️Binario previsto: {1}\n   ▫️Binario reale: {2}", _ore, _binarioprevisto, _binarioreale);
                        if (_messaggiopulito.Contains("in orario"))
                            _messaggiocompleto += "\n   ✅In orario\n\n";
                        else
                            _messaggiocompleto += "\n   🔴In ritardo di " + _ritardo + " minuti\n\n";
                    }
                    else if (toftitle[i].InnerHtml.Contains("<div class=\"bloccotreno\">\r\n\t\t\t\t\tDa") && _arriviopartenze == "arrivi")
                    {
                        _messaggiocompleto += "🚉Treno " + _codicetreno;
                        _messaggiocompleto += string.Format("\n   📍Da " + _per);
                        _messaggiocompleto += string.Format("\n   🕓Delle ore {0}\n   ▪️Binario previsto: {1}\n   ▫️Binario reale: {2}", _ore, _binarioprevisto, _binarioreale);
                        if (_messaggiopulito.Contains("in orario"))
                            _messaggiocompleto += "\n   ✅In orario\n\n";
                        else
                            _messaggiocompleto += "\n   🔴In ritardo di " + _ritardo + " minuti\n\n";
                    }

                    _messaggiofinale += _messaggiocompleto;
                    if (_arriviopartenze == "partenze" && i < toftitle.Count / 4)
                        _messaggiouno += _messaggiocompleto;
                    else if(_arriviopartenze == "partenze" && i > toftitle.Count / 4)
                        _messaggiodue += _messaggiocompleto;
                    else if (_arriviopartenze == "arrivi" && i < toftitle.Count / 4*3)
                        _messaggiouno += _messaggiocompleto;
                    else if (_arriviopartenze == "arrivi" && i > toftitle.Count / 4 * 3)
                        _messaggiodue += _messaggiocompleto;


                }

            }
            catch { }


            try
            {
                await Bot._bot.SendTextMessageAsync(_chatID, _messaggiofinale, false, false, 0, RitornaTastieraGiustaTabellone(_chatID));

            }
            catch (Telegram.Bot.Exceptions.ApiRequestException) //Messaaggio troppo lungo
            { 
                await Bot._bot.SendTextMessageAsync(_chatID, _messaggiouno, false, false, 0, RitornaTastieraGiustaTabellone(_chatID));
                await Bot._bot.SendTextMessageAsync(_chatID, _messaggiodue, false, false, 0, RitornaTastieraGiustaTabellone(_chatID));
            }
            catch { await Bot._bot.SendTextMessageAsync(_chatID, "🤖Qualcosa è andato storto", false, false, 0, Keyboard.OttieniTastieraHome()); }





            // Reimposto la testiera(Menu)
            //Keyboard.InviaCambioTastiera(e.CallbackQuery.Message.Chat.Id);
            //Keyboard.Invia_Tastiera_Tabelloni(_chatID);

        }

        /// <summary>
        /// Metodo per aggiornare lo status della stazione
        /// </summary>
        /// <param name="e"></param>
        public static void Aggiorna(Telegram.Bot.Args.MessageEventArgs e)
        {
            SoluzioneStazione(null, _modalitàselezionata[e.Message.Chat.Id], e);
        }

        public static ReplyKeyboardMarkup RitornaTastieraGiustaTabellone(long _chatID)
        {
            string _stationID = string.Empty;

            if (_stazioneselezionata[_chatID][0] == '@')
            {
                DataSet _ds = new DataSet();
                SQLiteDataAdapter _da = new SQLiteDataAdapter();
                _da.SelectCommand = new SQLiteCommand("select stationID from Station where Name LIKE @valore1", LocalDatabase.conn);
                _da.SelectCommand.Parameters.AddWithValue("@valore1", "%"+_stazioneselezionata[_chatID].Remove(0, 1).ToUpper());
                _da.Fill(_ds);

                _stationID = _ds.Tables[0].Rows[0]["stationID"].ToString();
               
            }
            else
                _stationID = _stazioneselezionata[_chatID].Substring(0, 6);


            DataSet ds = new DataSet();
            SQLiteDataAdapter da = new SQLiteDataAdapter();
            da.SelectCommand = new SQLiteCommand("select * from LikedStation where stationID = @valore1 and chatID = @valore2", LocalDatabase.conn);
            da.SelectCommand.Parameters.AddWithValue("@valore1", _stationID);
            da.SelectCommand.Parameters.AddWithValue("@valore2", _chatID);
            da.Fill(ds);
            
            if (ds.Tables[0].Rows.Count == 0)
                return Keyboard.OttieniTastieraTabellone();
            else
                return Keyboard.OttieniTastieraTabelloneSenzaPreferiti();

        }

        public static void AggiungiStazioneAiPreferiti(Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                if (_stazioneselezionata[e.Message.Chat.Id].Substring(0, 1) == "@")
                    Preferiti.AggiungiStazione(e.Message.Chat.Id, _stazioneselezionata[e.Message.Chat.Id].Substring(1));
                else
                    Preferiti.AggiungiStazione(e.Message.Chat.Id, _stazioneselezionata[e.Message.Chat.Id].Substring(6));
            }
            catch (KeyNotFoundException)
            {
                // Chiave non presente
            }
        }

        public static async void Posizione(Telegram.Bot.Args.MessageEventArgs e)
        {
            long _chatID = e.Message.Chat.Id;
            string _stationID = string.Empty;

            try
            {
                if (_stazioneselezionata[_chatID][0] == '@')
                {
                    DataSet _ds = new DataSet();
                    SQLiteDataAdapter _da = new SQLiteDataAdapter();
                    _da.SelectCommand = new SQLiteCommand("select stationID from Station where Name LIKE @valore1", LocalDatabase.conn);
                    _da.SelectCommand.Parameters.AddWithValue("@valore1", "%"+_stazioneselezionata[_chatID].Remove(0, 1).ToUpper());
                    _da.Fill(_ds);

                    _stationID = _ds.Tables[0].Rows[0]["stationID"].ToString();
                }
                else
                    _stationID = _stazioneselezionata[_chatID].Substring(0, 6);

                DataSet ds = new DataSet();
                SQLiteDataAdapter da = new SQLiteDataAdapter();
                da.SelectCommand = new SQLiteCommand("select * from Station where stationID = @valore1", LocalDatabase.conn);
                da.SelectCommand.Parameters.AddWithValue("@valore1", _stationID);
                da.Fill(ds);
                DataRow dr = ds.Tables[0].Rows[0];
                float longitudine = float.Parse(dr["Longitudine"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                float latitudine = float.Parse(dr["Latitudine"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                await Bot._bot.SendLocationAsync(e.Message.Chat.Id, latitudine, longitudine, replyMarkup: RitornaTastieraGiustaTabellone(e.Message.Chat.Id));

            }
            catch(System.FormatException) { await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "❗️Posizione non disponibile", replyMarkup: Tabellone.RitornaTastieraGiustaTabellone(_chatID)); }
            catch { }

        }
    }
}
