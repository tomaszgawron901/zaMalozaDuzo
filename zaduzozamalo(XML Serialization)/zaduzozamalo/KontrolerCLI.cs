using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using GraZaDuzoZaMalo.Model;
using static GraZaDuzoZaMalo.Model.Gra.Odpowiedz;
using System.Security.Cryptography.Xml;

namespace AppGraZaDuzoZaMaloCLI
{
    public class KontrolerCLI
    {
        private static string filePath = @"save.txt";

        private Gra gra;
        private WidokCLI widok;
        private Thread autoSave;
        private DateTime lastSave;
        private object _object = new Object();

        public int MinZakres { get; private set; } = 1;
        public int MaxZakres { get; private set; } = 100;

        public DateTime CzasRozpoczecia { get => gra.CzasRozpoczecia; }
        public DateTime? CzasZakonczenia { get => gra.CzasZakonczenia; }

        public IReadOnlyList<Gra.Ruch> ListaRuchow {
            get
            { return gra.ListaRuchow;  }
 }

        public KontrolerCLI()
        {
            gra = new Gra();
            widok = new WidokCLI(this);
            RozpocznijAutomatyczyZapis();
        }

        public void Uruchom()
        {
            widok.OpisGry();
            try
            {
                if (!File.Exists(filePath)) throw new FileNotFoundException();
                WczytajRozgryke();//Może zgłosić wyjątek.
                if (gra.StatusGry == Gra.Status.Zakonczona || gra.StatusGry == Gra.Status.Poddana)
                    throw new Exception("Odczytana rozgrywka jest już zakończona.");
                if (widok.ChceszKontynuowac("Czy chcesz wczytać zapis poprzedniej rozgrywki (t/n)? "))
                {
                    widok.CzyscEkran();
                    widok.HistoriaGry();
                    WznowRozgrywke();
                    UruchomRozgrywke();
                }
            }
            catch (Exception) { }
            finally { UsunPlik(filePath); }
            while( widok.ChceszKontynuowac("Czy chcesz kontynuować aplikację (t/n)? ") )
            {
                InicjalizujNowaRozgrywke();
                UruchomRozgrywke();
                UsunPlik(filePath);
            }
                
        }

        public void UsunPlik( string path )
        {
            lock (_object)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        public void WczytajRozgryke()
        {
            lock (_object)
            {
                using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var data = new XmlDocument();
                    data.Load(stream);
                    gra = data.Odszyfruj();
                    stream.Close();
                }
            }
        }

        public void ZapiszRozgrywke()
        {
            lock(_object)
            {
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    gra.Zaszyfruj().Save(stream);
                }
            }
        }

        public void InicjalizujNowaRozgrywke()
        {
            widok.CzyscEkran();
            // ustaw zakres do losowania
            gra = new Gra(MinZakres, MaxZakres); //może zgłosić ArgumentException
        }

        public void UruchomRozgrywke()
        {
            do
            {
                //wczytaj propozycję
                int propozycja = 0;
                try
                {
                    propozycja = widok.WczytajPropozycje();
                }
                catch( KoniecGryException e)
                {
                    switch(e.Message)
                    {
                        case "Escape":
                            ZakonczGre();
                            break;
                        case "Surrender":
                            PoddajRozgrywke();
                            break;
                        case "Stop":
                            WstrzymajRozgrywke();
                            break;
                    }
                }

                if (gra.StatusGry == Gra.Status.Poddana) break;
                if (gra.StatusGry == Gra.Status.Zawieszona)
                {
                    do
                    {
                        widok.CzyscEkran();
                        widok.KomunikatRozgrywkaWstrzymana();
                    }
                    while (!widok.ChceszKontynuowac("Czy chcesz wznowić rozgrywkę (t/n)?"));
                    WznowRozgrywke();
                    continue;
                }
                Console.WriteLine(propozycja);

                //Console.WriteLine( gra.Ocena(propozycja) );
                //oceń propozycję, break
                widok.CzyscEkran();
                switch ( gra.Ocena(propozycja) )
                {
                    case ZaDuzo:
                        widok.KomunikatZaDuzo();
                        break;
                    case ZaMalo:
                        widok.KomunikatZaMalo();
                        break;
                    case Trafiony:
                        widok.KomunikatTrafiono();
                        break;
                    default:
                        break;
                }
                widok.HistoriaGry();
            }
            while (gra.StatusGry == Gra.Status.WTrakcie);
                      
            //if StatusGry == Przerwana wypisz poprawną odpowiedź
            //if StatusGry == Zakończona wypisz statystyki gry
        }

        ///////////////////////

