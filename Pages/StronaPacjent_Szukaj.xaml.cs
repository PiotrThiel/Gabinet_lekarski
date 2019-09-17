using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;


namespace GabinetLekarski.Strony
{
    public enum Parametr_StronaPacjent_Szukaj
    {
        SzukajPoDanychOsobowych = 0,
        SzukajPoUwagach = 1
    }

    public partial class StronaPacjent_Szukaj : Page
    {

        #region PARAMETRY

        SqlDataReader reader;
        public int parametrStronySzukaj = (int)Parametr_StronaPacjent_Szukaj.SzukajPoDanychOsobowych;
        public bool insert;    
        public bool update; 
        public string idPacjenta = "";

        #endregion
        #region Paramerty startowe formularza

        // W zależności od ustawionego parametru, do ramki strony 'StronaPacjent_Szukaj' będzie ładowana odpowiednia strona.
        // parametrWyszukiwania:
        // 0 - standardowe otworzenie formularza - wyszukuje pacjentow po danych osobowych
        // 1 - formularz wyszukuje tylko pocjentow po wpisanych uwagach w danych pacjenta
        // ???2 - standardowe otworzenie formularza - wyszukuje pacjentow po danych osobowych -  dla WYSZUKANIA WIZYT PO DANYCH OSOBOWYCH PACJENTA

        #endregion
        public StronaPacjent_Szukaj(int parametrWyszukiwania)
        {
            InitializeComponent();
            parametrStronySzukaj = parametrWyszukiwania;
            gridDanePacjenta.Visibility = Visibility.Hidden;
            if (parametrWyszukiwania == (int)Parametr_StronaPacjent_Szukaj.SzukajPoUwagach)
                grupaDanePacjenta.Visibility = Visibility.Hidden;
            else grupaDanePacjenta.Visibility = Visibility.Visible;
        }

        #region FUNKCJE

