using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.SqlClient;
using System.Data;

//Moje przestrzenie


namespace GabinetLekarski.Form
{

    public partial class Okno_Miasto : Window
    {
        int wiersz;                                     //Indeks grida gridMiejscowosc nad którym znajduje się wskażnik myszki
        int wierszOld = -1;                             //Indeks poprzedniego wiersza grida
        int liczbaKodow;                                //Liczba rekordów załadowana do grida - PRZED dodaniem dodatkowego rekordu (kodu choroby)
        int wierszDoDodania;                            //Numer wiersza nowego kodu ZusZla (ostani) który nalezy dodać dao bazy
        int wierszSzukanegoKodu = -1;
        string Miejscowosc = string.Empty;              //Zawiera nazwę miejscowości w bazie danych
        string Opis = string.Empty;                     //Zawiera opis choroby który bedzie zapisany w bazie danych 
        string szukanaMiejscowosc = string.Empty;       //Szukany kod ZusZla podany w TexBoksie txtWyszukaj 
        bool dodaj;
        bool edytuj;
        bool jestMiejscowosc = false;                   //Flaga - informująca funkcję ZaladujZusZla czy w DataGridzie wyświetlić tylko szukany wiersz (po txtBox txtWyszukaj) czy wszystkie
        DataTable dt;                                   //Zawiera wykaz wszystkich kodów chorób
        DataTable dtSzukana;                            //Zawiera tylko wiersz z szukanym kodem choroby
        DataSet dsWyszukiwanie;                         //Przechowuje tabelę z 1 wierszem SZUKANEGO KODU ZusZla,który podana w TextBoksie txtWyszukaj
        DataSet ds;                                     //Przechowuje tabelę z kodami ZusZla      
        StronaPacjent_Dane stronaPacjent_Dane;          //Utworzenei obiektu strony, na którym będe wykonywał działania

        public Okno_Miasto(StronaPacjent_Dane frm)
        {
            InitializeComponent();
            stronaPacjent_Dane = frm;
            ZaladujMiejscowosc();
        }

        private void Wiersz_MouseEnter(object sender, MouseEventArgs e)
        {
            wiersz = gridMiejscowosc.ItemContainerGenerator.IndexFromContainer((DataGridRow)sender);
            txt.Text = Convert.ToString(wiersz + 1);
        }
        private void Wiersz_MouseDown(object sender, MouseEventArgs e)
        {
            lblUwagi.Content = "";
        }
        public void btnDodajWiersz_Click(object sender, RoutedEventArgs e)
        {
            dodaj = true;                                                //Flaga  dla funkcji gridMiejscowosc_CellEditEnding - zezwalajaca na dodanie nowego rekordu
            //Formatowanie strony
            gridMiejscowosc.CanUserAddRows = true;

            DataGridRow row = gridMiejscowosc.ItemContainerGenerator.ContainerFromIndex(wiersz) as DataGridRow;
            #region UKRYCIE WIERSZY poza nowo dodanym

            for (int i = 0; i < gridMiejscowosc.Items.Count; i++)
            {
                row = gridMiejscowosc.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row != null)
                {
                    row.Focus();
                    row.Visibility = Visibility.Collapsed;
                }
            }

            #endregion
            gridMiejscowosc.Columns[0].IsReadOnly = false;                  //Miejscowosc
            gridMiejscowosc.Columns[1].Visibility = Visibility.Collapsed;   //btnDodaj
            gridMiejscowosc.Columns[2].Visibility = Visibility.Collapsed;   //btnEdytuj
            gridMiejscowosc.Columns[3].Visibility = Visibility.Collapsed;   //btnUsun

        }
        public void btnEdytuj_Click(object sender, RoutedEventArgs e)
        {
            edytuj = true;                                                //Flaga  dla funkcji gridMiejscowosc_CellEditEnding - zezwalajaca na edytowanie kodu choroby
            //Formatowanie strony
            gridMiejscowosc.Columns[0].IsReadOnly = false;                  //Miejscowosc
            gridMiejscowosc.Columns[1].Visibility = Visibility.Collapsed;   //btnDodaj
            gridMiejscowosc.Columns[2].Visibility = Visibility.Collapsed;   //btnEdytuj
            gridMiejscowosc.Columns[3].Visibility = Visibility.Collapsed;   //btnUsun
        }
        public void btnUsun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Paramtry

                wiersz = gridMiejscowosc.SelectedIndex;
                Miejscowosc = dt.Rows[wiersz][0].ToString();

