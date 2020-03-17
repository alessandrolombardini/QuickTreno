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

namespace QuickTreno
{
    static class TrovaTreno
    {
        /// <summary>
        /// Identificatore della chat e stazione di partenza inserita
        /// </summary>
        static Dictionary<long, string> _stazionepartenza = new Dictionary<long, string>();

        /// <summary>
        /// Identificatore della chat e stazione di arrivo inserita
        /// </summary>
        static Dictionary<long, string> _stazionearrivo = new Dictionary<long, string>();

        /// <summary>
        /// Identificatore della chat e stazione di arrivo inserita
        /// </summary>
        static Dictionary<long, string> _data = new Dictionary<long, string>();

        /// <summary>
        /// Identificatore della chat e stazione di arrivo inserita
        /// </summary>
        static Dictionary<long, string> _fasciaoraria = new Dictionary<long, string>();



        /// <summary>
        /// Treni disponibili per una tratta in un lasso di tempo breve
        /// Richiesta della stazione di partenza
        /// </summary>
        /// <param name="e"></param>
        public static async void StartTrova(Telegram.Bot.Args.MessageEventArgs e, string _c = null)
        {
            await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "📌Inserisci stazione di partenza", replyMarkup: Keyboard.OttieniTastoAnnulla());

            // Registra il comando "/treno" riferito all'identificativo della chat
            Bot._comandi[e.Message.Chat.Id] = "/trova_partenza";
        }
        //*************************************************************************************************************************************************************************************************
        /// <summary>
        /// Identificatore della chat e dizionario contenente le possibili stazioni disponibili e relativo codice identifiativo
        /// </summary>
        static Dictionary<long, Dictionary<string, string>> _possibilità = new Dictionary<long, Dictionary<string, string>>();

        /// <summary>
        /// Identificatore della chat e stazione di partenza selezionata
        /// </summary>
        static Dictionary<long, string> _stazioneselezionatapartenza = new Dictionary<long, string>();

        /// <summary>
        /// Identificatore della chat e stazione di arrivo selezionata
        /// </summary>
        static Dictionary<long, string> _stazioneselezionataarrivo = new Dictionary<long, string>();


        //**************************************************************************************************************************************************************************************************