        private void btSzukaj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btSzukaj.Content.ToString() == "Szukaj")
                {
                    #region Ustawienie warunku wyszukiwania dla SELECTa

                    string warunek = "";

                    if (txtImie.Text.Length > 0) warunek = "Imie LIKE '" + txtImie.Text + "%'";

                    if (txtNazwisko.Text.Length > 0)
                    {
                        if (warunek.Length > 0) { warunek = warunek + " AND Nazwisko LIKE '" + txtNazwisko.Text + "%'"; }
                        else { warunek = warunek + "Nazwisko LIKE '" + txtNazwisko.Text + "%'"; }
                    }

                    if (txtTelefon.Text.Length > 0)
                    {
                        if (warunek.Length > 0) { warunek = warunek + " AND Telefon LIKE '%" + txtTelefon.Text + "%'"; }
                        else { warunek = warunek + "Telefon LIKE '%" + txtTelefon.Text + "%'"; }
                    }

                    if (txtPesel.Text.Length > 0)
                    {
                        if (warunek.Length > 0) { warunek = warunek + " AND Pesel LIKE '%" + txtPesel.Text + "%'"; }
                        else { warunek = warunek + "Pesel LIKE '%" + txtPesel.Text + "%'"; }
                    }

                    if (txtUwagi.Text.Length > 0)
                    {
                        if (warunek.Length > 0) { warunek = warunek + " AND Uwagi LIKE '%" + txtUwagi.Text + "%'"; }
                        else { warunek = warunek + "Uwagi LIKE '%" + txtUwagi.Text + "%'"; }
                    }

                    if (warunek.Length == 0 & parametrStronySzukaj == (int)Parametr_StronaPacjent_Szukaj.SzukajPoDanychOsobowych)
                    {
                        labUwagi.Content = "Wypełnij szukane pole";
                        return;
                    }

                    #endregion

                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                    {
                        #region SELECT wyszukujący pacjentów

                        string query = "";
                        DataTable dt = new DataTable();

                        if (parametrStronySzukaj == (int)Parametr_StronaPacjent_Szukaj.SzukajPoDanychOsobowych)
                        {
                            query = "SELECT * FROM View_Pacjent_Dane WHERE " + warunek;
                            SqlCommand cmd = new SqlCommand(query, con);
                            con.Open();
                            reader = cmd.ExecuteReader();
                            dt.Load(reader);
                        }
                        else
                        {
                            query = "SELECT IDPacjent, Imie, Nazwisko, Pesel, Uwagi, FKAdres FROM tabPacjent WHERE (Uwagi <> '')";
                            SqlCommand cmd = new SqlCommand(query, con);
                            con.Open();
                            reader = cmd.ExecuteReader();
                            dt.Load(reader);
                        }

                        #endregion

                        if (dt.Rows.Count > 0)
                        {
                            int ileWyszukanychPAcjentow = dt.Rows.Count;
                            string idPacjenta = dt.Rows[0][(int)KolViewPacjentDane.IdPacjent].ToString();
                            insert = false;
                            update = true;

                            if (ileWyszukanychPAcjentow == 1)
                            {
                                btSzukaj.Visibility = Visibility.Visible;
                                this.NavigationService.Navigate(new StronaPacjent_Dane(insert, update, idPacjenta, 0));
                            }
                            else
                            {
                                grupaDanePacjenta.Visibility = Visibility.Hidden;
                                gridDanePacjenta.Visibility = Visibility.Visible;
                                gridDanePacjenta.ItemsSource = dt.DefaultView;
                                gridDanePacjenta.Columns[(int)KolViewPacjentDane.IdPacjent].Visibility = Visibility.Collapsed;
                                gridDanePacjenta.Columns[(int)KolViewPacjentDane.Telefon].Visibility = Visibility.Collapsed;
                                gridDanePacjenta.Columns[(int)KolViewPacjentDane.Pesel].Visibility = Visibility.Collapsed;
                                gridDanePacjenta.Columns[(int)KolViewPacjentDane.Uwagi].Visibility = Visibility.Collapsed;
                                gridDanePacjenta.Columns[(int)KolViewPacjentDane.IDAdres].Visibility = Visibility.Collapsed;
                                gridDanePacjenta.Columns[(int)KolViewPacjentDane.Typ].Visibility = Visibility.Collapsed;
                                reader.Close();

                                gbUwagi.Visibility = Visibility.Collapsed;
                                btSzukaj.Content = "Wybierz";
                                labTytul.Content = "Wybierz pacjenta";
                                labUwagi.Foreground = Brushes.Blue;
                                labUwagi.FontWeight = FontWeights.Normal;
                                labUwagi.Content = "Znaleziono " + ileWyszukanychPAcjentow + " pacjentów, wybierz szukanego.";
                            }
                        }
                        else labUwagi.Content = "Nie znaleziono pacjenta";
                    }
                }
                else //'WYBIERZ'
                {
                    DataRowView dataRowView = gridDanePacjenta.SelectedItem as DataRowView;
                    if (dataRowView != null)
                    {
                        idPacjenta = dataRowView["IDPacjent"].ToString();
                        if (parametrStronySzukaj == (int)Parametr_StronaPacjent_Szukaj.SzukajPoDanychOsobowych)
                            this.NavigationService.Navigate(new StronaPacjent_Dane(insert, update, idPacjenta, 0));
                        else if (parametrStronySzukaj == (int)Parametr_StronaPacjent_Szukaj.SzukajPoUwagach)
                            this.NavigationService.Navigate(new StronaPacjent_Dane(insert, update, idPacjenta, 1));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btWyczysc_Click(object sender, RoutedEventArgs e)
        {
            if (gridDanePacjenta.Visibility == Visibility.Visible)
                if (gridDanePacjenta.Items.Count > 0) gridDanePacjenta.Items.Refresh();
            labTytul.Content = "Podaj dane szukanego pacjenta";
            labUwagi.Content = "";
            txtImie.Text = "";
            txtNazwisko.Text = "";
            txtPesel.Text = "";
            txtTelefon.Text = "";
            labUwagi.Content = "";
            btOpcje.Content = "Wyczyść";
            btSzukaj.Content = "Szukaj";
            grupaDanePacjenta.Visibility = Visibility.Visible;
            gridDanePacjenta.Visibility = Visibility.Hidden;
        }

        private void txt_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (labUwagi.Content != null)
            {
                if (((TextBox)(sender)).Text == "" & labUwagi.Content.ToString() == "Wypełnij szukane pole") labUwagi.Content = "";
            }
        }
        private void Page_LostFocus(object sender, RoutedEventArgs e)
        {
            labUwagi.Content = "";
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            Button btn = sender as Button;
            var typ = e.OriginalSource.GetType();
            if (e.Key == Key.Return)
            {
                if (typ.Name == "TextBox")
                {
                    TextBox textBox = e.OriginalSource as TextBox;
                    string nazwa = textBox.Name.ToString();

                    if ((nazwa == "txtImie" || nazwa == "txtNazwisko" || nazwa == "txtTelefon" || nazwa == "txtPesel" || nazwa == "txtUwagi") && nazwa != "")
                    {
                        btSzukaj_Click(sender, e);
                    }
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            txtImie.Focus();
        }

        #endregion
    }
}