                #endregion
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    try
                    {
                        #region DELETE
                        //Usunięcie z bazy wybranej Miejscowosci

                        SqlCommand cmd = new SqlCommand("spTabMiejscowosc_Delete", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Miejscowosc", Miejscowosc);
                        con.Open();
                        cmd.ExecuteNonQuery();

                        #endregion
                        ZaladujMiejscowosc();
                        lblUwagi.Content = "Usunięto miejscowość: " + Miejscowosc;
                        lblUwagi.Foreground = Brushes.Blue;
                    }
                    catch (SqlException ex)
                    {
                        if (ex.Number == 547) //Próba usunięcia kodu z istniejącym juz powiązaniem (referencja)
                        {
                            MessageBox.Show("Nie można usunąć miejscowości: \n\n" + Miejscowosc + "\n\ngdyż jest ona powiązana z innymi pacjentami.", "Uwaga!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                        else
                            //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                            MessageBox.Show(ex.ToString());
                    }
                }

                stronaPacjent_Dane.ZaladujMiasta();               //Odswieżam listę misat w ComboBox cmbMiejscowosc strony StronaPacjent_Zestawienie
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                MessageBox.Show(ex.ToString());
            }
        }
        private void gridMiejscowosc_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            #region Opis funkcji

            // Funkcja po wyjsciu z komórki datagrida :
            // - spradza czy dane należa do nowego wiersza a jesli tak, to podstawia je do zmiennych
            // - po podstawieniu wszystkich zmiennych wykonywany jest automatyczny INSERT do bazy danych 

