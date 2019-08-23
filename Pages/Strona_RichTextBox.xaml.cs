using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using System.Collections;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;

namespace GabinetLekarski.Strony
{
    public partial class Strona_RichTextBox : Page
    {
        #region Parametry

        IList dictionaries;
        StronaWizyta_Zestawienie stronaWizyta_Zestawienie;
        List<TextRange> listaPozycji = new List<TextRange>();                                                   //służy do przechowywania pozycji szukanych slów
        private string wybraneSlowo;
        private string adresSlownika = @"C:\Gabinet\dictionary.lex";
        public string sciezkaPlikuRTF = "";                                                                     //sciezka dostepu do pliku
        private int parametrStrony;
        private double margines = 56.69;                                                                        //Margines wydruku - 1,5cm = 56.69 pix. Odejmuję korektę -5 pix na margines mojej drukarki.
        #endregion

        #region Paramerty startowe formularza

        //Parametr int zawiera informację do jakiej zakladki będzie RTB ladowany.
        //I w zależności od tego będą różne funkcje przypisywane do przycisku BtnSave.
        //1 - Strona_RichTextBox ładowany do zakłdaki WIZYTA
        //2 - Strona_RichTextBox ładowany do zakłdaki HISTORIA
        //3 - Strona_RichTextBox ładowany do zakłdaki SKIEROWANIA

        #endregion
        public Strona_RichTextBox(StronaWizyta_Zestawienie page, int parametrStrony)
        {
            InitializeComponent();
            stronaWizyta_Zestawienie = page;
            this.parametrStrony = parametrStrony;

            #region Załadowanie Fontów do kontroli ComboBoc 'cmbFontFamily' w menu

            cmbFontFamily.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
            cmbFontFamily.FontSize = 16;
            cmbFontSize.ItemsSource = new List<double>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
            cmbFontSize.FontSize = 16;

            #endregion
            #region SŁOWNIK - dodanie słownika i menu kontekstowego

            SpellCheck.SetIsEnabled(rtbEditor, true);                                                                           //Włączenie sprawdzania pisowni
            dictionaries = SpellCheck.GetCustomDictionaries(rtbEditor);                                                         //Dodanie CUSTOM słownika do RichTexBoxa 'rtbEditor'
            //dictionaries.Add(new Uri(@"C:\Gabinet\dictionary.lex"));
            dictionaries.Add(new Uri(adresSlownika));                                                                           //Załadowanie słownika
            rtbEditor.ContextMenu = GetContextMenu();                                                                          //!!! załadowanie kontext menu

            #endregion
            #region Drag&Drop

            // Drag&Drop textu z wykorzystaniem System.Windows.Controls (XAML mi nie chodzi, dlaczego?)
            rtbEditor.AddHandler(RichTextBox.DragOverEvent, new DragEventHandler(RichTextBox_DragOver), true);
            rtbEditor.AddHandler(RichTextBox.DropEvent, new DragEventHandler(RichTextBox_Drop), true);

            #endregion

            rtbEditor.Padding = new Thickness(margines);                                                    //Ustawiam margines wydruku dla RTB
            rtbEditor.AllowDrop = true;
            gdZamien.Visibility = Visibility.Collapsed;
        }

        #region Przyciski Menu

        private void RtbEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //Wytłuszczenie
            object temp = rtbEditor.Selection.GetPropertyValue(Inline.FontWeightProperty);
            btnBold.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(FontWeights.Bold));

