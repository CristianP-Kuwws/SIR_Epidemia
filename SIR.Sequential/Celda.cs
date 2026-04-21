using System;

namespace SIR.Sequential
{
    public enum EstadoCelda : byte
    {
        Susceptible = 0,
        Infectada = 1,
        Recuperada = 2,
        Muerta = 3
    }

    public class Celda
    {
        public readonly int Filas;

        public readonly int Columnas;


        // array 1d para rendimiento, el indice es igual a (fils * Columnas + col)
        private EstadoCelda[] _actual;
        private EstadoCelda[] _siguiente;

        private readonly double _beta;
        private readonly double _gamma;
        private readonly double _mu;

        private readonly Random _numeroRandom;

        public Celda(int filas, int columnas, double beta, double gamma, double mu, int semilla = 12)
        {
            Filas = filas;
            Columnas = columnas;
            _beta = beta;
            _gamma = gamma;
            _mu = mu;
            _numeroRandom = new Random(semilla);

            _actual = new EstadoCelda[filas * columnas];
            _siguiente = new EstadoCelda[filas * columnas];
        }

        private int Idx(int f, int c) => f * Columnas + c;

        public void Inicializar(int inicialesInfectados = 10)
        {
            // Todos los susceptibles
            for (int i = 0; i < _actual.Length; i++)
            {
                _actual[i] = EstadoCelda.Susceptible;
            }

            // Colocar infectados cerca del centro

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

        // Contar vecinos infectados en direcciones arriba, abajo, izq, der

        private int ContarVecinosInfectados(int f, int c)
        {
            int count = 0;

            if (f > 0 && _actual[Idx(f - 1, c)] == EstadoCelda.Infectada) count++;

            if (f < Filas - 1 && _actual[Idx(f + 1, c)] == EstadoCelda.Infectada) count++;

            if (c > 0 && _actual[Idx(f, c - 1)] == EstadoCelda.Infectada) count++;

            if (c < Columnas - 1 && _actual[Idx(f, c + 1)] == EstadoCelda.Infectada) count++;

            return count;
        }

        // Avanzar dia para aplicar las reglas SIR a toda la grilla
        public void AvanzarDia()
        {
            for (int f = 0; f < Filas; f++)
            {
                for (int c = 0; c < Columnas; c++)
                {
                    EstadoCelda estado = _actual[Idx(f, c)];

                    switch (estado)
                    {
                        case EstadoCelda.Susceptible:
                            int infectados = ContarVecinosInfectados(f, c);
                            if (infectados > 0)
                            {
                                // probabilidad de no contagiarse equivale a (1-beta)^infectados
                                double probSegura = Math.Pow(1.0 - _beta, infectados);

                                if (_numeroRandom.NextDouble() > probSegura)
                                    _siguiente[Idx(f, c)] = EstadoCelda.Infectada;
                                else
                                    _siguiente[Idx(f, c)] = EstadoCelda.Susceptible;
                            }
                            else
                            {
                                _siguiente[Idx(f, c)] = EstadoCelda.Susceptible;
                            }
                            break;

                        case EstadoCelda.Infectada:
                            double roll = _numeroRandom.NextDouble();

                            if (roll < _mu)
                                _siguiente[Idx(f, c)] = EstadoCelda.Muerta;
                            else if (roll < _mu + _gamma)
                                _siguiente[Idx(f, c)] = EstadoCelda.Recuperada;
                            else
                                _siguiente[Idx(f, c)] = EstadoCelda.Infectada;
                            break;

                        // recuperados y muertos se mantienen igual
                        default:
                            _siguiente[Idx(f, c)] = estado;
                            break;
                    }
                }
            }

            // Intercambiar buffers para convertir _siguiente en _actual
            EstadoCelda[] temp = _actual;
            _actual = _siguiente;
            _siguiente = temp;
        }

        // Obtener conteos para estadisticas
        public void ObtenerConteos(out int s, out int i, out int r, out int m)
        {
            s = i = r = m = 0;

            foreach (EstadoCelda cell in _actual)
            {
                switch (cell)
                {
                    case EstadoCelda.Susceptible: s++; break;
                    case EstadoCelda.Infectada: i++; break;
                    case EstadoCelda.Recuperada: r++; break;
                    case EstadoCelda.Muerta: m++; break;
                }
            }
        }

        // Acceso de lectura al estado actual 
        public EstadoCelda ObtenerCeldas(int r, int c) => _actual[Idx(r, c)];
    }
}
