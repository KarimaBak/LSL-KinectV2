using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LSL_Kinect
{
    /// <summary>
    /// Logique d'interaction pour Saisie.xaml
    /// </summary>
    public partial class Saisie : Window
    {
        public string selectionNom { get; set; }
        private bool clickOk = false;

        public bool selectionBool { get; set; }

        public Saisie()
        {
            InitializeComponent();
            saisie_tb.Focus();
        }

        private void ok_btn_Click(object sender, RoutedEventArgs e)
        {
            
           if(saisie_tb.Text=="")
           {
                MessageBox.Show("Veuillez saisir un nom valide pour l'enregistrement");
           }
           else
            {
               selectionNom = saisie_tb.Text;
               selectionBool= this.clickOk = true;
            }

            Close();
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            selectionBool = clickOk = false;
            Close();
        }

        private void saisie_tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void saisie_tb_GotFocus(object sender, RoutedEventArgs e)
        {
            saisie_tb.Text = "";
        }
               
    }
}
