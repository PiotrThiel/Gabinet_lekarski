using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Configuration;
using GabinetLekarski.Klasy;

namespace GabinetLekarski.Strony
{
    #region Opis

    /*
     * Strona służy do administrowania uprawnieniami osób obsługujących program.
     * W zależności od typu przydzielonego uprawnienia, jest odblokowywana na stronie głównej FrmStart odpowienie pole Menu, tj uprawnienie:
     *  - Administrator - zezwala na uruchomienie menu: Admin
     *  - Pacjent - zezwala na uruchomienie menu: Pacjent
     *  - Rejestracja - zezwala na uruchomienie menu: Rejestracja
     *  - Gabinet - zezwala na uruchomienie menu: Gabinet
     *  
     *  Dane są pobierane z widoku View_Hasla_Uprawnienia
     */

    #endregion

    public enum KolUprawnienia
    {
        //tabHasla
        Uzytkownik = 0,
        Haslo = 1,
        Zablokowane = 2,
        FKPacjent = 3,
        //tabUprawnienia 
        FKUzytkownik = 4,
        Administrator = 5,
        Pacjent = 6,
        Rejestracja = 7,
        Gabinet = 8
    }

    public partial class Strona_Uprawnienia : Page
    {
        #region Parametry


        ObservableCollection<klUprawnienia> obs = new ObservableCollection<klUprawnienia>();            //lista typu ObservableCollection zawierająca obieky typu KlSkierowanie   - zawiera wykaz wystionych Skierowań dla danego pacjenta
        List<klUprawnienia> listaUprawnien = new List<klUprawnienia>();                                 //Zawiera wykaz wszystkich uprawnień przypisanych dla użytkowników programu (z tabeli tabHasla + tabUprawnienia)
        DataTable dtUprawnienia;                                                                        //Zawiera wykaz wszystkich uprawnień przypisanych dla użytkowników programu
        DataTable dtOsoby;                                                                              //Zawiera wykaz osób zarejestrowanych w bazie
        int wiersz;
        string nazwaUprawnienia = string.Empty;                     //Nazwa kolumny uprawnienia tabeli SQL tabUprawnienia
        int uprawnienie;                                    //Czy posiada uprawnienie w tabeli SQL tabUprawnienia
        int IDOsobyDodoawnej;                                   //Zawiewriera ID z tabPacjent nowo wstawianej osoby, której będą dodawane hasło logowania i uprawnienia
        bool flagaWykonajZapytanie;                              //Flaga pomijająca wykonania funkcje checkboxów podczas ładowania) mozna wykonać zapytanie do bazy 


