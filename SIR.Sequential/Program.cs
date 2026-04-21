using System;

namespace SIR.Sequential
{
    class Program
    {
        static void Main(string[] args)
        {
            // Validar con grilla 10x10

            Console.WriteLine("--- Validacion 10x10 a 30 dias ---");

            Celda grillaPeq = new Celda(
                filas: 10, columnas: 10,
                beta: 0.5, gamma: 0.05, mu: 0.005,
                semilla: 15
            );

            grillaPeq.Inicializar(inicialesInfectados: 3);

            int poblacionTotal = 10 * 10;

            for (int dia = 0; dia <= 30; dia++)
            {
                int s, i, r, m;
                grillaPeq.ObtenerConteos(out s, out i, out r, out m);

                int suma = s + i + r + m;
                string valido = (suma == poblacionTotal) ? "VALIDO" : "ERROR";

                Console.WriteLine($"Dia {dia,2}: S={s,4} I={i,3} R={r,3} M={m,3} | Total={suma} {valido}");

                if (dia < 30)
                    grillaPeq.AvanzarDia();
            }

            Console.WriteLine();
            Console.WriteLine("Presiona Enter para correr la simulacion completa 1000x1000...");
            Console.ReadLine();

            Console.WriteLine("--- Simulacion 1000x1000 a 365 dias ---");

            var sw = System.Diagnostics.Stopwatch.StartNew();

            Celda grilla = new Celda(
                filas: 1000, columnas: 1000,
                beta: 0.5, gamma: 0.05, mu: 0.005,
                semilla: 15
            );

            grilla.Inicializar(inicialesInfectados: 5);

            // guardar resultados en CSV
            string rutaCsv = @"C:\Users\hazky\source\repos\SIR_Epidemia\results\sequential_stats.csv";
            using (var writer = new System.IO.StreamWriter(rutaCsv))
            {
                writer.WriteLine("dia,susceptibles,infectados,recuperados,muertos");

                for (int dia = 0; dia <= 365; dia++)
                {
                    int s, i, r, m;
                    grilla.ObtenerConteos(out s, out i, out r, out m);

                    writer.WriteLine($"{dia},{s},{i},{r},{m}");

                    if (dia % 30 == 0)
                        Console.WriteLine($"Dia {dia,3}: S={s,7} I={i,6} R={r,6} M={m,5}");

                    if (dia < 365)
                        grilla.AvanzarDia();
                }
            }

            sw.Stop();
            double segundos = sw.Elapsed.TotalSeconds;

            Console.WriteLine();
            Console.WriteLine($"Tiempo total: {segundos:F2} segundos");
            Console.WriteLine($"CSV guardado en: {rutaCsv}");
            Console.WriteLine("Presiona Enter para salir.");
            Console.ReadLine();
        }
    }
}

