using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;
using System.Data;


namespace QuickTreno
{
    static class LocalDatabase
    {
        public static SQLiteConnection conn { get; set; }


        /// <summary>
        /// Creazione DB
        /// </summary>
        public static void CreateDatabase()
        {
            // Indirizzo del DB
            conn = new SQLiteConnection("Data Source = " + Directory.GetCurrentDirectory() + "\\Contactdb.sqlite");
            conn.Open();

            string cmd = string.Empty;
            SQLiteCommand command;

            // Creo le tabelle
            cmd = "CREATE TABLE User(chatID INT, FirstName varchar, LastName varchar, Username varchar, primary key(chatID))";
            command = new SQLiteCommand(cmd, conn);
            command.ExecuteNonQuery();
            cmd = "CREATE TABLE LikedTrain(chatID INT, trainCode varchar, primary key(chatID, trainCode));";
            command = new SQLiteCommand(cmd, conn);
            command.ExecuteNonQuery();
            cmd = "CREATE TABLE LikedStation(stationID varchar, chatID varchar, primary key(stationID, chatID));";
            command = new SQLiteCommand(cmd, conn);
            command.ExecuteNonQuery();
            cmd = "CREATE TABLE Station(Name varchar, stationID varchar, Regione varchar, Latitudine varchar, Longitudine varchar, primary key(stationID));";
            command = new SQLiteCommand(cmd, conn);
            command.ExecuteNonQuery();

            // Carico la tabella 'Station' di tutte le stazione conosciute all'interno del file "Stazioni.csv"
            var fs = File.OpenRead(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Stazioni.csv").ToString());
            int i = 0;
            using (var rd = new StreamReader(fs))
            {
                while (!rd.EndOfStream)
                {
                    var splits = rd.ReadLine().Split(',');


                    // Query parametrica
                    SQLiteCommand _command = new SQLiteCommand(conn);
                    _command.CommandText = "INSERT INTO Station(Name, stationID,Regione, Latitudine, Longitudine) VALUES(@valore1,@valore2,@valore3,@valore4,@valore5)";
                    _command.CommandType = CommandType.Text;
                    _command.Parameters.Add(new SQLiteParameter("@Valore1",splits[0]));
                    _command.Parameters.Add(new SQLiteParameter("@Valore2", splits[1]));
                    _command.Parameters.Add(new SQLiteParameter("@Valore3", splits[2]));
                    _command.Parameters.Add(new SQLiteParameter("@Valore4", splits[5]));
                    _command.Parameters.Add(new SQLiteParameter("@Valore5", splits[6]));
                    _command.ExecuteNonQuery();

                    i++;
                }
            }
            conn.Close();

        }

        public static void OpenConnection()
        {
            conn = new SQLiteConnection("Data Source = " + Directory.GetCurrentDirectory() + "\\Contactdb.sqlite");
            conn.Open();
        }
        

    }
}
