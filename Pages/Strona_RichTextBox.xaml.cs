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
using GabinetLekarski.Klasy;
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
        public string sciezkaPlikuRTF = "";
        private int parametrStrony;
        private double margines = 56.69;

        #endregion

        #region Paramerty startowe strony

        //Parametr int zawiera informację do jakiej zakladki będzie RTB ladowany.
        //I w zależności od tego będą różne funkcje przypisywane do przycisku BtnSave.
        //1 - Strona_RichTextBox ładowany do zakłdaki WIZYTA
        //2 - Strona_RichTextBox ładowany do zakłdaki HISTORIA
        //3 - Strona_RichTextBox ładowany do zakłdaki SKIEROWANIA

        #endregion

        public Strona_RichTextBox(StronaWizyta_Zestawienie page, int parametrStrony)
        {
            try
            {
                InitializeComponent();
                stronaWizyta_Zestawienie = page;
                this.parametrStrony = parametrStrony;

                cmbFontFamily.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
                cmbFontFamily.FontSize = 16;
                cmbFontSize.ItemsSource = new List<double>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
                cmbFontSize.FontSize = 16;

                SpellCheck.SetIsEnabled(rtbEditor, true);
                dictionaries = SpellCheck.GetCustomDictionaries(rtbEditor);
                dictionaries.Add(new Uri(adresSlownika));
                rtbEditor.ContextMenu = GetContextMenu();

                //Drag&Drop
                rtbEditor.AddHandler(RichTextBox.DragOverEvent, new DragEventHandler(RichTextBox_DragOver), true);
                rtbEditor.AddHandler(RichTextBox.DropEvent, new DragEventHandler(RichTextBox_Drop), true);
                rtbEditor.Padding = new Thickness(margines);
                rtbEditor.AllowDrop = true;

                gdZamien.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        #region Menu

        private void RtbEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                object temp = rtbEditor.Selection.GetPropertyValue(Inline.FontWeightProperty);
                btnBold.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(FontWeights.Bold));

                temp = rtbEditor.Selection.GetPropertyValue(Inline.FontStyleProperty);
                if (temp != null)
                    btnUnderline.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextDecorations.Underline));

                //Set type of font in ComboBox cmbFontFamily
                temp = rtbEditor.Selection.GetPropertyValue(Inline.FontFamilyProperty);
                cmbFontFamily.SelectedItem = temp;

                //Set Size of fot in ComboBox cmbFontSize
                temp = rtbEditor.Selection.GetPropertyValue(Inline.FontSizeProperty);
                cmbFontSize.Text = temp.ToString();
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            stronaWizyta_Zestawienie.ZapiszRTB(parametrStrony, false);
        }

        private void CmbFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbFontFamily.SelectedItem != null)
                    rtbEditor.Selection.ApplyPropertyValue(Inline.FontFamilyProperty, cmbFontFamily.SelectedItem);
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
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
                ErrorMessage(ex);
            }

        }


        #endregion
        #region Context menu & Dictionary

        private ContextMenu GetContextMenu()     
        {
            ContextMenu cm = new ContextMenu();
            return cm;
        }

        private void RtbEditor_ContextMenuOpening(object sender, ContextMenuEventArgs e) 
        {
            //Funkcja wywołuje menu kontekstowe w momencie kliknieca PKM - w RichTextBoxie
            try
            {
                ContextMenu cm = new ContextMenu();   
                rtbEditor.ContextMenu = cm;         
                int contextIndex = 0;                                                   
                SpellingError spellingError;                                 

                #region === CONTEXTMENU TEXTBOX ----------------------------------

                spellingError = rtbEditor.GetSpellingError(rtbEditor.CaretPosition);
                //error menu
                if (spellingError != null) 
                {
                    //Load sugestions of wrong word
                    foreach (string str in spellingError.Suggestions)
                    {
                        MenuItem mi = new MenuItem();
                        mi.Header = str;
                        mi.FontWeight = FontWeights.Bold;
                        mi.Command = EditingCommands.CorrectSpellingError;
                        mi.CommandParameter = str;
                        mi.CommandTarget = rtbEditor;
                        rtbEditor.ContextMenu.Items.Insert(contextIndex, mi);
                        contextIndex++;
                    }

                    #region --- separator ------------------------------------------

                    Separator separatorMenuItem1 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, separatorMenuItem1);

                    #endregion
                    #region --- Ignore all -----------------------------------------

                    contextIndex++;                                                                         
                    MenuItem ignorujSlowa = new MenuItem();
                    ignorujSlowa.Header = "Zignoruj wszystkie";
                    ignorujSlowa.Command = EditingCommands.IgnoreSpellingError;
                    ignorujSlowa.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, ignorujSlowa);

                    #endregion
                    #region --- Add to dictionary ----------------------------------

                    contextIndex++;
                    MenuItem Dodaj = new MenuItem
                    {
                        Header = "Dodaj do słownika",
                        CommandTarget = rtbEditor
                    };

                    Dodaj.Click += new RoutedEventHandler(MenuItem_Dodaj_Click);
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, Dodaj);

                    #endregion
                    #region --- separator ------------------------------------------

                    contextIndex++;
                    Separator separatorMenuItem2 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, separatorMenuItem2);

                    #endregion
                    #region --- Cut ------------------------------------------------

                    contextIndex++;
                    MenuItem Wytnij = new MenuItem();
                    Wytnij.Header = "Wytnij";
                    Wytnij.Command = ApplicationCommands.Cut;
                    Wytnij.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, Wytnij);
                    Wytnij.Icon = new System.Windows.Controls.Image
                    {

                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/wytnij.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Copy -----------------------------------------------

                    contextIndex++;
                    MenuItem Kopiuj = new MenuItem();
                    Kopiuj.Header = "Kopiuj";
                    Kopiuj.Command = ApplicationCommands.Copy;
                    Kopiuj.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, Kopiuj);
                    Kopiuj.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/kopiuj.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Insert ---------------------------------------------

                    contextIndex++;
                    MenuItem Wklej = new MenuItem();
                    Wklej.Header = "Wklej";
                    Wklej.Command = ApplicationCommands.Paste;
                    Wklej.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, Wklej);
                    Wklej.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/wklej.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Select word ----------------------------------------

                    //Zaznaczenie wybranego słowa i pobranie so zmiennej 'wybraneSlowo'
                    contextIndex++;
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
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, separatorMenuItem3);

                    #endregion
                    #region --- Text format ----------------------------------------

                    contextIndex++;
                    MenuItem formatujTekst = new MenuItem();
                    formatujTekst.Header = "Format tekstu";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, formatujTekst);
                    formatujTekst.Click += FormatujTekst_Click;
                    formatujTekst.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/kolor_tekstu-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Text color -----------------------------------------

                    contextIndex++;
                    MenuItem kolorTekstu = new MenuItem();
                    kolorTekstu.Header = "Kolor tekstu";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, kolorTekstu);
                    kolorTekstu.Click += KolorTekstu_Click;
                    kolorTekstu.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/paleta_kolorow-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Background color -----------------------------------

                    contextIndex++;
                    MenuItem kolorPodloza = new MenuItem();
                    kolorPodloza.Header = "Kolor podłoża";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, kolorPodloza);
                    kolorPodloza.Click += KolorPodloza_Click;
                    kolorPodloza.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/paleta_kolorow-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- separator ------------------------------------------

                    contextIndex++;
                    Separator separatorMenuItem4 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, separatorMenuItem4);

                    #endregion
                    #region --- Change text ----------------------------------------

                    contextIndex++;
                    MenuItem zamienTekst = new MenuItem();
                    zamienTekst.Header = "Zamień tekst";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, zamienTekst);
                    zamienTekst.Click += ZamienTekst_Click;

                    #endregion
                }
                else  //Standard menu
                {
                    #region --- Cut ------------------------------------------------

                    MenuItem Wytnij = new MenuItem();
                    Wytnij.Header = "Wytnij";
                    Wytnij.Command = ApplicationCommands.Cut;
                    Wytnij.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, Wytnij);
                    Wytnij.Icon = new System.Windows.Controls.Image
                    {

                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/wytnij-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Copy -----------------------------------------------

                    contextIndex++;
                    MenuItem Kopiuj = new MenuItem();
                    Kopiuj.Header = "Kopiuj";
                    Kopiuj.Command = ApplicationCommands.Copy;
                    Kopiuj.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, Kopiuj);
                    Kopiuj.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/kopiuj.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Insert ---------------------------------------------

                    contextIndex++;
                    MenuItem Wklej = new MenuItem();
                    Wklej.Header = "Wklej";
                    Wklej.Command = ApplicationCommands.Paste;
                    Wklej.CommandTarget = rtbEditor;
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, Wklej);
                    Wklej.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/wklej.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- separator ------------------------------------------

                    contextIndex++;
                    Separator separatorMenuItem3 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, separatorMenuItem3);

                    #endregion
                    #region --- Text formt -----------------------------------------

                    contextIndex++;
                    MenuItem formatujTekst = new MenuItem();
                    formatujTekst.Header = "Format tekstu";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, formatujTekst);
                    formatujTekst.Click += FormatujTekst_Click;
                    formatujTekst.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/kolor_tekstu-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Tekst color ----------------------------------------

                    contextIndex++;
                    MenuItem kolorTekstu = new MenuItem();
                    kolorTekstu.Header = "Kolor tekstu";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, kolorTekstu);
                    kolorTekstu.Click += KolorTekstu_Click;
                    kolorTekstu.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/paleta_kolorow-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Backgroud color ------------------------------------

                    contextIndex++;
                    MenuItem kolorPodloza = new MenuItem();
                    kolorPodloza.Header = "Kolor podłoża";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, kolorPodloza);
                    kolorPodloza.Click += KolorPodloza_Click;
                    kolorPodloza.Icon = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"/Images/paleta_kolorow-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- Select picture -------------------------------------

                    contextIndex++;
                    MenuItem zaznaczObrazy = new MenuItem();
                    zaznaczObrazy.Header = "Zaznacz obrazy";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, zaznaczObrazy);
                    zaznaczObrazy.Icon = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(@"/Images/zmien-rozmiar-16.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    #endregion
                    #region --- Select picture - proportionally --------------------

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
                    #region --- Select picture - freely ----------------------------

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
                    #region --- Unselect picture -----------------------------------

                    contextIndex++;
                    MenuItem odznaczObrazy = new MenuItem();
                    odznaczObrazy.Header = "Odznacz obrazy";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, odznaczObrazy);
                    odznaczObrazy.Click += OdznaczObrazy_Click;
                    odznaczObrazy.Icon = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(@"/Images/zmien-rozmiar-usun-.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    #endregion
                    #region --- separator ------------------------------------------

                    contextIndex++;
                    Separator separatorMenuItem4 = new Separator();
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, separatorMenuItem4);

                    #endregion
                    #region --- Change text tekst ----------------------------------

                    contextIndex++;
                    MenuItem zamienTekst = new MenuItem();
                    zamienTekst.Header = "Zamień tekst";
                    rtbEditor.ContextMenu.Items.Insert(contextIndex, zamienTekst);
                    zamienTekst.Click += ZamienTekst_Click;

                    #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        #region Context menu function's 

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
                ErrorMessage(ex);
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
                ErrorMessage(ex);
            }
        }

        private void ZaznaczObrazyProp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResizeImages(true, true);
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
            
        }

        private void ZaznaczObrazyDow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResizeImages(true, false);
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        private void OdznaczObrazy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResizeImages(false, true);
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
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
                ErrorMessage(ex);
            }
        }

        private TextRange ExtendSelection(LogicalDirection direction)
        {
            //For new context menu in RTB --- Extendet selected word
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

        private void MenuItem_Dodaj_Click(object sender, EventArgs e)
        {
            try
            {
                //Save into dictionary
                dictionaries.Remove(new Uri(@"C:\Gabinet\dictionary.lex"));                                                                //Odłaczenie słownika
                if (!File.Exists(@"C:\Gabinet\dictionary.lex"))                                                                            //Utworzenie pliku słownika jak go nie ma
                {
                    StreamWriter sw2 = File.CreateText(@"C:\Gabinet\dictionary.lex");
                    sw2.Close();
                }
                else 
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
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
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
                ErrorMessage(ex);
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
            try
            {
                IEnumerable<Image> images = rtbEditor.Document.Blocks.OfType<BlockUIContainer>()
                    .Select(c => c.Child).OfType<Image>()
                    .Union(rtbEditor.Document.Blocks.OfType<Paragraph>()
                    .SelectMany(pg => pg.Inlines.OfType<InlineUIContainer>())
                    .Select(inline => inline.Child).OfType<Image>());

                foreach (var image in images)
                {
                    AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(image);
                    if (adornerLayer != null)
                    {
                        if (wlaczycZnaczniki)
                        {
                            adornerLayer.Add(new ResizingAdorner(image, rozcProporcjonalne)); 
                        }
                        else 
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
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        #endregion

        #endregion
        #region Drag & Drop Function's

        private void RichTextBox_DragOver(object sender, DragEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
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
                ErrorMessage(ex);
            }
        }
        
        #endregion
        #region PRINT    

        private void btnPrintRTB_Click(object sender, RoutedEventArgs e)
        {
            //Print RTB file
            try
            {
                FlowDocument document = rtbEditor.Document;

                //Make copy of FlowDocument for pritning - witch pictures
                string copyString = XamlWriter.Save(rtbEditor.Document);
                FlowDocument kopiaFlowDocument = XamlReader.Parse(copyString) as FlowDocument;
                MemoryStream memorySrteam = new MemoryStream();                                                                     //Przygotowanie się do przechowywania zawartości w pamięci

                System.Printing.PrintDocumentImageableArea ia = null;
                System.Windows.Xps.XpsDocumentWriter docWriter = System.Printing.PrintQueue.CreateXpsDocumentWriter(ref ia);        //tu następuje otwarcie okno dialogowego drukowania który zwraca prarametr - referencje: ia
                                                                                                                                    //parametr (ia) reprezentuje informacje o obszarze obrazowania i wymiarze nośnika.
                if (docWriter != null && ia != null)
                {
                    DocumentPaginator paginator = ((IDocumentPaginatorSource)kopiaFlowDocument).DocumentPaginator;                  ///Dzielenie zawartości na strony.
                    //Change size PageSize and PagePadding of document - for CanvasSize 
                    paginator.PageSize = new Size(ia.MediaSizeWidth, ia.MediaSizeHeight);                                           //Ustawia rozmiar stron zgodnie z wymiarem fizycznym kartki papieru. (793/1122)
                    Thickness t = new Thickness(margines + kopiaFlowDocument.PagePadding.Left);                                     //Ustawiam marginesy wydruku w PagePadding. Należy zastosować korektę o starowy margines drukarki . Tu daje tyle co RichTextBox Domyślnie pobiera z drukarki i wynoszą (5,0,5,0) piksela 
                    kopiaFlowDocument.PagePadding = new Thickness(                                                                  //Ustawienie nowego obszaru zadrukowania strony PagePadding
                                     Math.Max(ia.OriginWidth, t.Left),
                                       Math.Max(ia.OriginHeight, t.Top),
                                       Math.Max(ia.MediaSizeWidth - (ia.OriginWidth + ia.ExtentWidth), t.Right),
                                       Math.Max(ia.MediaSizeHeight - (ia.OriginHeight + ia.ExtentHeight), t.Bottom));
                    kopiaFlowDocument.ColumnWidth = double.PositiveInfinity;                                                        //Ustawienie szerokości kolumny drukowanej. double.PositiveInfinity - reprezentuje nieskończoności dodatniej. To pole jest stałe.
                    // Send content to the printer.
                    docWriter.Write(paginator);


                }
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                memorySrteam.Dispose();                                                                                              
            }

            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        #endregion
        #region Funkcja Text change functions 

        private void BtnSzukaj_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string szukaneSlowo = txbSzukaj.Text;
                IEnumerable<TextRange> wordRanges = GetAllWordRanges(rtbEditor.Document);
                foreach (TextRange wordRange in wordRanges)
                {
                    if (wordRange.Text == szukaneSlowo)
                    {
                        listaPozycji.Add(wordRange);
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
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }            
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
            try
            {
                ///* get start pointer */
                TextPointer startPtr = rtbEditor.Document.ContentStart;

                ///* get current caret position */
                int start = startPtr.GetOffsetToPosition(rtbEditor.CaretPosition);

                txtSzukaj.Content = Convert.ToString(start);
            }
            catch (Exception ex)
            {
                ErrorMessage(ex);
            }
        }

        private void ErrorMessage(Exception ex)
        {
            MessageBox.Show("Nastąpij wyjątek w działaniu programu: \n\n" + ex.ToString(), "Wyjątek", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
