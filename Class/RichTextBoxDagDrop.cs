using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;


namespace GabinetLekarski.Klasy
{
    class RichTextBoxDagDrop
    {
        public BitmapImage bitmap;
        public Image image;

        public void Drop(RichTextBox richTextBox, object sender, DragEventArgs e)
        {
            #region Opid funkcji
            /* Funkcja podczas upuszczenia pliku na RichTextBox'ie wstawia do niego w zależności od rozszerzenia nazwy pliku:
             *  - obraz (pliki: jpg, jpeg, bmb, gif, tif
             *  - tekst (pliki txt)
             */
            #endregion
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    //Pobranie scieżki do pliku
                    string[] filePath = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (filePath != null)
                    {
                        if (File.Exists(filePath[0]))
                        {
                            foreach (var file in filePath)
                            {
                                int dlugosc = file.Length;
                                int znak = file.LastIndexOf(".");
                                string formatPliku = file.Substring(dlugosc - (dlugosc - znak - 1)).ToLower();

                                if (formatPliku.ToLower() == "txt")  //Plik tekstowy
                                {
                                    richTextBox.AppendText(File.ReadAllText(file) + "\n");
                                }
                                else if (formatPliku == "jpg" || formatPliku == "jpeg" || formatPliku == "bmp" || formatPliku == "gif" || formatPliku == "tif")   //Plik obrazu
                                {
                                    bitmap = new BitmapImage(new Uri(file, UriKind.Absolute))
                                    {
                                        CacheOption = BitmapCacheOption.OnLoad
                                    };
                                    bitmap.Freeze();
                                    image = new Image();
                                    image.Source = bitmap;

                                    if (image != null)
                                    {
                                        image.Height = bitmap.Height;   //przypisanie rozmiaru Image z orginału (Bitmapy)
                                        image.Width = bitmap.Width;
                                        //INSERT IMAGE
                                        BlockUIContainer container = new BlockUIContainer(image);
                                        richTextBox.Document.Blocks.Add(container);

                                        //Dodoanie ZNACZNIKÓW dla narozników AKTUALNIE wstawianego zdjecia dla RESIZE za pomocą klasy ResizingAdorner
                                        image.Loaded += delegate
                                        {
                                            AdornerLayer al = AdornerLayer.GetAdornerLayer(image);
                                            if (al != null)
                                            {
                                                al.Add(new ResizingAdorner(image, true)); //true - rozciąganie proporcjonalne
                                            }
                                        };

                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
    }
}
