using System.Windows;
using System.Windows.Controls;

namespace LSL_Kinect
{
    /// <summary>
    /// Logique d'interaction pour Saisie.xaml
    /// </summary>
    public partial class Saisie : Window
    {
        public string selectionNom { get; set; }

        public bool userChoseFileName { get; set; }

        public Saisie()
        {
            InitializeComponent();
            saisie_tb.Focus();
        }

        private void ok_btn_Click(object sender, RoutedEventArgs e)
        {
            if (saisie_tb.Text == "")
            {
                MessageBox.Show("Veuillez saisir un nom valide pour l'enregistrement");
            }
            else
            {
                selectionNom = saisie_tb.Text;
                userChoseFileName = true;
            }

            Close();
        }

        private void cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            userChoseFileName = false;
            Close();
        }

        private void OnTextBlockTextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void saisie_tb_GotFocus(object sender, RoutedEventArgs e)
        {
            saisie_tb.Text = "";
        }
    }
}