// https://giga.chat/link/gcsTHjKtci
// Giga

using System;

// 1. Абстрактный класс-скелет (Abstract Class)
// Содержит неизменяемый алгоритм (шаблонный метод) и хуки.

public abstract class BeverageMaker
{
    // Это и есть "Шаблонный метод". 
    // Он объявлен как public, но лучше использовать sealed, 
    // чтобы запретить дочерним классам менять общую логику процесса.
    public void PrepareBeverage()
    {
        Console.WriteLine($"--- Начинаем готовить {GetName()} ---");
        BoilWater();           // Общий шаг (всегда одинаковый)
        Brew();                // Специфичный шаг (переопределяется)
        PourInCup();           // Общий шаг

        // Условный шаг (хук). Наследник решает, нужно ли добавлять сахар.
        if (CustomerWantsCondiments())
        {
            AddCondiments();
        }

        Console.WriteLine($"--- {GetName()} готов! ---\n");
    }

    // Шаг 1: Общая реализация, единая для всех напитков
    private void BoilWater()
    {
        Console.WriteLine("Кипятим воду...");
    }

    // Шаг 2: Абстрактный метод. Обязан быть реализован в каждом наследнике.
    protected abstract void Brew();

    // Шаг 3: Общая реализация
    private void PourInCup()
    {
        Console.WriteLine("Переливаем в чашку...");
    }

    // Шаг 4: Абстрактный метод для добавок
    protected abstract void AddCondiments();

    // Хук (Hook): Метод с реализацией по умолчанию. 
    // Дочерний класс может его переопределить, чтобы изменить условие.
    protected virtual bool CustomerWantsCondiments()
    {
        return true;
    }

    // Вспомогательный метод для красивого вывода
    protected abstract string GetName();
}

// 2. Первый конкретный исполнитель (Concrete Class)
public class TeaMaker : BeverageMaker
{
    protected override void Brew()
    {
        Console.WriteLine("Завариваем чайные листья...");
    }

    protected override void AddCondiments()
    {
        Console.WriteLine("Добавляем лимон...");
    }

    protected override string GetName()
    {
        return "Чай";
    }
}

// 3. Второй конкретный исполнитель (Concrete Class)
public class CoffeeMaker : BeverageMaker
{
    protected override void Brew()
    {
        Console.WriteLine("Варим молотый кофе...");
    }

    protected override void AddCondiments()
    {
        Console.WriteLine("Добавляем сахар и молоко...");
    }

    protected override string GetName()
    {
        return "Кофе";
    }

    // Переопределение хука: клиент кофемашины никогда не хочет добавки сам.
    protected override bool CustomerWantsCondiments()
    {
        return false;
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Создаем объекты конкретных классов
        BeverageMaker tea = new TeaMaker();
        BeverageMaker coffee = new CoffeeMaker();

        Console.WriteLine("Попросим приготовить напитки:\n");

        // Вызываем один и тот же шаблонный метод,
        // но получаем разный результат благодаря полиморфизму.
        tea.PrepareBeverage();
        coffee.PrepareBeverage();

        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
}
