using System;
using System.Threading;
using System.Threading.Tasks;
using ParallelTasks = System.Threading.Tasks.Parallel;

namespace SIR.Parallel
{
    public enum EstadoCelda : byte
    {
        Susceptible = 0,
        Infectada = 1,
        Recuperada = 2,
        Muerta = 3
    }

    public class CeldaParalela
    {
        public readonly int Filas;
        public readonly int Columnas;

        private EstadoCelda[] _actual;
        private EstadoCelda[] _siguiente;

        private readonly double _beta;
        private readonly double _gamma;
        private readonly double _mu;

        // usamos un random por hilo para evitar condiciones de carrera
        [ThreadStatic]
        private static Random _hiloAleatorio;

        private int _semilla;

        public CeldaParalela(int filas, int columnas, double beta, double gamma, double mu, int semilla = 12)
        {
            Filas = filas;
            Columnas = columnas;

            _beta = beta;
            _gamma = gamma;
            _mu = mu;
            _semilla = semilla;

            _actual = new EstadoCelda[filas * columnas];
            _siguiente = new EstadoCelda[filas * columnas];
        }

        private int Idx(int f, int c) => f * Columnas + c;

        // cada hilo cuenta con su propio Random al uso
        private Random ObtenerRandom()
        {
            if (_hiloAleatorio == null)
            {
                _hiloAleatorio = new Random(Interlocked.Increment(ref _semilla) * 1000);
            }
            return _hiloAleatorio;
        }

        public void Inicializar(int inicialesInfectados = 5)
        {
            for (int i = 0; i < _actual.Length; i++)
            {
                _actual[i] = EstadoCelda.Susceptible;

            }

            int filaColumna = Filas / 2;
            int columnaCol = Columnas / 2;

            int infectadosColocados = 0;


            for (int dr = -2; dr <= 2 && infectadosColocados < inicialesInfectados; dr++)
            {
                for (int dc = -2; dc <= 2 && infectadosColocados < inicialesInfectados; dc++)
                {
                    _actual[Idx(filaColumna + dr, columnaCol + dc)] = EstadoCelda.Infectada;
                    infectadosColocados++;
                }
            }
                
        }

        private int ContarVecinosGhostCells(int f, int c, int filaInicio, int filaFin,
            EstadoCelda[] ghostArriba, EstadoCelda[] ghostAbajo)
        {
            int contador = 0;

            if (f > 0)
            {
                EstadoCelda arriba = (f == filaInicio) ? ghostArriba[c] : _actual[Idx(f - 1, c)];
                if (arriba == EstadoCelda.Infectada) contador++;
            }

            if (f < Filas - 1)
            {
                EstadoCelda abajo = (f == filaFin - 1) ? ghostAbajo[c] : _actual[Idx(f + 1, c)];
                if (abajo == EstadoCelda.Infectada) contador++;
            }

            if (c > 0 && _actual[Idx(f, c - 1)] == EstadoCelda.Infectada) contador++;
            if (c < Columnas - 1 && _actual[Idx(f, c + 1)] == EstadoCelda.Infectada) contador++;

            return contador;
        }

        public void AvanzarDia(int maxThreads)
        {
            int filasPorBloque = Filas / maxThreads;
            var opciones = new ParallelOptions { MaxDegreeOfParallelism = maxThreads };

            ParallelTasks.For(0, maxThreads, opciones, idBloque =>
            {
                Random numeroRandom = ObtenerRandom();

                int filaInicio = idBloque * filasPorBloque;
                int filaFin = (idBloque == maxThreads - 1) ? Filas : filaInicio + filasPorBloque;

                // copiar las filas fronteras de bloques vecinos a arreglos temporales (ghost cells)
                EstadoCelda[] ghostArriba = new EstadoCelda[Columnas];
                EstadoCelda[] ghostAbajo = new EstadoCelda[Columnas];

                if (filaInicio > 0)
                {
                    Array.Copy(_actual, (filaInicio - 1) * Columnas, ghostArriba, 0, Columnas);

                }

                if (filaFin < Filas)
                {
                    Array.Copy(_actual, filaFin * Columnas, ghostAbajo, 0, Columnas);

                }

                for (int f = filaInicio; f < filaFin; f++)
                {
                    for (int c = 0; c < Columnas; c++)
                    {
                        EstadoCelda estado = _actual[Idx(f, c)];

                        switch (estado)
                        {
                            case EstadoCelda.Susceptible:
                                int infectados = ContarVecinosGhostCells(f, c, filaInicio, filaFin, ghostArriba, ghostAbajo);
                                if (infectados > 0)
                                {
                                    double probSegura = Math.Pow(1.0 - _beta, infectados);
                                    if (numeroRandom.NextDouble() > probSegura)
                                        _siguiente[Idx(f, c)] = EstadoCelda.Infectada;
                                    else
                                        _siguiente[Idx(f, c)] = EstadoCelda.Susceptible;
                                }
                                else
                                    _siguiente[Idx(f, c)] = EstadoCelda.Susceptible;
                                break;

                            case EstadoCelda.Infectada:
                                double randomDouble = numeroRandom.NextDouble();
                                if (randomDouble < _mu)
                                    _siguiente[Idx(f, c)] = EstadoCelda.Muerta;
                                else if (randomDouble < _mu + _gamma)
                                    _siguiente[Idx(f, c)] = EstadoCelda.Recuperada;
                                else
                                    _siguiente[Idx(f, c)] = EstadoCelda.Infectada;
                                break;

                            default:
                                _siguiente[Idx(f, c)] = estado;
                                break;
                        }
                    }
                }
            });

            // intercambiar buffers
            EstadoCelda[] temp = _actual;
            _actual = _siguiente;
            _siguiente = temp;
        }

        public void ObtenerConteos(out int s, out int i, out int r, out int m)
        {
            int ts = 0, ti = 0, tr = 0, tm = 0;

            ParallelTasks.For(0, Filas,
                () => (s: 0, i: 0, r: 0, m: 0),
                (f, estado, local) =>
                {
                    for (int c = 0; c < Columnas; c++)
                    {
                        switch (_actual[Idx(f, c)])
                        {
                            case EstadoCelda.Susceptible: local.s++; break;
                            case EstadoCelda.Infectada: local.i++; break;
                            case EstadoCelda.Recuperada: local.r++; break;
                            case EstadoCelda.Muerta: local.m++; break;
                        }
                    }
                    return local;
                },
                local =>
                {
                    Interlocked.Add(ref ts, local.s);
                    Interlocked.Add(ref ti, local.i);
                    Interlocked.Add(ref tr, local.r);
                    Interlocked.Add(ref tm, local.m);
                }
            );

            s = ts; i = ti; r = tr; m = tm;
        }

        public EstadoCelda GetCell(int f, int c) => _actual[Idx(f, c)];
    }
}