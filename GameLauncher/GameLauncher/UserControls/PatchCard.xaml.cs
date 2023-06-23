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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameLauncher.UserControls
{
    /// <summary>
    /// Logique d'interaction pour PatchCard.xaml
    /// </summary>
    public partial class PatchCard : UserControl
    {
        public PatchCard()
        {
            InitializeComponent();
        }

        public string Etiquette
        {
            get { return (string)GetValue(EtiquetteProperty); }
            set { SetValue(EtiquetteProperty, value); }
        }

        public static readonly DependencyProperty EtiquetteProperty =
            DependencyProperty.Register("Etiquette", typeof(string), typeof(PatchCard));

        public string Titre
        {
            get { return (string)GetValue(TitreProperty); }
            set { SetValue(TitreProperty, value); }
        }

        public static readonly DependencyProperty TitreProperty =
            DependencyProperty.Register("Titre", typeof(string), typeof(PatchCard));

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(PatchCard));

        public ImageSource ImagePath
        {
            get { return (ImageSource)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(ImageSource), typeof(PatchCard));
    }
}