        public void UstawZakresDoLosowania(ref int min, ref int max)
        {

        }

        public int LiczbaProb() => gra.ListaRuchow.Count();

        public void ZakonczGre()
        {
            WstrzymajRozgrywke();
            ZapiszRozgrywke();
            gra = null;
            widok.CzyscEkran(); //komunikat o końcu gry
            widok = null;
            System.Environment.Exit(0);
        }

        public void PoddajRozgrywke()
        {
            widok.CzyscEkran();
            widok.KomunikatRozgrywkaPoddana();
            Console.WriteLine($"Poszukwana liczba to {gra.Poddaj()}.");
            widok.HistoriaGry();
        }

        public void WstrzymajRozgrywke()
        {
            gra.Wstrzymaj();
        }

        public void WznowRozgrywke()
        {
            gra.Wznow();
            widok.CzyscEkran();
            widok.KomunikatRozgrywkaWznowiona();
            widok.HistoriaGry();
        }

        private void RozpocznijAutomatyczyZapis()
        {
            autoSave = new Thread(automatycznyZapis);
            autoSave.Start();
        }

        private void automatycznyZapis()
        {
            while(true)
            {
                Thread.Sleep(5000);
                if(gra != null && gra.StatusGry == Gra.Status.WTrakcie)
                {
                    ZapiszRozgrywke();
                    lastSave = DateTime.Now;
                }
            }
        }
    }

    static internal class XMLSerializationManager
    {
        static private RijndaelManaged _klucz = new RijndaelManaged();
        static XMLSerializationManager()
        {
            _klucz.Key = new byte[32] { 118, 123, 23, 17, 161, 152, 35, 68, 126, 213, 16, 115, 68, 217, 58, 108, 56, 218, 5, 78, 28, 128, 113, 208, 61, 56, 10, 87, 187, 162, 233, 38 };
            _klucz.IV = new byte[16] { 33, 241, 14, 16, 103, 18, 14, 248, 4, 54, 18, 5, 60, 76, 16, 191};
        }

        static public string serializujDoString(this Gra obiektDoSerializacji)
        {
            using (var output = new StringWriter())
            {
                var writer = new XmlTextWriter(output);
                var dataContractSerializer = new DataContractSerializer(typeof(Gra));
                dataContractSerializer.WriteObject(writer, obiektDoSerializacji);
                writer.Close();
                return output.GetStringBuilder().ToString();
            }
        }

        static public Gra deserializujDoGra(this string xml)
        {
            var output = new StringReader(xml);
            var reader = new XmlTextReader(output);
            DataContractSerializer serializer = new DataContractSerializer(typeof(Gra));
            return (Gra)serializer.ReadObject(reader, true);
        }

        static public XmlDocument Zaszyfruj(this Gra obiektDoZaszyfrowania)
        {
            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(obiektDoZaszyfrowania.serializujDoString());
            byte[] zaszyfrowanyElement = new EncryptedXml().EncryptData(Doc.DocumentElement, _klucz, false);
            EncryptedData edElement = new EncryptedData();
            edElement.Type = EncryptedXml.XmlEncElementUrl;
            string encryptionMethod = null;
            if (_klucz is Rijndael)
            {
                switch (_klucz.KeySize)
                {
                    case 128:
                        encryptionMethod = EncryptedXml.XmlEncAES128Url;
                        break;
                    case 192:
                        encryptionMethod = EncryptedXml.XmlEncAES192Url;
                        break;
                    case 256:
                        encryptionMethod = EncryptedXml.XmlEncAES256Url;
                        break;
                }
            }
            else
            {
                throw new CryptographicException("The specified algorithm is not supported for XML Encryption.");
            }
            edElement.EncryptionMethod = new EncryptionMethod(encryptionMethod);
            edElement.CipherData.CipherValue = zaszyfrowanyElement;
            EncryptedXml.ReplaceElement(Doc.DocumentElement, edElement, false);
            return Doc;
        }

        static public Gra Odszyfruj(this XmlDocument Doc)
        {
            EncryptedData edElement = new EncryptedData();
            edElement.LoadXml(Doc.DocumentElement);
            EncryptedXml exml = new EncryptedXml();
            byte[] rgbOutput = exml.DecryptData(edElement, _klucz);
            exml.ReplaceData(Doc.DocumentElement, rgbOutput);
            return Doc.InnerXml.deserializujDoGra();
        }

    }

    [Serializable]
    internal class KoniecGryException : Exception
    {
        public KoniecGryException()
        {
        }

        public KoniecGryException(string message) : base(message)
        {
        }

        public KoniecGryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected KoniecGryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
