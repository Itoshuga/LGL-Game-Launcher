using GameLauncher.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameLauncher.Classes
{
    internal class cNewsManager : cNews
    {
        public async Task<List<cNews>> GetPublishedNews()
        {
            // Récupérer les actualités publiées depuis Firestore
            return await cNews.GetPublishedNews();
        }
        public List<PatchCard> AddNews(List<cNews> newsList)
        {
            List<PatchCard> patchCards = new List<PatchCard>();
            foreach (cNews news in newsList)
            {
                PatchCard patchCard = new PatchCard
                {
                    Etiquette = news.tag,
                    Titre = news.title,
                    Description = news.content,
                    ImagePath = news.image
                };
                patchCards.Add(patchCard);
            }
            return patchCards;
        }

    }
}
