using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace GameLauncher.Classes
{
    internal class cNews
    {
        [FirestoreProperty] public string tag { get; set; }
        [FirestoreProperty] public string title { get; set; }
        [FirestoreProperty] public string content { get; set; }
        [FirestoreProperty] public string image { get; set; }
        [FirestoreProperty] public bool isPublished { get; set; }

        public BitmapImage ImageSource // Propriété pour afficher l'image dans WPF
        {
            get
            {
                if (!string.IsNullOrEmpty(image))
                {
                    return new BitmapImage(new Uri(image));
                }
                return null;
            }
        }

        public static async Task<List<cNews>> GetPublishedNews()
        {
            List<cNews> newsList = new List<cNews>();

            // Récupération de l'instance Firestore
            FirestoreDb db = FirestoreDb.Create("duel-de-regnes");

            // Récupération d'une référence à la collection "news"
            CollectionReference newsCollectionRef = db.Collection("news");

            // Création d'une requête pour récupérer les nouvelles publiées
            Query query = newsCollectionRef.WhereEqualTo("isPublished", true);

            // Exécution de la requête
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            // Parcours des documents résultants
            foreach (DocumentSnapshot documentSnapshot in snapshot)
            {
                cNews news = new cNews
                {
                    tag = documentSnapshot.GetValue<string>("tag"),
                    title = documentSnapshot.GetValue<string>("title"),
                    content = documentSnapshot.GetValue<string>("content"),
                    image = documentSnapshot.GetValue<string>("image"),
                    isPublished = documentSnapshot.GetValue<bool>("isPublished")
                };

                newsList.Add(news);
            }

            return newsList;
        }
    }
}