            #endregion
            try
            {
                #region Parametry

                var typ = e.GetType().BaseType.ToString();
                typ = sender.GetType().BaseType.ToString();
                typ = sender.GetType().ToString();

                // string zmienna = ((TextBox)(e.EditingElement)).Text;
                int wiersz = gridMiejscowosc.SelectedIndex;

                if (dodaj && wierszOld != wiersz && wierszOld != -1)           //jezeli doszlo do przejscia pomiedzy wierszami w trakcie dodawania kodu - resetuje wszystko
                {
                    #region RESET

                    ZaladujMiejscowosc();                                   //Pobranie listy miejscowosci z tabMiejscowosc do gridMiejscowosc 
                    stronaPacjent_Dane.ZaladujMiasta();                     //Odswieżam listę misat w ComboBox cmbMiejscowosc strony StronaPacjent_Zestawienie
                    dodaj = false;
                    gridMiejscowosc.CanUserAddRows = false;
                    wierszOld = -1;                                     //dla pierwszej edytowanej komórki daje -1 aby od razu nie resetowało
                    Miejscowosc = string.Empty;

                    //Formatowanie strony
                    gridMiejscowosc.Columns[0].IsReadOnly = false;                //Miejscowosc
                    gridMiejscowosc.Columns[1].Visibility = Visibility.Visible;   //btnDodaj
                    gridMiejscowosc.Columns[2].Visibility = Visibility.Visible;   //btnEdytuj
                    gridMiejscowosc.Columns[3].Visibility = Visibility.Visible;   //btnUsun
                    return;

                    #endregion
                }

                //Pobranie zmienianej wartości do zmiennej
                Miejscowosc = ((TextBox)(e.EditingElement)).Text;


                wierszOld = wiersz;
                int ostatniWiersz = gridMiejscowosc.Items.Count;

                #endregion
                lblUwagi.Content = "";

                if (dodaj)      //wcisnieto przycisk dodania kodu choroby btnDodajWiersz_Click
                {
                    #region DODAJ kod

                    if (Miejscowosc != "")
                    {
                        #region INSERT do tabeli tabMiejscowosc
                        //Dodanie do bazy danych nowego kodu choroby ZusZla


                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            try
                            {
                                #region INSERT

                                SqlCommand cmd = new SqlCommand("spTabMiejscowosc_Insert", con);
                                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Miejscowosc", Miejscowosc);
                                con.Open();
                                cmd.ExecuteNonQuery();

                                #endregion
                                dodaj = false;
                                lblUwagi.Content = "Dodano miejscowość: " + Miejscowosc;
                                lblUwagi.Foreground = Brushes.Blue;
                            }
                            catch (System.Data.SqlClient.SqlException ex)
                            {
                                if (ex.Number == 2627)      //Dadanie tej samej danej będacej kluczem tabeki
                                {
                                    MessageBox.Show("Podana miejscowość:\n\n" + Miejscowosc + "\n\njest już na liście.", "Miejscowości...", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                    //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    MessageBox.Show(ex.ToString());
                            }
                        }

                        #endregion
                        Miejscowosc = string.Empty;
                        string temp = stronaPacjent_Dane.cmbMiejscowosc.Text;   //Pobranie zawartości combo przed odswieżeniem nazw miejscowości
                        ZaladujMiejscowosc();                                   //Pobranie listy miejscowosci z tabMiejscowosc do gridMiejscowosc 
                        stronaPacjent_Dane.ZaladujMiasta();                     //Odswieżam listę misat w ComboBox cmbMiejscowosc strony StronaPacjent_Zestawienie
                        stronaPacjent_Dane.cmbMiejscowosc.Text = temp;          //Przywrócenie zawartości combo po odswieżeniem nazw miejscowości
                        gridMiejscowosc.CanUserAddRows = false;
                        dodaj = false;
                        wierszOld = -1;

                        //Formatowanie strony
                        gridMiejscowosc.Columns[0].IsReadOnly = true;
                    }

                    //Formatowanie strony
                    gridMiejscowosc.Columns[1].Visibility = Visibility.Visible;   //btnDodaj
                    gridMiejscowosc.Columns[2].Visibility = Visibility.Visible;   //btnEdytuj
                    gridMiejscowosc.Columns[3].Visibility = Visibility.Visible;   //btnUsun

                    #endregion
                }
                else if (edytuj)                                                                                                    //wcisnieto przycisk edycji kodu choroby btnEdytujj_Click
                {
                    #region EDYCJA

                    edytuj = false;                                                                                                 //Kasowanie flagi
                    string MiejscowoscOld = string.Empty;
                    string MiejscowoscNew = string.Empty;

                    if (jestMiejscowosc)      //Przypadek dla SZUKANEJ miejscowosci => Dane znajudją się w DataTable dtSzukana
                    {
                        MiejscowoscOld = dtSzukana.Rows[wiersz][0].ToString();                                                          //Wprowadzana poprawka dla kodu
                    }
                    else //Przypadek dla edytowanego wiersza przy zaladowanych wszystkich kodach => Dane znajudją się w DataTable dt
                    {
                        if (txtWyszukaj.Text != "" && wierszSzukanegoKodu >= 0) //jest ustawiona jakość wartość w  FILTRZE wyszukiwania pojedynczego kodu (TextBox txtWyszukaj)
                        {
                            MiejscowoscOld = dt.Rows[wiersz][0].ToString();                                                          //Wprowadzana poprawka dla kodu
                        }
                        else
                        {
                            MiejscowoscOld = dt.Rows[wiersz][0].ToString();                                                          //Wprowadzana poprawka dla kodu
                        }
                    }

                    if ((Miejscowosc != MiejscowoscOld && Miejscowosc != "") || jestMiejscowosc)
                    {


                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            try
                            {
                                #region Podstawienie parametów procedury UPDATE

                                if (Miejscowosc.Length > 0) //zmieniono stary kod 
                                {
                                    MiejscowoscNew = Miejscowosc;
                                }
                                else  //zmieniono stary kod 
                                {
                                    MiejscowoscNew = MiejscowoscOld;
                                }


                                #endregion
                                #region UPDATE

                                SqlCommand cmd = new SqlCommand("spTabMiejscowosc_Update", con);
                                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@MiejscowoscNew", MiejscowoscNew);
                                cmd.Parameters.AddWithValue("@Miejscowosc", MiejscowoscOld);
                                con.Open();
                                cmd.ExecuteNonQuery();

                                #endregion
                                lblUwagi.Content = "Poprawiono nazwę z : " + MiejscowoscOld + "  na: " + MiejscowoscNew;
                                lblUwagi.Foreground = Brushes.Blue;
                            }
                            catch (System.Data.SqlClient.SqlException ex)
                            {
                                if (ex.Number == 2627)      //Dadanie tej samej danej będaćej kluczem tabeki
                                {
                                    MessageBox.Show("Podana miejscowość kod:\n\n" + Miejscowosc + "\n\njest już na liście miejscowości!\nPoprawienie nazwy zostało anulowane.", "Miejscowości...", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    MessageBox.Show(ex.ToString());
                                }
                            }
                        }

                    }
                    #region Formatowanie strony

                    ZaladujMiejscowosc();
                    if (stronaPacjent_Dane.cmbMiejscowosc.Text == MiejscowoscOld)
                    {
                        stronaPacjent_Dane.ZaladujMiasta();               //Odswieżam listę misat w ComboBox cmbMiejscowosc strony StronaPacjent_Zestawienie
                        stronaPacjent_Dane.Miasto(MiejscowoscNew);
                    }

                    Miejscowosc = string.Empty;
                    MiejscowoscOld = string.Empty;

                    //Formatowanie strony
                    gridMiejscowosc.Columns[0].IsReadOnly = true;
                    gridMiejscowosc.Columns[1].Visibility = Visibility.Visible;   //btnDodaj
                    gridMiejscowosc.Columns[2].Visibility = Visibility.Visible;   //btnEdytuj
                    gridMiejscowosc.Columns[3].Visibility = Visibility.Visible;   //btnUsun

                    #endregion
                    #endregion
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                MessageBox.Show(ex.ToString());
            }
        }
        private void ZaladujMiejscowosc()
        {
            //Funkcja pobiera wykaz miejscowości z tabMiejscowosc do gridMiejscowosc 
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                try
                {
                    string query = "SELECT * FROM tabMiejscowosc ORDER BY Miejscowosc";        //Pobiera tylko nieusuniete kody
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    ds = new DataSet();
                    sda.Fill(ds);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        if (jestMiejscowosc)  //tryb SZUKANIA kodu
                        {
                            if (dtSzukana != null)
                            {
                                dtSzukana.Clear();
                                dsWyszukiwanie.Tables[0].Clear();
                                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                {
                                    if (ds.Tables[0].Rows[i][0].ToString().Contains(szukanaMiejscowosc))
                                    {
                                        dsWyszukiwanie.Tables[0].Rows.Add(ds.Tables[0].Rows[i][0].ToString());
                                    }
                                }
                            }

                            Miejscowosc = dsWyszukiwanie.Tables[0].Rows[wiersz][0].ToString();
                            gridMiejscowosc.ItemsSource = dsWyszukiwanie.Tables[0].DefaultView;
                            dtSzukana = ((DataView)gridMiejscowosc.ItemsSource).ToTable();
                            gridMiejscowosc.Items.Refresh();
                        }
                        else
                        {
                            gridMiejscowosc.ItemsSource = ds.Tables[0].DefaultView;
                            gridMiejscowosc.Items.Refresh();
                            //Konwersja DataGrid na DataTable
                            dt = ((DataView)gridMiejscowosc.ItemsSource).ToTable();
                            liczbaKodow = ds.Tables[0].Rows.Count;
                        }
                        txt2.Text = "/" + liczbaKodow.ToString();
                        wierszDoDodania = liczbaKodow - 1;
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        private void txtWyszukaj_TextChanged(object sender, TextChangedEventArgs e)
        {
            jestMiejscowosc = false;          //Flaga
            TextBox txtszukana = (TextBox)e.Source;
            szukanaMiejscowosc = txtszukana.Text;
            WyswietlSzukanaMiejscowosc(szukanaMiejscowosc);
        }
        private bool WyswietlSzukanaMiejscowosc(string szukanaMiejsc)
        {
            //Funkcja wyswietla w gridMiejscowosc tylko wiersze z poszukiwanym kodem dane z dsWyszukiwanie

            if (szukanaMiejsc != "") //Jezeli w polu wyszukiwania jest podana wartość
            {
                ds.Tables[0].Clear();
                dsWyszukiwanie = new DataSet();         //DataSet z przefiltrowanymi kodami po szukanej wartości 
                dtSzukana = new DataTable();            //tabela z przefiltrowanymi kodami po szukanej wartości 
                                                        //dsWyszukiwanie.Tables.Add("Table");
                dsWyszukiwanie = ds;    //Kopiuję strukturę tabeli
                string szukana = szukanaMiejsc.ToLower();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i][0].ToString().ToLower().Contains(szukana)) //Jezeli Szukana ="" to zaladuje wszystkie kody. Przypadek gdy wyczysilismy pole szukania
                    {
                        dsWyszukiwanie.Tables[0].Rows.Add(dt.Rows[i][0].ToString());
                        jestMiejscowosc = true;
                        wierszSzukanegoKodu = i;
                    }
                }

                // gridMiejscowosc.ItemsSource = ds.Tables[0].DefaultView;
                gridMiejscowosc.ItemsSource = dsWyszukiwanie.Tables[0].DefaultView;
                dtSzukana = ((DataView)gridMiejscowosc.ItemsSource).ToTable();
                gridMiejscowosc.Items.Refresh();
            }
            else //ładuje wszystkie kody ZusZla do ds
            {
                jestMiejscowosc = false;
                ZaladujMiejscowosc();
            }
            return true;
        }
        private void btnUsunSzukana_Click(object sender, RoutedEventArgs e)
        {
            txtWyszukaj.Text = "";
            ZaladujMiejscowosc();
            gridMiejscowosc.CanUserAddRows = false;
            gridMiejscowosc.Columns[0].IsReadOnly = true;
            gridMiejscowosc.Columns[1].Visibility = Visibility.Visible;   //btnDodaj
            gridMiejscowosc.Columns[2].Visibility = Visibility.Visible;   //btnEdytuj
            gridMiejscowosc.Columns[3].Visibility = Visibility.Visible;   //btnUsun
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (dt.Rows.Count == 0)   //Przypadek, gdy tabela z danymi jest pusta - to pokazuje pusty wiersz
            {
                gridMiejscowosc.CanUserAddRows = true;
            }
        }
    }
}

