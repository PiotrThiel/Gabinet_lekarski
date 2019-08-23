using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GabinetLekarski.Klasy
{
    class Pacjent
    {
        private string _lp;
        private string _iDWizyta;
        private string _godzinaWizyty;
        private string _status;
        private string _fKWizyty;
        private string _nazwiskoImie;
        private string _opisPrzycisku;

        public string Lp { get => _lp; set => _lp = value; }
        public string IDWizyta { get => _iDWizyta; set => _iDWizyta = value; }
        public string GodzinaWizyty { get => _godzinaWizyty; set => _godzinaWizyty = value; }
        public string Status { get => _status; set => _status = value; }
        public string FKWizyty { get => _fKWizyty; set => _fKWizyty = value; }
        public string NazwiskoImie { get => _nazwiskoImie; set => _nazwiskoImie = value; }
        public string OpisPrzycisku { get => _opisPrzycisku; set => _opisPrzycisku = value; }

        #region Konstruktory

        //Konstruktor
        public Pacjent(string lp, string iDWizyta, string godzinaWizyty, string status, string fKWizyty, string imie, string nazwisko)
        {
            Lp = lp;
            IDWizyta = iDWizyta;
            GodzinaWizyty = DateTime.ParseExact(godzinaWizyty, "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToShortTimeString();
            Status = status;
            FKWizyty = fKWizyty;
            NazwiskoImie = nazwisko + " " + imie;
            OpisPrzycisku = "Usuń";
        }

        //Konstruktor
        public Pacjent(string godzinaWizyty)
        {
            _godzinaWizyty = DateTime.ParseExact(godzinaWizyty, "HH:mm", System.Globalization.CultureInfo.InvariantCulture).ToShortTimeString();
        }

        #endregion
    }
}
