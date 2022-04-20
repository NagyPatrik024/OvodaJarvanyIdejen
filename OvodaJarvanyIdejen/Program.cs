using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OvodaJarvanyIdejen
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Szulo> szulok = Enumerable.Range(1, 40).Select(x => new Szulo()).ToList();
            List<Alkalmazott> alkalmazottak = Enumerable.Range(1, 5).Select(x => new Alkalmazott()).ToList();

            List<Task> ts = szulok.Select(x => new Task(() => { x.Work(alkalmazottak); }, TaskCreationOptions.LongRunning)).ToList();
            ts.AddRange(alkalmazottak.Select(x => new Task(() => { x.Work(szulok); }, TaskCreationOptions.LongRunning)));
            ts.Add(new Task(() =>
            {
                //int time = 0;
                //int perc = 0;
                int SLEEP = 50;
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                TimeSpan timespan;
                while (szulok.Any(x => x.Status != SzuloStatus.Hazament))
                {

                    Console.Clear();
                    foreach (var a in alkalmazottak)
                    {
                        Console.WriteLine(a);
                    }
                    Console.WriteLine();
                    foreach (var sz in szulok.Where(x =>x.Status != SzuloStatus.Init && x.Status != SzuloStatus.Hazament))
                    {
                        Console.WriteLine(sz);
                    }
                    int count = szulok.Count - szulok.Where(x => x.Status == SzuloStatus.Hazament).Count();
                    Console.WriteLine($"Óvodában váró gyerekek: {count}");
                    timespan = stopwatch.Elapsed;
                    Console.WriteLine(string.Format("{0} óra {1} perc", Math.Floor(timespan.TotalMinutes), timespan.ToString("ss")));
                    //perc = (time++ / (1000 / SLEEP));
                    //Console.WriteLine($"Idő: {9 + perc / 60} óra {perc % 60}");
                    Thread.Sleep(SLEEP);
                }
                stopwatch.Stop();
                Console.Clear();
                Console.WriteLine("Szimuláció véget ért");
                timespan = stopwatch.Elapsed;
                //Console.WriteLine(string.Format("{0}:{1}", Math.Floor(timespan.TotalMinutes), timespan.ToString("ss\\.ff")));
                Console.WriteLine(string.Format("{0} óra {1} perc", Math.Floor(timespan.TotalMinutes), timespan.ToString("ss")));
                //Console.WriteLine($"Idő: {9 + perc / 60} óra {perc % 60}");

            }, TaskCreationOptions.LongRunning));
            ts.ForEach(x => x.Start());

            Console.ReadKey();
        }
    }

    enum AlkalmazottStatus { Init, Szabad, GyereketKeres, GyerekekkelFoglalkozik, LazatMer, Vegzett }
    class Alkalmazott
    {
        public ConcurrentQueue<Szulo> varosor;
        static int _id = 0;
        public int Id { get; set; }
        public AlkalmazottStatus Status { get; set; }

        public Szulo Szulo { get; set; }

        public Alkalmazott()
        {
            Id = _id++;
            Status = AlkalmazottStatus.Init;
            varosor = new ConcurrentQueue<Szulo>();
        }

        public void Work(List<Szulo> szulok)
        {
            var timer = new Stopwatch();
            timer.Start();

            while (szulok.Any(x => x.Status != SzuloStatus.Hazament))
            {
                Status = AlkalmazottStatus.Szabad;
                if (Util.rnd.Next(1, 101) <= 30)
                {
                    Status = AlkalmazottStatus.GyerekekkelFoglalkozik;
                    Thread.Sleep(Util.rnd.Next(1000, 5001));
                }
                Status = AlkalmazottStatus.Szabad;
                Szulo sz;
                varosor.TryDequeue(out sz);
                if (sz != null)
                {
                    Status = AlkalmazottStatus.GyereketKeres;
                    Szulo = sz;
                    lock (sz.paciensLock)
                    {
                        Monitor.Pulse(sz.paciensLock);
                    }
                    Thread.Sleep(Util.rnd.Next(2000, 8001));
                    lock (sz.paciensLock)
                    {
                        Monitor.Pulse(sz.paciensLock);
                    }
                    Szulo = null;
                    Status = AlkalmazottStatus.Szabad;
                }
                else
                {
                    Thread.Sleep(100);
                }

                if (timer.ElapsedMilliseconds >= 60000)
                {
                    Status = AlkalmazottStatus.LazatMer;
                    Thread.Sleep(2000);
                    if (Util.rnd.Next(0, 101) <= 2)
                    {
                        break;
                    }
                    timer.Restart();
                }
                Thread.Sleep(150);
            }
            Status = AlkalmazottStatus.Vegzett;
        }

        public override string ToString()
        {
            if (Szulo != null)
            {
                return $"{Id} : {Status} Szülő: {Szulo.Id}";
            }
            return $"{Id} : {Status}";
        }
    }

    enum SzuloStatus { Init, MegErkezett, GyerekreVar, Hazament }
    class Szulo
    {
        static int _id = 0;
        public int Id { get; set; }
        public SzuloStatus Status { get; set; }
        public object paciensLock = new object();
        public Szulo()
        {
            Id = _id++;
            Status = SzuloStatus.Init;
        }

        public void Work(List<Alkalmazott> alkalmazottak)
        {
            Thread.Sleep(Id * (Util.rnd.Next(1000, 5001)));
            Status = SzuloStatus.MegErkezett;
            Alkalmazott alkalmazott = alkalmazottak.OrderBy(x => Util.rnd.Next(1, 100)).First();
            alkalmazott.varosor.Enqueue(this);
            //alkalmazottra várni kell hogy kivegye a queuebol
            lock (paciensLock)
            {
                Monitor.Wait(paciensLock);
            }
            Status = SzuloStatus.GyerekreVar;

            //alkalmazottra varni kell hogy megtalalja a gyereket
            lock (paciensLock)
            {
                Monitor.Wait(paciensLock);
            }
            Status = SzuloStatus.Hazament;
        }

        public override string ToString()
        {
            return $"{Id} : {Status}";
        }
    }

    static class Util
    {
        public static Random rnd = new Random();
    }
}
