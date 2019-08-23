using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GabinetLekarski.Klasy
{
    //Klasa służy do pobrania danych do wykazu napisanych skierowan dla danego pacjenta

    class KlSkierowanie
    {
        private string _dataWizyty;     //Data wizyty pacjenta z tabWizyty
        private string _nazwaSkrocona;  //Nazwa skrócano skierowania z tabSkierowanie
        private string _nazwaPelna;     //Nazwa pełna skierowania z tabSkierowanie
        private string _fKPacjent;      //IDpacjenta po fKPacjent z tabWizyta
        private string _iDSkierowania;  //ID
        private string _nazwaPliku;     //Nazwa pliku skierowania zapisanego na dysku dla danego pacjenta z tabWykazSkierowan

        public string DataWizyty { get => _dataWizyty; set => _dataWizyty = value; }
        public string NazwaSkrocona { get => _nazwaSkrocona; set => _nazwaSkrocona = value; }
        public string NazwaPelna { get => _nazwaPelna; set => _nazwaPelna = value; }
        public string FKPacjent { get => _fKPacjent; set => _fKPacjent = value; }
        public string IDSkierowania { get => _iDSkierowania; set => _iDSkierowania = value; }
        public string NazwaPliku { get => _nazwaPliku; set => _nazwaPliku = value; }

        //Konstruktor
        public KlSkierowanie(string dataWizyty, string nazwaSkrocona, string nazwaPelna, string fKPacjent, string iDSkierowania, string nazwaPliku)
        {
            DataWizyty = dataWizyty;
            NazwaSkrocona = nazwaSkrocona;
            NazwaPelna = nazwaPelna;
            FKPacjent = fKPacjent;
            IDSkierowania = iDSkierowania;
            NazwaPliku = nazwaPliku;

        }

    }
}
