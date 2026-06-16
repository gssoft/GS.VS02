//// Program.cs
///

// Program.cs - Финальная версия

using MassTransit;
using MyCompany.SchedulerDemo.Consumers; // <-- ВАЖНО! Добавьте using для ваших неймспейсов
using MyCompany.SchedulerDemo.Jobs;
using Quartz;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    [Obsolete]
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // --- НАСТРОЙКА MASSTRANSIT ---
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<MyBusinessWorker>();
                    //x.UsingRabbitMq((context, cfg) =>
                    //{
                    //    cfg.Host("localhost");
                    //    cfg.ConfigureEndpoints(context);
                    //});
                    x.UsingInMemory((context, cfg) =>
                    {
                        // Для InMemory не нужно указывать хост.
                        // Он автоматически найдет и свяжет потребителей (MyBusinessWorker)
                        // с опубликованными сообщениями.
                        cfg.ConfigureEndpoints(context);
                    });
                });

                // --- НАСТРОЙКА QUARTZ ---
                services.AddQuartz(q =>
                {
                    q.UseMicrosoftDependencyInjectionJobFactory();

                    q.AddJob<SchedulerJob>(opts => opts.WithIdentity("schedulerJob"));

                    // Триггеры (расписание)
                    q.AddTrigger(opts => opts
                        .ForJob("schedulerJob")
                        .WithIdentity("MorningStartTrigger")
                        .WithCronSchedule("0 0 9 ? * MON-FRI"));
                        // .WithCronSchedule("* * * * * ?"));
                        // .WithCronSchedule("* * * * ? ?"));

                    q.AddTrigger(opts => opts
                        .ForJob("schedulerJob")
                        .WithIdentity("EveningStopTrigger")
                        .WithCronSchedule("0 0 18 ? * MON-FRI"));
                        //.WithCronSchedule("* * * * * ?"));
                        //.WithCronSchedule("* * * * ? ?"));
            });

                // Запускает Quartz как фоновую службу
                services.AddQuartzHostedService();
            });
}

//// Program.cs - Верно для Worker-проекта

//using MassTransit;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using MyCompany.SchedulerDemo;
//using Quartz;

//public class Program
//{
//    public static void Main(string[] args)
//    {
//        CreateHostBuilder(args).Build().Run();
//    }

//    // Этот атрибут [Obsolete] появляется, если у вас включен анализатор,
//    // предупреждающий о старом стиле создания хоста. Он не мешает компиляции.
//    [System.Obsolete]
//    public static IHostBuilder CreateHostBuilder(string[] args) =>
//        Host.CreateDefaultBuilder(args)
//            .ConfigureServices((hostContext, services) =>
//            {
//                // Здесь находится вся ваша конфигурация из предыдущего шага
//                services.AddMassTransit(x => { /* ... */ });
//                services.AddQuartz(q => { /* ... */ });
//                services.AddQuartzHostedService();

//                // Регистрация стандартного Worker-а из шаблона
//                services.AddHostedService<Worker>();
//            });
//}

//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using MassTransit;
//using MyCompany.SchedulerDemo.Consumers;
//using MyCompany.SchedulerDemo.Jobs;
//using Quartz;
//using Quartz.Logging;

//var builder = WebApplication.CreateBuilder(args);

//// --- 1. Настройка MassTransit (Шина сообщений) ---
//builder.Services.AddMassTransit(x =>
//{
//    // Регистрируем нашего потребителя (Worker)
//    x.AddConsumer<MyBusinessWorker>();

//    // Настраиваем RabbitMQ как транспорт.
//    // 'localhost' - адрес, где запущен RabbitMQ.
//    x.UsingRabbitMq((context, cfg) =>
//    {
//        cfg.Host("localhost");

//        // Автоматически создает очереди и связывает их с потребителями.
//        cfg.ConfigureEndpoints(context);
//    });
//});

//// --- 2. Настройка Quartz (Планировщик) ---
//builder.Services.AddQuartz(static q =>
//{
//    q.UseMicrosoftDependencyInjectionScopedJobFactory();

//    // Регистрируем саму задачу (Job)
//    q.AddJob<SchedulerJob>(opts => opts.WithIdentity("schedulerJob"));

//    // --- Триггеры (Расписания) ---

//    // Триггер на старт работы по будням в 09:00
//    q.AddTrigger(opts => opts
//        .ForJob("schedulerJob")
//        .WithIdentity("MorningStartTrigger")
//        .WithCronSchedule("0 0 9 ? * MON-FRI"));

//    // Триггер на остановку работы по будням в 18:00
//    q.AddTrigger(opts => opts
//        .ForJob("schedulerJob")
//        .WithIdentity("EveningStopTrigger")
//        .WithCronSchedule("0 0 18 ? * MON-FRI"));
//});
//builder.Services.AddQuartzHostedService();

//// --- 3. Запуск приложения ---
//var app = builder.Build();
//app.Run();

//using MyCompany.SchedulerDemo;

//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();
