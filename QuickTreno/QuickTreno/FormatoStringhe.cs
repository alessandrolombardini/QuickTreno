using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace QuickTreno
{
    static class FormatoStringhe
    {
        /// <summary>
        /// Rimuove un commento identificato da <> da una stringa fornita
        /// </summary>
        /// <param name="_stringa"></param>
        /// <returns></returns>
        static public string RimuoviCommento(string _stringa)
        {
            int _start = _stringa.IndexOf('<');
            int _end = _stringa.IndexOf('>');

            return _stringa.Remove(_start, _end - _start + 1);

        }

        /// <summary>
        /// Rimuove elementi inerenti alla formattazione della stringa passata
        /// </summary>
        /// <param name="_stringa"></param>
        /// <returns></returns>
        static public string Pulisci(string _stringa)
        {
            bool _space = false;
            for (int i = 0; i < _stringa.Length; i++)
            {
                if (_stringa[i] == '\t' || _stringa[i] == '\n' || _stringa[i] == '\r' || _stringa[i] == ' ')
                {
                    _stringa = _stringa.Remove(i, 1);

                    if (_space == true)
                    {
                        _stringa = _stringa.Insert(i, " ");
                        _space = false;
                        i++;
                    }
                    i--;



                }
                else _space = true;

            }
            return _stringa;
        }
    }
}
