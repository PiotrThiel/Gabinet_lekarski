using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.SqlClient;
using System.Data;
using System.Collections.ObjectModel;
using System.Configuration;
using GabinetLekarski.Klasy;

namespace GabinetLekarski.Strony
{
    public enum ZusZlaColumn
    {
        DiseaseCode = 0,
        Description = 1,
        Add = 2,
        Edit = 3,
        Delete = 4
    }
    public enum ZusZlaView
    {
        Standard=0,
        Add =1,
        Edit=2
    }

    public partial class StronaZusZla : Page
    {
        #region Parametry

        int wiersz;                                                                                                                 //Indeks grida gridKodZusZla nad którym znajduje się wskażnik myszki
        int wierszOld = -1;                                                                                                         //Indeks poprzedniego wiersza grida
        int liczbaKodow;                                                                                                            //Liczba rekordów załadowana do grida - PRZED dodaniem dodatkowego rekordu (kodu choroby)
        int wierszDoDodania;                                                                                                        //Numer wiersza nowego kodu ZusZla (ostani) który nalezy dodać dao bazy
        int wierszSzukanegoKodu = -1;
        int UniqueKeyViolation = 2627;
        int foreignKeyViolations = 547;
        string KodChoroby = string.Empty;                                                                                           //Zawiera kod choroby który bedzie zapisany w bazie danych
        string Opis = string.Empty;                                                                                                 //Zawiera opis choroby który bedzie zapisany w bazie danych 
        string info = string.Empty;
        string szukanyKodZusZla = string.Empty;                                                                                     //Szukany kod ZusZla podany w TexBoksie txtWyszukaj 
        bool dodaj;                                                                                                                 //Flaga  dla funkcji gridKodZusZla_CellEditEnding - zezwalajaca na dodanie nowego rekordu
        bool edytuj;                                                                                                                 //Flaga  dla funkcji gridKodZusZla_CellEditEnding - zezwalajaca na edytowanie kodu choroby
        bool jestKodZusZla = false;                                                                                                 //Flaga - informująca funkcję ZaladujZusZla czy w DataGridzie wyświetlić tylko szukany wiersz (po txtBox txtWyszukaj) czy wszystkie
        DataTable dt;                                                                                                               //Zawiera wykaz wszystkich kodów chorób
        DataTable dtSzukana;                                                                                                        //Zawiera tylko wiersz z szukanym kodem choroby
        DataSet dsWyszukiwanie;                                                                                                     //Przechowuje tabelę z 1 wierszem SZUKANEGO KODU ZusZla,który podana w TextBoksie txtWyszukaj
        DataSet ds;                                                                                                                 //Przechowuje tabelę z kodami ZusZla      
        ObservableCollection<KlZusZla> obs = new ObservableCollection<KlZusZla>();                                                  //lista typu ObservableCollection zawierająca obieky typu KlZusZla   - zawiera wykaz kodów chorób ZusZla
        StronaWizyta_Zestawienie stronaWizyta_Zestawienie;                                                                          //Formularz matki - do przechwycenia funkcji ZaladujZusZla(); 

        #endregion

        public StronaZusZla(StronaWizyta_Zestawienie frm)
        {
            InitializeComponent();
            stronaWizyta_Zestawienie = frm;
            ZaladujZusZla();
        }

        private void Wiersz_MouseEnter(object sender, MouseEventArgs e)
        {
            wiersz = gridKodZusZla.ItemContainerGenerator.IndexFromContainer((DataGridRow)sender);
            txt.Text = Convert.ToString(wiersz);
        }

        private void Wiersz_MouseDown(object sender, MouseEventArgs e)
        {
            lblStronaZusZla.Content = "";
        }

