using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
gffgfhfgfh
-
using System.Data.SqlClient;
using System.Data;
using System.Collections.ObjectModel;

namespace GabinetLekarski.Strony
{
    public partial class StronaZusZla : Page
    {
        #region Parametry

        int wiersz;                                                                                                                 //Indeks grida gridKodZusZla nad którym znajduje się wskażnik myszki
        int wierszOld = -1;                                                                                                         //Indeks poprzedniego wiersza grida
        int liczbaKodow;                                                                                                            //Liczba rekordów załadowana do grida - PRZED dodaniem dodatkowego rekordu (kodu choroby)
        int wierszDoDodania;                                                                                                        //Numer wiersza nowego kodu ZusZla (ostani) który nalezy dodać dao bazy
        int wierszSzukanegoKodu = -1;
        string KodChoroby = string.Empty;                                                                                           //Zawiera kod choroby który bedzie zapisany w bazie danych
        string Opis = string.Empty;                                                                                                 //Zawiera opis choroby który bedzie zapisany w bazie danych 
        string szukanyKodZusZla = string.Empty;//Szukany kod ZusZla podany w TexBoksie txtWyszukaj 
        ObservableCollection<KlZusZla> obs = new ObservableCollection<KlZusZla>();                                                  //lista typu ObservableCollection zawierająca obieky typu KlZusZla   - zawiera wykaz kodów chorób ZusZla
        StronaWizyta_Zestawienie stronaWizyta_Zestawienie;                                                                          //Formularz matki - do przechwycenia funkcji ZaladujZusZla(); 
        bool dodaj;
        bool edytuj;
        bool jestKodZusZla = false;                                                                                                 //Flaga - informująca funkcję ZaladujZusZla czy w DataGridzie wyświetlić tylko szukany wiersz (po txtBox txtWyszukaj) czy wszystkie
        DataTable dt;                                                                                                               //Zawiera wykaz wszystkich kodów chorób
        DataTable dtSzukana;                                                                                                        //Zawiera tylko wiersz z szukanym kodem choroby
        DataSet dsWyszukiwanie;                                                                                                     //Przechowuje tabelę z 1 wierszem SZUKANEGO KODU ZusZla,który podana w TextBoksie txtWyszukaj
        DataSet ds;                                                                                                                 //Przechowuje tabelę z kodami ZusZla      

        #endregion

        public StronaZusZla(StronaWizyta_Zestawienie frm)
        {
            InitializeComponent();
            stronaWizyta_Zestawienie = frm;
            ZaladujZusZLA();
        }

        private void Wiersz_MouseEnter(object sender, MouseEventArgs e)
        {
            wiersz = gridKodZusZla.ItemContainerGenerator.IndexFromContainer((DataGridRow)sender);
            txt.Text = Convert.ToString(wiersz);
            lblStronaZusZla.Content = "";
            //KodChoroby = string.Empty;
            //Opis = string.Empty;

        }
        private void Wiersz_MouseDown(object sender, MouseEventArgs e)
        {
            lblStronaZusZla.Content = "";
        }
        #region Funkcje DataGrid gridKodZusZla

        public void btnDodajWiersz_Click(object sender, RoutedEventArgs e)
        {
            dodaj = true;                                                //Flaga  dla funkcji gridKodZusZla_CellEditEnding - zezwalajaca na dodanie nowego rekordu

            //Formatowanie strony
            lblStronaZusZla.Content = "";
            gridKodZusZla.CanUserAddRows = true;

            DataGridRow row = gridKodZusZla.ItemContainerGenerator.ContainerFromIndex(wiersz) as DataGridRow;
            for (int i = 0; i < gridKodZusZla.Items.Count; i++)
            {
                row = gridKodZusZla.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                if (row != null) //Ukrywam wszystkie wiersze poza nowo dodanym
                {
                    row.Focus();
                    row.Visibility = Visibility.Collapsed;
                }
            }
            //var typ = sender.GetType().ToString();
            gridKodZusZla.Columns[0].IsReadOnly = false;
            gridKodZusZla.Columns[1].IsReadOnly = false;
            gridKodZusZla.Columns[2].Visibility = Visibility.Collapsed;   //btnDodaj
            gridKodZusZla.Columns[3].Visibility = Visibility.Collapsed;   //btnEdytuj
            gridKodZusZla.Columns[4].Visibility = Visibility.Collapsed;   //btnUsun

        }
        public void btnEdytuj_Click(object sender, RoutedEventArgs e)
        {
            edytuj = true;                                                //Flaga  dla funkcji gridKodZusZla_CellEditEnding - zezwalajaca na edytowanie kodu choroby
            //Formatowanie strony
            lblStronaZusZla.Content = "";
            gridKodZusZla.Columns[0].IsReadOnly = false;
            gridKodZusZla.Columns[1].IsReadOnly = false;
            gridKodZusZla.Columns[2].Visibility = Visibility.Collapsed;   //btnDodaj
            gridKodZusZla.Columns[3].Visibility = Visibility.Collapsed;   //btnEdytuj
            gridKodZusZla.Columns[4].Visibility = Visibility.Collapsed;   //btnUsun
        }
        public void btnUsun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Paramtry

