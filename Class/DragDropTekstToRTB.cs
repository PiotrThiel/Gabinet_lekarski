using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GabinetLekarski.Klasy
{
    class DragDropTekstToRTB:RichTextBox
    {
        public DragDropTekstToRTB() //Konstruktor
        {
            this.AllowDrop = true;
            this.DragDrop += DragDropTekstToRTB_DragDrop;
        }

        private void DragDropTekstToRTB_DragDrop(object sender, DragEventArgs e)
        {
            string[] nazwyPlikow = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (nazwyPlikow != null)
            {
                foreach (string name in nazwyPlikow)
                {
                    try
                    {
                        this.AppendText(File.ReadAllText(name));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
    }
}
