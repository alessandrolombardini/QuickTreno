using System;
using System.Collections.Generic;
using System.Text;


using System.Data.SQLite;
using System.IO;
using System.Data;
using Telegram.Bot.Types.ReplyMarkups;

namespace QuickTreno
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Avvio la ricezione del bot
                Bot._bot.StartReceiving();
                Console.WriteLine("Avvio bot: OK");


                // Creo un evento ad Interrupt legato alla ricezione dei messaggi da parte del Bot
                Bot._bot.OnMessage += _bot_OnMessage; ;

                // Creo un evento legato alle ricezione di CallbackQuery
                Bot._bot.OnCallbackQuery += _bot_OnCallbackQuery; ;

                // Creo DB
                try { LocalDatabase.CreateDatabase(); } catch { }
                LocalDatabase.OpenConnection();
                Console.WriteLine("Connessione DB: OK");


                Console.WriteLine("\nServer in attesa...");


                // Questo while non mi piace ma non sapevo come fare a non far chiudere la console
                while (true) { }
            }
            catch { Console.WriteLine("Errore server..."); }
        }
        
        /// <summary>
        /// Avvio del Bot da parte di un utente
        /// </summary>
        /// <param name="e"></param>
        private static void Start(Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                // Inserisco l'untente all'interno del DB
                SQLiteCommand _command = new SQLiteCommand(LocalDatabase.conn);
                _command.CommandText = "INSERT INTO User (chatID, FirstName, LastName, Username) VALUES(@valore1,@valore2,@valore3,@valore4)";
                _command.CommandType = CommandType.Text;
                _command.Parameters.Add(new SQLiteParameter("@Valore1", e.Message.Chat.Id));
                _command.Parameters.Add(new SQLiteParameter("@Valore2", e.Message.From.FirstName));
                _command.Parameters.Add(new SQLiteParameter("@Valore3", e.Message.From.LastName));
                _command.Parameters.Add(new SQLiteParameter("@Valore4", e.Message.From.Username));
                _command.ExecuteNonQuery();
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n🔥Nuovo utente\nNome: {0}\nCognome: {1}\nUsername: {2}\nID: {3}\n", e.Message.From.FirstName, e.Message.From.LastName, e.Message.From.Username, e.Message.Chat.Id);
                Console.ResetColor();

                MessaggioBackup(string.Format("\n🔥Nuovo utente:\nNome: {0}\nCognome: {1}\nUsername: {2}\nID: {3}\n", e.Message.From.FirstName, e.Message.From.LastName, e.Message.From.Username, e.Message.Chat.Id));
            }
            catch (System.Data.SQLite.SQLiteException) { Console.WriteLine("Utente già registrato."); }

            Keyboard.InviaCambioTastiera(e.Message.Chat.Id);
        }

        /// <summary>
        /// Metodo ad interrupt richiamato in seguito alla ricezione di un messaggio: 
        /// Controlla se il messaggio contiene un comando riconosciuto
        /// In caso contrario l'incarico passa ad un secondo metodo 'FreeMessage()'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            {
                Console.WriteLine("Utente: {0}\nID: {1}\nMessaggio: {2}\n",e.Message.Chat.FirstName+" "+e.Message.Chat.LastName,e.Message.Chat.Id, e.Message.Text);

                switch (e.Message.Text)
                {
                    // Comandi supportati ma non offerti graficamente
                    case "/start":
                        Start(e);
                        break;

                    case "/help":
                        Help(e);
                        break;

                    case "/treno":
                        Treno._Treno(e.Message.Chat.Id);
                        break;

                    case "/dettaglio":
                        Dettaglio.DettaglioRichiesta(e);
                        break;

                    case "/tabellone":
                        Tabellone.TabelloneRichiesta(e);
                        break;

                    case "/trova":
                        TrovaTreno.StartTrova(e);
                        break;
                    /*
                case "/tratta":
                    Tratta.StartTratta(e);
                    break;

                    */


                    // Comandi grafici home

                    case "🚂Status treno":
                        Treno._Treno(e.Message.Chat.Id);
                        break;

                    case "🔔Tabellone stazione":
                        Tabellone.TabelloneRichiesta(e);
                        break;

                    case "🔎Trova treno":
                        TrovaTreno.StartTrova(e);
                        break;

                    case "⭐️Treni preferiti":
                        Preferiti.ListaTreniPreferiti(e.Message.Chat.Id);
                        break;

                    case "⭐️Stazioni preferite":
                        Preferiti.ListaStazioniPreferite(e.Message.Chat.Id);
                        break;

                    case "📦Altro":
                        Keyboard.Invia_Tastiera_Altro(e.Message.Chat.Id);
                        break;


                    // Sottotastiere

                    case "🏠Home":
                        Keyboard.InviaCambioTastiera(e.Message.Chat.Id);
                        break;

                    // Sottotastiera tabellone
                    case "🔄Aggiorna":
                        Tabellone.SoluzioneStazione(null, null, e);
                        break;
                    case "↩️Cambia stazione":
                        Tabellone.TabelloneRichiesta(e);
                        break;
                    case "⭐️Aggiungi stazione ai preferiti":
                        Tabellone.AggiungiStazioneAiPreferiti(e);
                        break;
                    case "📍Posizione":
                        Tabellone.Posizione(e);
                        break;


                    // Sottotastiera treni
                    case "🔄Aggiorna status":
                        Treno.Aggiorna(e);
                        break;
                    case "↩️Cambia treno":
                        Treno._Treno(e.Message.Chat.Id);
                        break;
                    case "▶️Dettaglio":
                        Treno.TrovaDettaglio(e);
                        break;
                    case "⭐️Aggiungi treno ai preferiti":
                        Treno.AggiungiTrenoAiPreferiti(e);
                        break;


                    // Sottotastiera altro
                    case "🗒Modifica preferiti":
                        Preferiti.ModificaPreferiti_Start(e.Message.Chat.Id);
                        break;
                    case "❓Aiuto":
                        Aiuto(e);
                        break;
                    case "ℹ️Informazioni":
                        Informazioni(e);
                        break;



                    // Risposta in assenza di comando
                    default:
                        FreeMessage(e);
                        break;


                    //Amministrazione
                    case "/broadcast":
                        Broadcast(e);
                        break;
                }
            }
        }

        //*************************************************************************************************************************************************************************************************************************************************
        private static void _bot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            Console.WriteLine("Utente: {0}\nID: {1}\nCallBackQuery: {2}\n", e.CallbackQuery.Message.Chat.FirstName + " " + e.CallbackQuery.Message.Chat.LastName, e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Data.ToString());
            {
                // Formato /[ClasseDiProvenienenza]_[comando]:[valore]
                string[] comando_valore = e.CallbackQuery.Data.ToString().Split(':');

                // Ottengo
                // comando_valore[0]: comando
                // comando_valore[1]: valore passato 

                // Separo [valore] dal resto del messaggio (/[ClasseDiProvenienza]_[comando]) utilizzato come valore di selezione da parte dello switch
                switch (comando_valore[0])
                {
                    // Richiama una descrizione dettagliata delle fermate di un treno di cui viene fornito il codice
                    case "/Dettaglio_codicetreno":
                        Dettaglio._Dettaglio(null, e, comando_valore[1]);
                        break;

                    // Risposta al doppio pulsante Arrivi e Partenze
                    case "/Tabellone_arriviopartenze":
                        Tabellone.SoluzioneStazione(e, comando_valore[1], null);
                        break;

                    // Dettaglio di un treno offerto dal tabellone
                    case "/Tabellone_dettagliotreno":
                        Treno.CodiceTreno(null, comando_valore[1], e);
                        break;


                    // Gestione selezione stazioni corrette e consigliate dal sito nella funzionalità 'Cerca treno'
                    case "/TrovaTreno_SceltaPartenza_Callback":
                        TrovaTreno.Risposta_CallBack_Partenza(e, comando_valore[1]);
                        break;
                    case "/TrovaTreno_SceltaArrivo_Callback":
                        TrovaTreno.Risposta_CallBack_Arrivo(e, comando_valore[1]);
                        break;

                    // Scelta stazione fra le possibilità offerte nella funzionalità 'Tabellone stazione'
                    case "/Tabellone_SceltaStazione_Callback":
                        Tabellone.Risposta_CallBack_SelezioneStazione(e, comando_valore[1]);
                        break;

                    // Ottiene la data di oggi che verrà poi utilizzata dalla funzionalità 'TrovaTreno' per trovare un itinerario
                    case "/TrovaTreno_Data_CallBackQuery":
                        TrovaTreno.Data_Oggi(e);
                        break;
                    // Ottiene la fascia oraria desiderata dall'utente
                    case "/TrovaTreno_FasciaOraria":
                        TrovaTreno.TrovaFasciaOraria(e, comando_valore[1]);
                        break;



                    // Definisce il treno di cui si vuole ottenere lo status (Funzionalità '⭐️Treni preferiti')
                    case "/Preferiti_StatusTreno":
                        Treno.CodiceTreno(null, comando_valore[1], e);
                        break;

                    case "/Preferiti_Stazione":
                        Tabellone.Risposta_CallBack_SelezioneStazione(e, comando_valore[1]);
                        break;

                    // Gestione dei preferiti

                    case "/ModificaPreferiti_Start":
                        Preferiti.ModificaPreferiti_OffriListaPreferiti(e, comando_valore[1]);
                        break;
                    case "/ModificaPreferiti_OffriListaTreniPreferiti":
                        Preferiti.ModificaPreferiti_RimuoviTreno(e, comando_valore[1]);
                        break;
                    case "/ModificaPreferiti_OffriListaStazioniPreferite":
                        Preferiti.ModificaPreferiti_RimuoviStazione(e, comando_valore[1]);
                        break;

                    // Pulsanti base

                    case "/🔙TornaIndietro":
                        Preferiti.TornaIndietro(e);
                        break;

                    case "/🏠Home":
                        Keyboard.InviaCambioTastiera(e.CallbackQuery.Message.Chat.Id);
                        break;

                    //*******************************************************************************************************************************

                    // L'utente richiede in dettaglio un treno trovato dalla ricerca di un itinerario
                    // comando_valore[1] = dettaglio=0&sessionId=1494941553678&lang=IT (LINK INCOMPLETO)
                    // Link completo: /vt_pax_internet/mobile/programmato?dettaglio=0&sessionId=1494941553678&lang=IT
                    case "/linkdettaglio":
                        TrovaTreno.TrovaCodice(e, comando_valore[1]);
                        // Come funziona?
                        // Mi arriva il link, lo inoltro nuovamente alla classe TrovaTreno la quale, dopo aver estratto il codice del treno, lo inoltra alla classe Treno
                        break;
                        // Non funziona, perchè? L'applicativo web gestisce la richiesta mediante una sessione
                        // Come posso gestire la sessione con visual studio?



                }
            }
        }

        /// <summary>
        /// Gestione dei messaggi che non contendendo comandi supportati o non ne contengono affatto  
        /// (La gestione avviene per mezzo di un dizionario contente l'ultimo comando effettuato da sciascun utente)
        /// *Necessario per la gestione delle risposte a domande proposte dal Bot
        /// </summary>
        /// <param name="e"></param>
        private static void FreeMessage(Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                // Controllo dell'ultimo comando effettuato ed indirizzamento della risposta al metodo opportuno
                switch (Bot._comandi[e.Message.Chat.Id])
                {
                    // Status di un treno con un determinato codice
                    case "/treno":
                        Treno.CodiceTreno(e);
                        break;

/*
                    // Treni presenti su una specifica tratta
                    case "/tratta_partenza":
                        Tratta.TrattaPartenza(e);
                        break;
                    case "/sceltapartenza":
                        Tratta.SceltaPartenza(e);
                        break;
                    case "/tratta_arrivo":
                        Tratta.TrattaArrivo(e);
                        break;
                    case "/sceltaarrivo":
                        Tratta.SceltaArrivo(e);
                        break;
*/

                    // Richiesta di dettagli sul percorso di un treno
                    case "/dettagliorichiesta":
                        Dettaglio._Dettaglio(e);
                        break;

                    // Richiesta del tabellone dei treni di una specifica stazione
                    case "/tabellonerichiesta":
                        Tabellone._Tabellone(e);
                        break;
                    /*case "/sceltaTabellone": // Caso in cui la stazione fornita dall'utente non è supportata dal sito (che si occupa di fornire le stazioni supportate relative alla scelta)
                        Tabellone.SceltaTabellone(e);
                        break;*/

                    // Treni presenti dati specifici dati
                    case "/trova_partenza":
                        TrovaTreno.TrovaPartenza(e);
                        break;
                    case "/trova_arrivo":
                        TrovaTreno.TrovaArrivo(e);
                        break;
                    case "/trova_data":
                        TrovaTreno.TrovaData(e);
                        break;
                    //case "/trova_fasciaoraria":
                    //    TrovaTreno.TrovaFasciaOraria(e);
                    //    break;


                    // Comando non gestito
                    default:
                        ErroreComando(e);
                        break;

                    

                    // Amministrazione
                    case "/broadcast":
                        _Broadcast(e);
                        break;
                }
            }
            catch { }// Errore 
        }
        

        /// <summary>
        /// Invio messaggio di testo
        /// </summary>
        /// <param name="_idchat">ID utente</param>
        /// <param name="_messaggio">Messaggio</param>
        /// <param name="rum">Opzionale: tastiera personalizzata</param>
        private static async void InvioMessaggio(long _idchat, string _messaggio, ReplyKeyboardMarkup rum = null)
        {
            try
            {
                await Bot._bot.SendTextMessageAsync(_idchat, _messaggio, replyMarkup: rum);
            }
            catch { }
        }

        private static void Aiuto(Telegram.Bot.Args.MessageEventArgs e)
        {
            InvioMessaggio(e.Message.Chat.Id, "Hai bisogno di aiuto?Contatta @Throughdreams!\n\nNon basta fare del bene, bisogna anche farlo bene.\n(Denis Didierot)", Keyboard.OttieniTastieraHome());
        }
        private static void Informazioni(Telegram.Bot.Args.MessageEventArgs e)
        {
            InvioMessaggio(e.Message.Chat.Id, "QuickTreno si occupa di fornire informazioni ufficiali sui treni della nota compagnia Trenitalia.\nLa fonte di provenienza dei dati è viaggiatreno.it.\n\nPer maggiori approfondimenti contattare  @Throughdreams.", Keyboard.OttieniTastieraHome());
        }

        /// <summary>
        /// Errore generale
        /// </summary>
        /// <param name="e"></param>
        private static void ErroreComando(Telegram.Bot.Args.MessageEventArgs e)
        {
            InvioMessaggio(e.Message.Chat.Id, "Errore", Keyboard.OttieniTastieraHome());
        }

        
        private static void Help(Telegram.Bot.Args.MessageEventArgs e)
        {
            InvioMessaggio(e.Message.Chat.Id, "❓Hai qualche difficoltà? Contatta @ThroughDreams");
            Keyboard.InviaCambioTastiera(e.Message.Chat.Id);
        }

        //****************************************** Amministrazione

        private const long IDAdmin = 55999160;
        private static void MessaggioBackup(string backup)
        {
            InvioMessaggio(IDAdmin, backup);
        }

        private static void Broadcast(Telegram.Bot.Args.MessageEventArgs e)
        {
            // Autorizzazione
            if (e.Message.Chat.Id != IDAdmin) return;

            InvioMessaggio(e.Message.Chat.Id, "📢Messaggio: ");
            Bot._comandi[e.Message.Chat.Id] = "/broadcast";
        }
        private static void _Broadcast(Telegram.Bot.Args.MessageEventArgs e)
        {
            DataSet ds = new DataSet();
            SQLiteDataAdapter da = new SQLiteDataAdapter();
            da.SelectCommand = new SQLiteCommand("select * from User", LocalDatabase.conn);
            da.Fill(ds);

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                long id = long.Parse(dr["chatID"].ToString());
                InvioMessaggio(id, "📢Messaggio d'amministazione:\n" + e.Message.Text);
            }
        }


    }
}
