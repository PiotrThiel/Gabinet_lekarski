using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using GabinetLekarski.Form;

namespace GabinetLekarski.Strony
{
    public enum KolViewPacjentDane
    {
        IdPacjent = 0,
        Nazwisko = 1,
        Imie = 2,
        Telefon = 3,
        Pesel = 4,
        Ulica = 5,
        Dom = 6,
        KodPocztowy = 7,
        Miejscowosc = 8,
        Uwagi = 9,
        IDAdres = 10,
        Typ = 11
    }

    public enum Parametr_StronaPacjent_Dane
    {
        WyswietlDane=0,
        WyswietUwagi=1
    }

    public partial class StronaPacjent_Dane : Page
    {
        #region PARAMETRY

        public bool insert;
        public bool update;
        string IDPacjent;
        string IDAdres;
        string query;
        public int widokStrony = (int)Parametr_StronaPacjent_Dane.WyswietlDane;
        int Typ = -1;                                       //tabTyp: 1-Pacjent, 2-Lekarz   
        string Miejscowosc = string.Empty;

        private int UniqueKeyViolation = 2627;
        private int IncorrectSyntax = 102;
        private int ErrorType = 8114;

        DataTable dtMiasta;
        #endregion
        #region Paramerty startowe formularza

        // W zależności od ustawionego parametru, do ramki strony 'StronaPacjent_Wizyta' będzie ładowana odpowiednia strona.
        // insert=TRUE , update=FALSE - Wstawienie nowego pacjenta do bazy 
        // insert=FALSE , update=TRUE - Poprawienie danych pacjenta 
        // idPacjenta - nr ID wstawianego/poprawianego pacjenta2
        // trybWidokuStrony:
        // 0 - standardowe otworzenie formularza z pełnymi danymi
        // 1 - otworzenie formularza w tybie UWAG

        #endregion
        public StronaPacjent_Dane(bool insert, bool update, string idPacjenta, int trybWidokuStrony)
        {
            InitializeComponent();
            widokStrony = trybWidokuStrony;
            this.insert = insert;
            this.update = update;
            this.IDPacjent = idPacjenta;
            txtIDPacjenta.Text = idPacjenta;
            btWyczysc.Visibility = Visibility.Collapsed;
            gbAdres.Visibility = Visibility.Collapsed;
            ZaladujCmbTyp();

            if (insert)
            {
                btZapisz.Content = "Zapisz";
                labTytul.Content = "Dodaj dane osobowe ";
            }
            
            if (update)
                WyswietlOsobe(IDPacjent);

            gbDanePacjenta.IsEnabled = false;
            gbAdres.IsEnabled = false;
            gbUwagi.IsEnabled = false;

            ZaladujMiasta();
        }

        #region METODY