        public void btnDodajWiersz_Click(object sender, RoutedEventArgs e)
        {
            dodaj = true;
            lblStronaZusZla.Content = "";
            gridKodZusZla.CanUserAddRows = true;
            DataGridRow row = gridKodZusZla.ItemContainerGenerator.ContainerFromIndex(wiersz) as DataGridRow;

            ZusZlaFormatView(Convert.ToInt32(ZusZlaView.Add));

            for (int i = 0; i < gridKodZusZla.Items.Count; i++)
            {
                row = gridKodZusZla.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                //Hide all lines except the newly added
                if (row != null)
                {
                    row.Focus();
                    row.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void btnEdytuj_Click(object sender, RoutedEventArgs e)
        {
            edytuj = true;
            lblStronaZusZla.Content = "";
            ZusZlaFormatView(Convert.ToInt32(ZusZlaView.Edit));
        }

        public void btnUsun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblStronaZusZla.Content = "";
                wiersz = gridKodZusZla.SelectedIndex;
                KodChoroby = dt.Rows[wiersz][0].ToString();
                ZusZlaDelete();
                ZaladujZusZla();
                stronaWizyta_Zestawienie.ZaladujZusZla();
                stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                var typ = e.GetType().BaseType.ToString();
                typ = sender.GetType().BaseType.ToString();
                typ = sender.GetType().ToString();
                string zmienna = ((TextBox)(e.EditingElement)).Text;
                info = string.Empty;
                int wiersz = gridKodZusZla.SelectedIndex;

                if (dodaj && wierszOld != wiersz && wierszOld != -1)                    //jezeli doszlo do przejscio pomiedzy wierszami w trakcie dodawania kodu - resetuje wszystko
                {
                    #region RESET

                    ZaladujZusZla();
                    stronaWizyta_Zestawienie.ZaladujZusZla();
                    stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);

                    dodaj = false;
                    gridKodZusZla.CanUserAddRows = false;
                    wierszOld = -1;                                                     //dla pierwszej edytowanej komórki daje -1 aby od razu nie resetowało
                    KodChoroby = string.Empty;
                    Opis = string.Empty;
                    ZusZlaFormatView(Convert.ToInt32(ZusZlaView.Standard));
                    return;

                    #endregion
                }

                string nazwaKolumny = e.Column.Header.ToString();                    

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
                lblStronaZusZla.Content = "";

                if (dodaj)
                {
                    #region ADD

                    if (KodChoroby != "" && Opis != "")
                    {
                        #region INSERT do tabeli tabWykazSkierowan

                        try
                        {
                            ZusZlaInsert();
                            dodaj = false;
                            info = "Dodano nowy kod choroby: " + KodChoroby;
                        }
                        catch (System.Data.SqlClient.SqlException ex)
                        {
                            if (ex.Number == UniqueKeyViolation)
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
                                    ZusZlaUpdateRemoved(false);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        #endregion

                        ZaladujZusZla();
                        stronaWizyta_Zestawienie.ZaladujZusZla();
                        stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);
                        ZusZlaFormatView(Convert.ToInt32(ZusZlaView.Standard));

                        KodChoroby = string.Empty;
                        Opis = string.Empty;
                        gridKodZusZla.CanUserAddRows = false;
                        dodaj = false;
                        wierszOld = -1;
                    }
                    #endregion
                }
                else if (edytuj)
                {
                    #region EDIT

                    edytuj = false;
                    string StaryKodChoroby = string.Empty;
                    string StaryOpis = string.Empty;
                    if (jestKodZusZla)
                    {
                        StaryKodChoroby = dtSzukana.Rows[wiersz][0].ToString();
                        StaryOpis = dtSzukana.Rows[wiersz][1].ToString();
                    }
                    else //Przypadek dla edytowanego wiersza przy zaladowanych wszystkich kodach => Dane znajudją się w DataTable dt
                    {
                        if (txtWyszukaj.Text != "" && wierszSzukanegoKodu >= 0) //Przypadek gdy jest ustawiona wartość w FILTRZE wyszukiwania pojedynczego kodu (TextBox txtWyszukaj)
                        {
                            StaryKodChoroby = dt.Rows[wiersz][0].ToString();  
                            StaryOpis = dt.Rows[wiersz][1].ToString();     
                        }
                        else
                        {
                            StaryKodChoroby = dt.Rows[wiersz][0].ToString(); 
                            StaryOpis = dt.Rows[wiersz][1].ToString();      
                        }
                    }

                    if ((KodChoroby != StaryKodChoroby && KodChoroby != "") || (Opis != StaryOpis && Opis != "") || jestKodZusZla)
                    {
                        #region UPDATE

                        try
                        {
                            string NowyKod = string.Empty;
                            string Op = string.Empty;

                            #region Podstawienie parametów zapytania

                            if (KodChoroby.Length > 0)  // ZusZla is Changed
                            {
                                NowyKod = KodChoroby;
                            }
                            else
                            {
                                NowyKod = StaryKodChoroby;
                            }

                            if (Opis.Length > 0) // ZusZla is Changed
                            {
                                Op = Opis;
                            }
                            else
                            {
                                Op = StaryOpis;
                            }
                            #endregion

                            ZusZlaUpdate(StaryKodChoroby, NowyKod, Op);
                            info = "Poprawiono dane kodu choroby";

                        }
                        catch (System.Data.SqlClient.SqlException ex)
                        {
                            if (ex.Number == UniqueKeyViolation)  
                            {
                                MessageBox.Show("Podany kod:\n\n" + KodChoroby + "\n\njest już na liście kodów ZusZla!\nPoprawienie kodu zostało anulowane.", "Kody chorób ZUS ZLA...", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                        #endregion
                    }

                    ZaladujZusZla();
                    stronaWizyta_Zestawienie.ZaladujZusZla();
                    stronaWizyta_Zestawienie.Zus(stronaWizyta_Zestawienie.wiersz);
                    ZusZlaFormatView(Convert.ToInt32(ZusZlaView.Standard));

                    KodChoroby = string.Empty;
                    Opis = string.Empty;

                    StaryKodChoroby = string.Empty;
                    StaryOpis = string.Empty;

                    #endregion
                }
                lblStronaZusZla.Content = info;
                lblStronaZusZla.Foreground = Brushes.Blue;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ZaladujZusZla()
        {
            //Funkcja pobiera wykaz kodów chorób z tabKodChoroby do gridKodZusZla
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                try
                {
                    string query = "SELECT * FROM tabKodChoroby WHERE Usuniety = 0 ORDER BY KodChoroby ";        //Pobiera tylko nieusuniete kody
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
                            //Conversion DataGrid to DataTable
                            dt = ((DataView)gridKodZusZla.ItemsSource).ToTable();
                            liczbaKodow = ds.Tables[0].Rows.Count;
                        }
                        txt2.Text = "Liczba kodów: " + liczbaKodow.ToString();
                        wierszDoDodania = liczbaKodow - 1;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (dt.Rows.Count == 0)
            {
                gridKodZusZla.CanUserAddRows = true;
            }
        }

        private void txtWyszukaj_TextChanged(object sender, TextChangedEventArgs e)
        {
            jestKodZusZla = false;       
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
                        jestKodZusZla = true;        
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
            if (szukanyKod != "") 
            {
                ds.Tables[0].Clear();
                dsWyszukiwanie = new DataSet();
                dtSzukana = new DataTable();   
                dsWyszukiwanie = ds;   
                string szukana = szukanyKod;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (dt.Rows[i][0].ToString().Contains(szukana))
                    {
                        dsWyszukiwanie.Tables[0].Rows.Add(dt.Rows[i][0].ToString(), dt.Rows[i][1].ToString(), dt.Rows[i][2].ToString());
                        jestKodZusZla = true;
                        wierszSzukanegoKodu = i;
                    }
                }

                gridKodZusZla.ItemsSource = dsWyszukiwanie.Tables[0].DefaultView;
                dtSzukana = ((DataView)gridKodZusZla.ItemsSource).ToTable();
                gridKodZusZla.Items.Refresh();
            }
            else
            {
                jestKodZusZla = false;
                ZaladujZusZla();
            }
            return true;
        }

        private void btnUsunSzukana_Click(object sender, RoutedEventArgs e)
        {
            txtWyszukaj.Text = "";
            ZaladujZusZla();
            ZusZlaFormatView(Convert.ToInt32(ZusZlaView.Standard));
        }

        private void ZusZlaFormatView(int typeOfView)
        {
            if (typeOfView == Convert.ToInt32(ZusZlaView.Add) || typeOfView == Convert.ToInt32(ZusZlaView.Edit))
            {
                gridKodZusZla.Columns[(int)ZusZlaColumn.DiseaseCode].IsReadOnly = false;
                gridKodZusZla.Columns[(int)ZusZlaColumn.Description].IsReadOnly = false;
                gridKodZusZla.Columns[(int)ZusZlaColumn.Add].Visibility = Visibility.Collapsed;
                gridKodZusZla.Columns[(int)ZusZlaColumn.Edit].Visibility = Visibility.Collapsed;
                gridKodZusZla.Columns[(int)ZusZlaColumn.Delete].Visibility = Visibility.Collapsed;
            }
            else if (typeOfView == Convert.ToInt32(ZusZlaView.Standard))
            {
                gridKodZusZla.Columns[(int)ZusZlaColumn.DiseaseCode].IsReadOnly = true;
                gridKodZusZla.Columns[(int)ZusZlaColumn.Description].IsReadOnly = true;
                gridKodZusZla.Columns[(int)ZusZlaColumn.Add].Visibility = Visibility.Visible;
                gridKodZusZla.Columns[(int)ZusZlaColumn.Edit].Visibility = Visibility.Visible;
                gridKodZusZla.Columns[(int)ZusZlaColumn.Delete].Visibility = Visibility.Visible;
                gridKodZusZla.CanUserAddRows = false;
            }
        }

        private void ZusZlaDelete()
        {
            info = string.Empty;
            
            try
            {
               using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                {
                    SqlCommand cmd = new SqlCommand("spTabKodChoroby_Delete", con);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@KodChoroby", KodChoroby);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    info = "Usunięto kod choroby: " + KodChoroby;
                }
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                if (ex.Number == foreignKeyViolations)
                {
                    ZusZlaUpdateRemoved(true);
                    info = "Ukryto kod choroby: " + KodChoroby;
                }
                else
                {
                    MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            ZaladujZusZla();
            lblStronaZusZla.Content = info;
            lblStronaZusZla.Foreground = Brushes.Blue;
        }

        private void ZusZlaUpdate(string StaryKodChoroby, string NowyNowyKodChoroby, string Opis)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("spTabKodChoroby_Update", con);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@KodChoroby", StaryKodChoroby);
                cmd.Parameters.AddWithValue("@NowyKodChoroby", NowyNowyKodChoroby);
                cmd.Parameters.AddWithValue("@Opis", Opis);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        
        private void ZusZlaUpdateRemoved(bool hide)
        {
            ///Ukrycie kodu choroby (Usuniety = True) z uwagi na istniejące powiązania (juz gdzies w kartotece jest odnośnik do usuwanego kodu, więc nie można go usunąć)
            try
            {
                MessageBoxResult odpowiedz;
                if (hide)
                {
                    odpowiedz = MessageBox.Show("Z uwagi na to, że podany kod choroby:\n" + KodChoroby + "\nzawiera już dokumentacja medyczna, można go jedynie ukryć.\n\nUkryć ten kod?", "Kody chorób ZUS ZLA...", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                }
                else
                {
                    odpowiedz = MessageBox.Show("Podany kod:\n" + KodChoroby + "\nbył już wcześniej podany i istnieją powiązane z nim dokumenty.\n\nPrzywrócić kod?", "Kody chorób ZUS ZLA...", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                }

                if (odpowiedz == MessageBoxResult.OK)                                                                      //YES => ustawia kod choroby jako ponownie widoczny: Usuniety=False
                {
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
                    {
                        SqlCommand cmd = new SqlCommand("spTabKodChoroby_Update_Usuniety", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@KodChoroby", KodChoroby);
                        cmd.Parameters.AddWithValue("@Usuniety", Convert.ToInt32(hide));
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ZusZlaInsert()
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("spTabKodChoroby_Insert", con);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@KodChoroby", KodChoroby);
                cmd.Parameters.AddWithValue("@Opis", Opis);
                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

    }
}

