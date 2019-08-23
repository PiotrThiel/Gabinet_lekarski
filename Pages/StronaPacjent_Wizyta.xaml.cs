using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.SqlClient;
using System.Data;

namespace GabinetLekarski.Strony
{

    public partial class StronaPacjent_Wizyta : Page
    {
        #region PARAMETRY

        bool insert;
        bool update;
        string query = "";                                                                                                                  //Zapytanie do bazy danych
        string dataWizyty = "";                                                                                                             //Data wizyty pacjenta 
        string nazwaDnia = "";                                                                                                              //SŁOWNA nazwa wybranego z kalendarza dnia otwarcia gabinetu
        string godzOtwarcia = "";                                                                                                           //Godzina otwarcia gabinetu dla wybranego z kalendarza dnia
        string godzZamkniecia = "";                                                                                                         //Godzina zamkniecia gabinetu dla wybranego z kalendarza dnia 
        string IDWizyty = "";                                                                                                               //ID tabeli tabWizyty wybranego dnia. Pobierana w funkcji PobierzGodzine() - sektor SELECT
        string IDPacjenta = "";                                                                                                             //FKPacjent tabeli tabWizyta. Pobierana z UCHYTU w funkjic btnZapiszWizyte_Click()
        string IDLekarza;                                                                                                                   //IDLekarza wybraneko w Combobox 

        Page aktywnaStrona;
        string nazwaAktynejStrony;
        public int parametrStronaPacjent_Wizyta = -1;                                                                                       //0 - standardowe otworzenie formularza - wyszukuje pacjentow po danych osobowych

        List<Pacjent> listaWizytyPacjentow = new List<Pacjent>();                                                                           //lista wizyt pacjentow na dany dzien
        DateTime godzOtwarciaGabinetu;                                                                                                      //godzina otwarcia gabinetu
        DateTime godzZamknięciaGabinetu;                                                                                                    //godzina zamkniecia gabinetu - do tej godz. jest liczona pusta lista pacjentow
        DataTable dtListyOtwarte = new DataTable();                                                                                         //Tablica zawierająca wykaz dni z dostępnymi wizytami do rejestrascji
        int wiersz = -1;                                                                                                                    //wskaźnik pozycji na liście DataTable dtListyOtwarte
        double interwal = 30;                                                                                                               //INTERWAL - odstep czasu pomiedzy kolejnymi wizytami pacjentow, podawana w minutach [min]

        //Lista aktywnych okien
        public List<object> listaStron = new List<object>();


        #endregion
        
        #region Paramerty startowe formularza
        // W zależności od ustawionego parametru, do ramki strony 'StronaPacjent_Wizyta' będzie ładowana odpowiednia strona.
        // 0 - do ramki 'ramkaWizyta' zostanie załadowany formularz 'StronaPacjent_Szukaj' w trybie wyszukiwania pacjenta po dnaych osobowych, 
        // 1 - do ramki 'ramkaWizyta' zostanie załadowany formularz 'StronaPacjent_Dane' w trybie DODANIA pacjenta do bazy
        // 2 - do ramki 'ramkaWizyta' zostanie załadowany UKRYTY formularz 'StronaPacjent_Szukaj' w trybie wyszukiwania UWAGA przypisanych pacjentowi, 
        // 3 - ramka 'ramkaWizyta' jest pusta i ukryta. Strona 'StronaPacjent_Wizyta pracuje w trybie Dodoawnia/Poprawiania WIZYTY
        // 4 - do ramki 'ramkaWizyta' zostanie załadowany formularz 'StronaPacjent_Szukaj' w trybie wyszukiwania ZAREJESTROWANYCH WIZYT po dnaych osobowych pacjenta, 
        #endregion

        public StronaPacjent_Wizyta(int parametrStrony)
        {
            InitializeComponent();
            parametrStronaPacjent_Wizyta = parametrStrony;
            txtInterwal.Text = interwal.ToString();
            groupSzukaj.Visibility = Visibility.Collapsed;
            dataGridWizyty.Visibility = Visibility.Collapsed;
            dgWizyty.Visibility = Visibility.Collapsed;
            btnWstawGodzine.IsEnabled = false;

            listaStron.Add(new StronaPacjent_Szukaj((0)));                              //[0] 0 - parametr formularza 'StronaPacjent_Szukaj'    - formularz wyszukuje pacjentow po danych osobowych
            listaStron.Add(new StronaPacjent_Szukaj((1)));                              //[1] 1 - parametr formularza 'StronaPacjent_Szukaj'    - formularz wyszukuje tylko pocjentow z wpisanymi uwagami
            listaStron.Add(new StronaPacjent_Dane(false, false, "", 0));                //[2] 0 - parametr formularza 'StronaPacjent_Dane'      - standardowe otworzenie formularza z pełnymi danymi
            listaStron.Add(new StronaPacjent_Szukaj((0)));                              //[3] 2 - parametr formularza 'StronaPacjent_Szukaj'    - formularz wyszukuje pacjentow po danych osobowych

            dataGridWizyty.Columns[0].Visibility = Visibility.Collapsed;

            #region Ustawienie zawartości ramki formularza "ramkaWizyta" - z prawej

            if (parametrStronaPacjent_Wizyta == 0 || parametrStronaPacjent_Wizyta == 2 || parametrStronaPacjent_Wizyta == 4)                          //SZUKAJ Pacjenta lub SZUKAJ uwag
            {
                #region ramkaWizyta = StronaPacjent_Szukaj(0|2|4)

                if (parametrStronaPacjent_Wizyta == 0 || parametrStronaPacjent_Wizyta == 4)
                {
                    ramkaWizyta.Navigate(listaStron[0]);                                    //Załadowanie formularza wyszukującego pacjentow po danych osobowych 'StronaPacjent_Szukaj(0)'
                    btnWyczyscRamke.Visibility = Visibility.Collapsed;
                }
                else
                    ramkaWizyta.Navigate(listaStron[1]);                                    //Załadowanie formularza wyszukującego pacjentow po UWAGACH 'StronaPacjent_Szukaj(1)'

                if (parametrStronaPacjent_Wizyta == 0 || parametrStronaPacjent_Wizyta == 2)
                    btnZapiszWizyte.Content = "Ustaw wizytę";
                else
                    btnZapiszWizyte.Content = "Znajdź wizytę";

                groupSzukaj.Visibility = Visibility.Visible;
                groupWizyta.Visibility = Visibility.Collapsed;
                chbListaZamknieta.Visibility = Visibility.Collapsed;
                labListaZamknieta.Visibility = Visibility.Collapsed;

                #endregion
            }
            else if (parametrStronaPacjent_Wizyta == 1)                                 //(1) Do ramki 'ramkaWizyta' zostanie załadowany formularz 'StronaPacjent_Dane' w trybie DODANIA pacjenta do bazy
            {
                #region ramkaWizyta = StronaPacjent_Dane(1)

                insert = true;
                update = false;
                ramkaWizyta.Navigate(new StronaPacjent_Dane(insert, update, IDPacjenta, 0));
                groupSzukaj.Visibility = Visibility.Visible;
                groupWizyta.Visibility = Visibility.Collapsed;
                chbListaZamknieta.Visibility = Visibility.Collapsed;
                labListaZamknieta.Visibility = Visibility.Collapsed;
                btnZapiszWizyte.Content = "Ustaw wizytę";

                #endregion
            }

            #endregion
        }
        #region METODY

        private void btnDodajUsunGodzineWizyty_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji

            //Funcja jest aktywowana w przypdaku klikniecia przyciku umieszczonego w gridzie 'dataGridWizyty' o nazwie "DODAJ" lub "USUN"
            //Nazwa przycisku jest zmienna: 
            // - napis "DODAJ" pojawia sie jak nie ma jeszzcze pacjenta z umuwiona wizyta. Po kliknieciu tego przyciku pojawia sie nowy formularz 'StronaPacjent_Szukaj' w RAMCE 'ramkaWizyta'
            // - napis "USUN" pojawia sie po przypisaniu godziny wizyty dla wybranego pacjenta. Po kliknieciu nastepuje usuniecie rekordu z tabeli 'tabWizyta'