        private void btZapisz_Click(object sender, RoutedEventArgs e)
        {
            Typ = cmbTyp.SelectedIndex + 1;
            if (txtImie.Text == "" || txtNazwisko.Text == "")
            {
                labUwagi.Content = "Wypełnij pola danych pacjenta";
                labUlica.Foreground = Brushes.Blue;
                return;
            }
            else if (!txtPesel.Text.All(Char.IsDigit))
            {
                labUwagi.Content = "PESEL należy podać w postaci liczbowej (11 znaków)";
                labUwagi.Foreground = Brushes.Red;
                return;
            }
            else if (cmbMiejscowosc.Text == "" || txtDom.Text == "")
            {
                labUwagi.Content = "Wypełnij pola adresu pacjenta";
                labUlica.Foreground = Brushes.Blue;
                return;
            }

            //Wybrano formularz w trybie dodania nowego pacjenta
            if (insert || (update == false & insert == false))
            {
                #region INSERT: tabPacjent, tabAdres

                try
                {
                    if (cmbTyp.SelectedIndex >= 0)
                    {
                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            #region INSERT tabAdres

                            SqlCommand cmd = new SqlCommand("spTabAdres_Insert_ScopeID", con);
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Ulica", txtUlica.Text);
                            cmd.Parameters.AddWithValue("@Dom", txtDom.Text);
                            cmd.Parameters.AddWithValue("@KodPocztowy", txtKodPocztowy.Text);
                            cmd.Parameters.AddWithValue("@Miejscowosc", cmbMiejscowosc.Text);
                            SqlParameter outputParameter = new SqlParameter();
                            outputParameter.ParameterName = "@IDAdres";
                            outputParameter.SqlDbType = System.Data.SqlDbType.Int;
                            outputParameter.Direction = System.Data.ParameterDirection.Output;
                            cmd.Parameters.Add(outputParameter);
                            con.Open();
                            cmd.ExecuteNonQuery();
                            IDAdres = outputParameter.Value.ToString();

                            #endregion
                            #region INSERT tabPacjent

                            cmd = new SqlCommand("spTabPacjent_Insert", con);
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Imie", txtImie.Text);
                            cmd.Parameters.AddWithValue("@Nazwisko", txtNazwisko.Text);
                            cmd.Parameters.AddWithValue("@Telefon", txtTelefon.Text);
                            cmd.Parameters.AddWithValue("@Pesel", txtPesel.Text);
                            cmd.Parameters.AddWithValue("@Uwagi", txtUwagi.Text);
                            cmd.Parameters.AddWithValue("@FKAdres", IDAdres);
                            cmd.Parameters.AddWithValue("@Typ", Typ);
                            cmd.ExecuteNonQuery();

                            #endregion
                            labUwagi.Content = "Dodano osobę: " + txtImie.Text + " " + txtNazwisko.Text;
                            labUwagi.Foreground = Brushes.Green;
                            btZapisz.IsEnabled = false;
                            gbDanePacjenta.IsEnabled = false;
                            gbAdres.IsEnabled = false;
                            txtIDPacjenta.Text = IDPacjent;
                            txtUwagi.IsEnabled = false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Wybierz typ wprowadzanej osoby.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    if (ex.Number == 2627) labUwagi.Content = "Brak numeru PESEL lub jest już przypisany!";
                    else if (ex.Number == 102) labUwagi.Content = "Wypełnij wszystkie pola formularza.";
                    else if (ex.Number == 8114) labUwagi.Content = "Wprowadź poprawne wartości w polach fomularza.";
                    else MessageBox.Show(ex.ToString());
                    labUlica.Foreground = Brushes.Red;
                }

                #endregion
            }
            else
            {
                #region UPDATE tabPacjent, tabAdres

                try
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                    {
                        #region UPDATE - tabPacjent

                        bool bylZablokowany = false;
                        if (gbDanePacjenta.IsEnabled == false)
                        {
                            gbDanePacjenta.IsEnabled = true;
                            bylZablokowany = true;
                        }
                        if (gbUwagi.IsEnabled == false)
                            gbUwagi.IsEnabled = true;

                        SqlCommand cmd = new SqlCommand("spTabPacjent_Update", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IDPacjent", IDPacjent);
                        cmd.Parameters.AddWithValue("@Imie", txtImie.Text);
                        cmd.Parameters.AddWithValue("@Nazwisko", txtNazwisko.Text);
                        cmd.Parameters.AddWithValue("@Telefon", txtTelefon.Text);
                        cmd.Parameters.AddWithValue("@Pesel", txtPesel.Text);
                        cmd.Parameters.AddWithValue("@Uwagi", txtUwagi.Text);
                        cmd.Parameters.AddWithValue("@Typ", Typ);
                        con.Open();
                        cmd.ExecuteNonQuery();

                        #endregion
                        #region UPDATE - tabAdres

                        if (gbAdres.IsEnabled == false)
                            gbAdres.IsEnabled = true;
                        cmd = new SqlCommand("spTabAdres_Update", con);
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@IDAdres", IDAdres);
                        cmd.Parameters.AddWithValue("@Ulica", txtUlica.Text);
                        cmd.Parameters.AddWithValue("@Dom", txtDom.Text);
                        cmd.Parameters.AddWithValue("@KodPocztowy", txtKodPocztowy.Text);
                        cmd.Parameters.AddWithValue("@Miejscowosc", cmbMiejscowosc.Text);
                        cmd.ExecuteNonQuery();
                        labUwagi.Foreground = Brushes.Blue;

                        if (!bylZablokowany)
                        {
                            labUwagi.Content = "Poprawiono dane pacjenta.";
                            bylZablokowany = false;
                        }

                        #endregion
                    }
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    if (ex.Number == UniqueKeyViolation) labUwagi.Content = "Brak numeru PESEL lub jest już przypisany!";
                    else if (ex.Number == IncorrectSyntax) labUwagi.Content = "Wypełnij wszystkie pola formularza.";
                    else if (ex.Number == ErrorType)
                    {
                        if (!txtPesel.Text.All(Char.IsDigit))
                        {
                            labUwagi.Content = "PESEL należy podać w postaci liczbowej (11 znaków)";
                        }
                        labUwagi.Content = "Wprowadź poprawne wartości w polach fomularza.";
                    }
                    else
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }

                #endregion
            }
        }
        private void WyswietlOsobe(string idWiersza)
        {
            try
            {
                #region Wyswietlenie danych szukanego pacjenta (tabPacjent, tabAdres)

                btZapisz.Content = "Popraw";
                labTytul.Content = "Dane pacjenta";
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    query = "SELECT * FROM View_Pacjent_Dane WHERE IDPacjent =" + IDPacjent;
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        DataTable dtDaneOsobowe = new DataTable();
                        if (rdr.HasRows)
                        {
                            dtDaneOsobowe.Load(rdr);

                            txtNazwisko.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Nazwisko].ToString();
                            txtImie.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Imie].ToString();
                            txtTelefon.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Telefon].ToString();
                            txtPesel.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Pesel].ToString();
                            txtUwagi.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Uwagi].ToString();