                lblStronaZusZla.Content = "";
                wiersz = gridKodZusZla.SelectedIndex;
                KodChoroby = dt.Rows[wiersz][0].ToString();

                #endregion
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    try
                    {
                        #region DELETE

                        //Usunięcie z bazy wybranego kodu choroby ZusZla
                        SqlCommand cmd = new SqlCommand("spTabKodChoroby_Delete", con);
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@KodChoroby", KodChoroby);
                        con.Open();
                        cmd.ExecuteNonQuery();

                        #endregion
                        ZaladujZusZLA();
                        lblStronaZusZla.Content = "Usunięto kod choroby: " + KodChoroby;
                        lblStronaZusZla.Foreground = Brushes.Blue;
                    }
                    catch (System.Data.SqlClient.SqlException ex)
                    {
                        if (ex.Number == 547) //Próba usunięcia kodu z istniejącym juz powiązaniem (referencja)
                        {
                            #region UPDATE - Usunięcie (ukrycie) kodu choroby w tabeli => Usuniety= True (1)
                            
                            //Ukrycie kodu z uwagi na istniejące powiązania (juz gdzies w kartotece jest odnośnik do usuwanego kodu, więc nie można go usunąć)
                            SqlCommand cmd = new SqlCommand("spTabKodChoroby_Update_Usuniety", con);
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@KodChoroby", KodChoroby);
                            cmd.Parameters.AddWithValue("@Usuniety", 1);
                            cmd.ExecuteNonQuery();

                            #endregion
                            ZaladujZusZLA();
                            lblStronaZusZla.Content = "Usunięto kod choroby: " + KodChoroby;
                            lblStronaZusZla.Foreground = Brushes.Blue;
                        }
                        else
                            //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                            MessageBox.Show(ex.ToString());
                    }
                }
                stronaWizyta_Zestawienie.ZaladujZusZla();               //Odswieżam kody chorób ZusZla w ComboBox formularza StronaWizyta_Zestawienie
                stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);  //Odswieżam wyświetlenie numeru kodu w ComboBox
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                MessageBox.Show(ex.ToString());
            }
        }
        private void gridKodZusZla_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
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
                string zmienna = ((TextBox)(e.EditingElement)).Text;
                int wiersz = gridKodZusZla.SelectedIndex;

                if (dodaj && wierszOld != wiersz && wierszOld != -1)                                                                        //jezeli doszlo do przejscio pomiedzy wierszami w trakcie dodawania kodu - resetuje wszystko
                {
                    #region RESET

                    ZaladujZusZLA();
                    stronaWizyta_Zestawienie.ZaladujZusZla();                                                                               //Odswieżam kody chorób ZusZla w ComboBox formularza StronaWizyta_Zestawienie
                    stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);                                                          //Odswieżam wyświetlenie numeru kodu w ComboBox

                    dodaj = false;
                    gridKodZusZla.CanUserAddRows = false;
                    wierszOld = -1;                                                                                                         //dla pierwszej edytowanej komórki daje -1 aby od razu nie resetowało
                    KodChoroby = string.Empty;
                    Opis = string.Empty;

                    //Formatowanie strony
                    gridKodZusZla.Columns[2].Visibility = Visibility.Visible;                                                               //btnDodaj
                    gridKodZusZla.Columns[3].Visibility = Visibility.Visible;                                                               //btnEdytuj
                    gridKodZusZla.Columns[4].Visibility = Visibility.Visible;                                                               //btnUsun

                    gridKodZusZla.Columns[0].IsReadOnly = false;
                    gridKodZusZla.Columns[1].IsReadOnly = false;
                    return;

                    #endregion
                }

                string nazwaKolumny = e.Column.Header.ToString();                                                                           //Pobranie nazwy kolumny kliknietej komórki
                //Pobranie zmienianej wartości do zmiennej w zalezności od nazwy kolumny
                if (nazwaKolumny.ToString() == "Kod choroby" && zmienna != "")
                {
                    KodChoroby = zmienna;
                }
                else if (nazwaKolumny.ToString() == "Opis" && zmienna != "")
                {
                    Opis = zmienna;
                }


                wierszOld = wiersz;
                int ostatniWiersz = gridKodZusZla.Items.Count;

                #endregion
                lblStronaZusZla.Content = "";

                if (dodaj)                                                                                                                  //wcisnieto przycisk dodania kodu choroby btnDodajWiersz_Click
                {
                    #region DODAJ kod

                    if (KodChoroby != "" && Opis != "")
                    {
                        #region INSERT do tabeli tabWykazSkierowan

                        //Dodanie do bazy danych nowego kodu choroby ZusZla
                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            try
                            {
                                SqlCommand cmd = new SqlCommand("spTabKodChoroby_Insert", con);
                                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@KodChoroby", KodChoroby);
                                cmd.Parameters.AddWithValue("@Opis", Opis);
                                con.Open();
                                cmd.ExecuteNonQuery();
                                dodaj = false;
                                lblStronaZusZla.Content = "Dodano nowy kod choroby";
                                lblStronaZusZla.Foreground = Brushes.Blue;
                                stronaWizyta_Zestawienie.ZaladujZusZla();                                                                   //Odswieżam kody chorób ZusZla w ComboBox formularza StronaWizyta_Zestawienie
                            }
                            catch (System.Data.SqlClient.SqlException ex)
                            {
                                if (ex.Number == 2627)                                                                                      //Dadanie tej samej danej będaćej kluczem tabeki
                                {
                                    bool jestNaLiscie = false;

                                    for (int i = 0; i < dt.Rows.Count; i++)
                                    {
                                        if (KodChoroby == dt.Rows[i][0].ToString())
                                        {
                                            MessageBox.Show("Podany kod:\n\n" + KodChoroby + "\n\njest już na liście.", "Kody chorób ZUS ZLA...", MessageBoxButton.OK, MessageBoxImage.Information);
                                            jestNaLiscie = true;
                                            break;
                                        }
                                    }
                                    if (!jestNaLiscie)
                                    {
                                        MessageBoxResult odpowiedz;
                                        odpowiedz = MessageBox.Show("Podany kod:\n\n" + KodChoroby + "\n\nbył już wcześniej podany i istnieją powiązane z nim dokumenty.\n Przywrócić kod?", "Kody chorób ZUS ZLA...", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                                        if (odpowiedz == MessageBoxResult.OK)                                                               //YES => ustawia kod choroby jako ponownie widoczny: Usuniety=False
                                        {
                                            #region UPDATE - Przywrócenie wcześniej usunietego|ukrytego kodu choroby (Usunięty = False|0)

                                            SqlCommand cmd = new SqlCommand("spTabKodChoroby_Update_Usuniety", con);
                                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                                            cmd.Parameters.AddWithValue("@KodChoroby", KodChoroby);
                                            cmd.Parameters.AddWithValue("@Usuniety", 0);
                                            cmd.ExecuteNonQuery();

                                            #endregion
                                            lblStronaZusZla.Content = "Przywrócono kod choroby: " + KodChoroby;
                                            lblStronaZusZla.Foreground = Brushes.Blue;
                                        }
                                        else if (odpowiedz == MessageBoxResult.Cancel)                                                                  
                                        {
                                            lblStronaZusZla.Content = "Dodawanie kodu   " + KodChoroby + "    zostało anulowane.";      
                                            lblStronaZusZla.Foreground = Brushes.Blue;
                                        }
                                        ZaladujZusZLA();                                                                                    //Ponowne zaladowanie datagrida gridKodZusZla kodami chorób
                                        stronaWizyta_Zestawienie.ZaladujZusZla();                                                           //Ponowne załadowanie ComboBoxów cmbZusZla i cmbZusZlaOld w formularzu nadrzędnym
                                        stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);                                      //Odsieżenie ComboBoxa cmbZusZla formularza nadrzędnego, gdzie WIERSZ - jest to index zaznaczonej osoby w treeViewGodziny wzięty z tegoż nadrzednego formularza
                                    }
                                }
                                else
                                    //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    MessageBox.Show(ex.ToString());
                            }
                        }
                        #endregion
                        KodChoroby = string.Empty;
                        Opis = string.Empty;
                        ZaladujZusZLA();
                        stronaWizyta_Zestawienie.ZaladujZusZla();                                                                           //Odswieżam kody chorób ZusZla w ComboBox formularza StronaWizyta_Zestawienie
                        stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);                                                      //Odswieżam wyświetlenie numeru kodu w ComboBox
                        gridKodZusZla.CanUserAddRows = false;
                        dodaj = false;
                        wierszOld = -1;

                        //Formatowanie strony
                        gridKodZusZla.Columns[0].IsReadOnly = true;
                        gridKodZusZla.Columns[1].IsReadOnly = true;
                    }

                    //Formatowanie strony
                    gridKodZusZla.Columns[2].Visibility = Visibility.Visible;                                                               //btnDodaj
                    gridKodZusZla.Columns[3].Visibility = Visibility.Visible;                                                               //btnEdytuj
                    gridKodZusZla.Columns[4].Visibility = Visibility.Visible;                                                               //btnUsun

                    #endregion
                }
                else if (edytuj)                                                                                                            //wcisnieto przycisk edycji kodu choroby btnEdytujj_Click
                {
                    #region EDYCJA

                    edytuj = false;                                                                                                         //Kasowanie flagi
                    string StaryKodChoroby = string.Empty;
                    string StaryOpis = string.Empty;
                    if (jestKodZusZla)                                                                                                      //Przypadek dla SZUKANEGO kodu ZuzZla => Dane znajudją się w DataTable dtSzukana
                    {
                        StaryKodChoroby = dtSzukana.Rows[wiersz][0].ToString();                                                             //Wprowadzana poprawka dla kodu
                        StaryOpis = dtSzukana.Rows[wiersz][1].ToString();
                    }
                    else                                                                                                                    //Przypadek dla edytowanego wiersza przy zaladowanych wszystkich kodach => Dane znajudją się w DataTable dt
                    {
                        if (txtWyszukaj.Text != "" && wierszSzukanegoKodu >= 0)                                                             //jest ustawiona jakość wartość w  FILTRZE wyszukiwania pojedynczego kodu (TextBox txtWyszukaj)
                        {
                            StaryKodChoroby = dt.Rows[wiersz][0].ToString();                                                                //Wprowadzana poprawka dla kodu
                            StaryOpis = dt.Rows[wiersz][1].ToString();                                                                      //Wprowadzana poprawka dla opisu
                        }
                        else
                        {
                            StaryKodChoroby = dt.Rows[wiersz][0].ToString();                                                                //Wprowadzana poprawka dla kodu
                            StaryOpis = dt.Rows[wiersz][1].ToString();                                                                      //Wprowadzana poprawka dla opisu
                        }
                    }

                    if ((KodChoroby != StaryKodChoroby && KodChoroby != "") || (Opis != StaryOpis && Opis != "") || jestKodZusZla)
                    {
                        #region UPDATE

                        using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                        {
                            try
                            {
                                string NowyKod = string.Empty;
                                string Op = string.Empty;



                                #region Podstawienie parametów procedury UPDATE

                                if (KodChoroby.Length > 0)                                                                                  //zmieniono stary kod 
                                {
                                    NowyKod = KodChoroby;
                                }
                                else                                                                                                        //zmieniono stary kod 
                                {
                                    NowyKod = StaryKodChoroby;
                                }

                                if (Opis.Length > 0)                                                                                        //zmieniono stary kod 
                                {
                                    Op = Opis;
                                }
                                else                                                                                                        //zmieniono stary kod 
                                {
                                    Op = StaryOpis;
                                }

                                #endregion

                                SqlCommand cmd = new SqlCommand("spTabKodChoroby_Update", con);
                                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@KodChoroby", StaryKodChoroby);
                                cmd.Parameters.AddWithValue("@NowyKodChoroby", NowyKod);
                                cmd.Parameters.AddWithValue("@Opis", Op);
                                con.Open();
                                cmd.ExecuteNonQuery();
                                lblStronaZusZla.Content = "Poprawiono dane kodu choroby";
                                lblStronaZusZla.Foreground = Brushes.Blue;
                            }
                            catch (System.Data.SqlClient.SqlException ex)
                            {
                                if (ex.Number == 2627)                                                                                      //Dadanie tej samej danej będaćej kluczem tabeki
                                {
                                    MessageBox.Show("Podany kod:\n\n" + KodChoroby + "\n\njest już na liście kodów ZusZla!\nPoprawienie kodu zostało anulowane.", "Kody chorób ZUS ZLA...", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.Message.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    MessageBox.Show(ex.ToString());
                                }
                            }
                        }
                        #endregion
                    }
                    #region Formatowanie strony

                    ZaladujZusZLA();
                    stronaWizyta_Zestawienie.ZaladujZusZla();                                                                               //Odswieżam kody chorób ZusZla w ComboBox formularza StronaWizyta_Zestawienie
                    stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);                                                          //Odswieżam wyświetlenie numeru kodu w ComboBox
                    KodChoroby = string.Empty;
                    Opis = string.Empty;
                    StaryKodChoroby = string.Empty;
                    StaryOpis = string.Empty;

                    //Formatowanie strony
                    gridKodZusZla.Columns[0].IsReadOnly = true;
                    gridKodZusZla.Columns[1].IsReadOnly = true;
                    gridKodZusZla.Columns[2].Visibility = Visibility.Visible;                                                               //btnDodaj
                    gridKodZusZla.Columns[3].Visibility = Visibility.Visible;                                                               //btnEdytuj
                    gridKodZusZla.Columns[4].Visibility = Visibility.Visible;                                                               //btnUsun

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
        private void ZaladujZusZLA()
        {
            //Funkcja pobiera wykaz kodów  chorób  z tabKodChoroby do gridKodZusZla
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                try
                {
                    string query = "SELECT * FROM tabKodChoroby WHERE Usuniety = 0 ORDER BY KodChoroby ";                                   //Pobiera tylko nieusuniete kody
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    SqlDataAdapter sda = new SqlDataAdapter(cmd);
                    ds = new DataSet();
                    sda.Fill(ds);
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        if (jestKodZusZla)  //tryb SZUKANIA kodu
                        {
                            if (dtSzukana != null)
                            {
                                dtSzukana.Clear();
                                dsWyszukiwanie.Tables[0].Clear();
                                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                {
                                    if (ds.Tables[0].Rows[i][0].ToString().Contains(szukanyKodZusZla))
                                    {
                                        dsWyszukiwanie.Tables[0].Rows.Add(ds.Tables[0].Rows[i][0].ToString(), ds.Tables[0].Rows[i][1].ToString(), ds.Tables[0].Rows[i][2].ToString());
                                    }
                                }
                            }

                            Opis = dsWyszukiwanie.Tables[0].Rows[wiersz][1].ToString();
                            KodChoroby = dsWyszukiwanie.Tables[0].Rows[wiersz][0].ToString();
                            gridKodZusZla.ItemsSource = dsWyszukiwanie.Tables[0].DefaultView;
                            dtSzukana = ((DataView)gridKodZusZla.ItemsSource).ToTable();
                            gridKodZusZla.Items.Refresh();
                        }
                        else
                        {
                            gridKodZusZla.ItemsSource = ds.Tables[0].DefaultView;
                            gridKodZusZla.Items.Refresh();
                            //Konwersja DataGrid na DataTable
                            dt = ((DataView)gridKodZusZla.ItemsSource).ToTable();
                            liczbaKodow = ds.Tables[0].Rows.Count;
                        }
                        txt2.Text = "Liczba kodów: " + liczbaKodow.ToString();
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

        #endregion
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (dt.Rows.Count == 0)                                                                                                         //Przypadek, gdy tabela z danymi jest pusta - to pokazuje pusty wiersz
            {
                gridKodZusZla.CanUserAddRows = true;
            }
        }
        private void txtWyszukaj_TextChanged(object sender, TextChangedEventArgs e)
        {
            jestKodZusZla = false;          //Flaga
            TextBox txtszukana = (TextBox)e.Source;
            szukanyKodZusZla = txtszukana.Text;
            WyswietlSzukanyKodZusZla(szukanyKodZusZla);
        }
        private bool WierszSzukanegoKodu(string szukanyKod)
        {
            string szukana = szukanyKod;
            if (szukana != "")
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (szukana == dt.Rows[i][0].ToString())
                    {
                        jestKodZusZla = true;                                                                                               //Flaga - informująca funkcję ZaladujZusZla czy w DataGridzie wyświetlić tylko szukany wiersz (po txtBox txtWyszukaj) czy wszystkie
                        wierszSzukanegoKodu = i;
                        return true;
                    }
                }
                return false;
            }
            else
            {
                wierszSzukanegoKodu = -1;
                jestKodZusZla = false;
                return true;
            }
        }
        private bool WyswietlSzukanyKodZusZla(string szukanyKod)
        {
            //Funkcja wyswietla w gridKodZusZla tylko wiersze z poszukiwanym kodem dane z dsWyszukiwanie

            if (szukanyKod != "") //Jezeli w polu wyszukiwania jest podana wartość
            {
                ds.Tables[0].Clear();
                dsWyszukiwanie = new DataSet();                                                                                             //DataSet z przefiltrowanymi kodami po szukanej wartości 
                dtSzukana = new DataTable();                                                                                                //tabela z przefiltrowanymi kodami po szukanej wartości 
                                                                                                                                            //dsWyszukiwanie.Tables.Add("Table");
                dsWyszukiwanie = ds;                                                                                                        //Kopiuję strukturę tabeli
                string szukana = szukanyKod;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i][0].ToString().Contains(szukana))                                                                         //Jezeli Szukana ="" to zaladuje wszystkie kody. Przypadek gdy wyczysilismy pole szukania
                    {
                        //ds.Tables[0].Rows.Add(dt.Rows[i][0].ToString(), dt.Rows[i][1].ToString(), dt.Rows[i][2].ToString());
                        dsWyszukiwanie.Tables[0].Rows.Add(dt.Rows[i][0].ToString(), dt.Rows[i][1].ToString(), dt.Rows[i][2].ToString());
                        jestKodZusZla = true;
                        wierszSzukanegoKodu = i;
                    }
                }

                //gridKodZusZla.ItemsSource = ds.Tables[0].DefaultView;
                gridKodZusZla.ItemsSource = dsWyszukiwanie.Tables[0].DefaultView;
                dtSzukana = ((DataView)gridKodZusZla.ItemsSource).ToTable();
                gridKodZusZla.Items.Refresh();
            }
            else //ładuje wszystkie kody ZusZla do ds
            {
                jestKodZusZla = false;
                ZaladujZusZLA();
            }
            return true;
        }
        private void btnUsunSzukana_Click(object sender, RoutedEventArgs e)
        {
            txtWyszukaj.Text = "";
            ZaladujZusZLA();
            gridKodZusZla.CanUserAddRows = false;
            gridKodZusZla.Columns[0].IsReadOnly = true;
            gridKodZusZla.Columns[1].IsReadOnly = true;
            gridKodZusZla.Columns[2].Visibility = Visibility.Visible;                                                                       //btnDodaj
            gridKodZusZla.Columns[3].Visibility = Visibility.Visible;                                                                       //btnEdytuj
            gridKodZusZla.Columns[4].Visibility = Visibility.Visible;                                                                       //btnUsun
        }
    }
}