            #endregion
            Button btn = sender as Button;
            if (btn.Content.ToString() == "Dodaj")
            {
                #region Wyswietlenie w RAMCE 'ramkaWizyta' formularza 'StronaPacjent_Szukaj' celem wyszukania osoby do zarejestrowania

                //POBRANIE DANYCH Z ZAZNACZONEGO WIERSZA DATAGRIDA 'dataGridWizyty'
                Pacjent pacjent = dataGridWizyty.SelectedItem as Pacjent;
                if (ramkaWizyta.Content == null || ramkaWizyta.Content.ToString() == "")
                {
                    ramkaWizyta.Navigate(listaStron[0]);    //[0] StronaPacjent_Szukaj(1) -  standardowe wyszukiwanie
                    groupSzukaj.Visibility = Visibility.Visible;
                    dataGridWizyty.RowBackground = Brushes.White;
                    dataGridWizyty.AlternatingRowBackground = Brushes.White;
                }
                else
                {
                    //btnZapiszWizyte.IsEnabled = true;
                }

                #endregion
            }
            else //btnZapiszWizyte.Content=="Usuń"
            {
                #region DELETE - Usunięcie wizyty z listy pacjentów

                Pacjent pacjent = dataGridWizyty.SelectedItem as Pacjent;
                try
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                    {
                        string IDWizyta = pacjent.IDWizyta;
                        query = "DELETE FROM tabWizyta WHERE (IDWizyta = 1045)";
                        query = "DELETE FROM tabWizyta WHERE (IDWizyta = " + IDWizyta + ")";
                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@IDWizyta", IDWizyta);
                        con.Open();
                        cmd.ExecuteNonQuery();

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                WyczyscRamke();
                PobierzGodzineOtwarciaGabinetu();
                labUwagiWizyta.Content = "Usunięto wizytę z godz: " + pacjent.GodzinaWizyty.ToString();
                labUwagiWizyta.Foreground = Brushes.Green;

                #endregion
            }
        }
        private void btnWyczyscRamke_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji
            //Funkcja jest aktwwowana
            #endregion
            WyczyscRamke();
            labUwagiWizyta.Content = "";

        }
        private void btnZapiszWizyte_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji
            //Funkcja jest wywolywana po wcisnieciu przycisku 'btnZapiszWizyte' w GroupBox 'groupSzukaj' formularza 'StronaPacjent_Wizyta'
            //W funkcji jest brany pod uwagę CONTENT przycisku
            //Po jego aktywowaniu jest robiony INSERT do tab 'tabWizyta' a nastepnie aktywowana funkcja 'PobierzGodzineOtwarciaGabinetu' celem uaktualnienie grida 'dataGridWizyty'formularza
            //Ramka formularza ładuje podstrone zaleznie od podanego argumentu dla strony głównej
            #endregion
            try
            {
                #region === UCHWYT - POBRANIE DANYCH Z AKTYWNEJ STRONY W RAMCE  ======================================
                if (ramkaWizyta.Content != null)
                {
                    aktywnaStrona = ramkaWizyta.Content as Page;
                    nazwaAktynejStrony = aktywnaStrona.ToString();
                }
                #endregion

                if (btnZapiszWizyte.Content.ToString() == "Ustaw wizytę")
                {
                    #region Wciśnieto przycisk w trybie "USTAW WIZYTĘ" - sprawdzenie i ustawienie warunków


                    if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Szukaj")
                    {
                        if (((StronaPacjent_Szukaj)(aktywnaStrona)).parametrStronaPacjent_Szukaj == 1 &             //z ramką z aktywną stroną StronaPacjent_Szukaj z parametrem formularza = 1 - Szukanie po uwagach 
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).btSzukaj.Content.ToString() == "Szukaj")        //bez wyszukania rekordów    
                        {
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Należy wyszukać uwagi lub wybrać konkretną.";
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                            return;
                        }
                        else //sprawdzenie czy jedno z pol wyszukiwania 'Dane pacjenta' jest wypełnione?  
                        {
                            if (((StronaPacjent_Szukaj)(aktywnaStrona)).gridDanePacjenta.Visibility == Visibility.Visible) //jest aktywny grid z danymi - to znaczy ze nie wybrano pacjenta
                            {
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Wybierz pacjenta.";        //uwaga w 'StronaPacjent_Dane'
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                                return; //wyjscie z funkcji
                            }
                            if (((StronaPacjent_Szukaj)(aktywnaStrona)).txtImie.Text == "" ||
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).txtNazwisko.Text == "" ||
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).txtPesel.Text == "" ||
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).txtTelefon.Text == "")
                            {
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Wypełnij jedno lub więcej szukanych danych.";        //uwaga w 'StronaPacjent_Dane'
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                                return; //wyjscie z funkcji
                            }
                        }
                    }
                    else if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Dane")     //'StronaPacjent_Dane'
                    {
                        //Czy pola 'Dane pacjenta' sa wypełnione?
                        if (((StronaPacjent_Dane)(aktywnaStrona)).txtImie.Text == "" &
                            ((StronaPacjent_Dane)(aktywnaStrona)).txtNazwisko.Text == "" &
                            ((StronaPacjent_Dane)(aktywnaStrona)).txtPesel.Text == "" &
                            ((StronaPacjent_Dane)(aktywnaStrona)).txtTelefon.Text == "")
                        {
                            ((StronaPacjent_Dane)(aktywnaStrona)).labUwagi.Content = "Wypełnij dane pacjenta.";        //uwaga w 'StronaPacjent_Dane'
                            return; //wyjscie z funkcji
                        }
                        //aktywowanie przycisku "Ustaw wizytę" dopiero po ZAPISANIU nowego pacjenta do bazy
                        if (((StronaPacjent_Dane)(aktywnaStrona)).btZapisz.IsEnabled == true & ((StronaPacjent_Dane)(aktywnaStrona)).btZapisz.Content.ToString() == "Zapisz")
                        {
                            MessageBox.Show("Zapisz dane pacjenat przed rejestacją wizyty", "Uwaga");
                            return;
                        }
                    }

                    btnZapiszWizyte.IsEnabled = true;
                    btnZapiszWizyte.Content = "Zarejestruj";
                    chbListaZamknieta.Visibility = Visibility.Visible;
                    labListaZamknieta.Visibility = Visibility.Visible;
                    groupWizyta.Visibility = Visibility.Visible;
                    dataGridWizyty.Columns[2].Visibility = Visibility.Collapsed;    //Ukrycie kolumny z przyciskami Dodaj/Usun grida

                    #endregion
                }
                else if (btnZapiszWizyte.Content.ToString() == "Zarejestruj") //btn ZAREJESTRUJ
                {
                    #region INSERT btn "ZAREJESTRUJ"

                    if (listaStron[0] != aktywnaStrona)                              //[0] 0 - parametr formularza 'StronaPacjent_Szukaj'    - formularz wyszukuje pacjentow po danych osobowych
                    {
                        IDPacjenta = ((StronaPacjent_Dane)(aktywnaStrona)).txtIDPacjenta.Text;
                        labUwagiWizyta.Content = IDPacjenta;

                        #region INSERT - DODANIE godziny wizyty

                        try
                        {
                            if (IDPacjenta.Length > 0 && dataGridWizyty.Items.Count > 0)                                                                              //wybrano juz pacjenta z formularza 'StronaPacjent_Dane' - sprawdzam po polu txtIDPacjenta
                            //if (((StronaPacjent_Dane)(aktywnaStrona)).txtIDPacjenta.Text.Length > 0) //wybrano pacjenta z formularza 'StronaPacjent_Dane' - sprawdzam po polu txtIDPacjenta
                            {
                                #region INSERT - ZAPISANIE GODZINY WIZYTY PACJENTA

                                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                                {
                                    Pacjent pacjent1 = dataGridWizyty.SelectedItem as Pacjent;
                                    string GodzinaWizyty = DateTime.ParseExact(pacjent1.GodzinaWizyty, "HH:mm", System.Globalization.CultureInfo.InvariantCulture).ToShortTimeString();
                                    query = "INSERT INTO tabWizyta (GodzinaWizyty, Status, FKPacjent,  FKWizyty) VALUES (CONVERT(DATETIME, '" + GodzinaWizyty + "', 102), 0, " + IDPacjenta + ", " + IDWizyty + ")";
                                    SqlCommand cmd = new SqlCommand(query, con);
                                    cmd.Parameters.AddWithValue("@GodzinaWizyty", GodzinaWizyty);
                                    cmd.Parameters.AddWithValue("@FKPacjent", IDPacjenta);
                                    cmd.Parameters.AddWithValue("@IDWizyty", IDWizyty);
                                    con.Open();
                                    cmd.ExecuteNonQuery();
                                }

                                #endregion
                                PobierzGodzineOtwarciaGabinetu();
                            }
                            else //NIE WYBRANO DNIA WIZYTY
                            {
                                labUwagiWizyta.Content = "Wybierz dzień wizyty pacjenta!";
                                labUwagiWizyta.Foreground = Brushes.Red;
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        #endregion
                        WyczyscRamke();
                        labUwagiWizyta.Content = "Zapisano wizytę.";
                        labUwagiWizyta.Foreground = Brushes.Green;
                        dataGridWizyty.Columns[2].Visibility = Visibility.Visible;
                    }
                    else  //NIE WYBRANO PACJENTA
                    {
                        if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Szukaj")
                        {
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Wybierz pacjenta.";        //uwaga w 'StronaPacjent_Dane'
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                            return; //wyjscie z funkcji
                        }
                    }

                    #endregion
                }
                else    //btnZapiszWizyte.Content = "Znajdź wizytę"
                {
                    if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Dane")
                    {
                        #region SELECT  wyszukujący pacjentów z uzyciem View_Pacjent_Wizyta

                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            try
                            {
                                IDPacjenta = ((StronaPacjent_Dane)(aktywnaStrona)).txtIDPacjenta.Text;
                                DataTable dt = new DataTable();

                                query = "SELECT * FROM View_Pacjent_Wizyta WHERE IDPacjent = " + @IDPacjenta;
                                SqlCommand cmd = new SqlCommand(query, con);
                                cmd.Parameters.AddWithValue("@IDPacjenta", IDPacjenta);
                                con.Open();
                                SqlDataReader reader = cmd.ExecuteReader();
                                dt.Load(reader);
                                dgWizyty.ItemsSource = null;
                                dgWizyty.Items.Refresh();
                                dgWizyty.ItemsSource = dt.DefaultView;
                                dgWizyty.Columns[5].Visibility = Visibility.Collapsed;    //Ukrycie kolumny IDPacjent
                                if (dt.Rows.Count > 0)
                                {
                                    #region Formatowanie strony

                                    groupWizyta.Visibility = Visibility.Visible;
                                    dataGridWizyty.Visibility = Visibility.Collapsed;
                                    labDzienWizyty.Visibility = Visibility.Collapsed;
                                    txtGodzOtwarcia.Visibility = Visibility.Collapsed;
                                    txtGodzZamkniecia.Visibility = Visibility.Collapsed;
                                    dgWizyty.Visibility = Visibility.Visible;
                                    btnWstawGodzine.Visibility = Visibility.Collapsed;
                                    chbListaZamknieta.Visibility = Visibility.Hidden;
                                    labListaZamknieta.Visibility = Visibility.Hidden;
                                    dataGridWizyty.Columns[2].Visibility = Visibility.Visible;    //Odkrycie kolumny z przyciskami Dodaj/Usun grida
                                    labUwagiWizyta.Content = "Rejestr wizyt szukanego pacjenta";
                                    labUwagiWizyta.Foreground = Brushes.Blue;

                                    #endregion
                                }
                                else
                                {
                                    if (groupWizyta.Visibility == Visibility.Collapsed) //wyswietlenie uwagi po stronie groupSzukaj w zakladce StronaPacjent_Dane
                                    {
                                        ((StronaPacjent_Dane)(aktywnaStrona)).labUwagi.Content = "Nie znaleziono rezerwacji";
                                        ((StronaPacjent_Dane)(aktywnaStrona)).labUwagi.Foreground = Brushes.Blue;
                                    }
                                    else    // //wyswietlenie uwgi po stronie groupWizyta w zakladce StronaPacjent_Wizyta
                                    {
                                        labUwagiWizyta.Content = "Nie znaleziono rezerwacji";
                                        labUwagiWizyta.Foreground = Brushes.Blue;
                                    }
                                }
                            }
                            catch (System.Data.SqlClient.SqlException ex)
                            {
                                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        #endregion
                    }
                    else  //NIE WYBRANO PACJENTA
                    {
                        if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Szukaj")
                        {
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Wybierz pacjenta.";        //uwaga w 'StronaPacjent_Dane'
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                            return; //wyjscie z funkcji
                        }
                    }
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void btnWstawGodzine_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji

            //Funkcja:
            // - tworzy date i godzinę otwarcia gabinetu w tabeli 'tabWizyty' celem pozniejszego umowienia wizyty pacjenta (region INSERT)
            // - poprawia godzinę otwarcia gabinetu (region UPDATE)
            // - wywołuje funkcje: PobierzListeWizytPacjentow() - odpowiedzialna za tworzenie listy pacjentów 'listaWizytyPacjentow'
            // - aktualizuje liste wolnych dni do rejestracji  - funkcja: pobierzOtwarteWizyt()

            #endregion

            try
            {
                #region INSERT, UPDATE - godziny otwarcia gabinetu


                if (txtGodzOtwarcia.Text.Length == 0 || txtGodzZamkniecia.Text.Length == 0 || txtGodzOtwarcia.Text == ":" || txtGodzZamkniecia.Text == ":")
                {
                    #region Nie wybrano dnia otwarcia gainetu z kalendarza lub nie wprowadzono godziny otwarcia gainetu

                    labUwagiWizyta.Content = "Wypełnij godziny pracy gabinetu.";
                    labUwagiWizyta.Foreground = Brushes.Red;
                    txtGodzOtwarcia.Text = ":";
                    txtGodzZamkniecia.Text = "";
                    txtGodzOtwarcia.Focus();

                    #endregion
                }
                else
                {
                    if (Convert.ToDateTime(txtGodzOtwarcia.Text) > Convert.ToDateTime(txtGodzZamkniecia.Text))
                    {
                        labUwagiWizyta.Content = "Godzina zamknięcia jest mniejsza od godziny otwarcia!";
                        labUwagiWizyta.Foreground = Brushes.Red;
                    }
                    else
                    {
                        if (btnWstawGodzine.Content.ToString() == "Wstaw godzinę")                          //INSERT - Gdy napis na przycisku ma 'Wstaw godzinę' 
                        {
                            #region INSERT - Godziny otwarcia gabinetu do 'tabWizyty' dnia wybranego z kalendarza

                            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                            {
                                godzOtwarcia = txtGodzOtwarcia.Text;                                        //Podstawiam do zmiennej Query godziny pracy gabinetu z txtboxa
                                godzZamkniecia = txtGodzZamkniecia.Text;
                                //=== INSERT tabWizyty ====
                                SqlCommand cmd = new SqlCommand("spTabWizyty_Insert_ScopeID", con);
                                cmd.CommandType = CommandType.StoredProcedure;                              //podaje typ wywolywanej komendy (StoredProcedure)
                                cmd.Parameters.AddWithValue("@DataWizyty", dataWizyty);                     //przypisuje parametry procedury - zgodne z zapisem w QL
                                cmd.Parameters.AddWithValue("@GodzinaOtwarcia", txtGodzOtwarcia.Text);
                                cmd.Parameters.AddWithValue("@GodzinaZamkniecia", txtGodzZamkniecia.Text);
                                cmd.Parameters.AddWithValue("@Interwal", txtInterwal.Text);
                                cmd.Parameters.AddWithValue("@LekProwadzacy", IDLekarza);

                                //OUTPUT parameter - zwraca IDWizyty po wywolaniu procedury INSERT
                                SqlParameter outputParameter = new SqlParameter();
                                outputParameter.ParameterName = "@IDWizyty";                                //nzawa parametru zgodna z zapisem w procedurze SQL
                                outputParameter.SqlDbType = SqlDbType.Int;                                  //przypisuje typ parametru
                                outputParameter.Direction = ParameterDirection.Output;                      //przypisuje kierunek parametru - OUTPUT (zewnetrzny)
                                                                                                            //Tworze parametry typu OUTPUT 
                                cmd.Parameters.Add(outputParameter);                                        //dodanie parametru do komendy cmd
                                con.Open();
                                cmd.ExecuteNonQuery();
                                //Pobieram IDWizyty dla wstawionej godziny otwarcia gabinetu celem jego wykorzystania przy generowaniu pustej listy przyjec pacjentow funkcji 'PobierzListeWizytPacjentow'
                                IDWizyty = outputParameter.Value.ToString();                                //przechwycenie parametru

                                //aktualizacja kontrolek formularza   
                                txtGodzOtwarcia.Visibility = Visibility.Hidden;
                                txtGodzZamkniecia.Visibility = Visibility.Hidden;
                                labUwagiWizyta.Content = "Wstawiono godziny pracy gabinetu.";
                                labUwagiWizyta.Foreground = Brushes.Green;
                                btnWstawGodzine.Content = "Popraw godzinę";
                                labDzienWizyty.Content = nazwaDnia.First().ToString().ToUpper() + nazwaDnia.Substring(1) + " " + txtGodzOtwarcia.Text + " - " + txtGodzZamkniecia.Text;
                            }

                            #endregion
                            PobierzListeWizytPacjentow();
                            pobierzOtwarteDniWizyt();
                        }
                        else
                        {
                            if (btnWstawGodzine.Content.ToString() == "Zmień")                          //UPDATE - Gdy napis na przycisku ma 'Zmień' 
                            {
                                #region UPDATE - poprawienie godziny otwarcia gabinetu w 'tabWizyty' dala dnia wybranego z kalendarza
                                if (godzOtwarcia != txtGodzOtwarcia.Text || godzZamkniecia != txtGodzZamkniecia.Text)                                                                   //Jak godzina z tetxtboxa jest rozna od godziny z tabeli - to robie UPDATE
                                {
                                    godzOtwarcia = txtGodzOtwarcia.Text;                                                                                                            //Podstawiam do zmiennej Query godziny z boxow
                                    godzZamkniecia = txtGodzZamkniecia.Text;
                                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                                    {
                                        try
                                        {
                                            #region UPDATE
                                            interwal = Convert.ToDouble(txtInterwal.Text, System.Globalization.CultureInfo.InvariantCulture);
                                            //UPDATE - Poprawa godziny otwarcia gabinetu do 'tabWizyty' dla wybranego dnia na kalendarzu
                                            query = "UPDATE tabWizyty SET GodzinaOtwarcia = CONVERT(DATETIME, '" + @godzOtwarcia + "', 102), GodzinaZamkniecia = CONVERT(DATETIME, '" + @godzZamkniecia + "', 102), Interwal = " + @interwal + " WHERE (DataWizyty = CONVERT(DATETIME, '" + @dataWizyty + "', 102)) AND LekProwadzacy = " + IDLekarza;
                                            SqlCommand cmd = new SqlCommand(query, con);
                                            cmd.Parameters.AddWithValue("@dataWizyty", dataWizyty);
                                            cmd.Parameters.AddWithValue("@godzOtwarcia", txtGodzOtwarcia.Text);
                                            cmd.Parameters.AddWithValue("@godzZamkniecia", txtGodzZamkniecia.Text);
                                            cmd.Parameters.AddWithValue("@interwal", txtInterwal.Text);
                                            con.Open();
                                            cmd.ExecuteNonQuery();

                                            #endregion
                                            #region Formatowanie formularza

                                            //aktualizacja kontrolek formularza  
                                            labUwagiWizyta.Content = "Poprawiono godzinę otwarcia gabinetu.";
                                            labUwagiWizyta.Foreground = Brushes.Green;
                                            txtGodzOtwarcia.Visibility = Visibility.Hidden;
                                            txtGodzOtwarcia.Height = 27;
                                            txtGodzZamkniecia.Visibility = Visibility.Hidden;
                                            txtGodzZamkniecia.Height = 27;
                                            btnWstawGodzine.Content = "Popraw godzinę";

                                            #endregion
                                            labDzienWizyty.Content = nazwaDnia.First().ToString().ToUpper() + nazwaDnia.Substring(1) + " " + txtGodzOtwarcia.Text + " - " + txtGodzZamkniecia.Text;
                                            PobierzListeWizytPacjentow();
                                        }
                                        catch
                                        {
                                            labUwagiWizyta.Content = "Wstaw poprawną godzine (HH:MM)";
                                            labUwagiWizyta.Foreground = Brushes.Red;
                                        }
                                    }
                                }
                                else                                                                                                       //Ggodzina z boxa jest TA SAMA co z tabeli 'tabWyzyty' - to nic nie robie
                                {
                                    #region Formatowanie formularza

                                    labUwagiWizyta.Content = "Nie zmieniono godziny otwarcia gabinetu!";
                                    labUwagiWizyta.Foreground = Brushes.Red;
                                    txtGodzOtwarcia.Visibility = Visibility.Hidden;
                                    txtGodzOtwarcia.Height = 27;

                                    txtGodzZamkniecia.Visibility = Visibility.Hidden;
                                    txtGodzZamkniecia.Height = 27;

                                    btnWstawGodzine.Content = "Popraw godzinę";

                                    #endregion
                                }
                                labDzienWizyty.Content = nazwaDnia.First().ToString().ToUpper() + nazwaDnia.Substring(1) + " " + txtGodzOtwarcia.Text + " - " + txtGodzZamkniecia.Text;
                                labDzienWizyty.Visibility = Visibility.Visible;

                                #endregion
                            }
                            else if (btnWstawGodzine.Content.ToString() == "Popraw godzinę")            //POPRAW GODZINE - Edycja formatek godzin otwarcia gabinetu
                            {
                                #region POPRAW GODZINĘ - aktualizacja kontrolek formularza

                                btnWstawGodzine.Content = "Zmień";      //zmiana nazwy przycisku
                                txtGodzOtwarcia.Visibility = Visibility.Visible;
                                txtGodzOtwarcia.Height = 40;
                                txtGodzOtwarcia.Focus();
                                txtGodzOtwarcia.SelectionStart = 0;
                                txtGodzOtwarcia.SelectionLength = txtGodzOtwarcia.Text.Length - 3;
                                labUwagiWizyta.Content = "Popraw godziny otwarcia gabinetu";
                                labUwagiWizyta.Foreground = Brushes.Blue;
                                txtGodzZamkniecia.Visibility = Visibility.Visible;
                                txtGodzZamkniecia.Height = 40;

                                #endregion
                            }
                        }
                    }

                }//else
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                //MessageBox.Show(ex.ToString());
            }

        }


        #region Funkcje nawigacji po otwartych dniach do rejestracji

        private void btnWolnyTerminPrawo_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji
            //Funkcja wyszukuje w tabeli 'dtListyOtwarte' najbliżego wolnego dnia wizyty (NIE ZAMKNIETGO) - DO PRZODU po tabeli
            //Zwróconą datę wstawiam do kalendarza (datePicker), ktory to po zmianie swojej zawartosci (SelectionChangedEventArgs) aktywuje funcje PobierzGodzineOtwarciaGabinetu i wypelnia DataGrida
            #endregion
            try
            {
                if (wiersz < dtListyOtwarte.Rows.Count - 1)                                         //nie przekroczono górnego zakresu tablicy. Tablica zaczyna się od 0.
                {
                    wiersz++;                                                                       //Przejscie do indeksu 0 lub wyżej dtListyOtwarte
                    string cellValue = dtListyOtwarte.Rows[wiersz][0].ToString();                   //pobranie daty po indeksie=wiersz 
                    datePicker.SelectedDate = DateTime.ParseExact(cellValue, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    MessageBox.Show("Brak wolnych terminów w późniejszym okresie.", "Wolne terminy rejestracji...", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void btnWolnyTerminLewo_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji
            //Funkcja wyszukuje w tabeli 'dtListyOtwarte' najbliżego wolnego dnia wizyty (NIE ZAMKNIETGO) - DO TYŁU (wstecz)
            //Zwróconą datę wstawiam do kalendarza (datePicker), ktory to po zmianie swojej zawartosci (SelectionChangedEventArgs) aktywuje funcje PobierzGodzineOtwarciaGabinetu i wypelnia DataGrida

            #endregion

            try
            {
                if (wiersz > 0)                                                                             //nie przekroczono dolnego zakresu tablicy. Tablica zaczyna się od 0.
                {
                    wiersz--;                                                                       //Przejscie do indeksu 0 lub wyżej dtListyOtwarte
                    string cellValue = dtListyOtwarte.Rows[wiersz][0].ToString();                   //pobranie daty po indeksie=wiersz 
                    datePicker.SelectedDate = DateTime.ParseExact(cellValue, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }

            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }
        private void btnWolnyTerminStart_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji
            //Funkcja wyszukuje w tabeli 'dtListyOtwarte' wolnego dnia wizyty (NIE ZAMKNIETGO) - W DNIU DZISIEJSZYM
            //Zwróconą datę wstawiam do kalendarza (datePicker), ktory to po zmianie swojej zawartosci (SelectionChangedEventArgs) aktywuje funcje PobierzGodzineOtwarciaGabinetu i wypelnia DataGrida

            #endregion
            try
            {
                if (IDLekarza != null)
                {
                    #region Przeszukanie listy 

                    for (int i = 0; i < dtListyOtwarte.Rows.Count; i++)
                    {
                        string szukanyDzien = dtListyOtwarte.Rows[i][0].ToString();                   //pobranie daty po indeksie=wiersz 
                        string dzisiaj = DateTime.Today.ToString();
                        if (szukanyDzien == dzisiaj)
                        {
                            datePicker.SelectedDate = DateTime.Today;                               //Przejście do dnia dzisiejszego
                            wiersz = 0;
                            return;
                        }
                    }

                    #endregion

                    //nie znaleziono dnia
                    MessageBox.Show("W dniu dzisiejszym gabinet nie jest otwarty.", "Wolne terminy...", MessageBoxButton.OK, MessageBoxImage.Information);
                    wiersz = -1;
                }
                else
                {
                    MessageBox.Show("Nie wybrano lekarza.", "Wolne terminy...", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void pobierzOtwarteDniWizyt()
        {
            #region Opis funkcji

            //Funkcja pobiera do tablicy dtListyOtwarte dni otwartego gabinetu, do których można zapisać wizyty

            #endregion
            try
            {
                dtListyOtwarte.Clear();
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("spTabWizyty_Select_OtwartyGabinet", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@LekProwadzacy ", IDLekarza);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            dtListyOtwarte.Load(rdr);
                        }

                    }
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion
        private void chbListaZamknieta_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji
            //Funkcja:
            // - pobiera dane z CheckBoxa 'chbListaZamknieta' i na jej podstawie aktualizuje pole 'ListaZamknieta' tabeli 'tabWizyty'
            // - pole 'ListaZamknieta' opisuje zamkniecie lub otwarcie listy przyjec pacjentow na dany dzien
            // - wywołuje funkcje: PobierzListeWizytPacjentow() - odpowiedzialna za tworzenie listy pacjentów 'listaWizytyPacjentow'
            // - aktualizuje liste wolnych dni do rejestracji  - funkcja: pobierzOtwarteWizyt()
            #endregion

            #region Parametry

            string zamkniete = "";          //dla @zamkniete
            //CheckBox cb = sender as CheckBox;
            //if (cb.IsChecked == false)
            if (chbListaZamknieta.IsChecked == false)
            {
                zamkniete = "0";
                dataGridWizyty.IsEnabled = true;
                labUwagiWizyta.Content = "";
                dataGridWizyty.Columns[3].Visibility = Visibility.Visible;
                if (chbLp.IsChecked == true)
                    dataGridWizyty.Columns[0].Visibility = Visibility.Visible;
                else
                    dataGridWizyty.Columns[0].Visibility = Visibility.Collapsed;

            }
            else
            {
                zamkniete = "1";
                dataGridWizyty.IsEnabled = false;
                dataGridWizyty.Columns[3].Visibility = Visibility.Collapsed;
                if (chbLp.IsChecked == true)
                    dataGridWizyty.Columns[0].Visibility = Visibility.Visible;
                else
                    dataGridWizyty.Columns[0].Visibility = Visibility.Collapsed;
            }

            #endregion

            #region UPDATE tabWizyty - Zamkniecie lub otwarcie listy przyjec pacjentow na dany dzien

            if (txtGodzOtwarcia.Text.Length > 4 && txtGodzZamkniecia.Text.Length > 4)
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    query = "";
                    query = "UPDATE tabWizyty SET ListaZamknieta = " + @zamkniete + " WHERE (IDWizyt=" + @IDWizyty + ")";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@zamkniete", zamkniete);
                    cmd.Parameters.AddWithValue("@IDWizyty", IDWizyty);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                //chbListaZamknieta.IsEnabled = false;
                //chbListaZamknieta.IsChecked = false;
            }
            #endregion
            PobierzListeWizytPacjentow();
            pobierzOtwarteDniWizyt();
        }
        private void chbLp_Click(object sender, RoutedEventArgs e)
        {
            if (chbLp.IsChecked == true)
                dataGridWizyty.Columns[0].Visibility = Visibility.Visible;
            else
                dataGridWizyty.Columns[0].Visibility = Visibility.Collapsed;
        }
        private void PobierzGodzineOtwarciaGabinetu()   //kalendarz
        {
            #region Opis funkcji
            //Metoda:
            // - pobiera godzine otwarcia gabinetu z 'tabWizyty' i wstawia do pola 'txtGodzOtwarcia' na podstawie daty otrzymanej z kalendarza
            // - pobiera godzine zamkiecia gabinetu z 'tabWizyty' i wstawia do pola 'txtGodzZamkniecia' na podstawie daty otrzymanej z kalendarza
            // - pobiera ListaZamknieta z 'tabWizyty' i wstawia do CheckBoxa
            // - formatuje opisy przycisku 'btnWstawGodzine'
            // - ustawia pole 'labDzienWizyty' z nazwa dnia i godzinami otwarci i zamkniecia gabinetu
            // - pobiera i sortuje liste przyjec pacjentow na dany dzien - przy uzyciu funkcji 'PobierzListePacjentow()'
            // - ukrywa dataGrida 'dataGridWizyty' przy braku godziny otwarcia gabinetu
            // Wywołanie:
            // - przy zmianie daty w kalendarzu
            #endregion

            dataWizyty = datePicker.SelectedDate.Value.ToShortDateString();
            nazwaDnia = System.Threading.Thread.CurrentThread.CurrentUICulture.DateTimeFormat.GetDayName(datePicker.SelectedDate.Value.DayOfWeek);
            labDzienWizyty.Content = nazwaDnia.First().ToString().ToUpper() + nazwaDnia.Substring(1);
            txtLiczbaPacjentow.Text = "";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                #region SELECT - Pobranie GODZIN PRZYJEC GABINETU dla danego dnia dla wybranego z kalendarza (po IDLekarza)

                query = "SELECT CAST(GodzinaOtwarcia as nvarchar(5)), CAST(GodzinaZamkniecia as nvarchar(5)), ListaZamknieta, IDWizyt " +
                    "FROM tabWizyty " +
                    "WHERE (DataWizyty = CONVERT(DATETIME, '" + @dataWizyty + "')) AND LekProwadzacy = " + IDLekarza;
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@dataWizyty", dataWizyty);
                con.Open();
                #endregion
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    if (rdr.HasRows)
                    {
                        #region Formatowanie formularza z danymi

                        dt.Load(rdr);
                        //ustawienie godziny otwarcia gabinetu
                        godzOtwarcia = dt.Rows[0][0].ToString();
                        txtGodzOtwarcia.Text = dt.Rows[0][0].ToString();
                        godzZamkniecia = dt.Rows[0][1].ToString();
                        txtGodzZamkniecia.Text = dt.Rows[0][1].ToString();
                        //ziana napisu przycisku
                        btnWstawGodzine.Content = "Popraw godzinę";
                        btnWstawGodzine.IsEnabled = true;
                        //ustawienie checkbox
                        chbListaZamknieta.IsChecked = (bool)Convert.ChangeType(dt.Rows[0][2], typeof(bool));     //Pobranie wartosci dla listy zamknietej

                        if (chbListaZamknieta.IsChecked == true)  //Ustawienie interwału w formularzu na "0" celem zablokowania wyswietlenia putych wizyty dla zamknietej listy
                        {
                            txtInterwal.Text = "0";
                            dataGridWizyty.IsEnabled = false;
                        }
                        else
                        {
                            dataGridWizyty.IsEnabled = true;
                        }

                        //ustawienie IDWizyty
                        IDWizyty = dt.Rows[0][3].ToString();
                        //Formatowanie formularza
                        txtGodzOtwarcia.Visibility = Visibility.Collapsed;
                        txtGodzOtwarcia.Height = 27;
                        txtGodzZamkniecia.Visibility = Visibility.Collapsed;
                        txtGodzOtwarcia.Height = 27;
                        labDzienWizyty.Content = nazwaDnia.First().ToString().ToUpper() + nazwaDnia.Substring(1) + "   " + txtGodzOtwarcia.Text + " - " + txtGodzZamkniecia.Text;
                        labUwagiWizyta.Content = "";

                        #endregion
                        #region Pobranie interwału z tabeli 'tabWizyty'

                        using (SqlConnection con2 = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            query = " SELECT Interwal FROM tabWizyty WHERE (IDWizyt = " + IDWizyty + ")";
                            SqlCommand cmd2 = new SqlCommand(query, con2);
                            con2.Open();
                            SqlDataReader reader2 = cmd2.ExecuteReader();
                            DataTable dt2 = new DataTable();
                            dt2.Load(reader2);
                            if (dt2.Rows.Count == 1)
                            {
                                txtInterwal.Text = dt2.Rows[0][0].ToString();
                                interwal = Convert.ToDouble(txtInterwal.Text, System.Globalization.CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                labUwagiWizyta.Content = "Brak interwału!";
                                labUwagiWizyta.Foreground = Brushes.Red;
                            }
                        }

                        #endregion
                        PobierzListeWizytPacjentow();
                    }
                    else    //Brak danych
                    {
                        #region Formatowanie formularza bez danymi - Dzień wizyty nie jest jeszcze ustawiony

                        txtGodzOtwarcia.Background = Brushes.White;
                        btnWstawGodzine.Content = "Wstaw godzinę";
                        btnWstawGodzine.IsEnabled = false;
                        txtGodzOtwarcia.Visibility = Visibility.Visible;
                        txtGodzOtwarcia.Text = ":";
                        txtGodzOtwarcia.Height = 45;
                        txtGodzZamkniecia.Text = "";
                        txtGodzZamkniecia.Height = 45;
                        labUwagiWizyta.Content = "Wpisz godziny pracy gabinetu:";
                        labUwagiWizyta.Foreground = Brushes.Blue;
                        chbListaZamknieta.IsChecked = false;
                        godzOtwarcia = "";
                        godzZamkniecia = "";
                        //czyszczenie i ukrycie dataGrida
                        listaWizytyPacjentow.Clear();
                        dataGridWizyty.ItemsSource = null;
                        dataGridWizyty.Items.Refresh();
                        dataGridWizyty.Visibility = Visibility.Collapsed;                       //UKRYCIE dataGrida
                        dgWizyty.Visibility = Visibility.Collapsed;                             //UKRYCIE dataGrida

                        #endregion
                    }
                }
            }
        }
        private void PobierzListeWizytPacjentow()
        {
            if (txtGodzOtwarcia.Text.Length > 0 & txtGodzZamkniecia.Text.Length > 0)    //Są dane
            {
                listaWizytyPacjentow.Clear();
                #region Pobranie i sformatowaie godzin otwarcia gabinetu


                if (txtGodzOtwarcia.Text.Length == 4)
                    godzOtwarciaGabinetu = DateTime.ParseExact(txtGodzOtwarcia.Text, "H:mm", System.Globalization.CultureInfo.InvariantCulture);
                else if (txtGodzOtwarcia.Text.Length == 5)
                    godzOtwarciaGabinetu = DateTime.ParseExact(txtGodzOtwarcia.Text, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                if (txtGodzZamkniecia.Text.Length == 4)
                    godzZamknięciaGabinetu = DateTime.ParseExact(txtGodzZamkniecia.Text, "H:mm", System.Globalization.CultureInfo.InvariantCulture);
                else if (txtGodzZamkniecia.Text.Length == 5)
                    godzZamknięciaGabinetu = DateTime.ParseExact(txtGodzZamkniecia.Text, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);


                #endregion
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    #region SELECT - Pobranie listy oacjentów z formularza

                    query = " SELECT tabWizyta.IDWizyta, tabWizyta.GodzinaWizyty, tabWizyta.Status, tabWizyta.FKWizyty, tabPacjent.Imie, tabPacjent.Nazwisko " +
                            "FROM tabPacjent " +
                            "INNER JOIN tabWizyta " +
                            "ON tabPacjent.IDPacjent = tabWizyta.FKPacjent " +
                            "RIGHT OUTER JOIN tabWizyty ON tabWizyta.FKWizyty = tabWizyty.IDWizyt " +
                            "WHERE (tabWizyta.FKWizyty = " + IDWizyty + ")";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@IDWizyty", IDWizyty);
                    con.Open();

                    #endregion
                    #region Utworzenie listy pacjentów 'listaWizytyPacjentow'

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        //Zaladowanie tabeli danymi pacjentow
                        DataTable dtWizytyPacjentow = new DataTable();
                        if (rdr.HasRows)
                        {
                            dtWizytyPacjentow.Load(rdr);
                            listaWizytyPacjentow.Clear();
                            //SORTOWANIE dtWizytyPacjentow po godzinach celem prawidlowego dodania LP
                            DataView dv = new DataView(dtWizytyPacjentow);
                            dv.Sort = "GodzinaWizyty ASC";
                            dtWizytyPacjentow = dv.ToTable();

                            for (int i = 0; i < dtWizytyPacjentow.Rows.Count; i++)
                            {
                                Pacjent pacjent = new Pacjent(
                                    Convert.ToString(i + 1),    // Lp
                                    dtWizytyPacjentow.Rows[i][0].ToString(),    // IDWizyta 
                                    dtWizytyPacjentow.Rows[i][1].ToString(),    // GodzinaWizyty
                                    dtWizytyPacjentow.Rows[i][2].ToString(),    // Status
                                    dtWizytyPacjentow.Rows[i][3].ToString(),    // FKWizyty
                                    dtWizytyPacjentow.Rows[i][4].ToString(),    // Imie
                                    dtWizytyPacjentow.Rows[i][5].ToString());   // Nazwisko

                                listaWizytyPacjentow.Add(pacjent);
                            }
                        }

                        if (dtWizytyPacjentow.Rows.Count > 0)
                            txtLiczbaPacjentow.Text = Convert.ToString(dtWizytyPacjentow.Rows.Count);
                        else
                            txtLiczbaPacjentow.Text = "0";
                    }
                    #endregion
                }
                #region Dodanie pustej listy pacjentów 'listaPustych'

                List<Pacjent> listaPustych = new List<Pacjent>();
                DateTime godzinaPusta = godzOtwarciaGabinetu;
                double interwalTemp;
                interwalTemp = Convert.ToDouble(txtInterwal.Text, System.Globalization.CultureInfo.InvariantCulture);
                if (txtInterwal.Text.Trim() != "0" && chbListaZamknieta.IsChecked == false)
                {
                    while (godzinaPusta < godzZamknięciaGabinetu)
                    {
                        bool jestPowtorzenie = false;
                        for (int i = 0; i < listaWizytyPacjentow.Count; i++)
                        {
                            if (listaWizytyPacjentow[i].GodzinaWizyty == godzinaPusta.ToShortTimeString())
                            {
                                jestPowtorzenie = true;
                                break;
                            }
                        }
                        if (!jestPowtorzenie) //czyli jest juz umówiony pacjent o tej godzinie
                        {
                            //Dodanie
                            Pacjent pacjent = new Pacjent(godzinaPusta.ToShortTimeString())
                            {
                                OpisPrzycisku = "Dodaj"
                            };
                            listaPustych.Add(pacjent);
                            jestPowtorzenie = false;
                        }
                        godzinaPusta = godzinaPusta.AddMinutes(interwalTemp);
                    }
                }
                else
                {
                    labUwagiWizyta.Content = "Wykaz tylko zarejestrowanych pacjentów.";
                    labUwagiWizyta.Foreground = Brushes.Blue;
                }

                //Polaczenie list przed wyswietleniem w dataGrid
                listaWizytyPacjentow.AddRange(listaPustych);
                //SORTOWANIE listy godzin z pomocą LINQ
                listaWizytyPacjentow = listaWizytyPacjentow.OrderBy(x => x.GodzinaWizyty).Cast<Pacjent>().ToList();

                #endregion
                #region Wypełnienie Datagrida wizytami


                if (listaWizytyPacjentow.Count > 0)
                    txtLiczbaPacjentow.Text = txtLiczbaPacjentow.Text + "/" + Convert.ToString(listaWizytyPacjentow.Count);
                else
                    txtLiczbaPacjentow.Text = txtLiczbaPacjentow.Text + "/0";

                dataGridWizyty.ItemsSource = null;
                dataGridWizyty.ItemsSource = listaWizytyPacjentow;
                dataGridWizyty.Items.Refresh();

                dataGridWizyty.Visibility = Visibility.Visible;
                dgWizyty.Visibility = Visibility.Hidden;

                #endregion
            }
            else //Brak danych
            {
                //chbListaZamknieta.IsEnabled = false;
                dataGridWizyty.Visibility = Visibility.Collapsed;
            }
        }
        private void PoprawGodzineWizytyGrid(object sender, DataGridCellEditEndingEventArgs e)
        {
            #region Opis funkcji
            //Funkcja jest aktywowoany w przypadku zmiany zawarosci komórki 'GodzinaWizyty' w gridzie 'dataGridWizyty' (po wyjsciu z komórki - 'CellEditEndingEvent')
            //W wyniku jej działania jes poprawiana tylko godzina wizyty pacjenta 'GodzinaWizyty' w tabeli 'tabWizyta'
            //W przypadku braku zmiany godziny lub nieprawidlowego jej podania - jest przypisana odpowienia informacja
            #endregion

            //POBRANIE ZMIENIONEGO TEKSTU z KOMORKI dataGrid
            string GodzinaPoprawiona = ((TextBox)(e.EditingElement)).Text;
            TextBox tb = sender as TextBox;
            
            #region UPDATE - POPRAWIENIE GODZINY WIZYTY PACJENTA w 'tabWizyta'

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                try
                {
                    Pacjent pacjent = (Pacjent)dataGridWizyty.SelectedItem;
                    string IDWizyta = pacjent.IDWizyta;
                    if (IDWizyta != null && GodzinaPoprawiona != pacjent.GodzinaWizyty.ToString())
                    {
                        //query = " UPDATE tabWizyta SET GodzinaWizyty = CONVERT(DATETIME, '2018-01-29 13:30:00', 102) WHERE (IDWizyta = 17)";
                        query = " UPDATE tabWizyta SET GodzinaWizyty = CONVERT(DATETIME, '" + GodzinaPoprawiona + "', 102) WHERE (IDWizyta = " + IDWizyta + ")";
                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@GodzinaPoprawiona", GodzinaPoprawiona);
                        con.Open();
                        cmd.ExecuteNonQuery();
                        labUwagiWizyta.Content = "Poprawiono godzinę wizyty pacjenta na: " + GodzinaPoprawiona;
                        labUwagiWizyta.Foreground = Brushes.Green;
                    }
                    else
                    {
                        if (IDWizyta != null)
                        {
                            labUwagiWizyta.Content = "Nie dokonano zmian";
                            labUwagiWizyta.Foreground = Brushes.Blue;
                        }
                        else
                        {
                            labUwagiWizyta.Content = "Brak pacjenta";
                            labUwagiWizyta.Foreground = Brushes.Red;
                        }
                    }
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    if (ex.Number == 242) MessageBox.Show("Wpisz poprawną godzinę wizyty pacjenta!");
                    else MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            #endregion
            PobierzGodzineOtwarciaGabinetu();
            labUwagiWizyta.Content = "Poprawiono godzinę wizyty.";
            labUwagiWizyta.Foreground = Brushes.Green;
        }
        private void WyczyscRamke()
        {
            ramkaWizyta.Content = null;
            while (ramkaWizyta.NavigationService.RemoveBackEntry() != null) ;
            groupSzukaj.Visibility = Visibility.Collapsed;
        }
        public void ClearHistory()
        {
            ramkaWizyta.Content = null;
            while (ramkaWizyta.NavigationService.RemoveBackEntry() != null) ;
        }
        private void txtInterwal_KeyDown(object sender, KeyEventArgs e)
        {
            #region Opis funkcji
            //Funkca jest aktywowana w przypadku zmiany interwału w 'txtInterwal'
            //W wyniku zmiany interwału - nastepuje ponowne pobranie listy pacjentów oraz listy pustych wizyt i scalenie ich w liscie 'PobierzListeWizytPacjentow'
            //INTERWAL - odstep czasu pomiedzy kolejnymi wizytami pacjentow, podawana w minutach [min]
            #endregion

            if (e.Key == Key.Return)
            {
                if (txtInterwal.Text != "")
                {
                    if (Convert.ToInt16(txtInterwal.Text) >= 5 & IDWizyty.Length > 0 & Convert.ToString(interwal) != txtInterwal.Text & Convert.ToInt16(txtInterwal.Text) < 60 || txtInterwal.Text.Trim() == "0")             //liczy jezeli czas wizyty pacjenta jest >- 5 min
                    {
                        labUwagiWizyta.Content = "";
                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            try
                            {
                                interwal = Convert.ToDouble(txtInterwal.Text, System.Globalization.CultureInfo.InvariantCulture);
                                #region UPDATE INTERWAŁ

                                //UPDATE - Poprawa godziny otwarcia gabinetu do 'tabWizyty' dla wybranego dnia na kalendarzu
                                query = "UPDATE tabWizyty SET Interwal = " + @interwal + " WHERE (DataWizyty = CONVERT(DATETIME, '" + @dataWizyty + "', 102))";
                                SqlCommand cmd = new SqlCommand(query, con);
                                cmd.Parameters.AddWithValue("@dataWizyty", dataWizyty);
                                cmd.Parameters.AddWithValue("@interwal", txtInterwal.Text);
                                con.Open();
                                cmd.ExecuteNonQuery();

                                labUwagiWizyta.Content = "Poprawiono planowaną długość wizyty.";
                                labUwagiWizyta.Foreground = Brushes.Blue;

                                #endregion
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            PobierzListeWizytPacjentow();
                        }
                    }
                    else
                    {
                        if (IDWizyty.Length == 0)
                            labUwagiWizyta.Content = "Aby zmienić interwał, należy najpierw wybrać i dzień wizyty.";
                        else if (Convert.ToInt16(txtInterwal.Text) < 5 & IDWizyty.Length > 0)
                            labUwagiWizyta.Content = "Czas wizyty jes mniejszy od 5 min. Popraw interwał.";
                        else if (Convert.ToInt16(txtInterwal.Text) > 59 & IDWizyty.Length > 0)
                            labUwagiWizyta.Content = "Czas wizyty musi być mniejszy od 1 godz. Popraw interwał.";
                        else if (Convert.ToString(interwal) == txtInterwal.Text)
                            labUwagiWizyta.Content = "Nie dokonano zmiany gdyż interwały są identyczne.";
                        labUwagiWizyta.Foreground = Brushes.Red;
                    }
                }
                else
                {
                    labUwagiWizyta.Content = "Interwał musi być większy od 4 min";
                }
            }
        }
        private void datePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (IDLekarza != null)
                {
                    if (ramkaWizyta.Content != null)
                    {
                        aktywnaStrona = ramkaWizyta.Content as Page;
                        nazwaAktynejStrony = aktywnaStrona.ToString();
                        if (btnZapiszWizyte.Content.ToString() != "Znajdź wizytę")   //Ukrycie kolumny z przyciskami Dodaj/Usun grida WYSZUKUJACEGO WIZYTY
                        {
                            dataGridWizyty.Columns[1].Visibility = Visibility.Visible;
                            dataGridWizyty.Columns[2].Visibility = Visibility.Visible;

                        }
                    }
                    PobierzGodzineOtwarciaGabinetu();
                    btnWstawGodzine.IsEnabled = true;
                    btnWstawGodzine.IsEnabled = true;
                    btnWstawGodzine.Visibility = Visibility.Visible;
                    chbListaZamknieta.Visibility = Visibility.Visible;
                    labListaZamknieta.Visibility = Visibility.Visible;
                }
                chbListaZamknieta_Click(sender, e);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                MessageBox.Show(ex.ToString(), "Błąd");
            }
        }
        private void txtGodzOtwarcia_GotFocus(object sender, RoutedEventArgs e)
        {
            txtGodzOtwarcia.Background = Brushes.White;
            labDzienWizyty.Content = "Godzina otwarcia:";
            if (txtGodzOtwarcia.Text == "" || txtGodzOtwarcia.Text == ":")
            {
                btnWstawGodzine.IsEnabled = false;
                txtGodzOtwarcia.Text = ":";
            }
        }
        private void txtGodzOtwarcia_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtGodzOtwarcia.Text == "" || txtGodzOtwarcia.Text == ":")
            {
                txtGodzOtwarcia.Text = "";
                labUwagiWizyta.Content = "Nie wpisano godziny otwarcia gabinetu!";
                labUwagiWizyta.Foreground = Brushes.Red;
                txtGodzOtwarcia.Background = Brushes.LightYellow;
                btnWstawGodzine.IsEnabled = false;
                return;
            }
            else if (txtGodzOtwarcia.Text.Length > 5 || txtGodzOtwarcia.Text.Length < 4 || (txtGodzOtwarcia.Text.Length == 5 && txtGodzOtwarcia.Text.IndexOf(":") != 2) || (txtGodzOtwarcia.Text.Length == 4 && txtGodzOtwarcia.Text.IndexOf(":") != 1)) //'LastIndexOf'sprawzam czy znak separatora godzin jest w prawidłpwym miejscu
            {
                labUwagiWizyta.Content = "Wprowadź prawidłową godzinę otwarcia gabinetu.";
                labUwagiWizyta.Foreground = Brushes.Red;
                txtGodzOtwarcia.SelectionStart = 0;
                txtGodzOtwarcia.Select(0, 0);
                txtGodzOtwarcia.Background = Brushes.LightYellow;
                btnWstawGodzine.IsEnabled = false;
                return;
            }
            else
            {
                txtGodzOtwarcia.Background = Brushes.LightGray;
                txtGodzZamkniecia.Visibility = Visibility.Visible;
                btnWstawGodzine.IsEnabled = true;
            }
        }
        private void txtGodzZamkniecia_GotFocus(object sender, RoutedEventArgs e)
        {
            txtGodzZamkniecia.Background = Brushes.White;
            labDzienWizyty.Content = "Godzina zamknięcia:";
            if (txtGodzZamkniecia.Text == "" || txtGodzZamkniecia.Text == ":")
            {
                txtGodzZamkniecia.Text = ":";
                btnWstawGodzine.IsEnabled = false;
            }
        }
        private void txtGodzZamkniecia_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtGodzZamkniecia.Text == ":")
            {
                txtGodzZamkniecia.Text = "";
                labUwagiWizyta.Content = "Wpisz godziny pracy gabinetu.";
                labUwagiWizyta.Foreground = Brushes.Red;
                txtGodzZamkniecia.Background = Brushes.LightYellow;
                btnWstawGodzine.IsEnabled = false;
                return;

            }
            else if (txtGodzZamkniecia.Text.Length > 5 || txtGodzZamkniecia.Text.Length < 4 || (txtGodzZamkniecia.Text.Length == 5 && txtGodzZamkniecia.Text.IndexOf(":") != 2) || (txtGodzZamkniecia.Text.Length == 4 && txtGodzZamkniecia.Text.IndexOf(":") != 1)) //'LastIndexOf'sprawzam czy znak separatora godzin jest w prawidłpwym miejscu
            {

                labUwagiWizyta.Content = "Wprowadź prawidłową godzinę zamknięcia gabinetu.";
                labUwagiWizyta.Foreground = Brushes.Red;
                txtGodzZamkniecia.Background = Brushes.LightYellow;
                btnWstawGodzine.IsEnabled = false;
                return;

            }
            btnWstawGodzine.IsEnabled = true;
            txtGodzZamkniecia.Background = Brushes.LightGray;
            labDzienWizyty.Content = "Czynne w godz:";
            labUwagiWizyta.Content = "";
        }
        private void cmbTyp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            #region Opis funkcji
            //Funcja sprawdza czy po wybraniu lekarza mozna wyswietlić godziny otwacia
            //Jezeli data kalendarza jest wybrana - to wywoluje funkcje wyswietlenia godzin dla danego lekarza
            //Item itm = (Item)cmbTyp.SelectedItem;
            #endregion

            KlLekarz klLekarz = (KlLekarz)cmbTyp.SelectedItem;
            IDLekarza = klLekarz.ID.ToString();

            if (datePicker.SelectedDate != null)

                PobierzGodzineOtwarciaGabinetu();
            else
                datePicker.IsEnabled = true;
        }
        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            Button btn = sender as Button;
            var typ = sender.GetType();
            var typ2 = e.OriginalSource.GetType();
            if (e.Key == Key.Return)
            {
                if (typ2.Name == "TextBox")
                {
                    TextBox textBox = e.OriginalSource as TextBox;
                    string nazwa = textBox.Name.ToString();

                    //Wciśnięto Enter w formularzu rejestracji wizyty
                    if (nazwa == "txtGodzOtwarcia" && txtGodzOtwarcia.Text != ":" && txtGodzOtwarcia.Text != "")                  //Wciśnięto enter w wypełnionym polu txtGodzOtwarcia
                    {
                        if (txtGodzZamkniecia.Text != ":" && txtGodzZamkniecia.Text != "")                                         //jeżeli pole txtGodzZamkniecia jest wypełnione
                        {
                            btnWstawGodzine.IsEnabled = true;
                            btnWstawGodzine.Focus();
                            btnWstawGodzine_Click(sender, e);                                                                       //wstawiam godzinę
                            dataGridWizyty.Focus();
                        }
                        else                                                                                                        //jeżeli pole txtGodzZamkniecia nie jest wypełnione       
                        {
                            txtGodzZamkniecia.Visibility = Visibility.Visible;
                            txtGodzZamkniecia.Focus();                                                                              //przechodzę do pola txtGodzZamkniecia
                        }
                    }
                    else if (nazwa == "txtGodzZamkniecia" && txtGodzZamkniecia.Text != ":" && txtGodzZamkniecia.Text != "")       //Wciśnięto enter w polu txtGodzZamkniecia z wypełnionymi polami: txtGodzOtwarcia i txtGodzZamkniecia
                    {
                        if (txtGodzOtwarcia.Text != ":" && txtGodzOtwarcia.Text != "")                                             //jeżeli pole txtGodzZamkniecia jest wypełnione
                        {
                            btnWstawGodzine.IsEnabled = true;
                            btnWstawGodzine.Focus();
                            btnWstawGodzine_Click(sender, e);                                                                       //wstawiam godzinę
                            dataGridWizyty.Focus();
                        }
                        else                                                                                                        //jeżeli pole txtGodzZamkniecia nie jest wypełnione       
                        {
                            txtGodzZamkniecia.Focus();                                                                              //przechodzę do pola txtGodzZamkniecia
                        }
                    }
                }
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            #region Załadowanie do ComboBox lekarzy

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                try
                {
                    query = "Select Imie, Nazwisko,  IDPacjent  from tabPacjent Where Typ=2 Order By Nazwisko";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        //Zaladowanie tabeli danymi pacjentow
                        DataTable dt = new DataTable();
                        if (rdr.HasRows)
                        {
                            dt.Load(rdr);

                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                cmbTyp.Items.Add(new KlLekarz(dt.Rows[i][0].ToString(), dt.Rows[i][1].ToString(), dt.Rows[i][2].ToString()));
                            }
                            //Ustawienie domyślnie wybranego lekarza
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                if (dt.Rows[i][1].ToString() == "Kowalski")
                                {
                                    cmbTyp.SelectedIndex = i;
                                }
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }

            #endregion

            //Załadowanie listy otwartch dni gabinetu
            dtListyOtwarte.Clear();
            pobierzOtwarteDniWizyt();

            // Formatowanie strony - formatuje wygląd strony w ramce w zalezności od trybu w jakim pracuje
            aktywnaStrona = ramkaWizyta.Content as Page;
            if (aktywnaStrona != null)
            {
                #region Formatowanie wygladu strony 'StronaPacjent_Szukaj'

                nazwaAktynejStrony = aktywnaStrona.ToString();
                if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Szukaj")
                {
                    if (((StronaPacjent_Szukaj)(aktywnaStrona)).parametrStronaPacjent_Szukaj == 0)                      //Szukanie po pacjencie
                    {
                        ((StronaPacjent_Szukaj)(aktywnaStrona)).labTytul.Content = "Podaj dane szukanego pacjenta";
                        ((StronaPacjent_Szukaj)(aktywnaStrona)).btOpcje.Visibility = Visibility.Visible;
                        ((StronaPacjent_Szukaj)(aktywnaStrona)).gbUwagi.Visibility = Visibility.Visible;
                    }
                    else if (((StronaPacjent_Szukaj)(aktywnaStrona)).parametrStronaPacjent_Szukaj == 1)                 //Szukanie po uwagach
                    {
                        ((StronaPacjent_Szukaj)(aktywnaStrona)).labTytul.Content = "Wyszukiwanie zapisanych uwag.";
                        ((StronaPacjent_Szukaj)(aktywnaStrona)).btOpcje.Visibility = Visibility.Collapsed;
                        ((StronaPacjent_Szukaj)(aktywnaStrona)).gbUwagi.Visibility = Visibility.Hidden;
                        btnWyczyscRamke.Visibility = Visibility.Collapsed;
                    }
                }
                #endregion
            }
        }

        #endregion
    }
}

