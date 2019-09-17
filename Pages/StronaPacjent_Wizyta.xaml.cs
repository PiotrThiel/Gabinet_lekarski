using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GabinetLekarski.Klasy;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace GabinetLekarski.Strony
{
    public partial class StronaPacjent_Wizyta : Page
    {
        private enum Widok
        {
            StronaPacjent_Szukaj_PoDanychOsobowych = 0,
            StronaPacjent_Dane_DodanieDoBazy = 1,
            StronaPacjent_Szukaj_PoPoUwagach = 2,
            RamkaUkryta = 3,
            StronaPacjent_Szukaj_Wizyty = 4
        }

        private enum Strona
        {
            Pacjent_Szukaj_PoDanychOsobowych = 0,
            Pacjent_Szukaj_PoPoUwagach = 1,
            Pacjent_Dane_WyswietlDane = 2
        }

        private enum KolWizyty
        {
            IDWizyta = 0,
            GodzinaWizyty = 1,
            Status = 2,
            FkWizyty = 3,
            Imie = 4,
            Nazwisko = 5
        }

        #region PARAMETRY

        bool insert;
        bool update;
        string query;
        string dataWizyty;
        string nazwaDnia;
        string godzOtwarcia;
        string godzZamkniecia;
        string IDWizyty;
        string IDPacjenta;
        string IDLekarza;
        string nazwaAktynejStrony;
        public int widokRamkiWizyty = -1;
        int wiersz = -1;
        double interwal = 30;                                               //INTERWAL - odstep czasu pomiedzy kolejnymi wizytami pacjentow, podawana w minutach [min]

        Page aktywnaStrona;
        List<Pacjent> listaWizytyPacjentow = new List<Pacjent>();
        DateTime godzOtwarciaGabinetu;
        DateTime godzZamknieciaGabinetu;
        DataTable dtListyOtwarte = new DataTable();
        public List<object> listaStron = new List<object>();

        #endregion
        #region Przykłady konwersji godziny ze stringu na format DateTime

        //datePicker.SelectedDate = DateTime.ParseExact("1972-01-09", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); 
        //DateTime godzX = DateTime.ParseExact(godzinaWizyty, "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        //DateTime godzX = DateTime.ParseExact(godzinaWizyty, "H:mm:ss", null, System.Globalization.DateTimeStyles.None);
        //DateTime godzX = DateTime.ParseExact(godzinaWizyty, "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToShortTimeString();
        //DateTime godzX = DateTime.ParseExact("2009-05-08 14:40:52,531", "yyyy-MM-dd HH:mm:ss,fff", System.Globalization.CultureInfo.InvariantCulture);
        //DateTime godzX = DateTime.ParseExact("14:40:52,531", "HH:mm:ss,fff", System.Globalization.CultureInfo.InvariantCulture);

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
            widokRamkiWizyty = parametrStrony;
            txtInterwal.Text = interwal.ToString();
            groupSzukaj.Visibility = Visibility.Collapsed;
            dataGridWizyty.Visibility = Visibility.Collapsed;
            dgWizyty.Visibility = Visibility.Collapsed;
            btnWstawGodzine.IsEnabled = false;

            listaStron.Add(new StronaPacjent_Szukaj(((int)Parametr_StronaPacjent_Szukaj.SzukajPoDanychOsobowych)));
            listaStron.Add(new StronaPacjent_Szukaj(((int)Parametr_StronaPacjent_Szukaj.SzukajPoUwagach)));
            listaStron.Add(new StronaPacjent_Dane(false, false, "", (int)Parametr_StronaPacjent_Dane.WyswietlDane));

            dataGridWizyty.Columns[0].Visibility = Visibility.Collapsed;

            #region Ustawienie zawartości ramki formularza 'ramkaWizyta'

            if (widokRamkiWizyty == (int)Widok.StronaPacjent_Szukaj_PoDanychOsobowych || widokRamkiWizyty == (int)Widok.StronaPacjent_Szukaj_PoPoUwagach || widokRamkiWizyty == (int)Widok.StronaPacjent_Szukaj_Wizyty)
            {
                if (widokRamkiWizyty == (int)Widok.StronaPacjent_Szukaj_PoDanychOsobowych || widokRamkiWizyty == (int)Widok.StronaPacjent_Szukaj_Wizyty)
                {
                    ramkaWizyta.Navigate(listaStron[(int)Strona.Pacjent_Szukaj_PoDanychOsobowych]);
                    btnWyczyscRamke.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ramkaWizyta.Navigate(listaStron[(int)Strona.Pacjent_Szukaj_PoPoUwagach]);
                }

                if (widokRamkiWizyty == (int)Widok.StronaPacjent_Szukaj_PoDanychOsobowych || widokRamkiWizyty == (int)Widok.StronaPacjent_Szukaj_PoPoUwagach)
                    btnZapiszWizyte.Content = "Ustaw wizytę";
                else
                    btnZapiszWizyte.Content = "Znajdź wizytę";

                groupSzukaj.Visibility = Visibility.Visible;
                groupWizyta.Visibility = Visibility.Collapsed;
                chbListaZamknieta.Visibility = Visibility.Collapsed;
                labListaZamknieta.Visibility = Visibility.Collapsed;
            }
            else if (widokRamkiWizyty == (int)Widok.StronaPacjent_Dane_DodanieDoBazy)
            {
                insert = true;
                update = false;
                ramkaWizyta.Navigate(new StronaPacjent_Dane(insert, update, IDPacjenta, (int)Parametr_StronaPacjent_Dane.WyswietlDane));
                groupSzukaj.Visibility = Visibility.Visible;
                groupWizyta.Visibility = Visibility.Collapsed;
                chbListaZamknieta.Visibility = Visibility.Collapsed;
                labListaZamknieta.Visibility = Visibility.Collapsed;
                btnZapiszWizyte.Content = "Ustaw wizytę";
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
            try
            {
                Button btn = sender as Button;
                if (btn.Content.ToString() == "Dodaj")
                {
                    #region Pacjent_Szukaj_PoDanychOsobowych

                    Pacjent pacjent = dataGridWizyty.SelectedItem as Pacjent;
                    if (ramkaWizyta.Content == null || ramkaWizyta.Content.ToString() == "")
                    {
                        ramkaWizyta.Navigate(listaStron[(int)Strona.Pacjent_Szukaj_PoDanychOsobowych]);
                        groupSzukaj.Visibility = Visibility.Visible;
                        dataGridWizyty.RowBackground = Brushes.White;
                        dataGridWizyty.AlternatingRowBackground = Brushes.White;
                    }

                    #endregion
                }
                else //btnZapiszWizyte.Content=="Usuń"
                {
                    #region DELETE

                    Pacjent pacjent = dataGridWizyty.SelectedItem as Pacjent;

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

                    WyczyscRamke();
                    PobierzGodzineOtwarciaGabinetu();
                    labUwagiWizyta.Content = "Usunięto wizytę z godz: " + pacjent.GodzinaWizyty.ToString();
                    labUwagiWizyta.Foreground = Brushes.Green;

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnWyczyscRamke_Click(object sender, RoutedEventArgs e)
        {
            #region Opis funkcji
            //Funkcja jest aktwwowana
            #endregion
            try
            {
                WyczyscRamke();
                labUwagiWizyta.Content = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
                //UCHWYT
                if (ramkaWizyta.Content != null)
                {
                    aktywnaStrona = ramkaWizyta.Content as Page;
                    nazwaAktynejStrony = aktywnaStrona.ToString();
                }

                if (btnZapiszWizyte.Content.ToString() == "Ustaw wizytę")
                {
                    #region USTAW WIZYTĘ

                    if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Szukaj")
                    {
                        if (((StronaPacjent_Szukaj)(aktywnaStrona)).parametrStronySzukaj == (int)Parametr_StronaPacjent_Szukaj.SzukajPoUwagach &
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).btSzukaj.Content.ToString() == "Szukaj")
                        {
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Należy wyszukać uwagi lub wybrać konkretną.";
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                            return;
                        }
                        else //sprawdzenie czy jedno z pol wyszukiwania 'Dane pacjenta' jest wypełnione?  
                        {
                            if (((StronaPacjent_Szukaj)(aktywnaStrona)).gridDanePacjenta.Visibility == Visibility.Visible)
                            {
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Wybierz pacjenta.";
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                                return;
                            }
                            if (((StronaPacjent_Szukaj)(aktywnaStrona)).txtImie.Text == "" ||
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).txtNazwisko.Text == "" ||
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).txtPesel.Text == "" ||
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).txtTelefon.Text == "")
                            {
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Wypełnij jedno lub więcej szukanych danych.";
                                ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                                return;
                            }
                        }
                    }
                    else if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Dane")
                    {
                        if (((StronaPacjent_Dane)(aktywnaStrona)).txtImie.Text == "" &
                            ((StronaPacjent_Dane)(aktywnaStrona)).txtNazwisko.Text == "" &
                            ((StronaPacjent_Dane)(aktywnaStrona)).txtPesel.Text == "" &
                            ((StronaPacjent_Dane)(aktywnaStrona)).txtTelefon.Text == "")
                        {
                            ((StronaPacjent_Dane)(aktywnaStrona)).labUwagi.Content = "Wypełnij dane pacjenta.";
                            return;
                        }

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
                    dataGridWizyty.Columns[2].Visibility = Visibility.Collapsed;

                    #endregion
                }
                else if (btnZapiszWizyte.Content.ToString() == "Zarejestruj")
                {
                    #region INSERT

                    if (listaStron[(int)Strona.Pacjent_Szukaj_PoDanychOsobowych] != aktywnaStrona)
                    {
                        IDPacjenta = ((StronaPacjent_Dane)(aktywnaStrona)).txtIDPacjenta.Text;
                        labUwagiWizyta.Content = IDPacjenta;

                        #region INSERT - dodanie godziny wizyty

                        if (IDPacjenta.Length > 0 && dataGridWizyty.Items.Count > 0)
                        {
                            #region Zapisanie godziny wizyty pacjenta

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
                        else //nie wybrano dnia wizyty
                        {
                            labUwagiWizyta.Content = "Wybierz dzień wizyty pacjenta!";
                            labUwagiWizyta.Foreground = Brushes.Red;
                            return;
                        }

                        #endregion
                        WyczyscRamke();
                        labUwagiWizyta.Content = "Zapisano wizytę.";
                        labUwagiWizyta.Foreground = Brushes.Green;
                        dataGridWizyty.Columns[2].Visibility = Visibility.Visible;
                    }
                    else  //nie wybrano pacjenta
                    {
                        if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Szukaj")
                        {
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Wybierz pacjenta.";
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                            return;
                        }
                    }

                    #endregion
                }
                else    //btnZapiszWizyte.Content = "Znajdź wizytę"
                {
                    if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Dane")
                    {
                        #region SELECT

                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
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
                            dgWizyty.Columns[5].Visibility = Visibility.Collapsed;
                            if (dt.Rows.Count > 0)
                            {
                                groupWizyta.Visibility = Visibility.Visible;
                                dataGridWizyty.Visibility = Visibility.Collapsed;
                                labDzienWizyty.Visibility = Visibility.Collapsed;
                                txtGodzOtwarcia.Visibility = Visibility.Collapsed;
                                txtGodzZamkniecia.Visibility = Visibility.Collapsed;
                                dgWizyty.Visibility = Visibility.Visible;
                                btnWstawGodzine.Visibility = Visibility.Collapsed;
                                chbListaZamknieta.Visibility = Visibility.Hidden;
                                labListaZamknieta.Visibility = Visibility.Hidden;
                                dataGridWizyty.Columns[2].Visibility = Visibility.Visible;
                                labUwagiWizyta.Content = "Rejestr wizyt szukanego pacjenta";
                                labUwagiWizyta.Foreground = Brushes.Blue;
                            }
                            else
                            {
                                if (groupWizyta.Visibility == Visibility.Collapsed)
                                {
                                    ((StronaPacjent_Dane)(aktywnaStrona)).labUwagi.Content = "Nie znaleziono rezerwacji";
                                    ((StronaPacjent_Dane)(aktywnaStrona)).labUwagi.Foreground = Brushes.Blue;
                                }
                                else
                                {
                                    labUwagiWizyta.Content = "Nie znaleziono rezerwacji";
                                    labUwagiWizyta.Foreground = Brushes.Blue;
                                }
                            }
                        }
                        #endregion
                    }
                    else  //nie wybrano pacjenta
                    {
                        if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Szukaj")
                        {
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Content = "Wybierz pacjenta.";
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labUwagi.Foreground = Brushes.Red;
                            return;
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
                if (txtGodzOtwarcia.Text.Length == 0 || txtGodzZamkniecia.Text.Length == 0 || txtGodzOtwarcia.Text == ":" || txtGodzZamkniecia.Text == ":")
                {
                    labUwagiWizyta.Content = "Wypełnij godziny pracy gabinetu.";
                    labUwagiWizyta.Foreground = Brushes.Red;
                    txtGodzOtwarcia.Text = ":";
                    txtGodzZamkniecia.Text = "";
                    txtGodzOtwarcia.Focus();
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
                        if (btnWstawGodzine.Content.ToString() == "Wstaw godzinę")
                        {
                            #region INSERT

                            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                            {
                                godzOtwarcia = txtGodzOtwarcia.Text;
                                godzZamkniecia = txtGodzZamkniecia.Text;
                                //=== INSERT tabWizyty ====
                                SqlCommand cmd = new SqlCommand("spTabWizyty_Insert_ScopeID", con);
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@DataWizyty", dataWizyty);
                                cmd.Parameters.AddWithValue("@GodzinaOtwarcia", txtGodzOtwarcia.Text);
                                cmd.Parameters.AddWithValue("@GodzinaZamkniecia", txtGodzZamkniecia.Text);
                                cmd.Parameters.AddWithValue("@Interwal", txtInterwal.Text);
                                cmd.Parameters.AddWithValue("@LekProwadzacy", IDLekarza);
                                //Output IDWizyty
                                SqlParameter outputParameter = new SqlParameter();
                                outputParameter.ParameterName = "@IDWizyty";
                                outputParameter.SqlDbType = SqlDbType.Int;
                                outputParameter.Direction = ParameterDirection.Output;
                                cmd.Parameters.Add(outputParameter);
                                con.Open();
                                cmd.ExecuteNonQuery();
                                IDWizyty = outputParameter.Value.ToString();

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
                            if (btnWstawGodzine.Content.ToString() == "Zmień")
                            {
                                #region UPDATE

                                if (godzOtwarcia != txtGodzOtwarcia.Text || godzZamkniecia != txtGodzZamkniecia.Text)                                                                   //Jak godzina z tetxtboxa jest rozna od godziny z tabeli - to robie UPDATE
                                {
                                    godzOtwarcia = txtGodzOtwarcia.Text;                                                                                                            //Podstawiam do zmiennej Query godziny z boxow
                                    godzZamkniecia = txtGodzZamkniecia.Text;
                                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                                    {
                                        try
                                        {
                                            interwal = Convert.ToDouble(txtInterwal.Text, System.Globalization.CultureInfo.InvariantCulture);
                                            query = "UPDATE tabWizyty SET GodzinaOtwarcia = CONVERT(DATETIME, '" + @godzOtwarcia + "', 102), GodzinaZamkniecia = CONVERT(DATETIME, '" + @godzZamkniecia + "', 102), Interwal = " + @interwal + " WHERE (DataWizyty = CONVERT(DATETIME, '" + @dataWizyty + "', 102)) AND LekProwadzacy = " + IDLekarza;
                                            SqlCommand cmd = new SqlCommand(query, con);
                                            cmd.Parameters.AddWithValue("@dataWizyty", dataWizyty);
                                            cmd.Parameters.AddWithValue("@godzOtwarcia", txtGodzOtwarcia.Text);
                                            cmd.Parameters.AddWithValue("@godzZamkniecia", txtGodzZamkniecia.Text);
                                            cmd.Parameters.AddWithValue("@interwal", txtInterwal.Text);
                                            con.Open();
                                            cmd.ExecuteNonQuery();

                                            labUwagiWizyta.Content = "Poprawiono godzinę otwarcia gabinetu.";
                                            labUwagiWizyta.Foreground = Brushes.Green;
                                            txtGodzOtwarcia.Visibility = Visibility.Hidden;
                                            txtGodzOtwarcia.Height = 27;
                                            txtGodzZamkniecia.Visibility = Visibility.Hidden;
                                            txtGodzZamkniecia.Height = 27;
                                            btnWstawGodzine.Content = "Popraw godzinę";
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
                                else  //Godziny te same
                                {
                                    labUwagiWizyta.Content = "Nie zmieniono godziny otwarcia gabinetu!";
                                    labUwagiWizyta.Foreground = Brushes.Red;
                                    txtGodzOtwarcia.Visibility = Visibility.Hidden;
                                    txtGodzOtwarcia.Height = 27;
                                    txtGodzZamkniecia.Visibility = Visibility.Hidden;
                                    txtGodzZamkniecia.Height = 27;
                                    btnWstawGodzine.Content = "Popraw godzinę";
                                }
                                labDzienWizyty.Content = nazwaDnia.First().ToString().ToUpper() + nazwaDnia.Substring(1) + " " + txtGodzOtwarcia.Text + " - " + txtGodzZamkniecia.Text;
                                labDzienWizyty.Visibility = Visibility.Visible;

                                #endregion
                            }
                            else if (btnWstawGodzine.Content.ToString() == "Popraw godzinę")
                            {
                                #region POPRAW GODZINĘ - aktualizacja kontrolek formularza

                                btnWstawGodzine.Content = "Zmień";
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                int kolDatyWizyty = 0;
                if (wiersz < dtListyOtwarte.Rows.Count - 1)
                {
                    wiersz++;
                    string cellValue = dtListyOtwarte.Rows[wiersz][kolDatyWizyty].ToString();
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
                int kolDatyWizyty = 0;
                if (wiersz > 0)
                {
                    wiersz--;
                    string cellValue = dtListyOtwarte.Rows[wiersz][kolDatyWizyty].ToString();
                    datePicker.SelectedDate = DateTime.ParseExact(cellValue, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                int kolDatyWizyty = 0;
                if (IDLekarza != null)
                {
                    for (int i = 0; i < dtListyOtwarte.Rows.Count; i++)
                    {
                        string szukanyDzien = dtListyOtwarte.Rows[i][kolDatyWizyty].ToString();
                        string dzisiaj = DateTime.Today.ToString();
                        if (szukanyDzien == dzisiaj)
                        {
                            datePicker.SelectedDate = DateTime.Today;
                            wiersz = 0;
                            return;
                        }
                    }
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
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            try
            {
                #region Parametry

                string zamkniete = "";

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
                #region UPDATE tabWizyty

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

                #endregion
                PobierzListeWizytPacjentow();
                pobierzOtwarteDniWizyt();
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void chbLp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (chbLp.IsChecked == true)
                    dataGridWizyty.Columns[0].Visibility = Visibility.Visible;
                else
                    dataGridWizyty.Columns[0].Visibility = Visibility.Collapsed;
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PobierzGodzineOtwarciaGabinetu()
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
            try
            {
                dataWizyty = datePicker.SelectedDate.Value.ToShortDateString();
                nazwaDnia = System.Threading.Thread.CurrentThread.CurrentUICulture.DateTimeFormat.GetDayName(datePicker.SelectedDate.Value.DayOfWeek);
                labDzienWizyty.Content = nazwaDnia.First().ToString().ToUpper() + nazwaDnia.Substring(1);
                txtLiczbaPacjentow.Text = "";

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    #region SELECT

                    //Pobranie GODZIN PRZYJEC GABINETU dla danego dnia dla wybranego z kalendarza po danym lekarzu
                    query = "SELECT CAST(GodzinaOtwarcia as nvarchar(5)), CAST(GodzinaZamkniecia as nvarchar(5)), ListaZamknieta, IDWizyt " +
                        "FROM tabWizyty WHERE (DataWizyty = CONVERT(DATETIME, '" + @dataWizyty + "')) AND LekProwadzacy = " + IDLekarza;
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
                            godzOtwarcia = dt.Rows[0][0].ToString();
                            txtGodzOtwarcia.Text = dt.Rows[0][0].ToString();
                            godzZamkniecia = dt.Rows[0][1].ToString();
                            txtGodzZamkniecia.Text = dt.Rows[0][1].ToString();
                            btnWstawGodzine.Content = "Popraw godzinę";
                            btnWstawGodzine.IsEnabled = true;
                            chbListaZamknieta.IsChecked = (bool)Convert.ChangeType(dt.Rows[0][2], typeof(bool));     

                            if (chbListaZamknieta.IsChecked == true)  
                            {
                                txtInterwal.Text = "0";
                                dataGridWizyty.IsEnabled = false;
                            }
                            else
                            {
                                dataGridWizyty.IsEnabled = true;
                            }

                            IDWizyty = dt.Rows[0][3].ToString();
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
                            listaWizytyPacjentow.Clear();
                            dataGridWizyty.ItemsSource = null;
                            dataGridWizyty.Items.Refresh();
                            dataGridWizyty.Visibility = Visibility.Collapsed;
                            dgWizyty.Visibility = Visibility.Collapsed;      

                            #endregion
                        }
                    }
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PobierzListeWizytPacjentow()
        {
            try
            {
                if (txtGodzOtwarcia.Text.Length > 0 & txtGodzZamkniecia.Text.Length > 0)
                {
                    listaWizytyPacjentow.Clear();
                    //Pobranie i sformatowaie godzin otwarcia gabinetu
                    if (txtGodzOtwarcia.Text.Length == 4)
                        godzOtwarciaGabinetu = DateTime.ParseExact(txtGodzOtwarcia.Text, "H:mm", System.Globalization.CultureInfo.InvariantCulture);
                    else if (txtGodzOtwarcia.Text.Length == 5)
                        godzOtwarciaGabinetu = DateTime.ParseExact(txtGodzOtwarcia.Text, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                    if (txtGodzZamkniecia.Text.Length == 4)
                        godzZamknieciaGabinetu = DateTime.ParseExact(txtGodzZamkniecia.Text, "H:mm", System.Globalization.CultureInfo.InvariantCulture);
                    else if (txtGodzZamkniecia.Text.Length == 5)
                        godzZamknieciaGabinetu = DateTime.ParseExact(txtGodzZamkniecia.Text, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);

                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                    {
                        #region SELECT - Pobranie listy pacjentów z formularza

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
                                        dtWizytyPacjentow.Rows[i][(int)KolWizyty.IDWizyta].ToString(),  
                                        dtWizytyPacjentow.Rows[i][(int)KolWizyty.GodzinaWizyty].ToString(),
                                        dtWizytyPacjentow.Rows[i][(int)KolWizyty.Status].ToString(),    
                                        dtWizytyPacjentow.Rows[i][(int)KolWizyty.FkWizyty].ToString(),
                                        dtWizytyPacjentow.Rows[i][(int)KolWizyty.Imie].ToString(),   
                                        dtWizytyPacjentow.Rows[i][(int)KolWizyty.Nazwisko].ToString()); 

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
                        while (godzinaPusta < godzZamknieciaGabinetu)
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
                    listaWizytyPacjentow = listaWizytyPacjentow.OrderBy(x => x.GodzinaWizyty).Cast<Pacjent>().ToList();

                    #endregion
                    //Wypełnienie Datagrida wizytami
                    if (listaWizytyPacjentow.Count > 0)
                        txtLiczbaPacjentow.Text = txtLiczbaPacjentow.Text + "/" + Convert.ToString(listaWizytyPacjentow.Count);
                    else
                        txtLiczbaPacjentow.Text = txtLiczbaPacjentow.Text + "/0";

                    dataGridWizyty.ItemsSource = null;
                    dataGridWizyty.ItemsSource = listaWizytyPacjentow;
                    dataGridWizyty.Items.Refresh();
                    dataGridWizyty.Visibility = Visibility.Visible;
                    dgWizyty.Visibility = Visibility.Hidden;
                }
                else //Brak danych
                {
                    dataGridWizyty.Visibility = Visibility.Collapsed;
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PoprawGodzineWizytyGrid(object sender, DataGridCellEditEndingEventArgs e)
        {
            #region Opis funkcji
            //Funkcja jest aktywowoany w przypadku zmiany zawarosci komórki 'GodzinaWizyty' w gridzie 'dataGridWizyty' (po wyjsciu z komórki - 'CellEditEndingEvent')
            //W wyniku jej działania jes poprawiana tylko godzina wizyty pacjenta 'GodzinaWizyty' w tabeli 'tabWizyta'
            //W przypadku braku zmiany godziny lub nieprawidlowego jej podania - jest przypisana odpowienia informacja

            #endregion
            try
            {
                string GodzinaPoprawiona = ((TextBox)(e.EditingElement)).Text;
                TextBox tb = sender as TextBox;
                #region UPDATE - POPRAWIENIE GODZINY WIZYTY PACJENTA w 'tabWizyta'

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    Pacjent pacjent = (Pacjent)dataGridWizyty.SelectedItem;
                    string IDWizyta = pacjent.IDWizyta;
                    if (IDWizyta != null && GodzinaPoprawiona != pacjent.GodzinaWizyty.ToString())
                    {
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
         
                #endregion
                PobierzGodzineOtwarciaGabinetu();
                labUwagiWizyta.Content = "Poprawiono godzinę wizyty.";
                labUwagiWizyta.Foreground = Brushes.Green;
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                if (ex.Number == 242) MessageBox.Show("Wpisz poprawną godzinę wizyty pacjenta!");
                else MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            try
            {
                if (e.Key == Key.Return)
                {
                    if (txtInterwal.Text != "")
                    {
                        if (Convert.ToInt16(txtInterwal.Text) >= 5 & IDWizyty.Length > 0 & Convert.ToString(interwal) != txtInterwal.Text & Convert.ToInt16(txtInterwal.Text) < 60 || txtInterwal.Text.Trim() == "0")             //liczy jezeli czas wizyty pacjenta jest >- 5 min
                        {
                            labUwagiWizyta.Content = "";
                            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                            {

                                interwal = Convert.ToDouble(txtInterwal.Text, System.Globalization.CultureInfo.InvariantCulture);
                                //UPDATE INTERWAŁ
                                query = "UPDATE tabWizyty SET Interwal = " + @interwal + " WHERE (DataWizyty = CONVERT(DATETIME, '" + @dataWizyty + "', 102))";
                                SqlCommand cmd = new SqlCommand(query, con);
                                cmd.Parameters.AddWithValue("@dataWizyty", dataWizyty);
                                cmd.Parameters.AddWithValue("@interwal", txtInterwal.Text);
                                con.Open();
                                cmd.ExecuteNonQuery();

                                labUwagiWizyta.Content = "Poprawiono planowaną długość wizyty.";
                                labUwagiWizyta.Foreground = Brushes.Blue;
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
            catch (System.Data.SqlClient.SqlException ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        if (btnZapiszWizyte.Content.ToString() != "Znajdź wizytę") 
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
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            try
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
                        
                        if (nazwa == "txtGodzOtwarcia" && txtGodzOtwarcia.Text != ":" && txtGodzOtwarcia.Text != "")            //Wciśnięto Enter w formularzu rejestracji wizyty 
                        {
                            if (txtGodzZamkniecia.Text != ":" && txtGodzZamkniecia.Text != "")
                            {
                                btnWstawGodzine.IsEnabled = true;
                                btnWstawGodzine.Focus();
                                btnWstawGodzine_Click(sender, e);                                                                     
                                dataGridWizyty.Focus();
                            }
                            else                                                                                                        
                            {
                                txtGodzZamkniecia.Visibility = Visibility.Visible;
                                txtGodzZamkniecia.Focus();                                                                              
                            }
                        }
                        else if (nazwa == "txtGodzZamkniecia" && txtGodzZamkniecia.Text != ":" && txtGodzZamkniecia.Text != "")       //Wciśnięto enter w polu txtGodzZamkniecia z wypełnionymi polami: txtGodzOtwarcia i txtGodzZamkniecia
                        {
                            if (txtGodzOtwarcia.Text != ":" && txtGodzOtwarcia.Text != "")                                             
                            {
                                btnWstawGodzine.IsEnabled = true;
                                btnWstawGodzine.Focus();
                                btnWstawGodzine_Click(sender, e);                                                                      
                                dataGridWizyty.Focus();
                            }
                            else                                                                                                       
                            {
                                txtGodzZamkniecia.Focus();                                                                              
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //Załadowanie do ComboBox lekarzy
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    query = "Select Imie, Nazwisko,  IDPacjent  from tabPacjent Where Typ=2 Order By Nazwisko";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();

                    SqlDataReader rdr = cmd.ExecuteReader();
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
                            if (dt.Rows[i][1].ToString() == "Babkin")
                            {
                                cmbTyp.SelectedIndex = i;
                            }
                        }
                    }
                }
                
                //Załadowanie listy otwartch dni gabinetu
                dtListyOtwarte.Clear();
                pobierzOtwarteDniWizyt();
                
                //Funkcja formatuje wygląd strony w ramce w zalezności od trybu w jakim pracuje
                aktywnaStrona = ramkaWizyta.Content as Page;
                if (aktywnaStrona != null)
                {
                    nazwaAktynejStrony = aktywnaStrona.ToString();
                    if (nazwaAktynejStrony == "GabinetLekarski.Strony.StronaPacjent_Szukaj")
                    {
                        if (((StronaPacjent_Szukaj)(aktywnaStrona)).parametrStronySzukaj == (int)Parametr_StronaPacjent_Szukaj.SzukajPoDanychOsobowych)  
                        {
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labTytul.Content = "Podaj dane szukanego pacjenta";
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).btOpcje.Visibility = Visibility.Visible;
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).gbUwagi.Visibility = Visibility.Visible;
                        }
                        else if (((StronaPacjent_Szukaj)(aktywnaStrona)).parametrStronySzukaj == (int)Parametr_StronaPacjent_Szukaj.SzukajPoUwagach) 
                        {
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).labTytul.Content = "Wyszukiwanie zapisanych uwag.";
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).btOpcje.Visibility = Visibility.Collapsed;
                            ((StronaPacjent_Szukaj)(aktywnaStrona)).gbUwagi.Visibility = Visibility.Hidden;
                            btnWyczyscRamke.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion
    }
}

