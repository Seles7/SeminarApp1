using System;
using System.Collections.Generic;

namespace SeminarApp1
{
    internal class Program
    {
        static int N = 8; // Число неизвестных аргументов в функции
        static int Np = N + 1; // Число точек в симплексе
        static int Nr = 5; // Число поисков решений

        static double[] rightBounds = { 1.0, 6.0, 12.0, 4.0, 1.0, 5.0, 10.0, 3.0 }; // Простые ограничения неизвестных аргументов (правая граница)
        static double[] leftBounds = { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }; // Простые ограничения неизвестных аргументов (левая граница)

        static double pfmCoef = 10e3; // Коэффициент штрафа в штрафной функции
        static double alpha = 1; // Коэффициент отражения
        static double betta = 0.5; // Коэффициент сжатия
        static double gamma = 2; // Коэффициент Растяжения

        static double sigma; // Оценка выполнения условия завершения вычислений
        static double eps = 10e-4; // Порог в условии завершения вычислений

        static Random rand = new Random();

        // Минимизируемая функция
        static double TargetFunction(double[] x)
        {
            return Math.Pow(113.0 - 3.0 * x[0] - 5.0 * x[1] - 7.0 * x[2] - 5.0 * x[3] - 6.0 * x[4] - 6.0 * x[5] - 6.0 * x[6] - 4.0 * x[7], 2);
        }

        static void Main()
        {
            if (rightBounds.Length != N)
            {
                Console.WriteLine("Ошибка длины массива ограничений справа!");
                return;
            }

            List<Point> results = new List<Point>();

            // Вычисление множества решений задачи
            for (int i = 0; i < Nr; i++)
            {
                Point newResult = NelderMead();
                bool isUniquePoint = true;

                // Проверка нового найденного решения на его уникальность (не является ли оно уже найденным)
                foreach (Point result in results)
                {
                    isUniquePoint = !AreEqual(result, newResult);
                    if (!isUniquePoint) break;
                }

                if (isUniquePoint)
                    results.Add(newResult);
            }

            // Вывод результатов
            Console.WriteLine("РЕЗУЛЬТАТЫ\n==========");
            Console.WriteLine($"Число поисков решений: {Nr}");
            Console.WriteLine($"Найденные уникальные решения:");
            for (int i = 0; i < results.Count; i++)
            {
                Console.Write($"Точка P_{i + 1}: (");
                for (int j = 0; j < N; j++)
                {
                    Console.Write($"{results[i].x[j]:F3}");
                    if (j < N - 1) Console.Write(";  ");
                }
                Console.Write(")\t");
                Console.WriteLine($"f(P_{i + 1}) = {results[i].value:F3}");
            }
            Console.ReadKey();

            return;
        }

