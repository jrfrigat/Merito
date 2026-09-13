using Merito.Server.Data;
using Merito.Shared;

namespace Merito.Server.Features.Families;

/// <summary>A ready catalog of tasks, penalties and a screen-time shop a new family can start from.</summary>
public static class ExampleCatalog
{
    private static readonly (string Title, int Points, int? Max, string? When)[] Daily =
    [
        ("Заправить кровать", 1, null, "утро"),
        ("Гигиена утром: зубы, душ, опрятность", 2, null, "утро"),
        ("Гигиена вечером: зубы, душ, опрятность", 2, null, "вечер"),
        ("Позавтракать и убрать за собой посуду", 1, null, "утро"),
        ("Собрать портфель и вещи", 2, null, "утро/вечер"),
        ("Домашнее задание сделано на следующий день и проверено родителями", 5, null, "вечер"),
        ("Домашнее задание сделано на 2 дня вперед и проверено родителями", 7, null, "день"),
        ("30 минут чтения", 3, null, "день"),
        ("30-60 минут физкультуры, спорта или прогулки", 3, null, "утро/вечер"),
        ("Убрать свою комнату или рабочее место", 3, null, "день"),
        ("Помощь по дому: посуда, мусор, уборка", 2, null, "день"),
        ("Помощь без напоминания", 2, null, "день"),
        ("Вежливое общение, без хамства и криков (включая школу)", 2, null, "весь день"),
        ("Не брал телефон или ПК тайком", 3, null, "весь день"),
        ("Лег спать вовремя без скандала", 2, null, "вечер"),
    ];

    private static readonly (string Title, int Points, int? Max, string? When)[] Extra =
    [
        ("Оценка 5 за контрольную или важную работу", 5, null, null),
        ("Оценка 4 за контрольную или важную работу", 3, null, null),
        ("Сделал проект или доклад заранее (на выходных)", 5, null, null),
        ("Дочитал книгу до конца", 10, null, null),
        ("Творческая работа: рисунок, музыка, поделка, программирование, видео, доп. изучение (математика, русский)", 3, 5, null),
        ("Неделя без штрафов", 10, null, null),
    ];

    private static readonly (string Title, int Points)[] PenaltyList =
    [
        ("Обман", 20),
        ("Хамство, агрессия", 5),
        ("Экран тайком ночью", 20),
        ("Не убрал за собой", 2),
        ("Не выполнил классную работу", 5),
        ("Получил 3 за контрольную и не объяснил почему", 10),
        ("Разбросанные вещи", 5),
    ];

    private static readonly (string Title, string Description, int Cost)[] Shop =
    [
        ("Телефон Мини", "30 минут телефона", 5),
        ("Телефон Стандарт", "60 минут телефона", 8),
        ("Телефон Макс", "120 минут телефона", 15),
        ("ПК Мини", "1 час за компьютером", 15),
        ("ПК Стандарт", "2 часа за компьютером", 25),
        ("ПК Макс", "3 часа за компьютером", 32),
        ("Полный безлимит", "12 часов телефона и ПК, с пятницы после уроков до вечера воскресенья, можно разбить на несколько дней", 80),
    ];

    /// <summary>Stages the example catalog for <paramref name="familyId"/> in <paramref name="db"/>.</summary>
    public static void AddTo(MeritoDbContext db, Guid familyId, DateTime now)
    {
        AddTasks(db, familyId, now, Daily, TaskCategory.Daily);
        AddTasks(db, familyId, now, Extra, TaskCategory.Extra);

        for (var i = 0; i < PenaltyList.Length; i++)
        {
            db.Penalties.Add(new Penalty
            {
                Id = Guid.NewGuid(), FamilyId = familyId, Title = PenaltyList[i].Title,
                Points = PenaltyList[i].Points, SortOrder = i, CreatedAt = now,
            });
        }

        for (var i = 0; i < Shop.Length; i++)
        {
            db.Rewards.Add(new Reward
            {
                Id = Guid.NewGuid(), FamilyId = familyId, Title = Shop[i].Title,
                Description = Shop[i].Description, Cost = Shop[i].Cost, SortOrder = i, CreatedAt = now,
            });
        }
    }

    private static void AddTasks(MeritoDbContext db, Guid familyId, DateTime now,
        (string Title, int Points, int? Max, string? When)[] items, TaskCategory category)
    {
        for (var i = 0; i < items.Length; i++)
        {
            db.Tasks.Add(new FamilyTask
            {
                Id = Guid.NewGuid(), FamilyId = familyId, Title = items[i].Title, Points = items[i].Points,
                MaxPoints = items[i].Max, TimeOfDay = items[i].When, Category = category, SortOrder = i, CreatedAt = now,
            });
        }
    }
}