        #endregion
        public Strona_Uprawnienia()
        {
            try
            {
                InitializeComponent();
                dgUprawnienia.Items.Clear();
                dgUprawnienia.ItemsSource = obs;
                ZaladujUprawnienia();
                gridDodajUprawnienia.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ZaladujUprawnienia()
        {
            //Funkcja pobiera dane z widoku View_Hasla_Uprawnienia
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    string query = "SELECT * FROM View_Hasla_Uprawnienia";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    cmd.ExecuteNonQuery();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        dtUprawnienia = new DataTable();
                        listaUprawnien.Clear();
                        obs.Clear();

                        if (rdr.HasRows)
                        {
                            dtUprawnienia.Load(rdr);
                            for (int i = 0; i < dtUprawnienia.Rows.Count; i++)
                            {
                                klUprawnienia uprawnienie = new klUprawnienia(
                                    dtUprawnienia.Rows[i][(int)KolUprawnienia.Uzytkownik].ToString(),
                                    dtUprawnienia.Rows[i][(int)KolUprawnienia.Haslo].ToString(),
                                    Convert.ToBoolean(dtUprawnienia.Rows[i][(int)KolUprawnienia.Zablokowane]),
                                    dtUprawnienia.Rows[i][(int)KolUprawnienia.FKPacjent].ToString(),
                                    dtUprawnienia.Rows[i][(int)KolUprawnienia.FKUzytkownik].ToString(),
                                    Convert.ToBoolean(dtUprawnienia.Rows[i][(int)KolUprawnienia.Administrator]),
                                    Convert.ToBoolean(dtUprawnienia.Rows[i][(int)KolUprawnienia.Pacjent]),
                                    Convert.ToBoolean(dtUprawnienia.Rows[i][(int)KolUprawnienia.Rejestracja]),
                                    Convert.ToBoolean(dtUprawnienia.Rows[i][(int)KolUprawnienia.Gabinet])
                                    );
                                obs.Add(uprawnienie);
                                dgUprawnienia.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            dgUprawnienia.Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #region Funkcje CheckBoxa


        private void Checked_Gabinet(object sender, RoutedEventArgs e)
        {
            if (flagaWykonajZapytanie && wiersz != -1)
            {
                nazwaUprawnienia = "Gabinet";
                uprawnienie = 1;
                UpdateTabUprawnienia(nazwaUprawnienia, uprawnienie);
            }

        }
        private void Unchecked_Gabinet(object sender, RoutedEventArgs e)
        {
            if (flagaWykonajZapytanie && wiersz != -1)
            {
                nazwaUprawnienia = "Gabinet";
                uprawnienie = 0;
                UpdateTabUprawnienia(nazwaUprawnienia, uprawnienie);
            }
        }

        private void Checked_Rejestracja(object sender, RoutedEventArgs e)
        {
            if (flagaWykonajZapytanie && wiersz != -1)
            {
                nazwaUprawnienia = "Rejestracja";
                uprawnienie = 1;
                UpdateTabUprawnienia(nazwaUprawnienia, uprawnienie);
            }
        }

        private void Unchecked_Rejestracja(object sender, RoutedEventArgs e)
        {
            if (flagaWykonajZapytanie && wiersz != -1)
            {
                nazwaUprawnienia = "Rejestracja";
                uprawnienie = 0;
                UpdateTabUprawnienia(nazwaUprawnienia, uprawnienie);
            }
        }

        private void Checked_Pacjent(object sender, RoutedEventArgs e)
        {
            if (flagaWykonajZapytanie && wiersz != -1)
            {
                nazwaUprawnienia = "Pacjent";
                uprawnienie = 1;
                UpdateTabUprawnienia(nazwaUprawnienia, uprawnienie);
            }
        }

        private void Unchecked_Pacjent(object sender, RoutedEventArgs e)
        {
            if (flagaWykonajZapytanie && wiersz != -1)
            {
                nazwaUprawnienia = "Pacjent";
                uprawnienie = 0;
                UpdateTabUprawnienia(nazwaUprawnienia, uprawnienie);
            }
        }

        private void Checked_Administrator(object sender, RoutedEventArgs e)
        {
            if (flagaWykonajZapytanie && wiersz != -1)
            {
                nazwaUprawnienia = "Administrator";
                uprawnienie = 1;
                UpdateTabUprawnienia(nazwaUprawnienia, uprawnienie);

            }
        }

        private void Unchecked_Administrator(object sender, RoutedEventArgs e)
        {
            if (flagaWykonajZapytanie && wiersz != -1)
            {
                nazwaUprawnienia = "Administrator";
                uprawnienie = 0;
                UpdateTabUprawnienia(nazwaUprawnienia, uprawnienie);
            }
        }

        #endregion

        private void Wiersz_MouseEnter(object sender, MouseEventArgs e)
        {
            wiersz = dgUprawnienia.ItemContainerGenerator.IndexFromContainer((DataGridRow)sender);
            lblUprawnieniaInfo.Content = Convert.ToString(wiersz);
        }

        private void UpdateTabUprawnienia(string nazwaUprawnienia, int uprawnienie)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    string FKUzytkownik = dtUprawnienia.Rows[wiersz][0].ToString();
                    string query = "UPDATE tabUprawnienia SET " + nazwaUprawnienia + " = '" + uprawnienie + "' WHERE FKUzytkownik = '" + FKUzytkownik + "'";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            flagaWykonajZapytanie = true;
        }

        private void btnDodaj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string haslo = txbHasloUzytkownika.Text;
                string uzytkownik = txbNazwaUzytkownika.Text;

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    if (uzytkownik != "" && uzytkownik != "Nazwa użytkownika" && haslo != "" && haslo != "Hasło użytkownika" && IDOsobyDodoawnej > 0)
                    {
                        string query = "INSERT INTO tabHasla (Uzytkownik, Haslo, Zablokowane, FKPacjent) VALUES ('" + uzytkownik + "', '" + haslo + "', 0, " + IDOsobyDodoawnej + ")";
                        SqlCommand cmd = new SqlCommand(query, con);
                        con.Open();
                        cmd.ExecuteNonQuery();

                        string query2 = "INSERT INTO tabUprawnienia (FKUzytkownik) VALUES ('" + uzytkownik + "')";
                        SqlCommand cmd2 = new SqlCommand(query2, con);
                        cmd2.ExecuteNonQuery();

                        //Formatowanie strony
                        gridDodajUprawnienia.Visibility = Visibility.Collapsed;
                        txbNazwaUzytkownika.Text = "Nazwa użytkownika";
                        txbHasloUzytkownika.Text = "Hasło użytkownika";
                        ZaladujUprawnienia();

                        wiersz = -1;    //ustawiam flagę, aby nie robił UPDEATA dla każdj zminay checkBoxa
                    }
                    else
                    {
                        MessageBox.Show("Aby dadać użytkownika należy wypełnić wszystkie pola", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                if (ex.Number == 2627) //Próba usunięcia kodu z istniejącym juz powiązaniem (referencja)
                {
                    string osoba = cmbOsoba.SelectedValue.ToString();
                    MessageBox.Show("Nie można dodać do listy uprawnień: \n - tej samej osoby (" + osoba + ")\n - lub\\i tej samej nazwy użytkownika (" + txbNazwaUzytkownika.Text + ")", "Uwaga", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void btnDodajWiersz_Click(object sender, RoutedEventArgs e)
        {
            if (gridDodajUprawnienia.Visibility != Visibility.Visible)
            {
                załadujOsoby();
                gridDodajUprawnienia.Visibility = Visibility.Visible;
            }
        }

        private void btnUsun_Click(object sender, RoutedEventArgs e)
        {
            //Funkcja usuwa uprawnienia użytkowania programu poprzez usunięcia uprawnień z tabeli tabHasla i tabUprawnienia
            try
            {
                //Pobranie kliknietego wiersza datagrida 'dataGridSkierNowe' zawierajacego wykaz wzorów skierowań => wartość != null
                var rowview = dgUprawnienia.SelectedItem as klUprawnienia;
                string uzytkownik = rowview.Uzytkownik.ToString();
                if (uzytkownik != string.Empty)
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                    {
                        string query = "DELETE FROM tabHasla WHERE (Uzytkownik = '" + uzytkownik + "')";
                        SqlCommand cmd = new SqlCommand(query, con);
                        con.Open();
                        cmd.ExecuteNonQuery();
                        flagaWykonajZapytanie = false;
                        ZaladujUprawnienia();
                        flagaWykonajZapytanie = true;
                        wiersz = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void załadujOsoby()
        {
            //Funkcja pobiera dane osobowe do COmboBoxa  cmbOsoba
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    string query = "SELECT Nazwisko, Imie , IDPacjent FROM tabPacjent ORDER BY Nazwisko";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    cmd.ExecuteNonQuery();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        dtOsoby = new DataTable();
                        if (rdr.HasRows)
                        {
                            cmbOsoba.Items.Clear();
                            dtOsoby.Load(rdr);
                            for (int i = 0; i < dtOsoby.Rows.Count; i++)
                            {
                                string nazwisko = dtOsoby.Rows[i][0].ToString();             //Nazwisko
                                string imie = dtOsoby.Rows[i][1].ToString();                //Imię
                                cmbOsoba.Items.Add(nazwisko + " " + imie);
                            }
                        }
                        else
                        {
                            dgUprawnienia.Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbOsoba_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            if (cmb != null && cmb.SelectedIndex >= 0)
            {
                int indeks = cmb.SelectedIndex;
                IDOsobyDodoawnej = Convert.ToInt16(dtOsoby.Rows[indeks][2]);
                lblUprawnieniaInfo2.Content = IDOsobyDodoawnej.ToString();
            }
        }

        private void txbNazwaUzytkownika_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txbNazwaUzytkownika.Text == "")
            {
                txbNazwaUzytkownika.Text = "Nazwa użytkownika";
                tekstSzary(((TextBox)sender).Name.ToString());
            }
            else
            {
                tekstCzarny(((TextBox)sender).Name.ToString());
            }
        }

        private void txbNazwaUzytkownika_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txbNazwaUzytkownika.Text.Length > 0)
            {
                if (txbNazwaUzytkownika.Text == "Nazwa użytkownika")
                {
                    txbNazwaUzytkownika.Text = "";
                }
                tekstCzarny(((TextBox)sender).Name.ToString());
            }
            else
            {
                tekstSzary(((TextBox)sender).Name.ToString());
            }
        }

        private void txbHasloUzytkownika_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txbHasloUzytkownika.Text == "")
            {
                txbHasloUzytkownika.Text = "Hasło użytkownika";
                tekstSzary(((TextBox)sender).Name.ToString());
            }
            else
            {
                tekstCzarny(((TextBox)sender).Name.ToString());
            }
        }

        private void txbHasloUzytkownika_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txbHasloUzytkownika.Text.Length>0)
            {
                if (txbHasloUzytkownika.Text == "Hasło użytkownika")
                {
                    txbHasloUzytkownika.Text = "";
                }
                tekstCzarny(((TextBox)sender).Name.ToString());
            }
            else
            {
                tekstSzary(((TextBox)sender).Name.ToString());
            }
        }
        private void tekstSzary(string nazwaTxb)
        {
            if (nazwaTxb == "txbNazwaUzytkownika")
            {
                txbNazwaUzytkownika.FontSize = 14;
                txbNazwaUzytkownika.FontStyle = FontStyles.Italic;
                txbNazwaUzytkownika.Foreground = Brushes.DarkGray;
            }
            else
            {
                txbHasloUzytkownika.FontSize = 14;
                txbHasloUzytkownika.FontStyle = FontStyles.Italic;
                txbHasloUzytkownika.Foreground = Brushes.DarkGray;
            }
        }

        private void tekstCzarny(string nazwaTxb)
        {
            if (nazwaTxb == "txbNazwaUzytkownika")
            {
                txbNazwaUzytkownika.FontSize = 16;
                txbNazwaUzytkownika.FontStyle = FontStyles.Normal;
                txbNazwaUzytkownika.Foreground = Brushes.Black;
            }
            else
            {
                txbHasloUzytkownika.FontSize = 16;
                txbHasloUzytkownika.FontStyle = FontStyles.Normal;
                txbHasloUzytkownika.Foreground = Brushes.Black;
            }
        }
    }
}