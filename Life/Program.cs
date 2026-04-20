using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;

        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }

        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Pattern
    {
        public string name;
        public string image;
        public int count;
        public int size;

        public Pattern(string _name, string _image, int _size)
        {
            name = _name;
            image = _image;
            size = _size;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int Generation { get; set; }
        public List<string> states = new List<string>();

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = 0.1)
        {
            CellSize = cellSize;
            Generation = 0;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        private readonly Random rand = new Random();

        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                 cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
            Generation++;
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public int GetAliveCount()
        {
            int count = 0;
            foreach (var cell in Cells)
                if (cell.IsAlive) count++;
            return count;
        }

        public void SetCell(int x, int y, bool isAlive)
        {
            if (x >= 0 && x < Columns && y >= 0 && y < Rows)
                Cells[x, y].IsAlive = isAlive;
        }

        public bool GetCell(int x, int y)
        {
            if (x >= 0 && x < Columns && y >= 0 && y < Rows)
                return Cells[x, y].IsAlive;
            return false;
        }

        public void SaveToFile(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                for (int row = 0; row < Rows; row++)
                {
                    for (int col = 0; col < Columns; col++)
                        writer.Write(Cells[col, row].IsAlive ? '*' : ' ');
                    writer.WriteLine();
                }
            }
        }

        public void LoadFigure(string filename)
        {
            foreach (var cell in Cells)
                cell.IsAlive = false;

            using (var reader = new StreamReader(filename))
            {
                for (int row = 0; row < Rows; row++)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    for (int col = 0; col < Columns && col < line.Length; col++)
                    {
                        char c = line[col];
                        if (c == '*' || c == '█')
                            Cells[col, row].IsAlive = true;
                    }
                }
            }
            Generation = 0;
        }

        public void FindPattern(Pattern pattern)
        {
            pattern.count = 0;
            for (int row = 0; row <= Rows - pattern.size; row++)
            {
                for (int col = 0; col <= Columns - pattern.size; col++)
                {
                    if (CheckPattern(pattern, row, col))
                        pattern.count++;
                }
            }
        }

        private bool CheckPattern(Pattern pattern, int row, int col)
        {
            for (int i = 0; i < pattern.size; i++)
            {
                for (int j = 0; j < pattern.size; j++)
                {
                    int x = col + j;
                    int y = row + i;
                    char expected = pattern.image[i * pattern.size + j];
                    bool cellAlive = Cells[x, y].IsAlive;

                    if (expected == '*' && !cellAlive) return false;
                    if (expected == '.' && cellAlive) return false;
                }
            }
            return true;
        }

        public bool CheckStable()
        {
            string str = "";
            foreach (var cell in Cells)
                str += cell.IsAlive ? "*" : " ";

            states.Add(str);
            if (states.Count > 3) states.RemoveAt(0);
            return states.Count > 2 && states.Take(states.Count - 1).Contains(str);
        }
    }

    public class Graph
    {
        public static Dictionary<int, int> CountAlive(double density, int width, int height)
        {
            var res = new Dictionary<int, int>();
            var board = new Board(width, height, 1, density);
            int gen = 0;

            while (true)
            {
                if (gen % 20 == 0)
                    res.Add(gen, board.GetAliveCount());
                if (board.CheckStable())
                    break;
                board.Advance();
                gen++;
                if (gen > 500) break;
            }
            return res;
        }

        public static void CreateGraph(int width, int height)
        {
            try
            {
                if (!Directory.Exists("Data"))
                    Directory.CreateDirectory("Data");

                var plot = new ScottPlot.Plot();
                plot.XLabel("Поколение");
                plot.YLabel("Количество живых клеток");
                plot.Title("Переход в стабильное состояние");

                Random rnd = new Random();
                List<double> densities = new List<double>() { 0.2, 0.3, 0.4, 0.5, 0.6, 0.7 };

                using (var writer = new StreamWriter("Data/data.txt"))
                {
                    writer.WriteLine("Density,Generation,AliveCount");

                    foreach (var density in densities)
                    {
                        var data = CountAlive(density, width, height);

                        foreach (var kvp in data)
                            writer.WriteLine($"{density},{kvp.Key},{kvp.Value}");

                        if (data.Count > 0)
                        {
                            var scatter = plot.Add.Scatter(data.Keys.ToArray(), data.Values.ToArray());
                            scatter.Label = $"Плотность {density}";
                            scatter.Color = new ScottPlot.Color(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                        }
                    }
                }

                plot.ShowLegend();
                plot.SavePng("Data/plot.png", 1920, 1080);
                Console.WriteLine("График сохранён в Data/plot.png");
                Console.WriteLine("Данные для графика сохранены в Data/data.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка построения графика: {ex.Message}");
            }
        }
    }

    public class Settings
    {
        public int Width { get; set; } = 50;
        public int Height { get; set; } = 20;
        public int CellSize { get; set; } = 1;
        public int DelayMs { get; set; } = 500;
        public double LiveDensity { get; set; } = 0.5;
        public int MaxGenerations { get; set; } = 500;

        public void Save(string filename = "settings.json")
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, json);
        }

        public static Settings Load(string filename = "settings.json")
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                return JsonSerializer.Deserialize<Settings>(json);
            }
            return new Settings();
        }
    }

    class Program
    {
        static Board board;
        static Settings settings;
        static bool autoMode = false;
        static bool exitRequested = false;

        static Pattern block = new Pattern("Блок", "****", 2);
        static Pattern beehive = new Pattern("Улей", ".**.*..*.**.....", 4);
        static Pattern boat = new Pattern("Лодка", ".*.*.*.**", 3);
        static Pattern ship = new Pattern("Корабль", "**.*.*.**", 3);
        static Pattern loaf = new Pattern("Каравай", ".**.*..*.*.*..*.", 4);

        static void Reset()
        {
            board = new Board(settings.Width, settings.Height, settings.CellSize, settings.LiveDensity);
        }

        static void Render()
        {
            Console.Clear();
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                    Console.Write(board.Cells[col, row].IsAlive ? '█' : ' ');
                Console.WriteLine();
            }
            Console.WriteLine($"Поколение: {board.Generation} | Живых клеток: {board.GetAliveCount()}");
        }

        static void ShowMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("ИГРА 'ЖИЗНЬ'\n");
                Console.WriteLine("1. Загрузить фигуру из файла");
                Console.WriteLine("2. Случайное поле");
                Console.WriteLine("3. Создать Блок");
                Console.WriteLine("4. Создать Мигалку");
                Console.WriteLine("5. Создать Планер");
                Console.WriteLine("6. Создать Улей");
                Console.WriteLine("7. Создать Лодку");
                Console.WriteLine("8. Создать Корабль");
                Console.WriteLine("9. Создать Каравай");
                Console.WriteLine("A. Анализ колонии");
                Console.WriteLine("B. Исследование стабильности");
                Console.WriteLine("C. Построить график");
                Console.WriteLine("N. Настройки");
                Console.WriteLine("X. Выход");
                Console.Write("\nВыбор: ");

                string choice = Console.ReadLine().ToUpper();

                switch (choice)
                {
                    case "1": LoadFigureDialog(); break;
                    case "2": CreateRandom(); break;
                    case "3": CreateBlock(); break;
                    case "4": CreateBlinker(); break;
                    case "5": CreateGlider(); break;
                    case "6": CreateBeehive(); break;
                    case "7": CreateBoat(); break;
                    case "8": CreateShip(); break;
                    case "9": CreateLoaf(); break;
                    case "A": AnalyzeColony(); break;
                    case "B": ResearchStability(); break;
                    case "C": CreateGraph(); break;
                    case "N": EditSettings(); break;
                    case "X": return;
                }
            }
        }

        static void LoadFigureDialog()
        {
            Console.Write("Введите имя файла: ");
            string filename = Console.ReadLine();

            if (!File.Exists(filename))
            {
                Console.WriteLine("Файл не найден!");
                Console.ReadKey();
                return;
            }

            Reset();
            board.LoadFigure(filename);
            Console.WriteLine($"Загружена фигура. Живых клеток: {board.GetAliveCount()}");
            Console.ReadKey();
            RunSimulation();
        }

        static void CreateRandom()
        {
            Reset();
            Console.WriteLine($"Случайное поле. Плотность: {settings.LiveDensity:P0}");
            Console.ReadKey();
            RunSimulation();
        }

        static void CreateBlock()
        {
            Reset();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx, cy].IsAlive = true;
            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 1].IsAlive = true;

            Console.WriteLine("Создан блок");
            Console.ReadKey();
            RunSimulation();
        }

        static void CreateBlinker()
        {
            Reset();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx - 1, cy].IsAlive = true;
            board.Cells[cx, cy].IsAlive = true;
            board.Cells[cx + 1, cy].IsAlive = true;

            Console.WriteLine("Создана мигалка");
            Console.ReadKey();
            RunSimulation();
        }

        static void CreateGlider()
        {
            Reset();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx, cy].IsAlive = true;
            board.Cells[cx + 1, cy + 1].IsAlive = true;
            board.Cells[cx + 2, cy].IsAlive = true;
            board.Cells[cx + 2, cy + 1].IsAlive = true;
            board.Cells[cx + 2, cy + 2].IsAlive = true;

            Console.WriteLine("Создан планер");
            Console.ReadKey();
            RunSimulation();
        }

        static void CreateBeehive()
        {
            Reset();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx + 2, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 3, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 2].IsAlive = true;
            board.Cells[cx + 2, cy + 2].IsAlive = true;

            Console.WriteLine("Создан улей");
            Console.ReadKey();
            RunSimulation();
        }

        static void CreateBoat()
        {
            Reset();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 2, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 2].IsAlive = true;
            board.Cells[cx + 2, cy + 2].IsAlive = true;

            Console.WriteLine("Создана лодка");
            Console.ReadKey();
            RunSimulation();
        }

        static void CreateShip()
        {
            Reset();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx, cy].IsAlive = true;
            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 2, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 2].IsAlive = true;
            board.Cells[cx + 2, cy + 2].IsAlive = true;

            Console.WriteLine("Создан корабль");
            Console.ReadKey();
            RunSimulation();
        }

        static void CreateLoaf()
        {
            Reset();
            foreach (var cell in board.Cells) cell.IsAlive = false;

            int cx = board.Columns / 2;
            int cy = board.Rows / 2;

            board.Cells[cx + 1, cy].IsAlive = true;
            board.Cells[cx + 2, cy].IsAlive = true;
            board.Cells[cx, cy + 1].IsAlive = true;
            board.Cells[cx + 3, cy + 1].IsAlive = true;
            board.Cells[cx + 1, cy + 2].IsAlive = true;
            board.Cells[cx + 3, cy + 2].IsAlive = true;
            board.Cells[cx + 2, cy + 3].IsAlive = true;

            Console.WriteLine("Создан каравай");
            Console.ReadKey();
            RunSimulation();
        }

        static void AnalyzeColony()
        {
            if (board == null)
            {
                Console.WriteLine("Сначала создайте колонию!");
                Console.ReadKey();
                return;
            }

            block.count = 0;
            beehive.count = 0;
            boat.count = 0;
            ship.count = 0;
            loaf.count = 0;

            board.FindPattern(block);
            board.FindPattern(beehive);
            board.FindPattern(boat);
            board.FindPattern(ship);
            board.FindPattern(loaf);

            Console.Clear();
            Console.WriteLine("АНАЛИЗ КОЛОНИИ\n");
            Console.WriteLine($"Всего живых клеток: {board.GetAliveCount()}\n");
            Console.WriteLine("НАЙДЕННЫЕ ФИГУРЫ\n");
            Console.WriteLine($"Блок: {block.count} шт.");
            Console.WriteLine($"Улей: {beehive.count} шт.");
            Console.WriteLine($"Лодка: {boat.count} шт.");
            Console.WriteLine($"Корабль: {ship.count} шт.");
            Console.WriteLine($"Каравай: {loaf.count} шт.");

            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        static void ResearchStability()
        {
            Console.Clear();
            Console.WriteLine("ИССЛЕДОВАНИЕ СТАБИЛЬНОСТИ\n");

            if (!Directory.Exists("Stability"))
                Directory.CreateDirectory("Stability");

            double[] densities = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7 };

            using (var writer = new StreamWriter("Stability/stability.txt"))
            {
                writer.WriteLine("Density,StableGeneration");

                foreach (var density in densities)
                {
                    Console.Write($"Плотность {density:P0}... ");
                    var testBoard = new Board(settings.Width, settings.Height, settings.CellSize, density);
                    int stableGen = -1;

                    for (int gen = 0; gen < 500; gen++)
                    {
                        testBoard.Advance();
                        if (testBoard.CheckStable())
                        {
                            stableGen = gen;
                            break;
                        }
                    }

                    writer.WriteLine($"{density:F2},{stableGen}");
                    Console.WriteLine($" стабилизация на {stableGen} поколении");
                }
            }

            Console.WriteLine("\nРезультаты сохранены в Stability/stability.txt");
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        static void CreateGraph()
        {
            Console.Clear();
            Console.WriteLine("ПОСТРОЕНИЕ ГРАФИКА\n");
            Graph.CreateGraph(settings.Width, settings.Height);
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        static void EditSettings()
        {
            Console.Clear();
            Console.WriteLine("НАСТРОЙКИ\n");
            Console.WriteLine($"1. Ширина поля: {settings.Width}");
            Console.WriteLine($"2. Высота поля: {settings.Height}");
            Console.WriteLine($"3. Задержка (мс): {settings.DelayMs}");
            Console.WriteLine($"4. Плотность: {settings.LiveDensity:P0}");
            Console.WriteLine($"5. Макс. поколений: {settings.MaxGenerations}");
            Console.Write("\nВыберите параметр (1-5): ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Console.Write("Новая ширина: ");
                    settings.Width = int.Parse(Console.ReadLine());
                    break;
                case "2":
                    Console.Write("Новая высота: ");
                    settings.Height = int.Parse(Console.ReadLine());
                    break;
                case "3":
                    Console.Write("Новая задержка (мс): ");
                    settings.DelayMs = int.Parse(Console.ReadLine());
                    break;
                case "4":
                    Console.Write("Новая плотность (0-1): ");
                    settings.LiveDensity = double.Parse(Console.ReadLine());
                    break;
                case "5":
                    Console.Write("Макс. поколений: ");
                    settings.MaxGenerations = int.Parse(Console.ReadLine());
                    break;
            }
            settings.Save();
            Console.WriteLine("\nНастройки сохранены!");
            Console.ReadKey();
        }

        static void RunSimulation()
        {
            autoMode = false;
            exitRequested = false;

            while (!exitRequested && board.Generation < settings.MaxGenerations)
            {
                Render();
                Console.WriteLine("\nУправление: Пробел - шаг | R - авто | S - сохранить | A - анализ | Q - выход");

                if (autoMode)
                {
                    Thread.Sleep(settings.DelayMs);
                    board.Advance();

                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.Q) exitRequested = true;
                        if (key == ConsoleKey.R) autoMode = false;
                    }
                }
                else
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Spacebar:
                            board.Advance();
                            break;
                        case ConsoleKey.R:
                            autoMode = true;
                            break;
                        case ConsoleKey.S:
                            string filename = $"save_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                            board.SaveToFile(filename);
                            Console.WriteLine($"Сохранено в {filename}");
                            Thread.Sleep(500);
                            break;
                        case ConsoleKey.A:
                            AnalyzeColony();
                            break;
                        case ConsoleKey.Q:
                            exitRequested = true;
                            break;
                    }
                }
            }

            if (board.Generation >= settings.MaxGenerations)
            {
                Console.WriteLine($"\nДостигнут лимит поколений ({settings.MaxGenerations})");
                Console.ReadKey();
            }
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            settings = Settings.Load();

            if (!Directory.Exists("figures"))
                Directory.CreateDirectory("figures");

            CreateExampleFigures();
            ShowMenu();
        }

        static void CreateExampleFigures()
        {
            if (!File.Exists("figures/block.txt"))
                File.WriteAllText("figures/block.txt", "**\n**");

            if (!File.Exists("figures/blinker.txt"))
                File.WriteAllText("figures/blinker.txt", "***");

            if (!File.Exists("figures/glider.txt"))
                File.WriteAllText("figures/glider.txt", ".*.\n..*\n***");

            if (!File.Exists("figures/beehive.txt"))
                File.WriteAllText("figures/beehive.txt", ".**.\n*..*\n.**.");
        }
    }
}
