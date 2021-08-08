using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ConsoleApp9
{
    class Program
    {
        static readonly string directory = "C:\\Users\\Александр\\Documents\\генерация";
        static readonly string filePathForSave = "C:\\Users\\Александр\\Documents\\Генерацияфайла.txt";
        static readonly string tempFilePathFinal = "C:\\Users\\Александр\\Documents\\BigFileSorted.txt";
        static void GenerateFile(int numRow, int maxLen, string filepath)
        {
            FileStream file1 = new FileStream(filepath, FileMode.Create); //создаем файловый поток
            StreamWriter writer = new StreamWriter(file1); //создаем «потоковый писатель» и связываем его с файловым потоком
            Random rnd = new Random();
            Random rnd1 = new Random();
            int len;
            string row = "";
            for (int i = 0; i < numRow; i++)
            {
                len = rnd.Next(1, maxLen);
                for (int j = 0; j < len; j++)
                {
                    row += (char)rnd1.Next(97, 123); //в таблице кодов ASCII английский алфавит находится на отрезке [97,123]
                }
                writer.WriteLine(row);
                row = "";
            }
            writer.Close();
            file1.Close();
        }
        static void Split(string filepath)
        {
            Directory.CreateDirectory(directory); // создаем папку для хранения временных файлов

            int split_num = 1; // переменная для нумерации split файлов
            StreamWriter writer = new StreamWriter(
              string.Format("{0}\\split{1}.txt", directory, split_num)); //создаем split файл и открываем для него "потоковый писатель"
            using (StreamReader sr = new StreamReader(filepath)) // открываем для чтения исходный файл
            {
                while (sr.Peek() >= 0) // пока не закончились символы 
                { 
                    writer.WriteLine(sr.ReadLine()); // записываем в split файл считанную строку

                    if (writer.BaseStream.Length > 100000000 && sr.Peek() >= 0) //если split файл достаточно заполнился или исходный закончился
                    {
                        writer.Close();
                        split_num++;
                        writer = new StreamWriter(
                          string.Format("{0}\\split{1}.txt", directory,split_num)); // создаем новый split файл для записи
                    }
                }
            }
            writer.Close();
        }
        static void SortFiles()
        {
            foreach (string path in Directory.GetFiles(directory+"\\", "split*.txt"))
            {
                
                string[] contents = File.ReadAllLines(path); //записываем считанные строки из файла в массив               
                Array.Sort(contents); // сортируем массив строк
                string newpath = path.Replace("split", "sorted"); // создаем местоположение для отсортированного файла
                File.WriteAllLines(newpath, contents); // записываем в него отсортированные строки
                File.Delete(path); // удаляем неотсортированный файл
                contents = null; // чистим память
                GC.Collect();
            }
        }

        static void Merge()
        {
            string[] paths = Directory.GetFiles(directory+"\\", "sorted*.txt");
            int fileCount = paths.Length; // количество получившихся файлов
            int recordsize = 100; // примерно столько занимает одна строка в памяти
            int maxusage = 500000000; // примерное максимальное значение оперативной памяти, которое нельзя превысить
            int buffersize = maxusage / fileCount; 
            int bufferlen = (int)(buffersize / recordsize); // количество элементов в каждой очереди

            // открываем файлы
            StreamReader[] readers = new StreamReader[fileCount];
            for (int i = 0; i < fileCount; i++)
                readers[i] = new StreamReader(paths[i]);

            // создаем очереди
            Queue<string>[] queues = new Queue<string>[fileCount];
            for (int i = 0; i < fileCount; i++)
                queues[i] = new Queue<string>(bufferlen);

            // заполяем очереди
            for (int i = 0; i < fileCount; i++)
                LoadQueue(queues[i], readers[i], bufferlen);

            // создаем файл в который будем записывать результат
            StreamWriter sw = new StreamWriter(tempFilePathFinal);
            bool done = false;
            int lowest_index, j;
            string lowest_value;
            while (!done)
            {
                // Находим очередь с наименьшим значением первого элемента
                lowest_index = -1;
                lowest_value = "";
                for (j = 0; j < fileCount; j++)
                {
                    if (queues[j] != null)
                    {
                        if (lowest_index < 0 ||
                          String.CompareOrdinal(queues[j].Peek(), lowest_value) < 0)
                        {
                            lowest_index = j;
                            lowest_value = queues[j].Peek();
                        }
                    }
                }

                
                if (lowest_index == -1) { done = true; break; } // если остался -1, значит не осталось очередей 
                sw.WriteLine(lowest_value); // записываем в файл наименьшую строку              
                queues[lowest_index].Dequeue(); // удаляем записанную строку из очереди
                if (queues[lowest_index].Count == 0) // если очередь пустая
                {
                    // загружаем следующую "порцию" строк в освободившуюся очередь
                    LoadQueue(queues[lowest_index], readers[lowest_index], bufferlen); 
                    // если после предыдущего действия очередь осталась пустая, делаем ее null
                    if (queues[lowest_index].Count == 0)
                    {
                        queues[lowest_index] = null;
                    }
                }
            }
            sw.Close();

           // Закрываем и удаляем файлы
            for (int i = 0; i < fileCount; i++)
            {
                readers[i].Close();
                File.Delete(paths[i]);
            }
            Directory.Delete(directory); // Удаляем директорию, где хранились временные файлы
            File.Delete(filePathForSave); // Удаляем изначальный файл
            File.Move(tempFilePathFinal, filePathForSave); // Меняем название получившейся файла на изначальный
        }
        // сохраняем полученные чтением из файла строки в очередь
        static void LoadQueue(Queue<string> queue,
          StreamReader file, int records)
        {
            for (int i = 0; i < records; i++)
            {
                if (file.Peek() < 0) break;
                queue.Enqueue(file.ReadLine());
            }
        }
        static void Main(string[] args)
        {
            
            GenerateFile(1000000000, 20, filePathForSave);
            Split(filePathForSave);
            SortFiles();
            Merge();
            Console.WriteLine("final");
            Console.ReadKey();
        }
    }
}
