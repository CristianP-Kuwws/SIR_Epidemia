using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Gif;
using System.Collections.Generic;

namespace SIR.Visualizer
{
    class Program
    {
        // colores por estado
        static readonly Rgba32 ColorSusceptible = new Rgba32(70, 130, 180);  // azul

        static readonly Rgba32 ColorInfectada = new Rgba32(220, 50, 50);  // rojo

        static readonly Rgba32 ColorRecuperada = new Rgba32(50, 180, 80);  // verde

        static readonly Rgba32 ColorMuerta = new Rgba32(40, 40, 40);  // gris oscuro

        enum EstadoCelda : byte
        {
            Susceptible = 0,
            Infectada = 1,
            Recuperada = 2,
            Muerta = 3
        }

        static void Main(string[] args)
        {
            const int filas = 200;  // usamos 200x200 para gif
            const int columnas = 200;

            const int dias = 365;
            const int frameStep = 5;    // frame cada 5 dias

            const double beta = 0.5;  
            const double mu = 0.005;
            const double gamma = 0.05;

            string gifPath = @"C:\Users\hazky\source\repos\SIR_Epidemia\results\animacion.gif";

            Console.WriteLine("Creando simulaciones para el gif (200x200 a 365 dias)...");

            // correr ambas simulaciones y guardar frames
            var framesSequencial = CorrerSimulacion(filas, columnas, dias, frameStep, beta, gamma, mu, semilla: 15);
            Console.WriteLine("Simulacion secuencial lista.");

            var framesParalelo = CorrerSimulacion(filas, columnas, dias, frameStep, beta, gamma, mu, semilla: 99);
            Console.WriteLine("Simulacion paralela lista.");

            // generar gif
            Console.WriteLine("Generando gif...");
            GenerarGif(framesSequencial, framesParalelo, filas, columnas, gifPath, frameStep);

            Console.WriteLine($"Gif guardado en: {gifPath}");


            string speedupPath = @"C:\Users\hazky\source\repos\SIR_Epidemia\results\speedup.png";

            Console.WriteLine("Generando grafica de speedup...");

            GenerarSpeedup(speedupPath);
            Console.WriteLine($"Grafica guardada en: {speedupPath}");

            Console.WriteLine("Presiona Enter para salir.");
            Console.ReadLine();
        }

        static EstadoCelda[][,] CorrerSimulacion(int filas, int columnas, int dias, int frameStep,
                double beta, double gamma, double mu, int semilla)
        {
            var cuadrilla = new EstadoCelda[filas, columnas];
            var siguienteCuadrilla = new EstadoCelda[filas, columnas];
            var numeroAleatorio = new Random(semilla);

            // inicializar susceptible
            for (int f = 0; f < filas; f++)
                for (int c = 0; c < columnas; c++)
                    cuadrilla[f, c] = EstadoCelda.Susceptible;

            // infectados iniciales al centro
            cuadrilla[filas / 2, columnas / 2] = EstadoCelda.Infectada;
            cuadrilla[filas / 2 + 1, columnas / 2] = EstadoCelda.Infectada;
            cuadrilla[filas / 2, columnas / 2 + 1] = EstadoCelda.Infectada;

            var frames = new List<EstadoCelda[,]>();

            for (int dia = 0; dia <= dias; dia++)
            {
                if (dia % frameStep == 0)
                {
                    var frame = new EstadoCelda[filas, columnas];

                    Array.Copy(cuadrilla, frame, cuadrilla.Length);
                    frames.Add(frame);
                }

                if (dia < dias)
                    AvanzarDia(cuadrilla, siguienteCuadrilla, filas, columnas, beta, gamma, mu, numeroAleatorio);
            }

            return frames.ToArray();
        }

        static void AvanzarDia(EstadoCelda[,] actual, EstadoCelda[,] siguiente,
            int filas, int columnas, double beta, double gamma, double mu, Random rng)
        {
            for (int f = 0; f < filas; f++)
            {
                for (int c = 0; c < columnas; c++)
                {
                    EstadoCelda estado = actual[f, c];

                    switch (estado)
                    {
                        case EstadoCelda.Susceptible:
                            int vecinos = 0;

                            if (f > 0 && actual[f - 1, c] == EstadoCelda.Infectada) vecinos++;

                            if (f < filas - 1 && actual[f + 1, c] == EstadoCelda.Infectada) vecinos++;

                            if (c > 0 && actual[f, c - 1] == EstadoCelda.Infectada) vecinos++;

                            if (c < columnas - 1 && actual[f, c + 1] == EstadoCelda.Infectada) vecinos++;

                            if (vecinos > 0)
                            {
                                double probSegura = Math.Pow(1.0 - beta, vecinos);
                                siguiente[f, c] = rng.NextDouble() > probSegura
                                    ? EstadoCelda.Infectada
                                    : EstadoCelda.Susceptible;
                            }
                            else
                                siguiente[f, c] = EstadoCelda.Susceptible;
                            break;

                        case EstadoCelda.Infectada:
                            double r = rng.NextDouble();
                            if (r < mu)
                                siguiente[f, c] = EstadoCelda.Muerta;
                            else if (r < mu + gamma)
                                siguiente[f, c] = EstadoCelda.Recuperada;
                            else
                                siguiente[f, c] = EstadoCelda.Infectada;
                            break;

                        default:
                            siguiente[f, c] = estado;
                            break;
                    }
                }


            }

            // intercambiar buffers
            for (int f = 0; f < filas; f++)
                for (int c = 0; c < columnas; c++)
                    actual[f, c] = siguiente[f, c];

        }

