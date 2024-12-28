public class Parallel_Programming
{
    static int Factorial(int x)
    {
        int result = 1;

        for (int i = 1; i <= x; i++)
        {
            result *= i;
        }
        return result;
    }

    public static void Main()
    {
        int[] numbers = new int[] { 1, 2, 3, 4, 5, 6, 7 };
        /*
            Метод AsParallel() позволяет распараллелить запрос к источнику данных. 
            Он реализован как метод расширения LINQ. При вызове данного метода 
            источник данных разделяется на части и над каждой частью отдельно 
            производятся операции.
        */
        var results = from n in numbers.AsParallel()
                      select new { Number = n, Factorial = Factorial(n)};

        Console.WriteLine("Запрос с помощью оператора LINQ");
        foreach (var n in results)
            Console.WriteLine($"Факториал числа {n.Number} равен {n.Factorial}");

        // Альтернативный вариант с помощью метода расширения Select
        var results2 = numbers.AsParallel().Select(n => new { Number = n, Factorial = Factorial(n) });

        // Для вывода результата параллельной операции используется цикл foreach.
        // Но его использование приводит к увеличению издержек - необходимо склеить
        // полученные в разных потоках данные в один набор и затем их перебрать в цикле.
        Console.WriteLine("Запрос с помощью метода расширения LINQ");
        foreach (var n in results2)
            Console.WriteLine($"Факториал числа {n.Number} равен {n.Factorial}");

        // Более оптимально было бы использование метода ForAll(),
        // который выводит данные в том же потоке, в котором они обрабатываются.
        Console.WriteLine("Вывод данных с помощью метода ForAll");
        results2.ForAll(Console.WriteLine);

        // Метод ForAll() в качестве параметра принимает делегат Action,
        // который указывает на выполняемое действие.

        /*
         * Применяя метод AsOrdered, результат будет упорядоченным в соответствии 
         * с тем, как элементы располагаются в исходной последовательности.
         */
        var results3 = from n in numbers.AsParallel().AsOrdered()
                       select new { Number = n, Factorial = Factorial(n) };
        Console.WriteLine("Упорядочивание в параллельной операции");
        foreach (var n in results3)
            Console.WriteLine($"Факториал числа {n.Number} равен {n.Factorial}");
        /*
         * Важно понимать, что упорядочивание в параллельной операции приводит 
         * к увеличению издержек, поэтому подобный запрос будет выполняться медленнее, 
         * чем неупорядоченный. И если задача не требует возвращение упорядоченного 
         * набора, то лучше не применять метод AsOrdered.
         * */

        /* Если в дальнейшем для полученного набора упорядочивание больше не требуется, 
         * можно применить метод AsUnordered()
         * */
        var query = from n in results3.AsUnordered()
                     select n;
        Console.WriteLine("Отмена упорядочивания в параллельной операции");
        foreach (var n in query)
            Console.WriteLine($"Факториал числа {n.Number} равен {n.Factorial}");

        int[] data = new int[10000000];

        for (int i = 0; i < data.Length; i++) data[i] = i;

        data[1000] = -1;
        data[14000] = -2;
        data[150000] = -3;
        data[676000] = -4;
        data[940000] = -5;
        data[1500000] = -6;
        data[7000000] = -7;
        data[8024540] = -8;
        data[9908000] = -9;
 
        var negatives = from val in data.AsParallel()
                        where val < 0
                        select val;
        Console.WriteLine("Вывод отобранных отрицательных элементов коллекции");
        negatives.ForAll(Console.WriteLine);

        string[] ArrayStrings = new string[100];
        for (int i = 0; i < ArrayStrings.Length; i++)
            ArrayStrings[i] = i.ToString();

        ArrayStrings[1] = "A";

        // При параллельной обработке коллекция разделяется на части
        // и каждая часть обрабатывается в отдельном потоке.
        // Однако если возникнет ошибка в одном из потоков,
        // то система прерывает выполнение всех потоков.

        var arrayNumbers = from n in ArrayStrings.AsParallel()
                      select Convert.ToInt32(n);
        Console.WriteLine("Обработка ошибок");
        try
        {
            arrayNumbers.ForAll(n => Console.WriteLine(n));
        }
        catch (AggregateException ex) 
        {
            // При генерации исключений все они агрегируются в одном исключении типа AggregateException
            foreach (var e in ex.InnerExceptions)
            {
                Console.WriteLine(e.Message);
            }
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        // Запускаем асинхронную задачу, в которой через 1010 миллисекунд прерываем операцию
        new Task(() =>
        {
            Thread.Sleep(1010);
            cts.Cancel();
            // В параллельной запущенной задаче вызывается метод cts.Cancel(),
            // что приводит к завершению операции и генерации исключения OperationCanceledException
        }).Start();

        try
        {
            var squares = from n in numbers.AsParallel().WithCancellation(cts.Token)
                          select Square(n);

            foreach (var n in squares)
                Console.WriteLine(n);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Операция была прервана");
        }
        finally
        {
            cts.Dispose();
        }
        int Square(int n)
        {
            var result = n * n;
            Console.WriteLine($"Квадрат числа {n} равен {result}");
            Thread.Sleep(1000); // имитация долгого вычисления
            return result;
        }

    }
}