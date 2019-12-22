using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace AppGraZaDuzoZaMaloCLI
{
    class WidokCLI
    {
        public const char ZNAK_ZAKONCZENIA_GRY = 'X';
        public const char ZNAK_ZAWIESZENIA_GRY = 'W';
        public const char ZNAK_PODDANIA_GRY = 'P';

        private KontrolerCLI kontroler;

        public WidokCLI(KontrolerCLI kontroler) => this.kontroler = kontroler;

        public void CzyscEkran() => Clear();

        public void KomunikatPowitalny() => WriteLine("Wylosowałem liczbę z zakresu ");

        public int WczytajPropozycje()
        {
            int wynik = 0;
            bool sukces = false;
            while (!sukces)
            {
                Write($"Podaj swoją propozycję (lub {ZNAK_ZAKONCZENIA_GRY} aby przerwać, {ZNAK_PODDANIA_GRY} aby poddać rozgrywkę, {ZNAK_ZAWIESZENIA_GRY} aby wstrzymać rozgrywkę.): ");
                try
                {
                    string value = ReadLine().TrimStart().ToUpper();
                    if (value.Length == 1 )
                    {
                        switch (value[0])
                        {
                            case ZNAK_ZAKONCZENIA_GRY:
                                throw new KoniecGryException("Escape");
                            case ZNAK_PODDANIA_GRY:
                                throw new KoniecGryException("Surrender");
                            case ZNAK_ZAWIESZENIA_GRY:
                                throw new KoniecGryException("Stop");
                        }
                            
                    }
                    //UWAGA: ponizej może zostać zgłoszony wyjątek 
                    wynik = Int32.Parse(value);
                    sukces = true;
                }
                catch (FormatException)
                {
                    WriteLine("Podana przez Ciebie wartość nie przypomina liczby! Spróbuj raz jeszcze.");
                    continue;
                }
                catch (OverflowException)
                {
                    WriteLine("Przesadziłeś. Podana przez Ciebie wartość jest zła! Spróbuj raz jeszcze.");
                    continue;
                }
                catch(KoniecGryException e)
                {
                    throw e;
                }
                catch (Exception)
                {
                    WriteLine("Nieznany błąd! Spróbuj raz jeszcze.");
                    continue;
                }
            }
            return wynik;
        }

        public void OpisGry()
        {
            WriteLine("Gra w \"Za dużo za mało\"." + Environment.NewLine
                + "Twoimm zadaniem jest odgadnąć liczbę, którą wylosował komputer." + Environment.NewLine + "Na twoje propozycje komputer odpowiada: za dużo, za mało albo trafiłeś");
        }

        public bool ChceszKontynuowac( string prompt )
        {
                Write( prompt );
                char odp = ReadKey().KeyChar;
                WriteLine();
                return (odp == 't' || odp == 'T');
        }

        public void HistoriaGry()
        {
            if (kontroler.ListaRuchow.Count == 0)
            {
                WriteLine("--- pusto ---");
                return;
            }

            WriteLine(string.Format("{0,-10}║{1,-10}║{2,-10}║{3,-10}║{4,-10}", "Nr", "Propozycja", "Odpowiedź", "Czas", "Status"));
            WriteLine(string.Format("{0}{1}{0}{1}{0}{1}{0}{1}{0}", new String('═', 10), '╬'));
            TimeSpan sumaCzasow = new TimeSpan(0, 0, 0);
            for(int i = 1; i<kontroler.ListaRuchow.Count; i++)
            {
                if (kontroler.ListaRuchow[i - 1].StatusGry != GraZaDuzoZaMalo.Model.Gra.Status.WTrakcie)
                    continue;
                var ruch = kontroler.ListaRuchow[i];
                sumaCzasow += ruch.Czas - kontroler.ListaRuchow[i - 1].Czas;
                WriteLine(string.Format("{0,-10}║{1,-10}║{2,-10}║{3,-10:F3}║{4,-10}", i, ruch.Liczba, ruch.Wynik, sumaCzasow.TotalSeconds, ruch.StatusGry));
            }
            ;
        }

        public void KomunikatRozgrywkaPoddana()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine("Rozgrywka Poddana!!!");
            Console.ResetColor();
        }

        public void KomunikatRozgrywkaWstrzymana()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine("Rozgrywka wstrzymana.");
            Console.ResetColor();
        }

        public void KomunikatRozgrywkaWznowiona()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLine("Rozgrywka wznowiona.");
            Console.ResetColor();
        }

        public void KomunikatZaDuzo()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine("Za dużo!");
            Console.ResetColor();
        }

        public void KomunikatZaMalo()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLine("Za mało!");
            Console.ResetColor();
        }

        public void KomunikatTrafiono()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLine("Trafiono!");
            Console.ResetColor();
        }
    }

}
