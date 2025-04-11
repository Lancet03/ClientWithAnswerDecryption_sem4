// See https://aka.ms/new-console-template for more information


using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using EncryptionLibrary;

class Program
{
    static HttpClient httpClient = new HttpClient(); // Создаём объект HttpClient для отправки HTTP-запросов
    static RSA rsa = new RSA(200);
    static async Task Main()
    {
        Console.WriteLine("Клиент для работы с базой данных студентов");
        while (true)
        {
            Console.WriteLine("\nВведите: \n1 для получения данных о студенте; \n2 для добавления студента; \nЛюбой другой ввод - для завершения программы\n");
            string option = Console.ReadLine();

            if (option != "1" && option != "2")
            {
                Console.WriteLine("Выход из программы.");
                break;
            }

            if (option == "1")
            {
                Console.Write("Фамилия: ");
                string name = Console.ReadLine();
                string hashedName = EncryptionLibrary.Hashing.Hash(name);

                using var response = await httpClient.GetAsync($"https://localhost:7102/student?name={hashedName}");
                string responseText = await response.Content.ReadAsStringAsync();
                
                List<BigInteger> lettersToDecrypt = GetLettersToDecrypt(responseText);
                if (lettersToDecrypt.Count == 0)
                {
                    Console.WriteLine($"Ошибка при обработке ответа с сервера: '{responseText}'");
                    continue;
                }
                responseText = rsa.DecryptString(lettersToDecrypt);

                string[] studentData = responseText.Split(';', StringSplitOptions.RemoveEmptyEntries);
                string age = studentData[1];
                string avgScore = studentData[2];


                Console.WriteLine($"Студент: {name}\nВозраст: {age}\nСредний балл:{avgScore}\n");
            }
            else if (option == "2")
            {
                Student student = GetStudentData();
                if (student == null) {
                    Console.WriteLine("Введены данные в неверном формате!");
                    continue;
                }

                using var response = await httpClient.PostAsJsonAsync("https://localhost:7102/student", student);
                string result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(result);
            }
        }
    }

    public static Student GetStudentData()
    {
        Console.WriteLine("Введите информацию о студенте: \nФамилия:");
        string name = Console.ReadLine().Trim();
        Console.WriteLine("Возраст:");
        string ageInput = Console.ReadLine();
        Console.WriteLine("Средний балл:");
        string avgScoreInput = Console.ReadLine();

        if (int.TryParse(ageInput.Trim(), out int age) && int.TryParse(avgScoreInput.Trim(), out int avgScore))
        {
            Student student = new Student { Name = name, Age = age, AvgScore = avgScore };
            return student;
        }
        else
        {
            return null;
        }
    }

    public static List<BigInteger> GetLettersToDecrypt(string text)
    {
        string[] encryptedLetters = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        List<BigInteger> lettersToDecrypt = new List<BigInteger>();

        foreach (string letter in encryptedLetters)
        {
            if (BigInteger.TryParse(letter, out BigInteger number))
            {
                lettersToDecrypt.Add(number);
            }
        }

        return lettersToDecrypt;
    }


    public class Student
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public double AvgScore { get; set; }
    }

}