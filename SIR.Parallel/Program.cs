using System;
using System.Diagnostics;
using System.IO;

namespace SIR.Parallel
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- Validacion 10x10 a 30 dias en base a 4 hilos ---");

            CeldaParalela grillaPequenia = new CeldaParalela(
                filas: 10, columnas: 10,
                beta: 0.5, gamma: 0.05, mu: 0.005,
                semilla: 15
            );

            grillaPequenia.Inicializar(inicialesInfectados: 3);

            int totalPoblacion = 10 * 10;

            for (int dia = 0; dia <= 30; dia++)
            {
                int s, i, r, m;
                grillaPequenia.ObtenerConteos(out s, out i, out r, out m);

                int suma = s + i + r + m;

                string valido = (suma == totalPoblacion) ? "VALIDO" : "ERROR";
                Console.WriteLine($"Dia {dia,2}: S={s,4} I={i,3} R={r,3} M={m,3} | Total={suma} {valido}");

                if (dia < 30)
                {
                    grillaPequenia.AvanzarDia(4);

                }
            }

            Console.WriteLine();
            Console.WriteLine("Presiona Enter para iniciar scaling...");
            Console.ReadLine();

            // scaling por core
            int[] configuraciones = new int[] { 1, 2, 4, 6 };
            double tiempoBase = 0;

            string csvScaling = @"C:\Users\hazky\source\repos\SIR_Epidemia\results\scaling_results.csv";

            using (var writer = new StreamWriter(csvScaling))
            {
                writer.WriteLine("cores,tiempo_segundos,speedup");

                foreach (int cores in configuraciones)
                {
                    Console.WriteLine($"Realizando simulacion con {cores} core(s)...");

                    CeldaParalela grilla = new CeldaParalela(
                        filas: 1000, columnas: 1000,
                        beta: 0.5, gamma: 0.05, mu: 0.005,
                        semilla: 15
                    );

                    grilla.Inicializar(inicialesInfectados: 5);

                    var sw = Stopwatch.StartNew();

                    for (int dia = 0; dia < 365; dia++)
                    {
                        grilla.AvanzarDia(cores);

                    }

                    sw.Stop();
                    double segundos = sw.Elapsed.TotalSeconds;

                    if (cores == 1)
                    {
                        tiempoBase = segundos;

                    }

                    double speedup = tiempoBase / segundos;

                    // conteos finales para validar
                    int s, i, r, m;
                    grilla.ObtenerConteos(out s, out i, out r, out m);

                    Console.WriteLine($"  Cores={cores} | Tiempo={segundos:F2}s | Speedup={speedup:F2}x");
                    Console.WriteLine($"  Conteos finales: S={s} I={i} R={r} M={m}");

                    writer.WriteLine($"{cores},{segundos:F2},{speedup:F2}");
                }

                double aproxR0 = 0.5 / (0.05 + 0.005);
                Console.WriteLine($"R0 aproximado del modelo: {aproxR0:F2}");
                writer.WriteLine($"# R0 aproximado: {aproxR0:F2}");
            }

            Console.WriteLine();
            Console.WriteLine($"CSV guardado en: {csvScaling}");
            Console.WriteLine("Presiona Enter para salir.");
            Console.ReadLine();
        }
    }
}