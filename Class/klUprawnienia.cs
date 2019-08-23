using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GabinetLekarski.Klasy
{
    //Klasa służy do pobrania i daministrowania uprawnieniami
    class klUprawnienia
    {
        private string _uzytkownik;
        private string _haslo;
        private bool _zablokowane;
        private string _FKPacjent;
        private string _FKUzytkownik;
        private bool _administrator;    //uprawnienia
        private bool _pacjent;          //uprawnienia
        private bool _rejestracja;      //uprawnienia
        private bool _gabinet;          //uprawnienia
        


        public string Uzytkownik { get => _uzytkownik; set => _uzytkownik = value; }
        public string Haslo { get => _haslo; set => _haslo = value; }
        public bool Zablokowane { get => _zablokowane; set => _zablokowane = value; }
        public string FKPacjent { get => _FKPacjent; set => _FKPacjent = value; }
        public string FKUzytkownik { get => _FKUzytkownik; set => _FKUzytkownik = value; }
        public bool Administrator { get => _administrator; set => _administrator = value; }
        public bool Pacjent { get => _pacjent; set => _pacjent = value; }
        public bool Rejestracja { get => _rejestracja; set => _rejestracja = value; }
        public bool Gabinet { get => _gabinet; set => _gabinet = value; }
        
        //Konstruktor
        public klUprawnienia(string uzytkownik, string haslo, bool zablokowane, string fKPacjent, string fkUzytkownik, bool administrator, bool pacjent, bool rejestracja, bool gabinet)
        {
            Uzytkownik = uzytkownik;
            Haslo = haslo;
            Zablokowane = zablokowane;
            FKPacjent = fKPacjent;
            FKUzytkownik = fkUzytkownik;
            Administrator = administrator;
            Pacjent = pacjent;
            Rejestracja = rejestracja;
            Gabinet = gabinet;
        }
    }
}