        // Реализация метода Нелдера-Мида
        static Point NelderMead()
        {
            List<Point> points = new List<Point>(Np); // Симплекс точек, характеризующий решение задачи

            for (int i = 0; i < Np; i++)
            {
                points.Add(new Point());
            }

            Point center; // Центральная точка (центр точек симплекса кроме наихудшей)
            Point reflected; // Отражённая точка
            Point expanded; // Растяжённая точка
            Point shrinked; // Сжатая точка

            while (true)
            {
                points.Sort((p1, p2) => p1.value.CompareTo(p2.value)); // Сортируем точки по возрастаний значения штрафной функции

                center = Center(points);
                reflected = new Point();
                expanded = new Point();
                shrinked = new Point();

                // Условие завершения вычислений
                sigma = SolveSigma(points, center);
                if (sigma < eps) break;

                // Отражение наихудшей точки
                for (int i = 0; i < N; i++)
                {
                    reflected.x[i] = center.x[i] + alpha * (center.x[i] - points[N].x[i]);
                }
                reflected.value = PenaltyFunction(reflected.x);

                // Проверка отражённой точки
                if (reflected.value < points[N].value)
                {
                    // Отражённая точка лучшая, растягиваем её
                    for (int i = 0; i < N; i++)
                    {
                        expanded.x[i] = center.x[i] + gamma * (reflected.x[i] - center.x[i]);
                    }
                    expanded.value = PenaltyFunction(expanded.x);

                    // Проверка растяжённой точки
                    if (expanded.value < reflected.value)
                    {
                        points[N] = expanded;
                        continue;
                    }
                    else
                    {
                        points[N] = reflected;
                        continue;
                    }
                }
                else if (points[0].value < reflected.value && reflected.value < points[1].value)
                {
                    // Отражённая точка неплохая
                    points[N] = reflected;
                    continue;
                }
                else if (points[1].value < reflected.value && reflected.value < points[N].value)
                {
                    // Отраженная точка не самая лучшая
                    points[N] = reflected;
                }
                // Если отражённая точка худшая, ничего не заменяем

                // Если алгоритм не перешёл ранее на следующую итерацию, то сжимаем худшую точку
                for (int i = 0; i < N; i++)
                {
                    shrinked.x[i] = center.x[i] + betta * (points[N].x[i] - center.x[i]);
                }
                shrinked.value = PenaltyFunction(shrinked.x);

                // Проверка сжатой точки
                if (shrinked.value < points[N].value)
                {
                    // Сжатая точка лучше худшей, производим замену
                    points[N] = shrinked;
                    continue;
                }
                else
                {
                    // Изначальные точки лучшие, сжимаем симплекс к лучшей в нём точке
                    for (int i = 1; i < Np; i++)
                    {
                        for (int j = 0; j < N; j++)
                        {
                            points[i].x[j] = points[0].x[j] + 0.5 * (points[i].x[j] - points[0].x[j]);
                        }
                        points[i].value = PenaltyFunction(points[i].x);
                    }
                    continue;
                }
            }

            return points[0];
        }

        // Штрафная функция (реализация метода штрафных функций)
        static double PenaltyFunction(double[] x)
        {
            return TargetFunction(x) + Penalty(x);
        }

        // Функция штрафа
        static double Penalty(double[] x)
        {
            double f = 0.0;

            for (int i = 0; i < N; i++)
            {
                f += pfmCoef * Math.Pow(Math.Max(x[i] - rightBounds[i], 0.0), 2);
                f += pfmCoef * Math.Pow(Math.Max(leftBounds[i] - x[i], 0.0), 2);
            }
            return f;
        }

        // Функция расчёта точки центра точек симплекса (кроме наихудшей)
        static Point Center(List<Point> points)
        {
            Point center = new Point();

            for (int i = 0; i < N - 1; i++)
            {
                center.x[i] = 0.0;
                for (int j = 0; j < points.Count; j++)
                {
                    center.x[i] += points[j].x[i];
                }
                center.x[i] /= points.Count;
            }

            return center;
        }

        // Функция расчёта оценки выполнения условия завершения вычислений
        static double SolveSigma(List<Point> points, Point center)
        {
            double s = 0.0;

            foreach (Point point in points)
            {
                s += Math.Pow(point.value - center.value, 2);
            }
            s = Math.Sqrt(s) / points.Count;

            return s;
        }

        // Функция проверки совпадания точек
        static bool AreEqual(Point p1, Point p2)
        {
            double diff = 0.0;

            for (int i = 0; i < N; i++)
            {
                diff += Math.Pow(p1.x[i] - p2.x[i], 2);
            }
            diff = Math.Sqrt(diff);

            if (diff < eps) return true;
            else return false;
        }

        // Класс точки (имеет поля: координаты этой точки x[] и значение штрафной функции в ней f)
        class Point
        {
            public double value;
            public double[] x;

            public Point()
            {
                this.x = new double[N];
                for (int i = 0; i < N; i++)
                {
                    this.x[i] = rand.NextDouble() * rightBounds[i];
                }

                value = PenaltyFunction(this.x);
            }
        }
    }
}