        /// <summary>
        /// Treni disponibili per una tratta in un lasso di tempo breve
        /// Richiesta della stazione di partenza
        /// </summary>
        /// <param name="e"></param>
        public static async void TrovaPartenza(Telegram.Bot.Args.MessageEventArgs e)
        {
            // Verifico la correttezza della stazione
            WebRequest request = WebRequest.Create("http://viaggiatreno.it/vt_pax_internet/mobile/stazione");
            request.Method = "POST";
            string postData = "stazione=" + e.Message.Text;
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

            // Il sito non accetta il mio input e mi offre delle soluzioni alternative
            if (doc.DocumentNode.Descendants("h1").ToList()[0].InnerText == "Cerca Treno Per Stazione")
            {
                OffriPossibilita(e, doc, "partenza");
            }
            // Il sito accetta l'input ed io rimando alla selezione degli arrivi o delle partenze
            else
            {
                // Ai nomi completi delle stazioni aggiungo una @ in modo da distinguerli dai 'codici stazione'
                _stazioneselezionatapartenza[e.Message.Chat.Id] = e.Message.Text;

                await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "📌Inserisci stazione di arrivo", replyMarkup: Keyboard.OttieniTastoAnnulla());

                // Registra il comando "/treno" riferito all'identificativo della chat
                Bot._comandi[e.Message.Chat.Id] = "/trova_arrivo";
            }




        }

        public static async void Risposta_CallBack_Partenza(Telegram.Bot.Args.CallbackQueryEventArgs e, string _stazione)
        {
            // Ai nomi completi delle stazioni aggiungo una @ in modo da distinguerli dai 'codici stazione'
            _stazioneselezionatapartenza[e.CallbackQuery.Message.Chat.Id] = _stazione;

            await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "📌Inserisci stazione di arrivo", replyMarkup: Keyboard.OttieniTastoAnnulla());

            // Registra il comando "/treno" riferito all'identificativo della chat
            Bot._comandi[e.CallbackQuery.Message.Chat.Id] = "/trova_arrivo";
        }
        public static async void Risposta_CallBack_Arrivo(Telegram.Bot.Args.CallbackQueryEventArgs e, string _stazione)
        {
            // Memorizza la stazione di partenza inserita dall'utente
            _stazioneselezionataarrivo[e.CallbackQuery.Message.Chat.Id] = _stazione;

            var items = new Dictionary<string, string>()
                {
                    {"🕓Oggi", "/TrovaTreno_Data_CallBackQuery:Oggi"},
                    {"⛔️Annulla", "/🏠Home"}
                };

            await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "📅Inserisci il giorno:\n\nFormato: gg/mm/aaaa\nDigitare _Oggi_ per inserire la data odierna", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items), parseMode: ParseMode.Markdown);

            // Registra il comando "/treno" riferito all'identificativo della chat
            Bot._comandi[e.CallbackQuery.Message.Chat.Id] = "/trova_data";
        }

        public static async void TrovaArrivo(Telegram.Bot.Args.MessageEventArgs e)
        {

            // Verifico la correttezza della stazione
            WebRequest request = WebRequest.Create("http://viaggiatreno.it/vt_pax_internet/mobile/stazione");
            request.Method = "POST";
            string postData = "stazione=" + e.Message.Text;
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

            // Il sito non accetta il mio input e mi offre delle soluzioni alternative
            if (doc.DocumentNode.Descendants("h1").ToList()[0].InnerText == "Cerca Treno Per Stazione")
            {
                OffriPossibilita(e, doc, "arrivo");
            }
            // Il sito accetta l'input ed io rimando alla selezione degli arrivi o delle partenze
            else
            {
                // Memorizza la stazione di partenza inserita dall'utente
                _stazioneselezionataarrivo[e.Message.Chat.Id] = e.Message.Text;

                var items = new Dictionary<string, string>()
                {
                    {"🕓Oggi", "/TrovaTreno_Data_CallBackQuery:Oggi"},
                    {"⛔️Annulla", "/🏠Home"}
                };

                await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "📅Inserisci il giorno:\n\nFormato: gg/mm/aaaa\nDigitare _Oggi_ per inserire la data odierna", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items), parseMode: ParseMode.Markdown);

                // Registra il comando "/treno" riferito all'identificativo della chat
                Bot._comandi[e.Message.Chat.Id] = "/trova_data";
            }

        }

        //*****************************************************************************************************************************************************
        /// <summary>
        /// Offre le stazioni possibili in seguito ad un input sbagliato
        /// </summary>
        /// <param name="e">Identificativo</param>
        /// <param name="doc">Documento che afferma l'errore dell'input e offre le soluzioni</param>
        /// <param name="_partenzaoarrivo">Indica se il metodo è stato richiamato dal metodo di richiesta della stazione di partenza o di arrivo (Valori possibili: "partenza" o "arrivo")</param>
        private static async void OffriPossibilita(Telegram.Bot.Args.MessageEventArgs e, HtmlDocument doc, string _partenzaoarrivo)
        {

            List<HtmlNode> _stazioni = doc.DocumentNode.Descendants().Where
            (x => (x.Name == "select" && x.Attributes["name"] != null &&
            x.Attributes["name"].Value.Contains("codiceStazione"))).ToList();

            // Singole stazioni possibili
            List<HtmlNode> _stazionisingole = doc.DocumentNode.Descendants().Where
            (x => (x.Name == "option" && x.Attributes["value"] != null)).ToList();

            // Non esistono soluzioni possibili
            if (_stazioni.Count == 0)
            {
                if (_partenzaoarrivo == "partenza")
                {
                    await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "🚫Reinserire stazione di partenza", replyMarkup: Keyboard.OttieniTastoAnnulla());
                    Bot._comandi[e.Message.Chat.Id] = "/trova_partenza";
                }
                else if (_partenzaoarrivo == "arrivo")
                {
                    await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "🚫Reiserire stazione di arrivo", replyMarkup: Keyboard.OttieniTastoAnnulla());
                    Bot._comandi[e.Message.Chat.Id] = "/trova_arrivo";
                }

                return;
            }


            string _messaggiopossibilità = string.Empty;

            Dictionary<string, string> _poss = new Dictionary<string, string>();
            // Codice identificativo della stazione richiesto dal sito
            string _codiceStazione = string.Empty;
            // Nome effettivo della stazione
            string _ns = string.Empty;

            var items = new Dictionary<string, string>();
            foreach (HtmlNode _c in _stazionisingole)
            {
                _codiceStazione = _c.Attributes["value"].Value;
                _ns = _codiceStazione.Substring(6, _codiceStazione.Length - 6);

                if (_partenzaoarrivo == "partenza")
                    items.Add(_ns, "/TrovaTreno_SceltaPartenza_Callback:" + _ns);
                else
                    items.Add(_ns, "/TrovaTreno_SceltaArrivo_Callback:" + _ns);
            }


            items.Add("⛔️Annulla", "/🏠Home");
            await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "☑️Seleziona la stazione", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
            return;
        }

        //*************************************************************************************************************************************************************************************
        private const string _messaggiofasciaoraria = "/f1 prima delle 6:00\n/f2 dalle 6:00 alle 13:00\n/f3 dalle 13:00 alle 18:00\n/f4 dalle 18:00 alle 22:00\n/f5 dopo le 22:00";
        /// <summary>
        /// Treni disponibili per una tratta in un lasso di tempo breve
        /// Richiesta della stazione di partenza
        /// </summary>
        /// <param name="e"></param>
        //public static async void TrovaData(Telegram.Bot.Args.MessageEventArgs e)
        //{
        //    // Memorizza la stazione di partenza inserita dall'utente
        //    _data[e.Message.Chat.Id] = e.Message.Text;

        //    await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "Quale fascia oraria:\n" + _messaggiofasciaoraria, replyMarkup: Keyboard.OttieniTastieraAltro());

        //    // Registra il comando "/treno" riferito all'identificativo della chat
        //    Bot._comandi[e.Message.Chat.Id] = "/trova_fasciaoraria";
        //}

        /// <summary>
        /// Treni disponibili per una tratta in un lasso di tempo breve
        /// Richiesta della stazione di partenza
        /// </summary>
        /// <param name="e"></param>
        public static async void TrovaData(Telegram.Bot.Args.MessageEventArgs e)
        {
            // Memorizza la stazione di partenza inserita dall'utente
            _data[e.Message.Chat.Id] = e.Message.Text;

            //await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "Quale fascia oraria:\n");

            // Registra il comando "/treno" riferito all'identificativo della chat
            Bot._comandi[e.Message.Chat.Id] = "/trova_fasciaoraria";


            var items = new Dictionary<string, string>()
                {
                    {"Prima delle 6:00", "/TrovaTreno_FasciaOraria:f1"},
                    {"Dalle 6:00 alle 13:00", "/TrovaTreno_FasciaOraria:f2"},
                    {"Dalle 13:00 alle 18:00", "/TrovaTreno_FasciaOraria:f3"},
                    {"Dalle 18:00 alle 22:00", "/TrovaTreno_FasciaOraria:f4"},
                    {"Dopo le 22:00", "/TrovaTreno_FasciaOraria:f5"},
                    {"⛔️Annulla", "/🏠Home"}
                };

            await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, "☑️Seleziona la fascia oraria", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
        }
        /// <summary>
        /// Alternativa al metodo 'TrovaData': utilizzato attraverso una CallBack ( Stabilisce come data quella attuale)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="_datadioggi"></param>
        //public static async void Data_Oggi(Telegram.Bot.Args.CallbackQueryEventArgs e)
        //{
        //    DateTime c = System.DateTime.Today;
        //    // Memorizza la stazione di partenza inserita dall'utente
        //    _data[e.CallbackQuery.Message.Chat.Id] = c.Day + "/" + c.Month + "/" + c.Year;

        //    await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Quale fascia oraria:\n" + _messaggiofasciaoraria, replyMarkup: Keyboard.OttieniTastieraAltro());

        //    // Registra il comando "/treno" riferito all'identificativo della chat
        //    Bot._comandi[e.CallbackQuery.Message.Chat.Id] = "/trova_fasciaoraria";
        //}
        public static async void Data_Oggi(Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            DateTime c = System.DateTime.Today;
            // Memorizza la stazione di partenza inserita dall'utente
            _data[e.CallbackQuery.Message.Chat.Id] = c.Day + "/" + c.Month + "/" + c.Year;

            // Registra il comando "/treno" riferito all'identificativo della chat
            Bot._comandi[e.CallbackQuery.Message.Chat.Id] = "/trova_fasciaoraria";

            var items = new Dictionary<string, string>()
                {
                    {"Prima delle 6:00", "/TrovaTreno_FasciaOraria:f0"},
                    {"Dalle 6:00 alle 13:00", "/TrovaTreno_FasciaOraria:f1"},
                    {"Dalle 13:00 alle 18:00", "/TrovaTreno_FasciaOraria:f2"},
                    {"Dalle 18:00 alle 22:00", "/TrovaTreno_FasciaOraria:f3"},
                    {"Dopo le 22:00", "/TrovaTreno_FasciaOraria:f4"},
                    {"⛔️Annulla", "/🏠Home"}
                };

            await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "☑️Seleziona la fascia oraria", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
        }

        private static Dictionary<string, string> _value = new Dictionary<string, string>()
        {
            ["f0"] = "1",
            ["f1"] = "2",
            ["f2"] = "3",
            ["f3"] = "4",
            ["f4"] = "5"

        };

        public static void TrovaFasciaOraria(Telegram.Bot.Args.CallbackQueryEventArgs e, string fascia)
        {
            // Memorizza la stazione di partenza inserita dall'utente
            _fasciaoraria[e.CallbackQuery.Message.Chat.Id] = _value[fascia];

            _Trova(e);
        }

        private static string[] FormatoData(string _dat)
        {
            string[] _datadivisa = new string[3];
            int _a = 0;
            for (int i = 0; i < _dat.Length && _a != 2; i++)
            {
                if (_dat[i] == ' ' || _dat[i] == '/' || _dat[i] == '-' || _dat[i] == '\\')
                {
                    _datadivisa[_a] = _dat.Substring(0, i);
                    _dat = _dat.Remove(0, i + 1);
                    _a++;
                    i = 0;
                }
            }
            // Ultima aggiunta: anno(_a = 2)
            _datadivisa[2] = _dat;

            return _datadivisa;
        }
        public static async void _Trova(Telegram.Bot.Args.CallbackQueryEventArgs e)
        {

            WebRequest request = WebRequest.Create("http://viaggiatreno.it/vt_pax_internet/mobile/programmato");

            request.Method = "POST";


            string postData = string.Empty;

            postData += "partenza=" + _stazioneselezionatapartenza[e.CallbackQuery.Message.Chat.Id];
            postData += "&arrivo=" + _stazioneselezionataarrivo[e.CallbackQuery.Message.Chat.Id];

            string[] _datadivisa = FormatoData(_data[e.CallbackQuery.Message.Chat.Id]);
            postData += "&giorno=" + _datadivisa[0];
            postData += "&mese=" + _datadivisa[1];
            postData += "&anno=" + _datadivisa[2];
            postData += "&fascia=" + _fasciaoraria[e.CallbackQuery.Message.Chat.Id];

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


            // Stazione di partenza contenuta nel doc
            string _spartenza = doc.DocumentNode.Descendants("strong").ToList()[0].InnerText;
            // Stazione di arrivo contenuta nel doc
            string _sarrivo = doc.DocumentNode.Descendants("strong").ToList()[1].InnerText;

            // Studio tabellone stazione
            List<HtmlNode> toftitle = doc.DocumentNode.Descendants().Where
            (x => (x.Name == "div" && x.Attributes["class"] != null &&
             x.Attributes["class"].Value.Contains("bloccorisultato"))).ToList();

            if (toftitle[0].InnerText == "Non e' stata trovata nessuna soluzione di viaggioin base alla ricerca effettuata")
            {
                await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "⚠️Non e' stata trovata nessuna soluzione di viaggio in base alla ricerca effettuata", replyMarkup: Keyboard.OttieniTastieraHome());

                return;
            }
            //string _messaggiocompleto = null;

            //await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "🔘Treni da " + _spartenza + " a " + _sarrivo +"\n");
            string _messaggiocompleto = string.Empty;

            // Analizzo il dettaglio di ciascun treno disponibile
            for (int i = 0; i < toftitle.Count; i++)
            {
                int _start;
                // Orario partenza
                _start = toftitle[i].InnerHtml.IndexOf("</strong> ");
                string _da = toftitle[i].InnerHtml.Substring(_start + 10, 5);
                // Orario arrivo
                _start = toftitle[i].InnerHtml.IndexOf("</strong> ", _start + 10);
                string _a = toftitle[i].InnerHtml.Substring(_start + 10, 5);

               
                // Link di dettaglio
                // Esempio: /vt_pax_internet/mobile/programmato?dettaglio=0&sessionId=1494941553678&lang=IT
                string linkHref = toftitle[i].Descendants("a").ToList()[0].GetAttributeValue("href", null);

                // Comando di CallBack
                // Esempio(con Substring): dettaglio=0&sessionId=1494941553678&lang=IT
                string _callback = "/linkdettaglio:" + linkHref.Substring(36);


                // Bottone di dettaglio
                var items = new Dictionary<string, string>()
                {
                    {
                        string.Format
                        ("🔸Partenza ore {1}\n🔹Arrivo ore {3}\n\n", _spartenza, _da, _sarrivo, _a)
                        , _callback
                    }
                };


                var inlineKeyboardMarkup = Keyboard.InlineKeyboardMarkupMaker(items);


                //_messaggiocompleto += string.Format("🚆"+ i+1+ "° soluzione\n🔸Partenza da {0} ore {1}\n🔹Arrivo a {2} ore {3}\n\n", _stazionepartenza[e.Message.Chat.Id], _da, _stazionearrivo[e.Message.Chat.Id],_a);
                //await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, string.Format("🚆" + (i + 1) + "° soluzione"), replyMarkup: inlineKeyboardMarkup);
                

                _messaggiocompleto += string.Format("🚆" + (i + 1) + "° soluzione\n 🔸Partenza da {0} ore {1}\n 🔹Arrivo a {2} ore {3}\n\n", _stazioneselezionatapartenza[e.CallbackQuery.Message.Chat.Id], _da, _stazioneselezionataarrivo[e.CallbackQuery.Message.Chat.Id], _a);


            }

            await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, _messaggiocompleto, replyMarkup: Keyboard.OttieniTastieraHome());
            //await Bot._bot.SendTextMessageAsync(e.Message.Chat.Id, _messaggiocompleto);
            //Bot._comandi[e.Message.Chat.Id] = "/vuoto";
        }


        //****************************************************************************************************************************
        // Non funzionante in quanto è richiesta una sessione. Ritentiamo!

        /// <summary>
        /// Trova codice treno di cui è stato richiesto il dettaglio fra le tante soluzioni offerte
        /// </summary>
        /// <param name="f">Parametro identificativo di CallBack</param>
        /// <param name="_link">Link di dettaglio: Non completo (Guardare il main)</param>

        // Link completo: /vt_pax_internet/mobile/programmato?dettaglio=0&sessionId=1494941553678&lang=IT
        // Link incompleto: dettaglio=0&sessionId=1494941553678&lang=IT 



        public static async void TrovaCodice(Telegram.Bot.Args.CallbackQueryEventArgs f, string _link)
        {
            try
            {
                //var client = new WebClient();
                //var content = client.DownloadString("http://viaggiatreno.it/vt_pax_internet/mobile/programmato");

                HtmlDocument doc = new HtmlDocument();
                string responsebody;
                WebClient client = new WebClient();

                var reqparm = new System.Collections.Specialized.NameValueCollection();
                reqparm.Add("partenza", _stazioneselezionatapartenza[f.CallbackQuery.Message.Chat.Id]);
                reqparm.Add("arrivo", _stazioneselezionataarrivo[f.CallbackQuery.Message.Chat.Id]);
                reqparm.Add("giorno", "13");
                reqparm.Add("mese", "07");
                reqparm.Add("anno", "2017");
                reqparm.Add("fascia", "3");
                byte[] responsebytes = client.UploadValues("http://viaggiatreno.it/vt_pax_internet/mobile/programmato", "POST", reqparm);
                responsebody = Encoding.UTF8.GetString(responsebytes);

                doc.LoadHtml(responsebody);



                // Studio tabellone stazione
                List<HtmlNode> toftitle = doc.DocumentNode.Descendants().Where
                (x => (x.Name == "div" && x.Attributes["class"] != null &&
                 x.Attributes["class"].Value.Contains("bloccorisultato"))).ToList();

                // Link di dettaglio
                // Esempio: /vt_pax_internet/mobile/programmato?dettaglio=0&sessionId=1494941553678&lang=IT
                string linkHref = "//" + toftitle[1].Descendants("a").ToList()[0].GetAttributeValue("href", null);

                // Comando di CallBack
                // Esempio(con Substring): dettaglio=0&sessionId=1494941553678&lang=IT
                string _callback = "/linkdettaglio:" + linkHref.Substring(36);
                //***********************************************************************************************************************************************************************
                // Errore 
                // Errore
                // Errore
                responsebody = client.DownloadString("http://viaggiatreno.it" + linkHref);
                // Errore 
                // Errore
                // Errore
                //**********************************************************************************************************************************************************************
            }
            catch { await Bot._bot.SendTextMessageAsync(f.CallbackQuery.Message.Chat.Id,"Non riesco a richiamare la risorsa dal server", replyMarkup: Keyboard.OttieniTastieraHome()); }
            /*
            WebRequest request = WebRequest.Create("http://viaggiatreno.it/vt_pax_internet/mobile/programmato")
            request.Method = "POST";
            string postData = _link;


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
            // string _treno = doc.DocumentNode.Descendants("h1").ToList()[0].InnerText;

    */
        }
    }
}
