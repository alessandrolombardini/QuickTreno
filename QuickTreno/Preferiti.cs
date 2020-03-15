using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTreno
{

    static class Preferiti
    {
        /// <summary>
        /// Aggiunge codice treno alla lista dei treni preferiti dell'utente
        /// </summary>
        /// <param name="chat_id"></param>
        /// <param name="_codicetreno"></param>
        static public async void AggiungiTreno(long chat_id, string _codicetreno)
        {
           
            SQLiteCommand _command = new SQLiteCommand(LocalDatabase.conn);
            _command.CommandText = "Insert INTO LikedTrain (ChatID, trainCode) Values(@valore1,@valore2)";
            _command.CommandType = CommandType.Text;
            _command.Parameters.Add(new SQLiteParameter("@Valore1", chat_id));
            _command.Parameters.Add(new SQLiteParameter("@Valore2", _codicetreno));
            _command.ExecuteNonQuery();

            // Response
            await Bot._bot.SendTextMessageAsync(chat_id, "✅Treno aggiunto ai preferiti", false, false, 0, Keyboard.OttieniTastieraStatusTrenoSenzaPreferiti());
        }

        static public async void AggiungiStazione(long chat_id, string _nomestazione)
        {
            DataSet ds = new DataSet();
            try
            {
                // Stazione di Viserba all'interno del DB è memorizzata come Rimini Viserba

                
                SQLiteDataAdapter da = new SQLiteDataAdapter();
                da.SelectCommand = new SQLiteCommand("select stationID from Station where Name LIKE @valore1", LocalDatabase.conn);
                da.SelectCommand.Parameters.AddWithValue("@valore1", "%"+_nomestazione.ToUpper());
                da.Fill(ds);
            }
            catch
            {
                await Bot._bot.SendTextMessageAsync(chat_id, "Errore", false, false, 0, Keyboard.OttieniTastieraHome());
                return;
            }

            DataRow dr = ds.Tables[0].Rows[0];
            string stationID = dr["stationID"].ToString();
            

            SQLiteCommand _command = new SQLiteCommand(LocalDatabase.conn);
            _command.CommandText = "Insert INTO LikedStation (chatID, stationID) Values(@valore1,@valore2)";
            _command.CommandType = CommandType.Text;
            _command.Parameters.Add(new SQLiteParameter("@Valore1", chat_id));
            _command.Parameters.Add(new SQLiteParameter("@Valore2",stationID));
            _command.ExecuteNonQuery();

            // Response
            await Bot._bot.SendTextMessageAsync(chat_id, "✅Stazione aggiunta ai preferiti", false, false, 0, Tabellone.RitornaTastieraGiustaTabellone(chat_id));
        }

        /// <summary>
        /// Ottiene la lista di InlineButton dei treni preferiti dall'utente richiedente: la pressione comporta come output lo status del treno
        /// </summary>
        /// <param name="chat_id"></param>
        static async public void ListaStazioniPreferite(long chat_id)
        {
            var items = new Dictionary<string, string>();

            DataSet ds = new DataSet();
            SQLiteDataAdapter da = new SQLiteDataAdapter();
            da.SelectCommand = new SQLiteCommand("select * from LikedStation where chatID = @valore1", LocalDatabase.conn);
            da.SelectCommand.Parameters.AddWithValue("@valore1", chat_id);
            da.Fill(ds);

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                string stationID = dr["stationID"].ToString();


                DataSet _ds = new DataSet();
                da.SelectCommand = new SQLiteCommand("select Name from Station where stationID = @valore1", LocalDatabase.conn);
                da.SelectCommand.Parameters.AddWithValue("@valore1", stationID);
                da.Fill(_ds);

                DataRow _dr = _ds.Tables[0].Rows[0];
                string Nome = _dr["Name"].ToString();

                items.Add(Nome, "/Preferiti_Stazione:" + stationID);

            }

            if (items.Count == 0)
            {
                await Bot._bot.SendTextMessageAsync(chat_id, "❌Non hai stazioni preferite.");
                Keyboard.InviaCambioTastiera(chat_id);
                return;
            }

            items.Add("⛔️Annulla", "/🏠Home");

            await Bot._bot.SendTextMessageAsync(chat_id, "☑️Selezionare stazione:", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));

        }

        /// <summary>
        /// Ottiene la lista di InlineButton dei treni preferiti dall'utente richiedente: la pressione comporta come output lo status del treno
        /// </summary>
        /// <param name="chat_id"></param>
        static async public void ListaTreniPreferiti(long chat_id)
        {
            DataSet ds = new DataSet();
            SQLiteDataAdapter da = new SQLiteDataAdapter();
            da.SelectCommand = new SQLiteCommand("select * from LikedTrain where chatID = @valore1", LocalDatabase.conn);
            da.SelectCommand.Parameters.AddWithValue("@valore1", chat_id);
            da.Fill(ds);

            var items = new Dictionary<string, string>();

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                try
                {
                    items.Add(dr["trainCode"].ToString(), "/Preferiti_StatusTreno:" + dr["trainCode"].ToString());
                }
                catch { }
            }

            if (items.Count == 0)
            {
                await Bot._bot.SendTextMessageAsync(chat_id, "❌Non hai treni preferiti.");
                Keyboard.InviaCambioTastiera(chat_id);
                return;
            }


            items.Add("⛔️Annulla", "/🏠Home");

            await Bot._bot.SendTextMessageAsync(chat_id, "☑️Selezionare treno:", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));

        }


        //*************************************** Modifica preferiti ************************************************************************************
        // Memorizza l'id dell'ultimo messaggio: EditMessageAsync
        static public Dictionary<long, int> _idultimomessaggio = new Dictionary<long, int>();


        static private Dictionary<string, string> _tastieraPreferiti = new Dictionary<string, string>
            {
                    { "Treno", "/ModificaPreferiti_Start:treni" },
                    {"Stazione", "/ModificaPreferiti_Start:stazioni"},
                    {"⛔️Annulla", "/🏠Home"}

            };

        /// <summary>
        /// Inzio percorso di rimozione preferiti
        /// </summary>
        /// <param name="chat_id"></param>

        static async public void ModificaPreferiti_Start(long chat_id)
        {
            var _message = await Bot._bot.SendTextMessageAsync(chat_id, "☑️Cosa vuoi rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(_tastieraPreferiti));
            _idultimomessaggio[chat_id] = _message.MessageId;
        }

        /// <summary>
        /// Metodo pubblico richiamato dal main che si occupa di stabilire se la richiesta di modifica riguarda treni o stazioni
        /// Una volta identificato questo dettagio inoltra la richiesta ad uno dei metodi privati della classe 'Preferiti' per inviare la lista dei treni preferiti o delle stazioni preferite
        /// </summary>
        /// <param name="chat_id">Identificativo della chat</param>
        /// <param name="_treniostazioni">Decisione utente: "Treni" o "Stazioni"</param>
        static public void ModificaPreferiti_OffriListaPreferiti(Telegram.Bot.Args.CallbackQueryEventArgs e, string _treniostazioni)
        {
            if (_treniostazioni == "treni") ModificaPreferiti_OffriListaTreniePreferiti(e.CallbackQuery.Message.Chat.Id);
            else ModificaPreferiti_OffriListaStazioniPreferite(e.CallbackQuery.Message.Chat.Id);
        }

        /// <summary>
        /// Metodo privato richiamato da 'ModificaPreferiti_OffriListaPreferiti' per l'invio della lista dei treni preferiti
        /// </summary>
        /// <param name="chat_id">Identificativo della chat</param>
        static async private void ModificaPreferiti_OffriListaTreniePreferiti(long chat_id)
        {
            try
            { 

                DataSet ds = new DataSet();
                SQLiteDataAdapter da = new SQLiteDataAdapter();
                da.SelectCommand = new SQLiteCommand("select * from LikedTrain where chatID = @valore1", LocalDatabase.conn);
                da.SelectCommand.Parameters.AddWithValue("@valore1", chat_id);
                da.Fill(ds);

                var items = new Dictionary<string, string>();

            


                // Non sono memorizzati treni nei preferiti
                if (ds.Tables[0].Rows.Count == 0)
                {
                    await Bot._bot.EditMessageTextAsync(chat_id, _idultimomessaggio[chat_id], "🛑Nei preferiti non sono disponibili treni al momento.\nCosa vuole rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(_tastieraPreferiti));
                    return;
                }
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    try
                    {
                        items.Add(dr["trainCode"].ToString(), "/ModificaPreferiti_OffriListaTreniPreferiti:" + dr["trainCode"].ToString());
                    }
                    catch { }
                }

                items.Add("🔙Torna indietro", "/🔙TornaIndietro");
                items.Add("⛔️Annulla", "/🏠Home");

                //await Bot._bot.SendTextMessageAsync(chat_id, "⭕️Quale treno vuoi rimuovere?:", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
                await Bot._bot.EditMessageTextAsync(chat_id, _idultimomessaggio[chat_id], "☑️Quale treno vuoi rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
            }
            catch { }
        }
        /// <summary>
        /// Metodo privato richiamato da 'ModificaPreferiti_OffriListaPreferiti' per l'invio della lista delle stazioni preferite
        /// </summary>
        /// <param name="chat_id">Identificativo della chat</param>
        static async private void ModificaPreferiti_OffriListaStazioniPreferite(long chat_id)
        {
            try
            {
                DataSet ds = new DataSet();
                SQLiteDataAdapter da = new SQLiteDataAdapter();
                da.SelectCommand = new SQLiteCommand("select * from LikedStation where chatID = @valore1", LocalDatabase.conn);
                da.SelectCommand.Parameters.AddWithValue("@valore1", chat_id);
                da.Fill(ds);

                var items = new Dictionary<string, string>();
                // Non sono memorizzate stazioni nei preferiti
                if (ds.Tables[0].Rows.Count == 0)
                {
                    await Bot._bot.EditMessageTextAsync(chat_id, _idultimomessaggio[chat_id], "🛑Nei preferiti non sono disponibili stazioni al momento.\nCosa vuole rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(_tastieraPreferiti));
                    return;
                }

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    DataSet _ds = new DataSet();
                    da.SelectCommand = new SQLiteCommand("select Name from Station where stationID = @valore1", LocalDatabase.conn);
                    da.SelectCommand.Parameters.AddWithValue("@valore1", dr["stationID"].ToString());
                    da.Fill(_ds);

                    DataRow _dr = _ds.Tables[0].Rows[0];
                    string Nome = _dr["Name"].ToString();
                    string ID = dr["stationID"].ToString();


                    items.Add(Nome, "/ModificaPreferiti_OffriListaStazioniPreferite:" + ID);
                 
                }


                items.Add("🔙Torna indietro", "/🔙TornaIndietro");
                items.Add("⛔️Annulla", "/🏠Home");

                //await Bot._bot.SendTextMessageAsync(chat_id, "⭕️Quale stazione vuoi rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
                await Bot._bot.EditMessageTextAsync(chat_id, _idultimomessaggio[chat_id], "☑️Quale stazione vuoi rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));

            }
            catch { await Bot._bot.SendTextMessageAsync(chat_id, "🤖Qualcosa è andato storto", false, false, 0, Keyboard.OttieniTastieraHome()); }
        }

        /// <summary>
        /// Preso come input il codice di un treno, elimina il record corrispondente ad un dato user e a quella data preferenza nella tabella 'LikedTrain'
        /// </summary>
        /// <param name="chat_id"></param>
        /// <param name="_codeTreno">Codice del treno da rimuovere (Es: 6464) </param>
        static async public void ModificaPreferiti_RimuoviTreno(Telegram.Bot.Args.CallbackQueryEventArgs e, string _codeTreno)
        {
            try
            {
                SQLiteCommand _command = new SQLiteCommand(LocalDatabase.conn);
                _command.CommandText = "Delete from LikedTrain where trainCode = @valore1 and chatID = @valore2";
                _command.CommandType = CommandType.Text;
                _command.Parameters.Add(new SQLiteParameter("@Valore1", _codeTreno));
                _command.Parameters.Add(new SQLiteParameter("@Valore2", e.CallbackQuery.Message.Chat.Id));
                _command.ExecuteNonQuery();

                DataSet ds = new DataSet();
                SQLiteDataAdapter da = new SQLiteDataAdapter();
                da.SelectCommand = new SQLiteCommand("select * from LikedTrain where chatID = @valore1", LocalDatabase.conn);
                da.SelectCommand.Parameters.AddWithValue("@valore1", e.CallbackQuery.Message.Chat.Id);
                da.Fill(ds);

                // Non sono memorizzati ulteriori treni nei preferiti
                if (ds.Tables[0].Rows.Count == 0)
                {
                    await Bot._bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat.Id, _idultimomessaggio[e.CallbackQuery.Message.Chat.Id], "✅Ultimo treno rimosso con successo.\nCosa vuole rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(_tastieraPreferiti));
                    return;
                }


                var items = new Dictionary<string, string>();

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    try
                    {
                        items.Add(dr["trainCode"].ToString(), "/ModificaPreferiti_OffriListaTreniPreferiti:" + dr["trainCode"].ToString());
                    }
                    catch { }

                }

                items.Add("🔙Torna indietro", "/🔙TornaIndietro");
                items.Add("⛔️Annulla", "/🏠Home");

                //await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "⭕️Treno rimosso con successo\nQuale altro treno vuoi rimuovere?:", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));

                await Bot._bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat.Id, _idultimomessaggio[e.CallbackQuery.Message.Chat.Id], "⭕️Treno rimosso con successo\nQuale altro treno vuoi rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
            }
            catch { await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "🤖Qualcosa è andato storto", false, false, 0, Keyboard.OttieniTastieraHome()); }
        }
        /// <summary>
        /// Preso come input il codice di una stazione, elimina il record corrispondente ad un dato user e a quella data preferenza nella tabella 'LikedStation'
        /// </summary>
        /// <param name="chat_id"></param>
        /// <param name="_codeStazione">codice stazione (Es: SF0282)</param>
        static async public void ModificaPreferiti_RimuoviStazione(Telegram.Bot.Args.CallbackQueryEventArgs e, string _codeStazione)
        {
            try
            {
                SQLiteCommand _command = new SQLiteCommand(LocalDatabase.conn);
                _command.CommandText = "Delete from LikedStation where stationID = @valore1 and chatID = @valore2";
                _command.CommandType = CommandType.Text;
                _command.Parameters.Add(new SQLiteParameter("@Valore1", _codeStazione));
                _command.Parameters.Add(new SQLiteParameter("@Valore2", e.CallbackQuery.Message.Chat.Id));
                _command.ExecuteNonQuery();

                DataSet ds = new DataSet();
                SQLiteDataAdapter da = new SQLiteDataAdapter();
                da.SelectCommand = new SQLiteCommand("select * from LikedStation where chatID =  @valore1", LocalDatabase.conn);
                da.SelectCommand.Parameters.AddWithValue("@valore1", e.CallbackQuery.Message.Chat.Id);
                da.Fill(ds);

                var items = new Dictionary<string, string>();

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    DataSet _ds = new DataSet();
                    da.SelectCommand = new SQLiteCommand("select Name from Station where stationID = @valore1", LocalDatabase.conn);
                    da.SelectCommand.Parameters.AddWithValue("@valore1", dr["stationID"].ToString());
                    da.Fill(_ds);

                    DataRow _dr = _ds.Tables[0].Rows[0];
                    string Nome = _dr["Name"].ToString();

                    items.Add(Nome, "/ModificaPreferiti_OffriListaStazioniPreferite:" + dr["stationID"].ToString());

                }
                items.Add("🔙Torna indietro", "/🔙TornaIndietro");
                items.Add("⛔️Annulla", "/🏠Home");
                if (items.Count == 2)
                {
                    await Bot._bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat.Id, _idultimomessaggio[e.CallbackQuery.Message.Chat.Id], "✅Ultima stazione rimossa con successo.\nCosa vuole rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(_tastieraPreferiti));
                    return;
                }

                //await Bot._bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "⭕️Treno rimosso con successo\nQuale altro treno vuoi rimuovere?:", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
                await Bot._bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat.Id, _idultimomessaggio[e.CallbackQuery.Message.Chat.Id], "⭕️Stazione rimosssa con successo\nQuale altra stazione vuoi rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(items));
            }
            catch { }
        }

        static public async void TornaIndietro(Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            var _message = await Bot._bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat.Id, _idultimomessaggio[e.CallbackQuery.Message.Chat.Id], "☑️Cosa vuoi rimuovere?", replyMarkup: Keyboard.InlineKeyboardMarkupMaker(_tastieraPreferiti));
            _idultimomessaggio[e.CallbackQuery.Message.Chat.Id] = _message.MessageId;
        }

        
    }

}
