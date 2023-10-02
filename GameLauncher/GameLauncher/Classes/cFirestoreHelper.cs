using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using DotNetEnv;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using System.Diagnostics;

namespace GameLauncher.Classes
{
    public static class cFirestoreHelper
    {
        static string fireConfig;

        static string filePath = "";

        public static FirestoreDb? database
        {
            get; private set;
        }

        public static void SetEnvironmentVariable()
        {
            Env.TraversePath().Load();

            fireConfig = Env.GetString("FIREBASE_CONFIG");

            Trace.WriteLine("FireConfig : " + fireConfig);
            filePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())) + ".json";
            File.WriteAllText(filePath, fireConfig);
            File.SetAttributes(filePath, FileAttributes.Hidden);
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filePath);
            database = FirestoreDb.Create("duel-de-regnes");
            File.Delete(filePath);
        }
    }
}
