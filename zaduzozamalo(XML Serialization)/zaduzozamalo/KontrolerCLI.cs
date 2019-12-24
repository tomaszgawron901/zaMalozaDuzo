using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GraZaDuzoZaMalo.Model;
using static GraZaDuzoZaMalo.Model.Gra.Odpowiedz;
using System.Security.Cryptography.Xml;

namespace AppGraZaDuzoZaMaloCLI
{
    public class KontrolerCLI
    {
        private Gra gra;
        private WidokCLI widok;

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
        }

        public void Uruchom()
        {
            widok.OpisGry();
            try
            {
                if (!File.Exists(@"save.txt")) throw new FileNotFoundException();
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
            finally { UsunPlik(@"save.txt"); }
            while( widok.ChceszKontynuowac("Czy chcesz kontynuować aplikację (t/n)? ") )
            {
                InicjalizujNowaRozgrywke();
                UruchomRozgrywke();
                UsunPlik(@"save.txt");
            }
                
        }

        public void UsunPlik( string path )
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public void WczytajRozgryke()
        {
            Stream stream = new FileStream(@"save.txt", FileMode.Open);
            XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas());
            DataContractSerializer serializer = new DataContractSerializer(typeof(Gra));
            gra = (Gra)serializer.ReadObject(reader, true);
            reader.Close();
            stream.Close();
        }

        public void ZapiszRozgrywke()
        {
            RijndaelManaged klucz = new RijndaelManaged();
            Gra nowaGra = gra.Zaszyfruj(klucz).Odszyfruj(klucz);
            using (var stream = new FileStream(@"save.txt", FileMode.Create, FileAccess.Write))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Gra));
                serializer.WriteObject(stream, gra);
                stream.Close();
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
            widok.KomunikatRozgrywkaPoddana();
            Console.WriteLine($"Poszukwana liczba to {gra.Poddaj()}.");
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
    }

    static internal class StreamManager
    {
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

        static public string Zaszyfruj(this Gra obiektDoZaszyfrowania, RijndaelManaged klucz)
        {
            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(obiektDoZaszyfrowania.serializujDoString());
            byte[] zaszyfrowanyElement = new EncryptedXml().EncryptData(Doc.DocumentElement, klucz, false);
            EncryptedData edElement = new EncryptedData();
            edElement.Type = EncryptedXml.XmlEncElementUrl;
            string encryptionMethod = null;
            if (klucz is Rijndael)
            {
                switch (klucz.KeySize)
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
            return Doc.InnerXml;
        }

        static public Gra Odszyfruj(this string xml, SymmetricAlgorithm Alg)
        {
            if (Alg == null)
                throw new ArgumentNullException("Alg");

            // Find the EncryptedData element in the XmlDocument.
            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(xml);

            // If the EncryptedData element was not found, throw an exception.
            if (Doc.DocumentElement == null)
            {
                throw new XmlException("The EncryptedData element was not found.");
            }


            // Create an EncryptedData object and populate it.
            EncryptedData edElement = new EncryptedData();
            edElement.LoadXml(Doc.DocumentElement);

            // Create a new EncryptedXml object.
            EncryptedXml exml = new EncryptedXml();


            // Decrypt the element using the symmetric key.
            byte[] rgbOutput = exml.DecryptData(edElement, Alg);

            // Replace the encryptedData element with the plaintext XML element.
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