                            txtUlica.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Ulica].ToString();
                            txtDom.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Dom].ToString();
                            txtKodPocztowy.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.KodPocztowy].ToString();
                            cmbMiejscowosc.Text = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Miejscowosc].ToString();
                            IDAdres = dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.IDAdres].ToString();
                            Typ = Convert.ToInt16(dtDaneOsobowe.Rows[0][(int)KolViewPacjentDane.Typ]);
                            cmbTyp.SelectedIndex = Typ - 1;
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btWyczysc_Click(object sender, RoutedEventArgs e)
        {
            WyczyscPolaFormularza();
            btZapisz.IsEnabled = true;
        }

        private void WyczyscPolaFormularza()
        {
            txtImie.Text = "";
            txtNazwisko.Text = "";
            txtPesel.Text = "";
            txtTelefon.Text = "";
            txtUlica.Text = "";
            txtDom.Text = "";
            txtKodPocztowy.Text = "";
            cmbMiejscowosc.Text = "";
            labUwagi.Content = "";

            btZapisz.IsEnabled = true;
            gbDanePacjenta.IsEnabled = true;
            gbAdres.IsEnabled = true;
        }

        private void btUwagi_Click(object sender, RoutedEventArgs e)
        {
            if (gbUwagi.Visibility == Visibility.Visible) gbUwagi.Visibility = Visibility.Collapsed;
            else gbUwagi.Visibility = Visibility.Visible;
        }

        private void ZaladujCmbTyp()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    #region ComboBox - pobranie listy typów

                    query = "Select * from tabTyp";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.HasRows)
                        {
                            DataTable dt = new DataTable();
                            dt.Load(rdr);
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                cmbTyp.Items.Add(dt.Rows[i][1].ToString());
                            }
                        }
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        #endregion

        private void cmbTyp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTyp.SelectedIndex != -1)
            {
                gbDanePacjenta.IsEnabled = true;
                gbAdres.IsEnabled = true;
                gbUwagi.IsEnabled = true;
            }
            else
            {
                gbDanePacjenta.IsEnabled = false;
                gbAdres.IsEnabled = false;
                gbUwagi.IsEnabled = false;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (widokStrony == (int)Parametr_StronaPacjent_Dane.WyswietlDane)
            {
                gbAdres.Visibility = Visibility.Visible;
                if (txtUwagi.Text.Length == 0)
                    gbUwagi.Visibility = Visibility.Collapsed;
                else
                    gbUwagi.Visibility = Visibility.Visible;
            }
            else if (widokStrony == (int)Parametr_StronaPacjent_Dane.WyswietUwagi)
            {
                gbDanePacjenta.Visibility = Visibility.Visible;
                gbDanePacjenta.IsEnabled = true;
                gbAdres.Visibility = Visibility.Visible;
                gbAdres.IsEnabled = true;
                gbUwagi.Visibility = Visibility.Visible;
                gbUwagi.IsEnabled = true;
            }
            cmbTyp.SelectedIndex = 0;
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            labUwagi.Content = "";
        }


        public void ZaladujMiasta()
        {
            //Funkcja ładuje nawy miejscowości z tamMiejscowoscdo ComboBox cmbMiejscowosc
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    cmbMiejscowosc.Items.Clear();
                    string query = "SELECT * FROM tabMiejscowosc ORDER BY Miejscowosc";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        dtMiasta = new DataTable();
                        if (rdr.HasRows)
                        {
                            dtMiasta.Load(rdr);
                            for (int i = 0; i < dtMiasta.Rows.Count; i++)
                            {
                                cmbMiejscowosc.Items.Add(dtMiasta.Rows[i][0].ToString());
                            }
                        }
                    }
                    cmbMiejscowosc.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbMiejscowosc_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                ComboBox cmb = sender as ComboBox;
                //Pomijam przypadek pobrania miasta do combo przy wyszukiwaniu danych pacjenta
                if (cmb.SelectedIndex == -1 && cmb.Text.Length > 0)
                {
                    if (dtMiasta != null)
                    {
                        foreach (DataRow wiersz in dtMiasta.Rows)
                        {
                            if (cmb.Text == wiersz[0].ToString())     //jest miejscowosc na liscie
                            {
                                return;
                            }
                        }
                    }

                    MessageBoxResult odpowiedz;
                    odpowiedz = MessageBox.Show("Nie znaleziono miasta:   " + cmb.Text + "\n\n Dodać miasto do listy?", "Wykaz miejscowości...", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (odpowiedz == MessageBoxResult.Yes)
                    {
                        #region INSERT tabMiejscowosc

                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            SqlCommand cmd = new SqlCommand("spTabMiejscowosc_Insert", con);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Miejscowosc", cmb.Text);
                            con.Open();
                            cmd.ExecuteNonQuery();
                        }

                        #endregion
                        ZaladujMiasta();
                        labUwagi.Foreground = Brushes.Blue;
                        labUwagi.Content = "Dodano miejscowość:" + cmb.Text;
                    }
                    else if (odpowiedz == MessageBoxResult.No)
                    {
                        //Nic nie robi
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmbMiejscowosc_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Funkcaj wywołuje listeę miast w trybie edycji
            try
            {
                Miejscowosc = cmbMiejscowosc.Text;
                Okno_Miasto okno_Miasto = new Okno_Miasto(this);
                okno_Miasto.Owner = Application.Current.MainWindow;                                                                      //Odniesienie do okna głownego
                okno_Miasto.WindowStartupLocation = WindowStartupLocation.CenterOwner;                                                   //Centrowanie do odniesienia
                okno_Miasto.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void Miasto(string Miejscowosc)
        {
            //Funkcja odswieża nazwę miejscowosći w ComboBox: cmbMiejscowosc w sytyacji, gdy zmienione jej nazwę w trybie edycji w oknie Okno_Miasto
            try
            {
                cmbMiejscowosc.Text = Miejscowosc;   
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