        static void GenerarGif(EstadoCelda[][,] framesSeq, EstadoCelda[][,] framesPar,
            int filas, int columnas, string path, int frameStep)
        {
            int escala = 3; // 3x3 pixeles
            int ancho = columnas * escala;
            int alto = filas * escala;
            int anchTotal = ancho * 2 + 10; 

            using (var gif = new Image<Rgba32>(anchTotal, alto))
            {
                gif.Metadata.GetFormatMetadata(GifFormat.Instance).RepeatCount = 0; // loop 

                for (int i = 0; i < framesSeq.Length; i++)
                {
                    var frameImg = new Image<Rgba32>(anchTotal, alto);

                    // lado secuencial a la izquierda
                    DibujarGrilla(frameImg, framesSeq[i], filas, columnas, escala, offsetX: 0);

                    // separador 
                    for (int y = 0; y < alto; y++)
                        for (int x = ancho; x < ancho + 10; x++)
                            frameImg[x, y] = new Rgba32(0, 0, 0);

                    // lado paralelo a la derecha
                    DibujarGrilla(frameImg, framesPar[i], filas, columnas, escala, offsetX: ancho + 10);

                    var gifMeta = frameImg.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance);
                    gifMeta.FrameDelay = 10; 
                    gifMeta.DisposalMethod = GifDisposalMethod.RestoreToBackground;

                    gif.Frames.AddFrame(frameImg.Frames.RootFrame);
                }


                gif.Frames.RemoveFrame(0);

                gif.SaveAsGif(path);
            }
        }

        static void DibujarGrilla(Image<Rgba32> img, EstadoCelda[,] grilla,
            int filas, int columnas, int escala, int offsetX)
        {
            for (int f = 0; f < filas; f++)
            {
                for (int c = 0; c < columnas; c++)
                {
                    Rgba32 color;
                    switch (grilla[f, c])
                    {
                        case EstadoCelda.Infectada: color = ColorInfectada; break;
                        case EstadoCelda.Recuperada: color = ColorRecuperada; break;
                        case EstadoCelda.Muerta: color = ColorMuerta; break;
                        default: color = ColorSusceptible; break;
                    }

                    for (int dy = 0; dy < escala; dy++)
                        for (int dx = 0; dx < escala; dx++)
                            img[offsetX + c * escala + dx, f * escala + dy] = color;
                }
            }
        }

        static void GenerarSpeedup(string path)
        {
            // datos
            int[] cores = { 1, 2, 4, 6 };
            double[] speedup = { 1.00, 1.64, 2.88, 3.80 };
            double[] ideal = { 1.00, 2.00, 4.00, 6.00 };

            int ancho = 600, alto = 400;
            int margenIzq = 70, margenDer = 30, margenArr = 30, margenAbajo = 50;
            int areaAncho = ancho - margenIzq - margenDer;
            int areaAlto = alto - margenArr - margenAbajo;

            using (var img = new Image<Rgba32>(ancho, alto))
            {
                // fondo blanco
                for (int y = 0; y < alto; y++)
                    for (int x = 0; x < ancho; x++)
                        img[x, y] = new Rgba32(255, 255, 255);

                // valor a pixel
                Func<int, int> xPix = c =>
                    margenIzq + (int)((c - 1) / 5.0 * areaAncho);
                Func<double, int> yPix = s =>
                    alto - margenAbajo - (int)(s / 6.0 * areaAlto);

                // linea ideal gris
                for (int i = 0; i < ideal.Length - 1; i++)
                    DibujarLinea(img,
                        xPix(cores[i]), yPix(ideal[i]),
                        xPix(cores[i + 1]), yPix(ideal[i + 1]),
                        new Rgba32(180, 180, 180));

                // linea real azul
                for (int i = 0; i < speedup.Length - 1; i++)
                    DibujarLinea(img,
                        xPix(cores[i]), yPix(speedup[i]),
                        xPix(cores[i + 1]), yPix(speedup[i + 1]),
                        new Rgba32(50, 100, 200));

                // puntos reales
                foreach (var i in new[] { 0, 1, 2, 3 })
                    DibujarCirculo(img, xPix(cores[i]), yPix(speedup[i]), 5, new Rgba32(220, 50, 50));

                // eje
                DibujarLinea(img, margenIzq, margenArr, margenIzq, alto - margenAbajo, new Rgba32(0, 0, 0));
                DibujarLinea(img, margenIzq, alto - margenAbajo, ancho - margenDer, alto - margenAbajo, new Rgba32(0, 0, 0));

                img.SaveAsPng(path);
            }
        }

        static void DibujarLinea(Image<Rgba32> img, int x0, int y0, int x1, int y1, Rgba32 color)
        {
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                if (x0 >= 0 && x0 < img.Width && y0 >= 0 && y0 < img.Height)
                    img[x0, y0] = color;
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        static void DibujarCirculo(Image<Rgba32> img, int cx, int cy, int radio, Rgba32 color)
        {
            for (int y = -radio; y <= radio; y++)
                for (int x = -radio; x <= radio; x++)
                    if (x * x + y * y <= radio * radio)
                    {
                        int px = cx + x, py = cy + y;
                        if (px >= 0 && px < img.Width && py >= 0 && py < img.Height)
                            img[px, py] = color;
                    }
        }
    }
}