            //Podkreślenie
            temp = rtbEditor.Selection.GetPropertyValue(Inline.FontStyleProperty);
            if (temp != null)
                btnUnderline.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextDecorations.Underline));

            //Ustawienie typu czcionki w ComboBox cmbFontFamily
            temp = rtbEditor.Selection.GetPropertyValue(Inline.FontFamilyProperty);
            cmbFontFamily.SelectedItem = temp;

            //Ustawienie rozmiaru czcionki w ComboBox cmbFontSize
            temp = rtbEditor.Selection.GetPropertyValue(Inline.FontSizeProperty);
            cmbFontSize.Text = temp.ToString();
        }
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Rich Text Format (*.rtf)|*.rtf|All files (*.*)|*.*";
            if (dlg.ShowDialog() == true)
            {
                FileStream fileStream = new FileStream(dlg.FileName, FileMode.Open);
                TextRange range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                range.Load(fileStream, DataFormats.Rtf);
            }
        }
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            //ramka1.Navigate(new Strona_RichTextBox());
            //RichTextBox nowy = new RichTextBox();
            //nowy.Show();
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            stronaWizyta_Zestawienie.ZapiszRTB(parametrStrony, false);         //Zapis RTB za pomocą funkcji ze strony StronaWizyta_Zestawienie
        }
        private void CmbFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFontFamily.SelectedItem != null)
                rtbEditor.Selection.ApplyPropertyValue(Inline.FontFamilyProperty, cmbFontFamily.SelectedItem);
        }
        private void CmbFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var selection = rtbEditor.Selection;
                if (!selection.IsEmpty & cmbFontSize.Text != "{DependencyProperty.UnsetValue}")
                {
                    selection.ApplyPropertyValue(TextElement.FontSizeProperty, cmbFontSize.Text);
                }
                else
                {
                    cmbFontSize.Text = "";
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }


        #endregion
        #region CONTEXT MENU i Słownik dla RICHTEXTBOXa

        private ContextMenu GetContextMenu()            // Nowy context menu dla Textoxa. 
        {
            ContextMenu cm = new ContextMenu();
            return cm;
        }
        private void RtbEditor_ContextMenuOpening(object sender, ContextMenuEventArgs e)  //Funkcja wywoływana w momencie kliknieca PKM - w RichTextBoxie
        {
            try
            {
                #region Zmienne

                ContextMenu cm = new ContextMenu();                                 //nowy obiekt context menu
                rtbEditor.ContextMenu = cm;                                         //przypisanie go do RichTextoxa
                int cmdIndex = 0;                                                   //ineks dla context menu
                SpellingError spellingError;                                        //obiekt zwracający sugestie poprawnych słów w przypadku wykrycią błedu 

                #endregion
                #region === CONTEXTMENU TEXTBOX ----------------------------------

                spellingError = rtbEditor.GetSpellingError(rtbEditor.CaretPosition);
                if (spellingError != null)  //Menu pojawiające się w przypadku zaznaczenia tekstu z błednym słowem
                {
                    #region --- załadowanie sugestii dotyczących błednego wyrazu ---
                    foreach (string str in spellingError.Suggestions)
                    {
                        MenuItem mi = new MenuItem();
                        mi.Header = str;
                        mi.FontWeight = FontWeights.Bold;
                        mi.Command = EditingCommands.CorrectSpellingError;
                        mi.CommandParameter = str;
                        mi.CommandTarget = rtbEditor;
                        rtbEditor.ContextMenu.Items.Insert(cmdIndex, mi);
                        cmdIndex++;
                    }

                    #endregion
                    #region --- separator ------------------------------------------

                    Separator separatorMenuItem1 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, separatorMenuItem1);

                    #endregion
                    #region --- Zignoruj wszystkie ---------------------------------

                    cmdIndex++;                                                                             //Zwiększenie indeksu 
                    MenuItem ignorujSlowa = new MenuItem();
                    ignorujSlowa.Header = "Zignoruj wszystkie";
                    ignorujSlowa.Command = EditingCommands.IgnoreSpellingError;
                    ignorujSlowa.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, ignorujSlowa);

                    #endregion
                    #region --- Dodaj do słownika ----------------------------------

                    cmdIndex++;
                    MenuItem Dodaj = new MenuItem
                    {
                        Header = "Dodaj do słownika",
                        CommandTarget = rtbEditor
                    };

                    Dodaj.Click += new RoutedEventHandler(MenuItem_Dodaj_Click);
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, Dodaj);

                    #endregion
                    #region --- separator ------------------------------------------

                    cmdIndex++;
                    Separator separatorMenuItem2 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, separatorMenuItem2);

                    #endregion
                    #region --- Wytnij ---------------------------------------------

                    cmdIndex++;
                    MenuItem Wytnij = new MenuItem();
                    Wytnij.Header = "Wytnij";
                    Wytnij.Command = ApplicationCommands.Cut;
                    Wytnij.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, Wytnij);
                    Wytnij.Icon = new System.Windows.Controls.Image
                    {

                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/wytnij.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Kopiuj ---------------------------------------------

                    cmdIndex++;
                    MenuItem Kopiuj = new MenuItem();
                    Kopiuj.Header = "Kopiuj";
                    Kopiuj.Command = ApplicationCommands.Copy;
                    Kopiuj.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, Kopiuj);
                    Kopiuj.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/kopiuj.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Wklej ----------------------------------------------

                    cmdIndex++;
                    MenuItem Wklej = new MenuItem();
                    Wklej.Header = "Wklej";
                    Wklej.Command = ApplicationCommands.Paste;
                    Wklej.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, Wklej);
                    Wklej.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/wklej.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Zaznaczenie wybranego słowa ------------------------

                    cmdIndex++;
                    //Zaznaczenie wybranego słowa i pobranie so zmiennej 'wybraneSlowo'
                    TextRange left = ExtendSelection(LogicalDirection.Backward);
                    TextRange right = ExtendSelection(LogicalDirection.Forward);
                    if (!left.IsEmpty && !right.IsEmpty)
                    {
                        rtbEditor.Selection.Select(left.Start, right.End);
                        wybraneSlowo = rtbEditor.Selection.Text;
                    }

                    #endregion
                    #region --- separator ------------------------------------------


                    Separator separatorMenuItem3 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, separatorMenuItem3);

                    #endregion
                    #region --- Format tekstu --------------------------------------

                    cmdIndex++;
                    MenuItem formatujTekst = new MenuItem();
                    formatujTekst.Header = "Format tekstu";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, formatujTekst);
                    formatujTekst.Click += FormatujTekst_Click;
                    formatujTekst.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/kolor_tekstu-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Kolor tekstu ---------------------------------------

                    cmdIndex++;
                    MenuItem kolorTekstu = new MenuItem();
                    kolorTekstu.Header = "Kolor tekstu";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, kolorTekstu);
                    kolorTekstu.Click += KolorTekstu_Click;
                    kolorTekstu.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/paleta_kolorow-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Kolor podłoża --------------------------------------

                    cmdIndex++;
                    MenuItem kolorPodloza = new MenuItem();
                    kolorPodloza.Header = "Kolor podłoża";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, kolorPodloza);
                    kolorPodloza.Click += KolorPodloza_Click;
                    kolorPodloza.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/paleta_kolorow-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- separator ------------------------------------------

                    cmdIndex++;
                    Separator separatorMenuItem4 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, separatorMenuItem4);

                    #endregion
                    #region --- Zamień tekst ---------------------------------------

                    cmdIndex++;
                    MenuItem zamienTekst = new MenuItem();
                    zamienTekst.Header = "Zamień tekst";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, zamienTekst);
                    zamienTekst.Click += ZamienTekst_Click;

                    #endregion
                }
                else  //Menu pojawiające się w przypadku zaznaczenia tekstu nie będacym błednym słowem
                {
                    #region --- Wytnij ---------------------------------------------

                    MenuItem Wytnij = new MenuItem();
                    Wytnij.Header = "Wytnij";
                    Wytnij.Command = ApplicationCommands.Cut;
                    Wytnij.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, Wytnij);
                    Wytnij.Icon = new System.Windows.Controls.Image
                    {

                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/wytnij-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Kopiuj ---------------------------------------------

                    cmdIndex++;
                    MenuItem Kopiuj = new MenuItem();
                    Kopiuj.Header = "Kopiuj";
                    Kopiuj.Command = ApplicationCommands.Copy;
                    Kopiuj.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, Kopiuj);
                    Kopiuj.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/kopiuj.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Wklej ----------------------------------------------

                    cmdIndex++;
                    MenuItem Wklej = new MenuItem();
                    Wklej.Header = "Wklej";
                    Wklej.Command = ApplicationCommands.Paste;
                    Wklej.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, Wklej);
                    Wklej.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/wklej.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- separator ------------------------------------------

                    cmdIndex++;
                    Separator separatorMenuItem3 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, separatorMenuItem3);

                    #endregion
                    #region --- Format tekstu --------------------------------------

                    cmdIndex++;
                    MenuItem formatujTekst = new MenuItem();
                    formatujTekst.Header = "Format tekstu";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, formatujTekst);
                    formatujTekst.Click += FormatujTekst_Click;
                    formatujTekst.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/kolor_tekstu-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Kolor tekstu ---------------------------------------

                    cmdIndex++;
                    MenuItem kolorTekstu = new MenuItem();
                    kolorTekstu.Header = "Kolor tekstu";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, kolorTekstu);
                    kolorTekstu.Click += KolorTekstu_Click;
                    kolorTekstu.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/paleta_kolorow-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Kolor podłoża --------------------------------------

                    cmdIndex++;
                    MenuItem kolorPodloza = new MenuItem();
                    kolorPodloza.Header = "Kolor podłoża";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, kolorPodloza);
                    kolorPodloza.Click += KolorPodloza_Click;
                    kolorPodloza.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/paleta_kolorow-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Zaznacz obrazy -------------------------------------

                    cmdIndex++;
                    MenuItem zaznaczObrazy = new MenuItem();
                    zaznaczObrazy.Header = "Zaznacz obrazy";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, zaznaczObrazy);
                    zaznaczObrazy.Icon = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(@"/Images/zmien-rozmiar-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    #endregion
                    #region --- Zaznacz obrazy Proporcjonlanie ---------------------

                    MenuItem zaznaczProporcjonalnie = new MenuItem();
                    zaznaczObrazy.Items.Add(zaznaczProporcjonalnie);
                    zaznaczProporcjonalnie.Header = "Rozciąganie proporcjonalne";
                    zaznaczProporcjonalnie.Click += OdznaczObrazy_Click;
                    zaznaczProporcjonalnie.Click += ZaznaczObrazyProp_Click;
                    zaznaczProporcjonalnie.Icon = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(@"/Images/zmien-rozmiar-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Zaznacz obrazy Dowolnie ----------------------------

                    MenuItem zaznaczDowolnie = new MenuItem();
                    zaznaczObrazy.Items.Add(zaznaczDowolnie);
                    zaznaczDowolnie.Header = "Rozciąganie dowolne";
                    zaznaczDowolnie.Click += OdznaczObrazy_Click;
                    zaznaczDowolnie.Click += ZaznaczObrazyDow_Click;
                    zaznaczDowolnie.Icon = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(@"/Images/zmien-rozmiar-nieproporcjonalnie-.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };


                    #endregion
                    #region --- Odznacz obrazy -------------------------------------

                    cmdIndex++;
                    MenuItem odznaczObrazy = new MenuItem();
                    odznaczObrazy.Header = "Odznacz obrazy";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, odznaczObrazy);
                    odznaczObrazy.Click += OdznaczObrazy_Click;
                    odznaczObrazy.Icon = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(@"/Images/zmien-rozmiar-usun-.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- separator ------------------------------------------

                    cmdIndex++;
                    Separator separatorMenuItem4 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, separatorMenuItem4);

                    #endregion
                    #region --- Zamień tekst ---------------------------------------

                    cmdIndex++;
                    MenuItem zamienTekst = new MenuItem();
                    zamienTekst.Header = "Zamień tekst";
                    rtbEditor.ContextMenu.Items.Insert(cmdIndex, zamienTekst);
                    zamienTekst.Click += ZamienTekst_Click;

                    #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region Funkcje CONTEXTMENU TEXTBOX 

        private void KolorTekstu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = rtbEditor.Selection;
                System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var kolor = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                    selection.ApplyPropertyValue(TextElement.ForegroundProperty, kolor);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void KolorPodloza_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = rtbEditor.Selection;
                System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var kolor = new SolidColorBrush(Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
                    selection.ApplyPropertyValue(TextElement.BackgroundProperty, kolor);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void ZaznaczObrazyProp_Click(object sender, RoutedEventArgs e)
        {
            ResizeImages(true, true);
        }
        private void ZaznaczObrazyDow_Click(object sender, RoutedEventArgs e)
        {
            ResizeImages(true, false);
        }
        private void OdznaczObrazy_Click(object sender, RoutedEventArgs e)
        {
            ResizeImages(false, true);
        }
        private void ZamienTekst_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (gdZamien.Visibility == Visibility.Collapsed)
                    gdZamien.Visibility = Visibility.Visible;
                else
                    gdZamien.Visibility = Visibility.Collapsed;
                txbZnajdz.Text = "";
                txbZamien.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private TextRange ExtendSelection(LogicalDirection direction)                                                               //do nowego kontext menu w RTB --- Zaznaczenie wybranego słowa ---
        {
            TextRange tr = new TextRange(rtbEditor.CaretPosition, rtbEditor.CaretPosition.GetInsertionPosition(direction));
            bool found = false;
            while (!found)
            {
                if (tr == null)
                {
                    break;
                }
                else
                {
                    // If we are not at the end of the document (or at the beginning)
                    TextPointer next = null;
                    if (LogicalDirection.Forward.CompareTo(direction) == 0 && tr.End.CompareTo(rtbEditor.Document.ContentEnd) == -1)
                    {
                        next = tr.End.GetNextInsertionPosition(direction);
                    }
                    else if (LogicalDirection.Backward.CompareTo(direction) == 0 && tr.Start.CompareTo(rtbEditor.Document.ContentStart) == 1)
                    {
                        next = tr.Start.GetNextInsertionPosition(direction);
                    }

                    // Be careful with boundaries!
                    if (next != null)
                    {
                        TextRange trNext = new TextRange(rtbEditor.CaretPosition, next);
                        char[] text = trNext.Text.ToCharArray();
                        for (int i = 0; i < text.Length; i++)
                        {
                            if (Char.IsWhiteSpace(text[i]) || Char.IsSeparator(text[i]))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            tr = trNext;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return tr;
        }
        private void MenuItem_Dodaj_Click(object sender, EventArgs e)                                                               //do nowego kontext menu w RTB --- Zaznaczenie wybranego słowa ---
        {
            //ZAPIS DO SŁOWNIKA
            dictionaries.Remove(new Uri(@"C:\Gabinet\dictionary.lex"));                                                                //Odłaczenie słownika
            if (!File.Exists(@"C:\Gabinet\dictionary.lex"))                                                                            //Utworzenie pliku słownika jak go nie ma
            {
                StreamWriter sw2 = File.CreateText(@"C:\Gabinet\dictionary.lex");
                sw2.Close();
            }
            else    //zapis słowa do słownika
            {
                FileStream fs = File.OpenWrite(@"C:\Gabinet\dictionary.lex");
                StreamWriter sw = new StreamWriter(fs);
                sw.BaseStream.Seek(0, SeekOrigin.End);                                                                              //oznacza ustawienie wskaźnika na końcu pliku.
                sw.WriteLine(wybraneSlowo);
                sw.Close();
                fs.Close();
            }
            dictionaries.Add(new Uri(@"C:\Gabinet\dictionary.lex"));
        }

        private void FormatujTekst_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selection = rtbEditor.Selection;
                System.Windows.Forms.FontDialog fontDialog = new System.Windows.Forms.FontDialog();
                if (fontDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!selection.IsEmpty)
                    {
                        selection.ApplyPropertyValue(TextElement.FontSizeProperty, Convert.ToDouble(fontDialog.Font.Size));
                        selection.ApplyPropertyValue(TextElement.FontFamilyProperty, fontDialog.Font.Name);
                        selection.ApplyPropertyValue(TextElement.FontWeightProperty, fontDialog.Font.Bold ? FontWeights.Bold : FontWeights.Normal);
                        selection.ApplyPropertyValue(TextElement.FontStyleProperty, fontDialog.Font.Italic ? FontStyles.Italic : FontStyles.Normal);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void ResizeImages(bool wlaczycZnaczniki, bool rozcProporcjonalne)
        {
            #region Opis funkcji [ResizeImages]
            /*
             *Funkcja wyszukuje obrazy w całym RichTextBox i dodajen na rogach znaczniku do modyfikowania rozmiaru obrazu - z pomoca klasy ResizingAdorner
             * Parametr wlaczycZnaczniki:
             *    - true - włączenie znaczników rozciągania obrazu
             *    - false - wyłączenie znaczników rozciągania obrazu
             * Parametr rozcProporcjonalne:
             *    - true - rozciąganie proporcjonalne, 
             *    - false - rozciąganie w dowolnym kirunku
             */
            #endregion
            IEnumerable<Image> images = rtbEditor.Document.Blocks.OfType<BlockUIContainer>()
                .Select(c => c.Child).OfType<Image>()
                .Union(rtbEditor.Document.Blocks.OfType<Paragraph>()
                .SelectMany(pg => pg.Inlines.OfType<InlineUIContainer>())
                .Select(inline => inline.Child).OfType<Image>());

            foreach (var image in images)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(image);
                if (adornerLayer != null) //true - włączenie znaczników
                {
                    if (wlaczycZnaczniki)
                    {
                        adornerLayer.Add(new ResizingAdorner(image, rozcProporcjonalne));  //true - rozciąganie proporcjonalne, false - rozciąganie w dowolnym kirunku
                    }
                    else  //false - wyłączenie znaczników
                    {
                        Adorner[] toRemoveArray = adornerLayer.GetAdorners(image);
                        Adorner toRemove;
                        if (toRemoveArray != null)
                        {
                            toRemove = toRemoveArray[0];
                            adornerLayer.Remove(toRemove);
                        }
                    }
                }
            }
        }

        #endregion

        #endregion
        #region Funkcje Drag & Drop

        private void RichTextBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }
        private void RichTextBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                RichTextBoxDagDrop DD = new RichTextBoxDagDrop();
                DD.Drop(rtbEditor, sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
      

        #endregion
        #region PRINT    

        private void btnPrintRTB_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                FrmPodgladWydruku frmPodgladWydruku = new FrmPodgladWydruku(rtbEditor);
                frmPodgladWydruku.ShowDialog();

                #region Print - XpsDocumentWriter (rtbEditor.Document)

                FlowDocument document = rtbEditor.Document;

                //Tworzę kopię FlowDocument pod wydruk 
                string copyString = XamlWriter.Save(rtbEditor.Document);
                FlowDocument kopiaFlowDocument = XamlReader.Parse(copyString) as FlowDocument;
                MemoryStream memorySrteam = new MemoryStream();                                                                     //Przygotowanie się do przechowywania zawartości w pamięci

                //Utworzenie obiektu XpsDocumentWriter, niejawnie otwierając okno dialogowe wspólnego drukowania w systemie Windows, pozwalając użytkownikowi wybrać drukarkę.
                //i pobranie informacje o wymiarach wybranej drukarki i nośnika.
   

                System.Printing.PrintDocumentImageableArea ia = null;
                System.Windows.Xps.XpsDocumentWriter docWriter = System.Printing.PrintQueue.CreateXpsDocumentWriter(ref ia);        //tu następuje otwarcie okno dialogowego drukowania który zwraca prarametr - referencje: ia
                                                                                                                                    //parametr (ia) reprezentuje informacje o obszarze obrazowania i wymiarze nośnika.
                                                                                                                                    //rozmiar strony fizyczny: 
                if (docWriter != null && ia != null)
                {
                    DocumentPaginator paginator = ((IDocumentPaginatorSource)kopiaFlowDocument).DocumentPaginator;                  ///Dzielenie zawartości na strony.
                    //Zmiana rozmiar PageSize i PagePadding dla dokumentu tak, aby pasował do CanvasSize drukarki.
                    paginator.PageSize = new Size(ia.MediaSizeWidth, ia.MediaSizeHeight);                                           //Ustawia rozmiar stron zgodnie z wymiarem fizycznym kartki papieru. (793/1122)
                    Thickness t = new Thickness(margines + kopiaFlowDocument.PagePadding.Left);                                     //Ustawiam marginesy wydruku w PagePadding. Należy zastosować korektę o starowy margines drukarki . Tu daje tyle co RichTextBox Domyślnie pobiera z drukarki i wynoszą (5,0,5,0) piksela 
                    kopiaFlowDocument.PagePadding = new Thickness(                                                                  //Ustawienie nowego obszaru zadrukowania strony PagePadding
                                     Math.Max(ia.OriginWidth, t.Left),
                                       Math.Max(ia.OriginHeight, t.Top),
                                       Math.Max(ia.MediaSizeWidth - (ia.OriginWidth + ia.ExtentWidth), t.Right),
                                       Math.Max(ia.MediaSizeHeight - (ia.OriginHeight + ia.ExtentHeight), t.Bottom));
                    kopiaFlowDocument.ColumnWidth = double.PositiveInfinity;                                                        //Ustawienie szerokości kolumny drukowanej. double.PositiveInfinity - reprezentuje nieskończoności dodatniej. To pole jest stałe.
                      docWriter.Write(paginator);


                }
                //Czyszczenie pamieci
                GC.Collect();
                GC.WaitForPendingFinalizers();
                memorySrteam.Dispose();                                                                                              //Zwolnienie zasobów pamięci
                #endregion
            }

            catch (Exception ex)
            {
                //MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
                MessageBox.Show(ex.ToString());
            }
        }

        #endregion
        #region Funkcja zamiany tekstu 

        private void BtnSzukaj_Click(object sender, RoutedEventArgs e)
        {

            string szukaneSlowo = txbSzukaj.Text;
            IEnumerable<TextRange> wordRanges = GetAllWordRanges(rtbEditor.Document);
            foreach (TextRange wordRange in wordRanges)
            {
                if (wordRange.Text == szukaneSlowo)
                {
                    listaPozycji.Add(wordRange);
                    //wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                    wordRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
                    rtbEditor.Selection.Select(wordRange.Start, wordRange.End);
                }
            }

            foreach (TextRange pozycja in listaPozycji)
            {
                rtbEditor.Selection.Select(pozycja.Start, pozycja.End);
            }
            listaPozycji.Clear();

        }
        public static IEnumerable<TextRange> GetAllWordRanges(FlowDocument document)
        {
            string pattern = @"[^\W\d](\w|[-']{1,2}(?=\w))*";
            TextPointer pointer = document.ContentStart;
            pointer = document.ContentStart;
            while (pointer != null)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                    MatchCollection matches = Regex.Matches(textRun, pattern);
                    foreach (Match match in matches)
                    {
                        int startIndex = match.Index;
                        int length = match.Length;
                        TextPointer start = pointer.GetPositionAtOffset(startIndex);
                        TextPointer end = start.GetPositionAtOffset(length);
                        yield return new TextRange(start, end);
                    }
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        #endregion
        #region Funkcja wyszukiwania tekstu 

        private void btnZamien_Click(object sender, RoutedEventArgs e)
        {
            //string keyword = "theStringToBeReplaced";
            string znajdz = txbZnajdz.Text;
            string zamien = txbZamien.Text;
            TextRange text = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
            TextPointer current = text.Start.GetInsertionPosition(LogicalDirection.Forward);
            while (current != null)
            {
                string textInRun = current.GetTextInRun(LogicalDirection.Forward);
                if (!string.IsNullOrWhiteSpace(textInRun))
                {
                    int index = textInRun.IndexOf(znajdz);
                    if (index != -1)
                    {
                        TextPointer selectionStart = current.GetPositionAtOffset(index, LogicalDirection.Forward);
                        TextPointer selectionEnd = selectionStart.GetPositionAtOffset(znajdz.Length, LogicalDirection.Forward);
                        TextRange selection = new TextRange(selectionStart, selectionEnd);
                        selection.Text = zamien;
                        //selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                        rtbEditor.Selection.Select(selection.Start, selection.End);
                        rtbEditor.Focus();

                    }
                }
                current = current.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
        private void txbSzukaj_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tekst = sender as TextBox;

            if (tekst.Text == "Szukany tekst")
            {
                tekst.Text = "";
                txbSzukaj.Foreground = Brushes.Black;
                txbSzukaj.FontSize = 16;
            }

        }
        private void txbSzukaj_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tekst = sender as TextBox;
            if (tekst.Text == "")
            {
                tekst.Text = "Szukany tekst";
                txbSzukaj.Foreground = Brushes.LightGray;
                txbSzukaj.FontSize = 14;
            }
        }

        #endregion

        private void rtbEditor_MouseMove(object sender, MouseEventArgs e)
        {
            //pobranie pozycji kursora
            TextPointer startPtr = rtbEditor.Document.ContentStart;

            //pobranie pozycji karetki
            int start = startPtr.GetOffsetToPosition(rtbEditor.CaretPosition);
            txtSzukaj.Content = Convert.ToString(start);
        }
    }
}
