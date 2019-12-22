using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GraZaDuzoZaMalo.Model;
using static GraZaDuzoZaMalo.Model.Gra.Odpowiedz;

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
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(@"save.txt", FileMode.Open, FileAccess.Read);
            gra= (Gra)formatter.Deserialize(stream);
            stream.Close();
        }

        public void ZapiszRozgrywke()
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(@"save.txt", FileMode.Create, FileAccess.Write);
            formatter.Serialize(stream, gra);
            stream.Close();
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
